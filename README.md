# SnowbreakTC Server (尘白禁区 | Snowbreak: Forbidden Zone Emulator)

[![build](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server/actions)
[![license](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/123456789.svg?logo=discord&color=7289DA)](https://discord.gg/your-invite-code)

**简体中文** | [English](./README_en.md)

一个为《尘白禁区》(Snowbreak: Forbidden Zone) 打造的开源服务器模拟器项目，旨在学习和研究游戏服务器的实现技术。

## ⚠️ 重要声明 (Disclaimer)

* **本项目完全免费、开源，仅用于学习和研究目的，严禁用于任何商业用途。**
* 本项目的建立并非为了牟利或鼓励盗版行为。我们强烈建议所有玩家通过官方渠道下载并体验游戏，以支持开发者和正版游戏。
* 所有游戏内的美术、音乐、角色、数据等资源的版权归 **西山居 (Seasun Games)** 所有。本项目不会分发任何游戏资源。
* 对于使用本项目可能产生的任何法律风险，项目开发者概不负责。如果你下载、使用或分发本项目的代码，即代表你同意并愿意承担所有潜在风险。
* **请在下载和使用本项目的 24 小时内删除相关文件。**

## ✨ 项目状态 (Current Status)

本项目目前处于早期开发阶段，许多功能尚不完善。我们的目标是模拟实现游戏的核心功能。

### 功能实现进度

* [x] 用户认证 (登录、注册)
* [✓] 玩家数据 (基础信息、货币)
* [✓] 角色系统 (获取、基础属性)
* [ ] 物品/仓库系统
* [ ] 抽卡/招募系统
* [ ] 战斗/关卡逻辑 (部分实现，仍在开发中)
* [ ] 商店系统
* [ ] 主线/支线任务
* [ ] 多人联机/基地系统
* [ ] ... 更多功能正在规划中

**图例:** `[x]` - 已完成, `[✓]` - 部分实现, `[ ]` - 未开始

## 🚀 快速开始 (Getting Started)

在开始之前，请确保你已经安装了以下必备软件：

### 环境要求 (Prerequisites)

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [MongoDB](https://www.mongodb.com/try/download/community) (推荐 6.0+ 版本)
* [Git](https://git-scm.com/downloads/)
* 游戏客户端 (适配版本: `1.8.x`)

### 服务器部署 (Server Deployment)

1.  **克隆仓库**
    ```bash
    git clone [https://github.com/TencentDataBank/SnowbreakTC_Server.git](https://github.com/TencentDataBank/SnowbreakTC_Server.git)
    cd SnowbreakTC_Server
    ```

2.  **配置服务器**
    复制 `config.json.example` 并重命名为 `config.json`。
    ```bash
    cp config.json.example config.json
    ```
    然后根据你的环境修改 `config.json` 文件，特别是数据库连接字符串和服务器监听的 IP 地址。
    ```json
    {
      "Database": {
        "ConnectionString": "mongodb://localhost:27017",
        "Name": "SnowbreakDB"
      },
      "Server": {
        "ListenIp": "0.0.0.0",
        "PublicIp": "127.0.0.1",
        "Port": 443
      }
    }
    ```

3.  **构建并运行**
    ```bash
    dotnet build
    dotnet run
    ```
    当看到控制台输出 "Server started successfully on port 443" 时，表示服务器已成功启动。

### 客户端连接 (Client Connection)

你需要使用流量重定向工具将游戏的网络请求指向你的私服。

1.  **推荐工具**: `Fiddler`, `Mitmproxy`, 或其他网络调试代理工具。
2.  **重定向规则**: 将所有 `*.seasungames.com` (此为示例，建议直接使用[专用proxy](https://github.com/TencentDataBank/STC_Proxy)) 的流量重定向至你服务器的 `PublicIp`。
3.  启动游戏客户端。如果一切正常，你应该能够连接到你的服务器并创建账号进入游戏。

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

* **Discord**: [加入我们的 Discord 服务器](https://www.bilibili.com/video/BV1GJ411x7h7)
* **telegram 交流群**: [点击加入](https://t.me/+NeVerGoNnAgivE–yOUUp)

遇到问题或有任何想法，欢迎在 Issues 区提出，或加入我们的社区进行讨论。

## 🙏 致谢 (Acknowledgements)

* 感谢 **anthropic/claude-sonnet-4** 重构了项目，让该公开库毫无意义

## 📄 许可证 (License)

本项目采用 [GNU Affero General Public License v3.0](https://github.com/TencentDataBank/SnowbreakTC_Server_Public/blob/main/LICENSE) 许可证。简而言之，如果你基于本项目的代码进行了修改并对外提供网络服务，你也必须以相同的许可证开源你的修改。
