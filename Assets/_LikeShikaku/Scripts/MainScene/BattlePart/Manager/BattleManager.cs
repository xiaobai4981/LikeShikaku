using Cysharp.Threading.Tasks;
using MainScene.BattlePart.Input;
using MainScene.BattlePart.Shikaku;
using System.Threading;
using UnityEngine;

namespace MainScene.BattlePart.Manager
{
    public class BattleManager
    {
        private MainSceneGameMode m_gameMode;

        // 输入管理器
        private BattleInputManager m_battleInputManager;
        public BattleInputManager BattleInputManager => m_battleInputManager;

        // 屏幕输入 -> 格子输入
        private ShikakuInputTransform m_shikakuInputTransform;

        // 战斗状态
        public E_BattleStatus battleStatus = E_BattleStatus.Home;
        private E_BattleStatus lastBattleStatus = E_BattleStatus.Home;

        // 战斗逻辑数据
        private ShikakuModel m_shikakuModel;
        public ShikakuModel ShikakuModel => m_shikakuModel;

        // 战斗标准逻辑管理器
        private ShikakuSimulation m_shikakuSimulation;
        public ShikakuSimulation ShikakuSimulation => m_shikakuSimulation;

        // 战斗道具逻辑管理器
        //private RingItemSystem m_itemSystem;

        // 战斗表现管理器
        private ShikakuController m_shikakuController;

        // TODO 初始化
        private void Init(MainSceneGameMode gameMode)
        {
            m_gameMode = gameMode;
            m_shikakuController = gameMode.ShikakuController;

            m_battleInputManager?.Clear();
            
            m_battleInputManager = new BattleInputManager();
            m_battleInputManager.Init();
            m_battleInputManager.OpenMenu();
        }

        public async UniTask NormalGameStart(MainSceneGameMode gameMode, CancellationToken ct)
        {

        }


        public void Tick()
        {
            if (battleStatus != E_BattleStatus.InBattle) return;
            if (m_shikakuModel == null) return;

            m_battleInputManager.Tick();

            var input = m_shikakuInputTransform.Resolve(m_battleInputManager.CurrentFrame);

            // 3. 执行游戏逻辑，修改 Model
            //m_shikakuSimulation.Tick(input, Time.deltaTime);

            SyncPresentation();
        }

        private void SyncPresentation()
        {
            m_shikakuController.Sync(m_shikakuModel);

        }

        public void Clear()
        {

        }
    }
}