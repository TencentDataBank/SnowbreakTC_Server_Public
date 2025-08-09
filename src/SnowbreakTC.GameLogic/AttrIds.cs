namespace SnowbreakTC.GameLogic;

public static class AttrIds
{
    public static class Currency
    {
        public const uint Group = 1;
        public const uint Money = 1;
        public const uint Gold = 2;
        public const uint Silver = 3;
        public const uint Vigor = 4;
        public const uint Token = 5;
        public const uint PayGold = 8;
        public const uint Repression = 12;
        public const uint Furniture = 13;
        public const uint Rmb = 100;

        public static uint SidOf(uint moneyType) => moneyType * 2 + 1;

        public static (uint, uint) KeyOf(uint moneyType) => (Group, SidOf(moneyType));
    }

    public static class CashExchange
    {
        public const uint Group = 70;
        public const uint VigorLimit = 1;
        public const uint SilverLimit = 2;
    }
}
