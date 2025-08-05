# SnowbreakTC Server (Snowbreak: Forbidden Zone Emulator)

[![build](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server/actions)
[![license](https://img.shields.io/badge/license-AGPL--3.0-blue.svg)](https://github.com/TencentDataBank/SnowbreakTC_Server_Public/blob/main/LICENSE)
[![Discord](https://img.shields.io/discord/123456789.svg?logo=discord&color=7289DA)](https://www.bilibili.com/video/BV1GJ411x7h7)

[简体中文](./README.md) | **English**

An open-source server emulator for "Snowbreak: Forbidden Zone," created for the purpose of learning and researching game server implementation technologies.

## ⚠️ Disclaimer

* **This project is completely free, open-source, and intended for learning and research purposes only. Commercial use is strictly prohibited.**
* This project was not created for profit or to encourage piracy. We strongly recommend all players to download and experience the game through official channels to support the developers and the official game.
* All copyrights for in-game assets such as art, music, characters, and data belong to **Seasun Games (西山居)**. This project does not distribute any game assets.
* The developers of this project are not responsible for any legal risks that may arise from its use. By downloading, using, or distributing the code from this project, you agree to assume all potential risks.
* **Please delete all related files within 24 hours of downloading and using this project.**

## ✨ Current Status

This project is currently in the early stages of development, and many features are not yet complete. Our goal is to simulate the core functionalities of the game.

### Feature Implementation Progress

* [x] User Authentication (Login, Register)
* [✓] Player Data (Basic Info, Currencies)
* [✓] Character System (Acquisition, Basic Attributes)
* [ ] Inventory/Item System
* [ ] Gacha/Recruitment System
* [ ] Combat/Stage Logic (Partially implemented, under development)
* [ ] Shop System
* [ ] Main/Side Quests
* [ ] Multiplayer/Base System
* [ ] ... More features are being planned.

**Legend:** `[x]` - Implemented, `[✓]` - Partially Implemented, `[ ]` - Not Started

## 🚀 Getting Started

Before you begin, ensure you have the following prerequisites installed:

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [MongoDB](https://www.mongodb.com/try/download/community) (Version 6.0+ recommended)
* [Git](https://git-scm.com/downloads/)
* Game Client (Compatible Version: `1.9.x`)

### Server Deployment

1.  **Clone the repository**
    ```bash
    git clone [https://github.com/TencentDataBank/SnowbreakTC_Server.git](https://github.com/TencentDataBank/SnowbreakTC_Server.git)
    cd SnowbreakTC_Server
    ```

2.  **Configure the server**
    Copy `config.json.example` and rename it to `config.json`.
    ```bash
    cp config.json.example config.json
    ```
    Then, modify the `config.json` file according to your environment, especially the database connection string and the server's listening IP address.
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

3.  **Build and run**
    ```bash
    dotnet build
    dotnet run
    ```
    When you see the console output "Server started successfully on port 443," the server has launched successfully.

### Client Connection

You will need to use a traffic redirection tool to point the game's network requests to your private server.

1.  **Recommended Tools**: `Fiddler`, `Mitmproxy`, or other network debugging proxy tools.
2.  **Redirection Rule**: Redirect all traffic from `*.seasungames.com` (this is an example; the specific domain may need to be confirmed via packet capture) to your server's `PublicIp`.
3.  Launch the game client. If everything is configured correctly, you should be able to connect to your server, create an account, and enter the game.

## 🤝 How to Contribute

We welcome all forms of contribution, whether it's code, documentation, or issue reports!

If you want to contribute code, please follow these steps:

1.  **Fork** this repository.
2.  Create a new branch (`git checkout -b feature/your-feature-name`).
3.  Make your changes and commit them (`git commit -m 'Add some amazing feature'`).
4.  Push your branch to your fork (`git push origin feature/your-feature-name`).
5.  Create a **Pull Request**.

Please ensure your code follows the existing coding style of the project and provide clear comments and commit messages.

## 💬 Community

* **Discord**: [Join our Discord Server](https://www.bilibili.com/video/BV1GJ411x7h7)
* **Telegram Group**: [Click to Join](https://t.me/+NeVerGoNnAgivE–yOUUp)

If you encounter any problems or have any ideas, feel free to open an issue in the Issues section or join our community for discussion.

## 🙏 Acknowledgements

* Thanks to **Seasun Games (西山居)** for developing the excellent game "Snowbreak: Forbidden Zone."
* Thanks to all the developers who have contributed to this project.
* Thanks to everyone in the open-source community who shares their knowledge and tools.

## 📄 License

This project is licensed under the [GNU Affero General Public License v3.0](https://github.com/TencentDataBank/SnowbreakTC_Server_Public/blob/main/LICENSE). In short, if you modify the code from this project and provide a network service based on it, you must also open-source your modifications under the same license.
