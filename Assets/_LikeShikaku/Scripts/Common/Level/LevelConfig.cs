using System;
using System.Collections.Generic;
using MainScene.BattlePart;
using MainScene.BattlePart.Shikaku;
using UnityEngine;

namespace Common.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Shikaku/Level")]
    public class LevelConfig : ScriptableObject
    {
        // 保留原序列化字段名，沿用已有资源和场景引用。
        public Vector2Int determinant;
        public CellData[] cellDatas;
        public string levelId;
        public string title;
        public string briefing;
        public E_FormationTheme formation;
        public bool requireArcherSupport;
        [Min(0)]
        public int maximumErrors;
        public List<TutorialStep> lessons = new List<TutorialStep>();
        public bool HasOrders => cellDatas != null && Array.Exists(cellDatas, x => x.number > 0);

        public ShikakuLevelData ToData()
        {
            var data = new ShikakuLevelData
            {
                id = string.IsNullOrWhiteSpace(levelId) ? name : levelId,
                title = string.IsNullOrWhiteSpace(title) ? name : title,
                briefing = briefing,
                formation = formation,
                requireArcherSupport = requireArcherSupport,
                rows = determinant.x,
                cols = determinant.y,
                maximumErrors = maximumErrors,
                lessons = lessons ?? new List<TutorialStep>()
            };
            var used = new HashSet<GridCell>();
            foreach (var entry in cellDatas ?? Array.Empty<CellData>())
            {
                var cell = new GridCell(entry.position.x, entry.position.y);
                if (!data.InBounds(cell) || !used.Add(cell))
                {
                    throw new ArgumentException(name + "：格子坐标越界或重复。");
                }

                if (entry.number < 0)
                {
                    throw new ArgumentException(name + "：将令数字不能为负数。");
                }

                if (entry.number > 0)
                {
                    data.clues.Add(new ArmyOrder { cell = cell, area = entry.number, unit = entry.armyType });
                }
            }

            var errors = data.Inspect();
            if (errors.Count > 0)
            {
                throw new ArgumentException(name + "：" + string.Join("；", errors));
            }

            return data;
        }

        public void SetData(ShikakuLevelData data)
        {
            levelId = data.id;
            title = data.title;
            briefing = data.briefing;
            formation = data.formation;
            requireArcherSupport = data.requireArcherSupport;
            determinant = new Vector2Int(data.rows, data.cols);
            maximumErrors = data.maximumErrors;
            var list = new List<CellData>();
            foreach (var order in data.clues)
            {
                list.Add(new CellData { position = new Vector2Int(order.cell.row, order.cell.col), number = order.area, armyType = order.unit });
            }


            cellDatas = list.ToArray();
            lessons = new List<TutorialStep>(data.lessons);
        }
    }

    [Serializable]
    public struct CellData
    {
        [Tooltip("x 为行，y 为列，从左上角 (0,0) 开始")]
        public Vector2Int position;
        public E_ShikakuCellType cellType;
        [Tooltip("0 为空地，正数为将令所需格数")]
        public int number;
        public E_ArmyType armyType;
    }
}
