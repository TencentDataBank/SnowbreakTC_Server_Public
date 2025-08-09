# SnowbreakTC Server 开发文档

## 项目架构

### 整体架构
```
SnowbreakTC_Server/
├── src/
│   ├── SnowbreakTC.Server/              # 主服务器项目
│   ├── SnowbreakTC.Core/                # 核心库
│   ├── SnowbreakTC.Database/            # 数据访问层
│   ├── SnowbreakTC.Protocol/            # 协议定义
│   ├── SnowbreakTC.GameLogic/           # 游戏逻辑
│   └── SnowbreakTC.WebAPI/              # Web API
├── tools/                               # 工具和脚本
├── docs/                               # 文档
├── config/                             # 配置文件
└── tests/                              # 测试项目
```

### 技术栈
- **.NET 8.0**: 主要开发框架
- **MongoDB**: 主数据库
- **Redis**: 缓存和会话存储
- **ASP.NET Core**: Web API 框架
- **Serilog**: 日志框架
- **Protocol Buffers**: 消息序列化

## 开发环境设置

### 必备软件
1. [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
2. [MongoDB Community Server](https://www.mongodb.com/try/download/community)
3. [Redis](https://redis.io/download)
4. [Visual Studio 2022](https://visualstudio.microsoft.com/) 或 [VS Code](https://code.visualstudio.com/)

### 环境配置
1. 克隆项目
   ```bash
   git clone <repository-url>
   cd SnowbreakTC_Server
   ```

2. 还原 NuGet 包
   ```bash
   dotnet restore
   ```

3. 配置数据库连接
   - 复制 `src/SnowbreakTC.Server/appsettings.json.example` 为 `appsettings.json`
   - 修改数据库连接字符串

4. 启动依赖服务
   ```bash
   # 启动 MongoDB
   mongod --dbpath /path/to/data
   
   # 启动 Redis
   redis-server
   ```

5. 构建并运行
   ```bash
   dotnet build
   dotnet run --project src/SnowbreakTC.Server
   ```

## 项目结构详解

### SnowbreakTC.Core
核心库，包含：
- 配置模型 (`Configuration/`)
- 服务接口 (`Services/`)
- 通用工具类 (`Utils/`)

### SnowbreakTC.Database
数据访问层，包含：
- 实体模型 (`Models/`)
- 仓储接口和实现 (`Repositories/`)
- 数据库上下文 (`Context/`)

### SnowbreakTC.Protocol
网络协议定义，包含：
- Protocol Buffers 定义 (`Protos/`)
- 消息处理器 (`Handlers/`)
- 网络客户端 (`Client/`)

### SnowbreakTC.GameLogic
游戏业务逻辑，包含：
- 玩家系统 (`Player/`)
- 角色系统 (`Character/`)
- 物品系统 (`Item/`)
- 战斗系统 (`Battle/`)

### SnowbreakTC.WebAPI
Web API 接口，包含：
- 控制器 (`Controllers/`)
- 中间件 (`Middleware/`)
- 模型 (`Models/`)

### SnowbreakTC.Server
主服务器项目，包含：
- 程序入口 (`Program.cs`)
- 服务实现 (`Services/`)
- 配置文件 (`appsettings.json`)

## 开发规范

### 代码风格
- 使用 C# 标准命名约定
- 公共 API 必须有 XML 文档注释
- 使用 `async/await` 进行异步编程
- 优先使用依赖注入

### 日志规范
```csharp
// 使用结构化日志
_logger.LogInformation("用户 {UserId} 登录成功", userId);

// 错误日志包含异常信息
_logger.LogError(ex, "处理请求时发生错误: {RequestId}", requestId);
```

### 配置管理
- 敏感信息使用环境变量或用户机密
- 开发环境配置放在 `appsettings.Development.json`
- 生产环境配置通过环境变量覆盖

## 测试

### 运行测试
```bash
dotnet test
```

### 测试覆盖率
```bash
dotnet test --collect:"XPlat Code Coverage"
```

## 部署

### 发布应用
```bash
dotnet publish -c Release -o ./publish
```

### Docker 部署
```bash
docker build -t snowbreaktc-server .
docker run -p 8080:8080 -p 22102:22102 snowbreaktc-server
```

## 故障排除

### 常见问题
1. **数据库连接失败**
   - 检查 MongoDB 是否运行
   - 验证连接字符串是否正确

2. **Redis 连接失败**
   - 检查 Redis 服务状态
   - 验证 Redis 配置

3. **端口占用**
   - 修改配置文件中的端口设置
   - 使用 `netstat` 检查端口占用

### 日志查看
- 应用日志：`logs/` 目录
- 控制台输出：实时日志信息

## 贡献指南

1. Fork 项目
2. 创建功能分支
3. 提交更改
4. 创建 Pull Request

详细信息请参考 [CONTRIBUTING.md](CONTRIBUTING.md)