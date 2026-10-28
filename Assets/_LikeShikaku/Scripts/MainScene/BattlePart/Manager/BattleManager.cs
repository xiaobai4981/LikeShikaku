using System;
using System.Collections.Generic;
using System.Threading;
using Common.Level;
using Cysharp.Threading.Tasks;
using MainScene.BattlePart.Input;
using MainScene.BattlePart.Shikaku;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MainScene.BattlePart.Manager
{
    /// <summary>战斗流程管理：组织数据、逻辑和输入，规则与表现各自独立。</summary>
    public class BattleManager
    {
        private MainSceneGameMode m_gameMode;
        private BattleInputManager m_battleInputManager;
        private ShikakuInputTransform m_shikakuInputTransform;
        private ShikakuModel m_shikakuModel;
        private ShikakuSimulation m_shikakuSimulation;
        private ShikakuController m_shikakuController;
        private ShikakuProgressStore m_progress;
        private int m_notifiedVersion = -1;
        private int m_hintRevision = -1;
        private int m_hintStage;
        private bool m_finishNotified;
        private readonly List<RaycastResult> m_uiHits = new List<RaycastResult>();
        public BattleInputManager BattleInputManager => m_battleInputManager;
        public ShikakuModel ShikakuModel => m_shikakuModel;
        public ShikakuSimulation ShikakuSimulation => m_shikakuSimulation;
        public ShikakuProgressStore Progress => m_progress;

        public E_BattleStatus battleStatus = E_BattleStatus.Home;
        public event Action<BattleChangedEvent> Changed;
        public event Action<BattleFinishedEvent> Finished;
        public void Init(MainSceneGameMode gameMode)
        {
            m_gameMode = gameMode;
            m_progress = new ShikakuProgressStore(gameMode.IsPlaytesting || gameMode.IsValidationRun);
        }

        public UniTask NormalGameStart(LevelConfig config, ShikakuController controller, CancellationToken ct)
        {
            return NormalGameStart(config.ToData(), controller, ct, true);
        }

        public UniTask NormalGameStart(ShikakuLevelData data, ShikakuController controller, CancellationToken ct, bool restore = false)
        {
            ct.ThrowIfCancellationRequested();
            // 先验证下一关的数据，再清理当前关卡。
            var nextModel = new ShikakuModel(data);
            var saved = restore ? m_progress.Data.resume : null;
            bool restored = saved != null && nextModel.Restore(saved);
            Clear();
            battleStatus = E_BattleStatus.Loading;
            try
            {
                m_shikakuModel = nextModel;
                m_shikakuController = controller;
                if (!controller)
                {
                    throw new ArgumentException("ShikakuController 未赋值。");
                }

                m_shikakuController.Init(m_gameMode, data);
                m_shikakuInputTransform = new ShikakuInputTransform(controller.GameCamera, controller.MapRoot, controller.CellLayer);
                m_shikakuSimulation = new ShikakuSimulation(nextModel);
                m_battleInputManager = new BattleInputManager();
                m_battleInputManager.Init();
                m_battleInputManager.EnterGameplay();
                m_finishNotified = false;
                m_notifiedVersion = -1;
                m_hintRevision = -1;
                m_hintStage = 0;
                nextModel.SetNotice(restored ? "已恢复上次布阵。" : data.briefing ?? "依将令列阵。");
                NotifyIfChanged();
                SyncPresentation();
                return UniTask.CompletedTask;
            }
            catch
            {
                Clear();
                throw;
            }
        }

        public void EnterGameplay()
        {
            if (m_shikakuModel == null)
            {
                return;
            }

            m_shikakuModel.Pause(false);
            NotifyIfChanged();
        }

        public void OpenMenu()
        {
            if (m_shikakuModel == null)
            {
                return;
            }

            m_shikakuSimulation.CancelSelection();
            m_shikakuModel.Pause(true);
            NotifyIfChanged();
        }

        public void Undo()
        {
            if (m_shikakuModel == null)
            {
                return;
            }

            m_shikakuSimulation.CancelSelection();
            m_shikakuModel.Undo();
            NotifyIfChanged();
            SyncPresentation();
        }

        public void SkipTutorial()
        {
            if (m_shikakuModel == null || m_shikakuModel.Status != E_ShikakuPlayStatus.Playing)
            {
                return;
            }

            m_shikakuModel.SkipLesson();
            NotifyIfChanged();
            SyncPresentation();
        }

        public void Hint()
        {
            var model = m_shikakuModel;
            if (model == null || model.Status != E_ShikakuPlayStatus.Playing)
            {
                return;
            }

            m_shikakuSimulation.CancelSelection();
            var solved = ShikakuSolver.Solve(model.Level, model.Regions, 1, 50000);
            if (solved.solutions.Count == 0)
            {
                model.SetNotice(solved.budgetExceeded ? "推演达到计算上限，暂时无法确认。" : model.Level.requireArcherSupport ? "当前布阵无法同时完成覆盖与护卫。请撤回或撤销阵地，为弓阵留出步阵或枪阵共边的位置。" : "当前布阵无法完成棋盘，请撤回或撤销一块阵地后再推演。");
                NotifyIfChanged();
                return;
            }

            var remaining = solved.solutions[0].FindAll(r =>
            {
                foreach (var placed in model.Regions)
                {
                    if (placed.Equals(r))
                    {
                        return false;
                    }
                }

                return true;
            });
            if (remaining.Count == 0)
            {
                return;
            }

            if (m_hintRevision != model.Revision)
            {
                m_hintRevision = model.Revision;
                m_hintStage = 0;
            }

            m_hintStage = Math.Min(3, m_hintStage + 1);
            model.UseHint();
            var rect = remaining[0];
            var clue = model.Level.clues.Find(x => rect.Contains(x.cell));
            if (m_hintStage == 1)
            {
                model.SetHint(null, model.Level.requireArcherSupport ? "孔明：" + ShikakuNames.FormationIdea(model.Level.formation) + " 先考虑哪支步阵或枪阵能与弓阵共边，再比较数字的长宽组合。" : "孔明：从棋盘外沿的将令入手，列出数字的长宽组合。");
            }
            else if (m_hintStage == 2)
            {
                model.SetHint(null, $"留意第 {clue.cell.row + 1} 行、第 {clue.cell.col + 1} 列的{ShikakuNames.Unit(clue.unit)}兵将令（{clue.area} 格）。");
            }
            else
            {
                model.SetHint(rect, "已标出一个可行矩形，请自行拖动部署。");
            }

            NotifyIfChanged();
            SyncPresentation();
        }

        public void Tick()
        {
            if (m_shikakuModel == null || m_shikakuSimulation == null)
            {
                return;
            }

            if (battleStatus == E_BattleStatus.InBattle)
            {
                m_battleInputManager.Tick();
                var raw = m_battleInputManager.CurrentFrame;
                if (raw.Cancelled)
                {
                    m_shikakuSimulation.CancelSelection();
                }

                var input = m_shikakuInputTransform.Resolve(raw);
                if (raw.Cancelled)
                {
                    input = default;
                }

                if (raw.PressDown && IsOverUI(raw.ScreenPosition))
                {
                    input.PressDown = false;
                }

                if (raw.PressUp && IsOverUI(raw.ScreenPosition))
                {
                    input.HasCell = false;
                }

                m_shikakuSimulation.Tick(input, Time.deltaTime);
            }
            else
            {
                m_shikakuModel.AdvanceTime(Time.deltaTime);
            }

            NotifyIfChanged();
            SyncPresentation();
        }

        private bool IsOverUI(Vector2 screenPosition)
        {
            if (!EventSystem.current)
            {
                return false;
            }

            m_uiHits.Clear();
            EventSystem.current.RaycastAll(new PointerEventData(EventSystem.current) { position = screenPosition }, m_uiHits);
            foreach (var hit in m_uiHits)
            {
                if (hit.module is GraphicRaycaster)
                {
                    return true;
                }
            }

            return false;
        }

        private void NotifyIfChanged()
        {
            var model = m_shikakuModel;
            if (model == null)
            {
                return;
            }

            var next = model.Status == E_ShikakuPlayStatus.Playing ? E_BattleStatus.InBattle : model.Status == E_ShikakuPlayStatus.Paused ? E_BattleStatus.InMenu : E_BattleStatus.Over;
            if (battleStatus != next)
            {
                battleStatus = next;
                if (next == E_BattleStatus.InBattle)
                {
                    m_battleInputManager?.EnterGameplay();
                }
                else
                {
                    m_battleInputManager?.OpenMenu();
                }
            }

            if (m_notifiedVersion == model.ResultVersion)
            {
                return;
            }

            m_notifiedVersion = model.ResultVersion;
            Save();
            Changed?.Invoke(new BattleChangedEvent(model));
            if (!m_finishNotified && (model.Status == E_ShikakuPlayStatus.Won || model.Status == E_ShikakuPlayStatus.Lost))
            {
                m_finishNotified = true;
                Finished?.Invoke(new BattleFinishedEvent(model));
            }
        }

        private void SyncPresentation()
        {
            if (m_shikakuController && m_shikakuModel != null)
            {
                m_shikakuController.Sync(m_shikakuModel);
            }
        }

        public void Save()
        {
            if (m_shikakuModel != null)
            {
                m_progress?.Save(m_shikakuModel);
            }
        }

        public void Clear()
        {
            Save();
            m_shikakuSimulation?.CancelSelection();
            m_battleInputManager?.Clear();
            m_shikakuController?.ClearMap();
            m_battleInputManager = null;
            m_shikakuInputTransform = null;
            m_shikakuModel = null;
            m_shikakuSimulation = null;
            m_shikakuController = null;
            battleStatus = E_BattleStatus.Home;
        }
    }
}
