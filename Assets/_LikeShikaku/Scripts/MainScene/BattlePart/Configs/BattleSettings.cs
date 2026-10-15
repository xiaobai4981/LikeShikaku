using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Configs
{
    [CreateAssetMenu(fileName = "BattleSettings", menuName = "Shikaku/Configs/BattleSettings")]
    public class BattleSettings : ScriptableObject
    {
        [Tooltip("方块间间隔")]
        public float BlockSpacing = 2.5f;

        [Tooltip("不同的方块")]
        [TableList]
        public List<Block> blocks;

        public GameObject GetBlockByType(E_ShikakuCellType blockType)
        {
            for (int i = 0; i < blocks.Count; i++)
            {
                if (blocks[i].blockType == blockType)
                {
                    return blocks[i].blockPrefab;
                }
            }
            // 理论上不会到这里
            return null;
        }

        [Serializable]
        public class Block
        {
            public E_ShikakuCellType blockType;
            public GameObject blockPrefab;
        }
    }
}