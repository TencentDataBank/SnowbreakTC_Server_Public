# SnowbreakTC Server

《尘白禁区》私有服务器模拟器，仅供学习研究。

> 本项目完全免费且开源，仅用于技术学习与研究，严禁任何形式的商业用途。游戏内所有资源版权均归西山居所有，本仓库不分发任何游戏资源。

## 功能进度

* [x] 用户认证
* [x] 玩家基础数据
* [ ] 角色系统
* [ ] 商店系统
* [ ] 抽卡系统

## License

AGPL-3.0

## 快速开始

```bash
dotnet build
cp config.example.json config.json
dotnet run --project src/SnowbreakTC.Server
```

## 客户端接入

国服：将以下域名通过 hosts 指向服务器 IP：

```
<server-ip> sh-jxsj.xgsdk.com
<server-ip> xgdata.xgsdk.com
<server-ip> js2sdk.xoyo.com
<server-ip> sh-qrcode.xgsdk.com
<server-ip> activity.xgsdk.com
<server-ip> gpm-conf.testplus.cn
<server-ip> cbjq-serverlist.xoyocdn.com
```
