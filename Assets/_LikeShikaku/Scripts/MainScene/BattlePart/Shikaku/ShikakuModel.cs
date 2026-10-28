using System;
using System.Collections.Generic;

namespace MainScene.BattlePart.Shikaku
{
    [Serializable]
    public class ShikakuSnapshot
    {
        public List<ShikakuRect> regions = new List<ShikakuRect>();
    }

    [Serializable]
    public class ShikakuResume
    {
        public string levelId;
        public string fingerprint;
        public int errors;
        public int hintsUsed;
        public int lessonIndex;
        public List<ShikakuRect> regions = new List<ShikakuRect>();
        public List<ShikakuSnapshot> history = new List<ShikakuSnapshot>();
    }

    public sealed partial class ShikakuModel
    {
        public ShikakuLevelData Level { get; }

        private readonly List<ShikakuRect> m_regions = new List<ShikakuRect>();
        private readonly List<ShikakuSnapshot> m_history = new List<ShikakuSnapshot>();
        private readonly IReadOnlyList<ShikakuRect> m_readOnlyRegions;
        private int[] m_regionByCell;
        private int m_regionCacheRevision = -1;
        public IReadOnlyList<ShikakuRect> Regions => m_readOnlyRegions;
        public int Errors { get; private set; }
        public int HintsUsed { get; private set; }
        public int LessonIndex { get; private set; }
        public int Revision { get; private set; }
        public E_ShikakuPlayStatus Status { get; private set; } = E_ShikakuPlayStatus.Playing;
        public bool InLesson => LessonIndex < Level.lessons.Count;
        public TutorialStep CurrentLesson => InLesson ? Level.lessons[LessonIndex] : null;
        public bool CanUndo => Status == E_ShikakuPlayStatus.Playing && m_history.Count > 0;

        public int CoveredArea
        {
            get
            {
                int n = 0;
                foreach (var r in m_regions)
                {
                    n += r.Area;
                }

                return n;
            }
        }

        public int Stars => Status != E_ShikakuPlayStatus.Won ? 0 : Errors == 0 && HintsUsed == 0 ? 3 : Errors <= 1 ? 2 : 1;

        public ShikakuModel(ShikakuLevelData level)
        {
            var problems = level.Inspect();
            if (problems.Count > 0)
            {
                throw new ArgumentException(string.Join("\n", problems));
            }

            Level = level;
            m_readOnlyRegions = m_regions.AsReadOnly();
        }

        public int RegionAt(GridCell cell)
        {
            if (!Level.InBounds(cell))
            {
                return -1;
            }

            // 大棋盘逐帧着色使用格子索引；仅在布局变化时重建。
            if (m_regionCacheRevision != Revision)
            {
                if (m_regionByCell == null)
                {
                    m_regionByCell = new int[Level.AvailableArea];
                }

                for (int i = 0; i < m_regionByCell.Length; i++)
                {
                    m_regionByCell[i] = -1;
                }

                for (int i = 0; i < m_regions.Count; i++)
                {
                    var rect = m_regions[i];
                    for (int row = rect.top; row <= rect.bottom; row++)
                    {
                        for (int col = rect.left; col <= rect.right; col++)
                        {
                            m_regionByCell[row * Level.cols + col] = i;
                        }
                    }
                }

                m_regionCacheRevision = Revision;
            }

            return m_regionByCell[cell.row * Level.cols + cell.col];
        }

        public ShikakuRuleResult Submit(ShikakuRect rect)
        {
            if (Status != E_ShikakuPlayStatus.Playing)
            {
                return ShikakuRuleResult.Error(E_ShikakuRuleFailure.SessionClosed, "当前不能布阵。");
            }

            var result = ShikakuRules.Validate(Level, rect, m_regions);
            if (!result.IsValid)
            {
                bool teaching = InLesson;
                AdvanceLesson(E_TutorialAction.InvalidSubmission, rect, result.failure);
                if (!teaching && Level.maximumErrors > 0)
                {
                    Errors++;
                    if (Errors >= Level.maximumErrors)
                    {
                        Status = E_ShikakuPlayStatus.Lost;
                    }
                }

                PresentFeedback(rect, result);
                return result;
            }

            Remember();
            m_regions.Add(rect);
            Revision++;
            AdvanceLesson(E_TutorialAction.Deploy, rect, E_ShikakuRuleFailure.None);
            CheckWin();
            SetNotice(Level.requireArcherSupport && Status != E_ShikakuPlayStatus.Won
                ? (CoveredArea == Level.AvailableArea ? "全盘已覆盖，仍有弓阵缺少护卫；点击军阵撤回调整，不扣错。" : "部队就位，可继续部署护卫。") + "\n" + ShikakuRules.FormationProgress(Level, m_regions)
                : result.message, E_ShikakuSelectResult.Success);
            return result;
        }

        public bool RemoveAt(GridCell cell)
        {
            if (Status != E_ShikakuPlayStatus.Playing)
            {
                return false;
            }

            int index = RegionAt(cell);
            if (index < 0)
            {
                return false;
            }

            Remember();
            var rect = m_regions[index];
            m_regions.RemoveAt(index);
            Revision++;
            AdvanceLesson(E_TutorialAction.Remove, rect, E_ShikakuRuleFailure.None);
            CheckWin();
            CancelSelection();
            SetNotice("已撤回整支部队，不扣次数。", E_ShikakuSelectResult.RegionCleared);
            return true;
        }

