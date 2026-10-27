using Common.Level;
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

        //  初始化
        public void Init(MainSceneGameMode gameMode)
        {
            m_gameMode = gameMode;
            
        }

        public async UniTask NormalGameStart(
            LevelConfig levelConfig, ShikakuController controller, CancellationToken ct)
        {
            battleStatus = E_BattleStatus.Loading;
            Clear();

            int rowCount = levelConfig.determinant.x;
            int colCount = levelConfig.determinant.y;

            // 1. 数据。
            m_shikakuModel = new ShikakuModel(rowCount, colCount);

            // 2. 逻辑。
            m_shikakuSimulation = new ShikakuSimulation(
                m_shikakuModel,
                requireSquare: false,
                minArea: 2,
                errorDuration: 0.4f);

            // 3. 表现。
            m_shikakuController = controller;
            m_shikakuController.Init(m_gameMode, levelConfig);

            // 4. 坐标适配。
            m_shikakuInputTransform = new ShikakuInputTransform(
                m_shikakuController.GameCamera,
                m_shikakuController.MapRoot,
                m_shikakuController.CellLayer);

            // 5. 输入。
            m_battleInputManager = new BattleInputManager();
            m_battleInputManager.Init();

            m_battleInputManager.EnterGameplay();
            battleStatus = E_BattleStatus.InBattle;

            SyncPresentation();
        }

        public void EnterGameplay()
        {
            m_battleInputManager?.EnterGameplay();
        }

        public void OpenMenu()
        {
            m_shikakuSimulation?.CancelSelection();
            m_battleInputManager?.OpenMenu();

            SyncPresentation();
        }

        public void Tick()
        {
            if (battleStatus != E_BattleStatus.InBattle)
            {
                return;
            }

            if (m_battleInputManager == null ||
                m_shikakuInputTransform == null ||
                m_shikakuModel == null ||
                m_shikakuSimulation == null ||
                m_shikakuController == null)
            {
                return;
            }

            // 1. 采集输入。
            m_battleInputManager.Tick();

            BattleInputFrame rawInput = m_battleInputManager.CurrentFrame;

            // 2. 屏幕坐标转换为格子坐标。
            ShikakuInputFrame input = m_shikakuInputTransform.Resolve(rawInput);

            // 3. 处理逻辑，修改 Model。
            m_shikakuSimulation.Tick(input, Time.deltaTime);

            // 4. 读取 Model，更新表现。
            SyncPresentation();
        }

        private void SyncPresentation()
        {
            if (m_shikakuController == null || m_shikakuModel == null)
            {
                return;
            }

            m_shikakuController.Sync(m_shikakuModel);
        }

        public void Clear()
        {
            m_shikakuSimulation?.CancelSelection();
            m_battleInputManager?.Clear();
            m_shikakuController?.ClearMap();

            m_battleInputManager = null;
            m_shikakuInputTransform = null;

            m_shikakuSimulation = null;
            m_shikakuModel = null;
            m_shikakuController = null;
        }
    }
}