# SnowbreakTC Server

[![build](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server/actions)
[![license](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/1474707499511840890.svg?logo=discord&color=7289DA)](https://discord.com/invite/fTJsUZK3JE)

[简体中文](./README.md) | [English](./README_en.md)

一个用于学习与研究《尘白禁区》(Snowbreak: Containment Zone) 服务端实现的开源模拟器项目，基于 .NET 8 编写。

> **免责声明**
>
> 本项目完全免费且开源，仅用于技术学习与研究，严禁任何形式的商业用途。游戏内所有美术、音频、角色及配置数据版权均归 **西山居 (Seasun Games)** 所有，本仓库不提供任何游戏客户端或资源文件。请在下载后 24 小时内删除相关文件。

## 功能进度

* [x] 登录与账号认证
* [x] 玩家基础数据
* [x] 角色、武器与后勤
* [x] 抽卡与商店
* [x] 关卡结算与任务签到
* [x] 好友、聊天与排行榜
* [x] 基地宿舍与活动玩法
* [x] GM 控制台与 Web 管理面板
* [✓] 物品与邮件（支持发放与存储，部分客户端操作待接）
* [✓] 多人联机（基础房间可用，匹配与联机战斗未完成）
* [ ] 客户端启动器 / 流量转发
* [ ] 真机联调测试

> `[x]` 已完成 · `[✓]` 部分实现 · `[ ]` 未开始

## 快速开始

### 1. 环境准备

只需要安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)（数据存储使用内嵌 SQLite，不需要额外装数据库）。

### 2. 获取代码与配置

```bash
git clone <repository-url>
cd SnowbreakTC_Server
cp config.example.json config.json
```

默认配置即可直接本机运行；如需修改监听端口、公开 IP 或管理接口密钥，编辑 `config.json` 即可。

### 3. 准备配置表资源

将从客户端提取的 JSON 配置表放入运行目录下的 `Resources/` 文件夹中。服务器启动时会自动加载这些表来驱动关卡、商店、抽卡等数值逻辑。

### 4. 启动服务器

```bash
dotnet run --project src/SnowbreakTC.Server
```

看到控制台输出监听端口信息，即表示服务已启动。也可以直接运行根目录的 `build.cmd` 进行 Release 构建。

## 客户端接入

服务器同时兼容**国服**与**国际服**客户端，SDK 服务会根据握手域名（SNI）动态签发自签名证书。本项目只提供服务端，不附带客户端代理补丁或流量转发器，接入需要自备转发工具（或手动改 hosts）。

### 国服客户端

将以下 7 个 SDK 域名通过 `hosts` 指向服务器 IP 即可：

```text
127.0.0.1 sh-jxsj.xgsdk.com
127.0.0.1 xgdata.xgsdk.com
127.0.0.1 js2sdk.xoyo.com
127.0.0.1 sh-qrcode.xgsdk.com
127.0.0.1 activity.xgsdk.com
127.0.0.1 gpm-conf.testplus.cn
127.0.0.1 cbjq-serverlist.xoyocdn.com
```

在以管理员权限运行服务器时，也可以直接在服务端控制台输入 `redirect on` 一键写入上述 hosts 记录（输入 `redirect off` 可随时清除）。

### 国际服客户端

国际服建议搭配流量代理或转发器使用，将以下域名后缀的请求转发至服务器的 SDK HTTPS 端口（默认 443）：

```text
*.amazingseasuncdn.com
*.amazingseasun.com
*.seasungames.com
*.snowbreak-game.com
*.xgsdk.com
*.xoyo.games
*.qcloud.com
*.tencentcs.com
```

### 默认端口

| 服务 | 默认端口 | 说明 |
|---|---|---|
| 游戏服 (TCP) | `20000` | 客户端长连接通信 |
| SDK (HTTPS) | `443`、`13443`、`18443`、`31443` | 登录验证、区服列表与热更 |
| 管理接口 (HTTP) | `18080` | 默认仅监听 `127.0.0.1`，需配合 `admin.token` 使用 |

## GM 与管理

服务启动后可直接在控制台输入 GM 命令，也可以通过后台 HTTP 接口或游戏内 WebSocket 控制台执行。常用示例：

```text
help                          # 查看可用命令
handbook                      # 导出完整 GM 手册到 docs/handbook.md
player list                   # 查看当前在线玩家
grant money <type> <amount>   # 为在线玩家设置货币
grant card <detail> <part>    # 为在线玩家发放角色
mail <title>                  # 向在线玩家发送邮件
server save                   # 立即保存所有在线玩家数据
```
## 🤝 如何贡献 (Contributing)

我们欢迎任何形式的贡献，无论是代码、文档还是问题报告！

如果你想贡献代码，请遵循以下步骤：

1.  **Fork** 本仓库。
2.  创建一个新的分支 (`git checkout -b feature/your-feature-name`)。
3.  进行修改并提交 (`git commit -m 'Add some amazing feature'`)。
4.  将你的分支推送到你的 Fork (`git push origin feature/your-feature-name`)。
5.  创建一个 **Pull Request**。

请确保你的代码遵循项目现有的编码风格，并提供清晰的注释和提交信息。

## 💬 社区 (Community)

* **Discord**: [加入我们的 Discord 服务器](https://discord.com/invite/fTJsUZK3JE)

遇到问题或有任何想法，欢迎在 Issues 区提出，或加入我们的社区进行讨论。

## 🙏 致谢 (Acknowledgements)

* 感谢老己

## 相关文档

* [项目架构说明](docs/ARCHITECTURE.md)
* [开发与扩展指南](docs/DEVELOPMENT.md)

## 许可证

本项目基于 [GNU Affero General Public License v3.0](./LICENSE) 开源。