        private void Remember()
        {
            m_history.Add(new ShikakuSnapshot { regions = new List<ShikakuRect>(m_regions) });
            if (m_history.Count > 100)
            {
                m_history.RemoveAt(0);
            }
        }

        public bool Undo()
        {
            if (!CanUndo)
            {
                return false;
            }

            var old = m_history[m_history.Count - 1];
            m_history.RemoveAt(m_history.Count - 1);
            m_regions.Clear();
            m_regions.AddRange(old.regions);
            Revision++;
            // 撤销仅恢复布局，不返还容错、提示或已完成教学。
            CheckWin();
            CancelSelection();
            SetNotice("已撤销上一步，错误与提示次数不回退。", E_ShikakuSelectResult.Undo);
            return true;
        }

        private void AdvanceLesson(E_TutorialAction action, ShikakuRect rect, E_ShikakuRuleFailure failure)
        {
            var step = CurrentLesson;
            if (step == null || step.action != action || (step.hasTarget && !rect.Contains(step.target)) || (action == E_TutorialAction.InvalidSubmission && step.expectedFailure != E_ShikakuRuleFailure.None && step.expectedFailure != failure))
            {
                return;
            }

            LessonIndex++;
            SkipAlreadySatisfiedDeploySteps();
        }

        private void SkipAlreadySatisfiedDeploySteps()
        {
            while (InLesson && CurrentLesson.action == E_TutorialAction.Deploy && m_regions.Exists(r => !CurrentLesson.hasTarget || r.Contains(CurrentLesson.target)))
            {
                LessonIndex++;
            }
        }

        public void SkipLesson()
        {
            LessonIndex = Level.lessons.Count;
            CheckWin();
            SetNotice("已结束本关教学。");
        }

        public void Pause(bool paused)
        {
            if (Status == E_ShikakuPlayStatus.Playing && paused)
            {
                Status = E_ShikakuPlayStatus.Paused;
                CancelSelection();
                SetNotice("已暂停。");
            }
            else if (Status == E_ShikakuPlayStatus.Paused && !paused)
            {
                Status = E_ShikakuPlayStatus.Playing;
                SetNotice("继续布阵。");
            }
        }

        public void UseHint()
        {
            if (Status == E_ShikakuPlayStatus.Playing)
            {
                HintsUsed++;
                SetResult(E_ShikakuSelectResult.Hint);
            }
        }

        private void CheckWin()
        {
            if (CoveredArea != Level.AvailableArea || !ShikakuRules.FormationSatisfied(Level, m_regions))
            {
                return;
            }

            // 棋盘合法完成后直接通关，不让可选教学阻碍结算。
            LessonIndex = Level.lessons.Count;
            Status = E_ShikakuPlayStatus.Won;
        }

        public ShikakuResume Capture()
        {
            var copy = new List<ShikakuSnapshot>();
            foreach (var h in m_history)
            {
                copy.Add(new ShikakuSnapshot { regions = new List<ShikakuRect>(h.regions) });
            }

            return new ShikakuResume
            {
                levelId = Level.id,
                fingerprint = Level.Fingerprint(),
                errors = Errors,
                hintsUsed = HintsUsed,
                lessonIndex = LessonIndex,
                regions = new List<ShikakuRect>(m_regions),
                history = copy
            };
        }

        public bool Restore(ShikakuResume data)
        {
            if (data == null || data.levelId != Level.id || data.fingerprint != Level.Fingerprint() || data.errors < 0 || (Level.maximumErrors == 0 && data.errors != 0) || (Level.maximumErrors > 0 && data.errors > Level.maximumErrors) || data.hintsUsed < 0 || data.lessonIndex < 0 || data.lessonIndex > Level.lessons.Count || !ValidRegions(data.regions))
            {
                return false;
            }

            // 先验证再修改当前数据；损坏的历史单独丢弃。
            m_regions.Clear();
            m_regions.AddRange(data.regions);
            m_history.Clear();
            if (data.history != null && data.history.Count <= 100)
            {
                foreach (var h in data.history)
                {
                    if (h == null || !ValidRegions(h.regions))
                    {
                        m_history.Clear();
                        break;
                    }

                    m_history.Add(new ShikakuSnapshot { regions = new List<ShikakuRect>(h.regions) });
                }
            }

            Errors = data.errors;
            HintsUsed = data.hintsUsed;
            LessonIndex = data.lessonIndex;
            Status = Level.maximumErrors > 0 && Errors >= Level.maximumErrors ? E_ShikakuPlayStatus.Lost : E_ShikakuPlayStatus.Playing;
            if (Status == E_ShikakuPlayStatus.Playing)
            {
                CheckWin();
            }

            Revision++;
            CancelSelection();
            SetNotice("已恢复上次布阵。");
            return true;
        }

        private bool ValidRegions(List<ShikakuRect> list)
        {
            if (list == null || list.Count > Level.clues.Count)
            {
                return false;
            }

            var accepted = new List<ShikakuRect>();
            foreach (var r in list)
            {
                if (!ShikakuRules.Validate(Level, r, accepted).IsValid)
                {
                    return false;
                }

                accepted.Add(r);
            }

            return true;
        }
    }
}
