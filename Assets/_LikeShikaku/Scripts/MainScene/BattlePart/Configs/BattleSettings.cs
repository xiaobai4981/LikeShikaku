using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using MainScene.BattlePart.Shikaku;
using UnityEngine;

namespace MainScene.BattlePart.Configs
{
    [CreateAssetMenu(fileName = "BattleSettings", menuName = "Shikaku/Configs/BattleSettings")]
    public class BattleSettings : ScriptableObject
    {
        [Min(.1f)]
        public float BlockSpacing = 2.5f;
        [TableList]
        public List<Block> blocks = new List<Block>();
        public Sprite groundSprite;
        public Sprite infantrySprite;
        public Sprite spearmanSprite;
        public Sprite archerSprite;
        public Sprite cavalrySprite;
        public Sprite infantryFlag;
        public Sprite spearmanFlag;
        public Sprite archerFlag;
        public Sprite cavalryFlag;
        [Header("兵种配色（将令、旗帜、军阵与边框）")]
        public Color infantryColor = DefaultUnitColor(E_ArmyType.Infantry);
        public Color spearmanColor = DefaultUnitColor(E_ArmyType.Spearman);
        public Color archerColor = DefaultUnitColor(E_ArmyType.Archer);
        public Color cavalryColor = DefaultUnitColor(E_ArmyType.Cavalry);
        [Tooltip("包含所需汉字的 TMP 字体，建议正式构建前显式指定")]
        public TMPro.TMP_FontAsset boardFont;
        public Font hudFont;
        public Sprite backgroundSprite;
        public Sprite advisorPortrait;
        public Sprite advisorVictory;
        public bool AutoFitCamera = true;
        [Range(.35f, .9f)]
        public float BoardViewportHeight = .64f;
        public float CameraPadding = 1.2f;
        [Range(0, 100)]
        public float CellGapPercent = 6;
        public bool ShowFallbackHUD = true;
        public static Color DefaultUnitColor(E_ArmyType type)
        {
            switch (type)
            {
                case E_ArmyType.Infantry: return new Color32(57, 123, 203, 255);
                case E_ArmyType.Spearman: return new Color32(33, 138, 101, 255);
                case E_ArmyType.Archer: return new Color32(215, 131, 36, 255);
                case E_ArmyType.Cavalry: return new Color32(142, 90, 181, 255);
                default: return Color.white;
            }
        }

        public Color UnitColor(E_ArmyType type)
        {
            switch (type)
            {
                case E_ArmyType.Infantry: return infantryColor;
                case E_ArmyType.Spearman: return spearmanColor;
                case E_ArmyType.Archer: return archerColor;
                case E_ArmyType.Cavalry: return cavalryColor;
                default: return Color.white;
            }
        }

        public GameObject GetBlockByType(E_ShikakuCellType blockType)
        {
            Block fallback = null;
            foreach (var block in blocks ?? new List<Block>())
            {
                if (block == null || !block.blockPrefab)
                {
                    continue;
                }

                if (block.blockType == blockType)
                {
                    return block.blockPrefab;
                }

                if (block.blockType == E_ShikakuCellType.Normal)
                {
                    fallback = block;
                }
            }

            return fallback == null ? null : fallback.blockPrefab;
        }

        public Sprite Unit(E_ArmyType type)
        {
            switch (type)
            {
                case E_ArmyType.Infantry:
                    return infantrySprite;
                case E_ArmyType.Spearman:
                    return spearmanSprite;
                case E_ArmyType.Archer:
                    return archerSprite;
                case E_ArmyType.Cavalry:
                    return cavalrySprite;
                default:
                    return null;
            }
        }

        public Sprite Flag(E_ArmyType type)
        {
            switch (type)
            {
                case E_ArmyType.Infantry:
                    return infantryFlag;
                case E_ArmyType.Spearman:
                    return spearmanFlag;
                case E_ArmyType.Archer:
                    return archerFlag;
                case E_ArmyType.Cavalry:
                    return cavalryFlag;
                default:
                    return null;
            }
        }

        [Serializable]
        public class Block
        {
            public E_ShikakuCellType blockType;
            public GameObject blockPrefab;
        }
    }
}
