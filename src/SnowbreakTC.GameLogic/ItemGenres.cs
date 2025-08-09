namespace SnowbreakTC.GameLogic;

public static class ItemGenres
{
    public const uint Card = 1;
    public const uint Weapon = 2;
    public const uint Support = 3;
    public const uint Useable = 4;
    public const uint Supplies = 5;
    public const uint WeaponPart = 6;
    public const uint CardSkin = 7;
    public const uint CardSkinPart = 20;
    public const uint WeaponSkin = 17;

    public static ulong Template(uint genre, uint detail, uint particular, uint level = 1) =>
        ((ulong)level << 48) | ((ulong)particular << 32) | ((ulong)detail << 16) | genre;

    public static (uint Genre, uint Detail, uint Particular, uint Level) Unpack(ulong template) =>
        ((uint)(template & 0xFFFF), (uint)((template >> 16) & 0xFFFF), (uint)((template >> 32) & 0xFFFF), (uint)(template >> 48));
}
