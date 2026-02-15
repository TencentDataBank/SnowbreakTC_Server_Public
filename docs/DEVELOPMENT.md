# 开发与扩展指南

## 开发环境

本地开发只需安装 [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)，使用 Visual Studio 2022、JetBrains Rider 或 VS Code 打开根目录的 `SnowbreakTC.sln` 即可。

常用命令行操作：

```bash
# 编译整个解决方案
dotnet build

# 运行单元测试
dotnet test

# 清理构建缓存（等价于手动删所有 bin/obj）
clean.cmd

# 启动服务器（在仓库根目录执行）
dotnet run --project src/SnowbreakTC.Server
```

若需要打包发布（Windows x64）：

```bash
dotnet publish src/SnowbreakTC.Server -c Release -r win-x64 --self-contained false -o publish
```

发布后的运行目录需要自备 `config.json` 以及客户端配置表目录 `Resources/`。

## 配置文件与目录约定

仓库内的 `.gitignore` 已忽略以下运行时文件，提交代码时请勿将它们加入版本控制：

* `config.json`：本地运行配置（由 `config.example.json` 复制修改而来）。
* `Resources/`：游戏配置表 JSON 文件目录。
* `Data/`：运行时生成的 SQLite 数据库、Token 密钥和动态自签证书。
* `logs/` 与 `bin/` / `obj/`：日志及编译产物。

`config.json` 中所有配置项都有默认值，常见需要调整的字段如下：

* `game.port` / `game.public_host`：游戏服端口（默认 20000）及下发给客户端的连接地址。
* `sdk.port` / `sdk.extra_tls_ports`：SDK HTTPS 监听端口。
* `admin.token`：开启后台管理接口时的访问密钥。
* `dlc.era_pin`：填入 `[yyyyMMddHHmm]` 格式时间可将活动时间窗锁定到指定版本周期。
* `hotfix.versions` / `hotfix.aes_keys`：按客户端版本配置热更加密密钥。

## 新增一条 CallGS 路由

在 `src/SnowbreakTC.Server/Handlers/CallGs/` 目录下新建类（或添加到对应模块文件中），标注 `[Route("API名称")]` 并实现 `ICallGsApi` 即可，服务器启动时会自动扫描注册：

```csharp
[Route("Example_DoAction")]
public sealed class ExampleDoAction : ICallGsApi
{
    public Task<CallGsReply> HandleAsync(GameSession session, string param)
    {
        var root = JsonParam.Parse(param)?.RootElement ?? default;
        var player = session.Player!;

        // 1. 读取请求参数与配置表校验
        // 2. 更新 player 状态，Router 会自动构建并附带变更同步包
        return Task.FromResult(new CallGsReply("{\"Ok\":1}"));
    }
}
```

如果参数不合法或条件未满足，直接返回 `CallGsReply.Error("error_key")` 即可触发客户端对应的错误提示。

## 新增协议消息

1. 在 `src/SnowbreakTC.Protocol/CmdIds.cs` 中添加请求与响应的命令号常量（遵循奇偶配对习惯）。
2. 在 `src/SnowbreakTC.Protocol/Messages/` 中创建对应的消息类，使用 `[Field(n)]` 标注字段编号。注意已有字段编号是二进制通信契约，不要随意更改。
3. 在 `Dispatcher` 中注册或添加对应的消息处理分支。

## 开发约定

* **表驱动优先**：物品消耗、升级经验、掉落和抽卡权重等数值统一从 `Resources/` 中读取，避免在代码里写死经验值或概率常数。
* **状态变更走聚合根**：对玩家数据的修改请统一通过 `PlayerInstance` 的方法操作，确保脏标记与自动存盘机制正常工作。
* **保持简洁**：遵循项目现有的编码风格与命名习惯，只保留必要的说明注释。
* **保持源码原创**：参考资料仅用于对照协议字段、命令号与配置表结构，请勿直接搬运外部源码。

## 常见问题排查

* **443 端口监听失败**：在 Windows 上绑定 443 端口需要管理员权限；如果不方便提权，可以在 `config.json` 中修改 `sdk.port` 为高位端口并配合端口转发工具使用。
* **控制台提示写入 hosts 失败**：`redirect on` 需要写入系统 `hosts` 文件，请以管理员身份启动服务端，或手动编辑 hosts。
* **客户端停在登录界面**：确认配置中的 `public_host` 可被客户端访问、7 个国服域名（或国际服代理规则）已生效，且 `Data/Cert` 目录具有写入权限。
* **配置表未生效**：检查 `Resources/` 文件夹是否放置在当前运行命令的工作目录下（例如直接使用 `dotnet run` 时应在执行目录或对应配置路径下，而非仅仅放在其他备份目录）。
