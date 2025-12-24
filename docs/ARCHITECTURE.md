# 项目架构说明

SnowbreakTC Server 采用单进程模块化设计，在一个 .NET 8 进程内同时承载游戏 TCP 长连接服务、SDK HTTPS 服务以及后台管理 API，本地使用内嵌 SQLite 保存数据。

## 解决方案结构

```text
src/
├── SnowbreakTC.Core       # 配置模型 (AppConfig) 与多语言文本 (I18n)
├── SnowbreakTC.Protocol   # 自定义 Wire 编解码器、命令号定义与协议消息
├── SnowbreakTC.Database   # SQLite (WAL) 持久化存储与签名 Token 服务
├── SnowbreakTC.GameLogic  # 玩家状态聚合与各系统玩法规则、资源表加载
├── SnowbreakTC.WebAPI     # 后台管理 HTTP 接口
└── SnowbreakTC.Server     # 程序入口、TCP 调度、SDK 服务、业务处理器与 GM 控制台
tests/
└── SnowbreakTC.Tests      # 协议编解码与封帧单元测试
```

为了避免循环引用，`WebAPI` 不直接依赖 `Server`，而是通过 `IAdminGame` 接口与 `Server` 中的 `AdminBridge` 适配器进行交互。

## 网络与协议层

### 自定义 Wire 编解码

游戏协议使用 Protobuf Wire 格式，本项目由 `SnowbreakTC.Protocol` 自实现了一套轻量编解码器：

* 协议消息直接写成普通 C# 类，在字段或属性上标注 `[Field(n)]` 指定 Wire 编号。
* 首次序列化/反序列化时通过反射生成字段读写计划并缓存，兼顾可读性与性能。
* 遇到未定义字段时按 Wire Type 自动跳过，保证前向兼容性。

### 双封帧格式

TCP 层支持两种封帧格式，接收时自动识别：

* **Plain**：4 字节小端长度 + 2 字节命令号 + 消息体。
* **Wrapped**：`0x011F` 魔数头 + 2 字节命令号 + 包装头 + 消息体；当载荷超过阈值（60000 字节）时自动启用 zlib 压缩。

此外，命令号采用奇偶配对设计（请求为奇数、响应为偶数）。对于尚未单独处理的请求，`Dispatcher` 会自动回发一个对应命令号的空响应，避免客户端一直等待超时。

## CallGS 业务路由

游戏内大部分功能交互（养成、关卡结算、抽卡、商店、活动等）都走统一的 `ReqCallGS` 通道，携带 API 名称和 JSON 参数。

* `Handlers/CallGs/` 下的每个处理器只需实现 `ICallGsApi` 接口并在类上标注 `[Route("ApiName")]`。
* `Router` 在启动时通过反射自动扫描并注册所有路由。
* 处理器返回结果经由 `NtfCallScript` 下发给客户端；若涉及角色、道具或属性变动，会一并附带 `ExtraSync` 增量同步包。

## 数据存储与表驱动

* **SQLite 文档存储**：数据库仅包含 `accounts`（账号与认证信息）和 `documents`（按模块键名划分的 JSON 文档）两张表，开启 WAL 模式以提升并发读写性能。
* **内存态与自动存盘**：玩家登录后将数据加载到 `PlayerInstance` 中，游戏过程中的状态变更直接作用于内存对象，由后台定时任务每隔一定周期（默认 60 秒）及停服时统一写回数据库。
* **配置表驱动**：`ResourceManager` 按需从运行目录的 `Resources/` 文件夹懒加载客户端配置表（如经验曲线、掉落、抽卡权重、商店限购等），不将硬编码数值写死在服务端逻辑中。

## SDK 与双区支持

SDK 服务通过 Kestrel 监听 443 及客户端常用的备用端口（13443、18443、31443），并在 TLS 握手阶段按请求的 SNI 域名动态签发并缓存 RSA-2048 自签名证书。

* **国服链路**：处理金山 v6 协议（`loginByToken`、`v6/login` 自动建号、短信/验证码、区服列表等）。
* **国际服链路**：处理 Seasun 对应路由。
* **热更分发**：支持在 `config.json` 中配置版本映射与 AES/ECB/PKCS7 密钥，用于热更清单加密下发。
* **活动时间窗**：对于带有开放时间窗的活动配置，支持通过 `dlc.era_pin` 锁定服务器所处的“时代时间”，使往期活动在本地离线环境下依然保持可进入状态。

## GM 与后台管理

本地终端控制台、后台 HTTP 接口（`POST /api/console`）以及游戏内 WebSocket 控制台（`/ws/gui-console`）底层共用同一个 `GmExecutor` 命令执行器，保证命令行为与输出完全一致。后台管理接口默认仅绑定 `127.0.0.1:18080`，并通过配置中的 `admin.token` 进行鉴权。
