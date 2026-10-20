using Common.Level;
using Core;
using Cysharp.Threading.Tasks;
using MainScene.BattlePart.Configs;
using MainScene.BattlePart.Manager;
using MainScene.BattlePart.Shikaku;
using UnityEngine;

namespace MainScene
{
    public class MainSceneGameMode : GameModeBase
    {
        // TODO 这里管理的是临时用户数据，用于游戏内，上传时还是用的MainGameInstance的LocalUserData
        //private UserData m_copyUserData;
        //public UserData CopyUserData => m_copyUserData;

        // 战斗管理器
        private BattleManager m_battleManager;
        public BattleManager BattleManager => m_battleManager;

        // 战斗配置
        [SerializeField]
        private BattleSettings m_battleSettings;
        public BattleSettings BattleSettings => m_battleSettings;

        // 表现管理器
        [SerializeField] private ShikakuController m_shikakuController;
        public ShikakuController ShikakuController => m_shikakuController;

        // TODO 后面删掉，这里测试用关卡
        [SerializeField] private LevelConfig m_testLevelConfig;

        public override async UniTask Init()
        {
            m_battleManager = new();
            await UniTask.Yield(Application.exitCancellationToken);

            // TODO 如果是第一次，则直接进入第一关，
            m_battleManager.Init(this, m_testLevelConfig, m_shikakuController);  // TODO 这里测试用，后面删掉
            m_battleManager.EnterGameplay();
            m_battleManager.battleStatus = BattlePart.E_BattleStatus.InBattle;

            // TODO 如果不是，则打开home面板
            // HomePart.UI.UIHome.ID.QueueOpen();


        }

        public override void Tick()
        {
            m_battleManager?.Tick();
        }

        public override void Cleanup()
        {
            MainGameInstance.MessagingSystem.UnsubscribeAll(this);
            m_shikakuController.ClearMap();
        }
    }
}