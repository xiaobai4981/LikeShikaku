using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    // TODO 单个方块表现管理
    [RequireComponent(typeof(SpriteRenderer))]
    [RequireComponent(typeof(BoxCollider2D))]
    public class ShikakuCell : MonoBehaviour
    {
        public int Row { get; private set; }
        public int Col { get; private set; }

        public E_ShikakuCellStatus NowStatus { get; private set; }

        private SpriteRenderer m_spriteRenderer;

        private Color m_normalColor;
        private Color m_previewColor;
        private Color m_successColor;
        private Color m_errorColor;

        public void Init(int row, int col,
            Color normalColor, Color previewColor,
            Color successColor, Color errorColor)
        {
            Row = row;
            Col = col;

            if (m_spriteRenderer == null)
            {
                m_spriteRenderer = GetComponent<SpriteRenderer>();
            }

            m_normalColor = normalColor;
            m_previewColor = previewColor;
            m_successColor = successColor;
            m_errorColor = errorColor;

            // 对象池复用时强制刷新一次颜色。
            NowStatus = E_ShikakuCellStatus.Normal;
            SetStatus(NowStatus);
        }

        public void ChangeStatus(E_ShikakuCellStatus cellStatus)
        {
            if (NowStatus == cellStatus)
                return;

            NowStatus = cellStatus;
            SetStatus(cellStatus);
        }

        private void SetStatus(E_ShikakuCellStatus cellStatus)
        {
            switch (cellStatus)
            {
                case E_ShikakuCellStatus.Normal:
                    m_spriteRenderer.color = m_normalColor;
                    break;

                case E_ShikakuCellStatus.Preview:
                    m_spriteRenderer.color = m_previewColor;
                    break;

                case E_ShikakuCellStatus.Success:
                    m_spriteRenderer.color = m_successColor;
                    break;

                case E_ShikakuCellStatus.Error:
                    m_spriteRenderer.color = m_errorColor;
                    break;
            }
        }
    }
}