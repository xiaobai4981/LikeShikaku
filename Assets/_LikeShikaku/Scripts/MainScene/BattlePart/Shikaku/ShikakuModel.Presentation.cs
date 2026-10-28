using System;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    // 当前选框及操作反馈。
    // 与 ShikakuModel.cs 共同维护同一份对局状态。
    public sealed partial class ShikakuModel
    {
        public int RowCount => Level.rows;
        public int ColCount => Level.cols;
        public bool IsSelecting { get; private set; }
        public Vector2Int StartCell { get; private set; }
        public Vector2Int EndCell { get; private set; }
        public E_ShikakuSelectResult LastResult { get; private set; }
        public int ResultVersion { get; private set; }
        public string LastMessage { get; private set; } = "";
        public ShikakuRuleResult LastRuleResult { get; private set; }
        public float ErrorDuration { get; set; } = .6f;
        public ShikakuRect? HintRectangle { get; private set; }

        private ShikakuRect? m_errorRect;
        private float m_errorRemainTime;
        public bool IsValidCell(Vector2Int pos)
        {
            return Level.InBounds(new GridCell(pos.x, pos.y));
        }

        public bool IsCompleted(int row, int col)
        {
            return RegionAt(new GridCell(row, col)) >= 0;
        }

        public int GetRegionId(int row, int col)
        {
            return RegionAt(new GridCell(row, col)) + 1;
        }

        public void BeginSelection(Vector2Int pos)
        {
            if (Status != E_ShikakuPlayStatus.Playing || !IsValidCell(pos))
            {
                return;
            }

            IsSelecting = true;
            StartCell = EndCell = pos;
            m_errorRemainTime = 0;
            HintRectangle = null;
        }

        public void UpdateSelection(Vector2Int pos)
        {
            if (IsSelecting && IsValidCell(pos))
            {
                EndCell = pos;
            }
        }

        public void FinishSelection()
        {
            IsSelecting = false;
        }

        public void CancelSelection()
        {
            IsSelecting = false;
            m_errorRemainTime = 0;
            HintRectangle = null;
        }

        public void CompleteRegion(int minRow, int maxRow, int minCol, int maxCol)
        {
            Submit(new ShikakuRect(minRow, maxRow, minCol, maxCol));
        }

        public bool TryClearRegion(Vector2Int pos)
        {
            return IsValidCell(pos) && RemoveAt(new GridCell(pos.x, pos.y));
        }

        public void SetResult(E_ShikakuSelectResult result)
        {
            LastResult = result;
            ResultVersion++;
        }

        public void SetNotice(string message, E_ShikakuSelectResult result = E_ShikakuSelectResult.StateChanged)
        {
            LastMessage = message ?? "";
            SetResult(result);
        }

        public void SetHint(ShikakuRect? rect, string message)
        {
            HintRectangle = rect;
            SetNotice(message, E_ShikakuSelectResult.Hint);
        }

        private void PresentFeedback(ShikakuRect rect, ShikakuRuleResult result)
        {
            m_errorRect = rect;
            m_errorRemainTime = ErrorDuration;
            LastRuleResult = result;
            HintRectangle = null;
            SetNotice(result.message, MapFailure(result.failure));
        }

        public void ShowError(E_ShikakuSelectResult result, float duration)
        {
            m_errorRect = ShikakuRect.Between(new GridCell(StartCell.x, StartCell.y), new GridCell(EndCell.x, EndCell.y));
            m_errorRemainTime = Math.Max(0, duration);
            SetResult(result);
        }

        public void AdvanceTime(float deltaTime)
        {
            m_errorRemainTime = Math.Max(0, m_errorRemainTime - Math.Max(0, deltaTime));
        }

        public E_ShikakuCellStatus GetCellStatus(int row, int col)
        {
            var cell = new GridCell(row, col);
            if (m_errorRemainTime > 0 && m_errorRect.HasValue && m_errorRect.Value.Contains(cell))
            {
                return E_ShikakuCellStatus.Error;
            }

            if (IsSelecting && ShikakuRect.Between(new GridCell(StartCell.x, StartCell.y), new GridCell(EndCell.x, EndCell.y)).Contains(cell))
            {
                return E_ShikakuCellStatus.Preview;
            }


            if (HintRectangle.HasValue && HintRectangle.Value.Contains(cell))
            {
                return E_ShikakuCellStatus.Hint;
            }

            return IsCompleted(row, col) ? E_ShikakuCellStatus.Success : E_ShikakuCellStatus.Normal;
        }

        private static E_ShikakuSelectResult MapFailure(E_ShikakuRuleFailure failure)
        {
            switch (failure)
            {
                case E_ShikakuRuleFailure.OutsideBoard:
                    return E_ShikakuSelectResult.OutsideBoard;
                case E_ShikakuRuleFailure.Overlap:
                    return E_ShikakuSelectResult.Overlap;
                case E_ShikakuRuleFailure.NoClue:
                    return E_ShikakuSelectResult.NoClue;
                case E_ShikakuRuleFailure.MultipleClues:
                    return E_ShikakuSelectResult.MultipleClues;
                case E_ShikakuRuleFailure.AreaMismatch:
                    return E_ShikakuSelectResult.AreaMismatch;
                default:
                    return E_ShikakuSelectResult.SessionClosed;
            }
        }
    }
}
