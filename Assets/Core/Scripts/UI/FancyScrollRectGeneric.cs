using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI.Extensions;
using UnityEngine.UI.Extensions.EasingCore;

namespace Core.UI
{
    public class FancyScrollRectGeneric : FancyScrollRect<FancyScrollRectGenericItemData, FancyScrollRectGenericContext>
    {
        public delegate void InitCellCallback<T>(ref T cell, Transform cellRoot, FancyScrollRectGenericCell genericCell);
        public delegate void UpdateCellContentCallback<T>(ref T cell, int dataIndex);

        [SerializeField]
        private float m_cellSize = 100f;

        [SerializeField]
        private FancyScrollRectGenericCell m_cellPrefab;

        protected override float CellSize => m_cellSize;
        protected override GameObject CellPrefab => m_cellPrefab.gameObject;

        private IList m_pooledCells = null;

        public void Init<T>(
            InitCellCallback<T> initCell,
            UpdateCellContentCallback<T> updateCellContent)
            where T : struct
        {
            Debug.Assert(m_pooledCells == null);
            m_pooledCells = new List<T>();

            Context.InitCell = (genericCell, cellRoot) => {
                var pooledCells = (List<T>)m_pooledCells;
                int cellInstanceId = pooledCells.Count;
                T cell = new();
                initCell(ref cell, cellRoot, genericCell);
                pooledCells.Add(cell);
                return cellInstanceId;
            };
            Context.UpdateCellContent = (cellInstanceId, dataIndex) => {
                var pooledCells = (List<T>)m_pooledCells;
                T cell = pooledCells[cellInstanceId];
                updateCellContent(ref cell, dataIndex);
                pooledCells[cellInstanceId] = cell; // 写回改动
            };
        }

        public void UpdateCells(int newCellCount)
        {
            // 不关心 ItemsSource 的内容，仅为了设置数量
            ResizeList(ItemsSource, newCellCount);
            UpdateContents(ItemsSource);
        }

        public void ScrollTo(int index)
        {
            base.ScrollTo(index, 0.3f, Ease.InOutQuint, 0.5f);
        }

        public void JumpTo(int index)
        {
            base.JumpTo(index, 0.5f);
        }

        public void RefreshCells()
        {
            base.Refresh();
        }

        private static void ResizeList<T>(IList<T> list, int newSize)
        {
            int delta = newSize - list.Count;
            while (delta > 0)
            {
                list.Add(default);
                --delta;
            }
            while (delta < 0)
            {
                list.RemoveAt(list.Count - 1);
                ++delta;
            }
        }
    }
}