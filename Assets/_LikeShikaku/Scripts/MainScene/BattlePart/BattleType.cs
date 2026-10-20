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
        Normal = 0,
        Preview = 1,
        Success = 2,
        Error = 3,
    }

    // 操作结果
    public enum E_ShikakuSelectResult
    {
        None = 0,
        Success = 1,
        NotSquare = 2,
        Overlap = 3,
        RegionCleared = 4,
    }
}