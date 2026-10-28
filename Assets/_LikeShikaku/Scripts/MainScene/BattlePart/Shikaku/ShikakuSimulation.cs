using System;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    /// <summary>只处理输入流程，规则和对局状态由 Model 负责。</summary>
    public class ShikakuSimulation
    {
        private readonly ShikakuModel m_model;
        public ShikakuSimulation(ShikakuModel model, bool requireSquare = false, int minArea = 1, float errorDuration = .6f)
        {
            m_model = model ?? throw new ArgumentNullException(nameof(model));
            // 保留旧调用参数，实际形状和面积限制由关卡配置决定。
            // 基础规则允许长方形和正方形。
            m_model.ErrorDuration = Math.Max(0, errorDuration);
        }

        public void Tick(ShikakuInputFrame input, float deltaTime)
        {
            m_model.AdvanceTime(deltaTime);
            if (m_model.Status != E_ShikakuPlayStatus.Playing)
            {
                m_model.CancelSelection();
                return;
            }

            if (input.PressDown && input.HasCell)
            {
                if (m_model.TryClearRegion(input.CellPosition))
                {
                    return;
                }

                m_model.BeginSelection(input.CellPosition);
            }

            if (!m_model.IsSelecting)
            {
                return;
            }

            if ((input.Pressing || input.PressUp) && input.HasCell)
            {
                m_model.UpdateSelection(input.CellPosition);
            }

            if (!input.PressUp)
            {
                return;
            }

            // 棋盘外松手只取消，不使用最后一个有效终点提交。
            if (!input.HasCell)
            {
                CancelSelection();
                return;
            }

            m_model.CompleteRegion(Math.Min(m_model.StartCell.x, m_model.EndCell.x), Math.Max(m_model.StartCell.x, m_model.EndCell.x), Math.Min(m_model.StartCell.y, m_model.EndCell.y), Math.Max(m_model.StartCell.y, m_model.EndCell.y));
            m_model.FinishSelection();
        }

        public void CancelSelection()
        {
            m_model.CancelSelection();
        }
    }
}
