using MainScene.BattlePart.Shikaku;

namespace MainScene.BattlePart
{
    // 由 BattleManager 发送事件，界面不直接修改 Model 集合。
    public readonly struct BattleChangedEvent
    {
        public readonly ShikakuModel model;
        public BattleChangedEvent(ShikakuModel model)
        {
            this.model = model;
        }
    }

    public readonly struct BattleFinishedEvent
    {
        public readonly string levelId;
        public readonly bool won;
        public readonly int stars;
        public BattleFinishedEvent(ShikakuModel model)
        {
            levelId = model.Level.id;
            won = model.Status == E_ShikakuPlayStatus.Won;
            stars = model.Stars;
        }
    }
}
