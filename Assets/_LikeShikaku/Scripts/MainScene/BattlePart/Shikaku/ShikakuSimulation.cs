using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    public class ShikakuSimulation
    {
        private readonly ShikakuModel m_model;

        private readonly bool m_requireSquare;
        private readonly float m_errorDuration;

        public ShikakuSimulation(ShikakuModel model, bool requireSquare = false, 
            int minArea = 2, float errorDuration = 0.4f)
        {
            m_model = model;

            m_requireSquare = requireSquare;
            m_errorDuration = Mathf.Max(0f, errorDuration);
        }

        public void Tick(ShikakuInputFrame input, float deltaTime)
        {
            m_model.AdvanceTime(deltaTime);

            // 1. 按下：取消已有区域，或者开始框选。
            if (input.PressDown && input.HasCell)
            {
                BeginSelection(input.CellPosition);
            }

            // 取消已有区域后这里会直接返回，
            // 所以这次拖动和松开不会重新提交矩形。
            if (!m_model.IsSelecting)
                return;

            // 2. 按住或松开：更新矩形终点。
            if ((input.Pressing || input.PressUp) && input.HasCell)
            {
                UpdateSelection(input.CellPosition);
            }

            // 3. 松开：提交矩形。
            if (input.PressUp)
            {
                ConfirmSelection();
            }
        }

        private void BeginSelection(Vector2Int pos)
        {
            if (!m_model.IsValidCell(pos))
                return;

            // 点击已完成区域：取消整个区域。
            if (m_model.TryClearRegion(pos))
                return;

            // 点击普通格子：开始框选。
            m_model.BeginSelection(pos);
        }

        private void UpdateSelection(Vector2Int pos)
        {
            if (!m_model.IsSelecting)
                return;

            if (!m_model.IsValidCell(pos))
                return;

            m_model.UpdateSelection(pos);
        }

        private void ConfirmSelection()
        {
            Vector2Int start = m_model.StartCell;
            Vector2Int end = m_model.EndCell;

            int minRow = Mathf.Min(start.x, end.x);
            int maxRow = Mathf.Max(start.x, end.x);

            int minCol = Mathf.Min(start.y, end.y);
            int maxCol = Mathf.Max(start.y, end.y);

            int width = maxCol - minCol + 1;
            int height = maxRow - minRow + 1;

            E_ShikakuSelectResult result = Validate(
                minRow,
                maxRow,
                minCol,
                maxCol,
                width,
                height);

            if (result == E_ShikakuSelectResult.Success)
            {
                m_model.CompleteRegion(
                    minRow,
                    maxRow,
                    minCol,
                    maxCol);

                m_model.FinishSelection();

                m_model.SetResult(
                    E_ShikakuSelectResult.Success);
            }
            else
            {
                m_model.ShowError(result, m_errorDuration);
                m_model.FinishSelection();
            }
        }

        private E_ShikakuSelectResult Validate(
            int minRow,
            int maxRow,
            int minCol,
            int maxCol,
            int width,
            int height)
        {
            if (m_requireSquare && width != height)
                return E_ShikakuSelectResult.NotSquare;

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    if (m_model.IsCompleted(row, col))
                    {
                        return E_ShikakuSelectResult.Overlap;
                    }
                }
            }

            // 后续数字约束、禁止区域、预设边界等规则，
            // 可以继续添加在这里。

            return E_ShikakuSelectResult.Success;
        }

        public void CancelSelection()
        {
            m_model.CancelSelection();
        }
    }
}