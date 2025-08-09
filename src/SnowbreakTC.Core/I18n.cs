namespace SnowbreakTC.Core;

public static class I18n
{
    public static string Language { get; set; } = "zh-CN";

    private static readonly Dictionary<string, Dictionary<string, string>> Tables = new()
    {
        ["zh-CN"] = new Dictionary<string, string>
        {
            ["login.rejected"] = "登录被拒绝",
            ["sign.already"] = "今日已签到",
            ["star.not_available"] = "星级奖励不可领取",
            ["material.not_enough"] = "材料不足",
            ["level.locked"] = "关卡未解锁",
            ["money.not_enough"] = "货币不足",
        },
        ["zh-TW"] = new Dictionary<string, string>
        {
            ["login.rejected"] = "登入被拒絕",
            ["sign.already"] = "今日已簽到",
            ["star.not_available"] = "星級獎勵不可領取",
            ["material.not_enough"] = "材料不足",
            ["level.locked"] = "關卡未解鎖",
            ["money.not_enough"] = "貨幣不足",
        },
        ["en-US"] = new Dictionary<string, string>
        {
            ["login.rejected"] = "Login rejected",
            ["sign.already"] = "Already signed in today",
            ["star.not_available"] = "Star award not available",
            ["material.not_enough"] = "Not enough materials",
            ["level.locked"] = "Level locked",
            ["money.not_enough"] = "Not enough currency",
        },
    };

    public static string T(string key) =>
        Tables.GetValueOrDefault(Language, Tables["zh-CN"]).GetValueOrDefault(key)
        ?? Tables["zh-CN"].GetValueOrDefault(key)
        ?? key;
}
