namespace MainScene.BattlePart
{
    public enum E_BattleStatus
    {
        Home = 0,
        Loading = 1,
        InBattle = 2,
        InMenu = 3,
        Over = 4,
    }

    // TODO 方块的类型
    public enum E_ShikakuCellType
    {
        Normal = 0,

    }

    // 方块状态
    public enum E_ShikakuCellStatus
    {
        UnSettled = 0,  // 未被框选
        Covering = 1,   // 箭头掠过
        Setting = 2,    // 正在被框选
        Settled = 3,    // 已被框选完毕
    }
}