using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    public class ShikakuModel
    {
        // 基础数据
        public int RowCount { get; }
        public int ColCount { get; }
        private readonly int[,] m_regionIds;
        private int m_nextRegionId = 1;

        // 运行时数据
        public bool IsSelecting { get; private set; }
        public Vector2Int StartCell { get; private set; }
        public Vector2Int EndCell { get; private set; }
        private Vector2Int m_errorStart;
        private Vector2Int m_errorEnd;
        private float m_errorRemainTime;

        // 每次操作结果
        public E_ShikakuSelectResult LastResult { get; private set; }
        public int ResultVersion { get; private set; }

        public ShikakuModel(int rows, int cols)
        {
            RowCount = rows;
            ColCount = cols;

            m_regionIds = new int[rows, cols];

            IsSelecting = false;
            LastResult = E_ShikakuSelectResult.None;
            ResultVersion = 0;
        }

        public bool IsValidCell(Vector2Int pos)
        {
            return pos.x >= 0 && pos.x < RowCount &&
                   pos.y >= 0 && pos.y < ColCount;
        }

        public bool IsCompleted(int row, int col)
        {
            return m_regionIds[row, col] != 0;
        }

        public int GetRegionId(int row, int col)
        {
            return m_regionIds[row, col];
        }

        public void BeginSelection(Vector2Int pos)
        {
            IsSelecting = true;
            StartCell = pos;
            EndCell = pos;

            m_errorRemainTime = 0f;

            SetResult(E_ShikakuSelectResult.None);
        }

        public void UpdateSelection(Vector2Int pos)
        {
            if (!IsSelecting)
                return;

            EndCell = pos;
        }

        public void FinishSelection()
        {
            IsSelecting = false;
        }

        public void CancelSelection()
        {
            IsSelecting = false;
            m_errorRemainTime = 0f;

            SetResult(E_ShikakuSelectResult.None);
        }

        /// <summary>
        /// 将整个矩形记录为一个已完成区域。
        /// </summary>
        public void CompleteRegion(int minRow, int maxRow, int minCol, int maxCol)
        {
            int regionId = m_nextRegionId++;

            for (int row = minRow; row <= maxRow; row++)
            {
                for (int col = minCol; col <= maxCol; col++)
                {
                    m_regionIds[row, col] = regionId;
                }
            }
        }

        /// <summary>
        /// 点击已完成区域中的任意格子，取消整个区域。
        /// </summary>
        public bool TryClearRegion(Vector2Int pos)
        {
            if (!IsValidCell(pos))
                return false;

            int regionId = m_regionIds[pos.x, pos.y];

            if (regionId == 0)
                return false;

            for (int row = 0; row < RowCount; row++)
            {
                for (int col = 0; col < ColCount; col++)
                {
                    if (m_regionIds[row, col] == regionId)
                    {
                        m_regionIds[row, col] = 0;
                    }
                }
            }

            // 本次操作只取消区域，不启动新的框选。
            IsSelecting = false;
            m_errorRemainTime = 0f;

            SetResult(E_ShikakuSelectResult.RegionCleared);

            return true;
        }

        public void ShowError(E_ShikakuSelectResult result, float duration)
        {
            m_errorStart = StartCell;
            m_errorEnd = EndCell;

            m_errorRemainTime = Mathf.Max(0f, duration);

            SetResult(result);
        }

        public void SetResult(E_ShikakuSelectResult result)
        {
            LastResult = result;
            ResultVersion++;
        }

        public void AdvanceTime(float deltaTime)
        {
            if (m_errorRemainTime <= 0f)
                return;

            m_errorRemainTime = Mathf.Max(0f, m_errorRemainTime - Mathf.Max(0f, deltaTime));
        }

        public E_ShikakuCellStatus GetCellStatus(int row, int col)
        {
            // 错误反馈优先。
            if (m_errorRemainTime > 0f && IsInsideRectangle(m_errorStart, m_errorEnd, row, col))
            {
                return E_ShikakuCellStatus.Error;
            }

            // 当前框选预览。
            if (IsSelecting && IsInsideRectangle(StartCell, EndCell, row, col))
            {
                return IsCompleted(row, col)
                    ? E_ShikakuCellStatus.Error
                    : E_ShikakuCellStatus.Preview;
            }

            return IsCompleted(row, col)
                ? E_ShikakuCellStatus.Success
                : E_ShikakuCellStatus.Normal;
        }

        private static bool IsInsideRectangle(Vector2Int start, Vector2Int end, int row, int col)
        {
            int minRow = Mathf.Min(start.x, end.x);
            int maxRow = Mathf.Max(start.x, end.x);

            int minCol = Mathf.Min(start.y, end.y);
            int maxCol = Mathf.Max(start.y, end.y);

            return row >= minRow && row <= maxRow && col >= minCol && col <= maxCol;
        }

    }
}