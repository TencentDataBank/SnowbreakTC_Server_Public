# SnowbreakTC Server

[简体中文](./README.md) | [English](./README_en.md)

An open-source server emulator for *Snowbreak: Containment Zone* written in .NET 8, created for learning and researching game server implementation.

> **Disclaimer**
>
> This project is free, open-source, and intended strictly for educational and research purposes. Commercial use is prohibited. All in-game art, audio, characters, and data assets belong to **Seasun Games**. This repository does not distribute any game client or game asset files. Please delete all related files within 24 hours of downloading.

## Progress

* [x] Login & account auth
* [x] Player profile & currencies
* [x] Characters, weapons & support cards
* [x] Gacha & shop
* [x] Stage settlement, quests & sign-in
* [x] Friends, chat & leaderboards
* [x] Dormitory & event modes
* [x] GM console & web admin panel
* [✓] Items & mail (storage/sending works; some client actions pending)
* [✓] Co-op (basic room works; matchmaking & co-op combat incomplete)
* [ ] Client launcher / traffic forwarder
* [ ] Full live-client QA

> `[x]` Done · `[✓]` Partial · `[ ]` Not started

## Quick Start

### 1. Prerequisites

You only need the [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) installed (data persistence uses embedded SQLite, so no external database setup is required).

### 2. Clone and Configure

```bash
git clone <repository-url>
cd SnowbreakTC_Server
cp config.example.json config.json
```

The default configuration works out of the box for local play. Edit `config.json` if you need to change ports, public host address, or the admin token.

### 3. Prepare Game Tables

Place the client-extracted JSON config tables into a `Resources/` folder inside the working directory. The server loads these tables on demand to drive stages, shop items, gacha pools, and progression curves.

### 4. Run the Server

```bash
dotnet run --project src/SnowbreakTC.Server
```

Once the console prints the listening ports, the server is ready. On Windows, you can also run `build.cmd` for a Release build.

## Client Connection

The server works with both **CN** and **Global** clients. The built-in SDK listener generates self-signed certificates dynamically per SNI hostname during TLS handshakes. Note that this project provides the server side only — no client-side proxy patch or traffic forwarder is included, so you will need your own forwarding tool (or manual hosts edits).

### CN Client

Point the following 7 SDK domains to your server IP in `hosts`:

```text
127.0.0.1 sh-jxsj.xgsdk.com
127.0.0.1 xgdata.xgsdk.com
127.0.0.1 js2sdk.xoyo.com
127.0.0.1 sh-qrcode.xgsdk.com
127.0.0.1 activity.xgsdk.com
127.0.0.1 gpm-conf.testplus.cn
127.0.0.1 cbjq-serverlist.xoyocdn.com
```

If you run the server as Administrator, you can also type `redirect on` directly in the server console to add these entries automatically (and `redirect off` to remove them).

### Global Client

Use a traffic proxy or forwarder to redirect requests with the following domain suffixes to the server's SDK HTTPS port (443 by default):

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

### Default Ports

| Service | Default Ports | Notes |
|---|---|---|
| Game Server (TCP) | `20000` | Persistent client connection |
| SDK (HTTPS) | `443`, `13443`, `18443`, `31443` | Auth, server list, and hotfix |
| Admin API (HTTP) | `18080` | Binds `127.0.0.1` by default, protected by `admin.token` |

## GM & Administration

Once started, you can type GM commands right into the server console, or invoke them over the Admin HTTP API and the in-game WebSocket console. Common commands:

```text
help                          # list available commands
handbook                      # export full GM handbook to docs/handbook.md
player list                   # show online players
grant money <type> <amount>   # set currency for online players
grant card <detail> <part>    # grant character to online players
mail <title>                  # send mail to online players
server save                   # flush all online players to disk
```

## Documentation

* [Architecture](docs/ARCHITECTURE.md)
* [Development Guide](docs/DEVELOPMENT.md)

## License

Licensed under the [GNU Affero General Public License v3.0](./LICENSE).
