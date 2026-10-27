using MainScene.BattlePart;
using System;
using UnityEngine;

namespace Common.Level
{
    [CreateAssetMenu(fileName = "LevelConfig", menuName = "Shikaku/Level")]
    public class LevelConfig : ScriptableObject
    {
        public Vector2Int determinant;
        public CellData[] cellDatas;

    }

    [Serializable]
    public struct CellData
    {
        // TODO 方块内拓展数据
        [Tooltip("x 为行，y 为列，从左上角 (0,0) 开始")]
        public Vector2Int position;
        public E_ShikakuCellType cellType;
        [Tooltip("0 为空地，正数为将令所需格数")]
        public int number;
        //public ArmyType armyType;
    }
}