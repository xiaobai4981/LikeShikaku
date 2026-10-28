namespace MainScene.BattlePart
{
    // 保留原枚举值，兼容已有序列化数据。
    public enum E_BattleStatus
    {
        Home = 0,
        Loading = 1,
        InBattle = 2,
        InMenu = 3,
        Over = 4
    }

    public enum E_ShikakuCellType
    {
        Normal = 0
    }

    public enum E_ShikakuCellStatus
    {
        Normal = 0,
        Preview = 1,
        Success = 2,
        Error = 3,
        Hint = 5
    }

    public enum E_ShikakuSelectResult
    {
        None = 0,
        Success = 1,
        NotSquare = 2,
        Overlap = 3,
        RegionCleared = 4,
        OutsideBoard = 5,
        NoClue = 8,
        MultipleClues = 9,
        AreaMismatch = 10,
        SessionClosed = 14,
        Undo = 15,
        Hint = 16,
        StateChanged = 17
    }
}
