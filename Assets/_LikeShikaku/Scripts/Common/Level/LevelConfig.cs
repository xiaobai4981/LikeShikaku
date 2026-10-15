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
    }
}