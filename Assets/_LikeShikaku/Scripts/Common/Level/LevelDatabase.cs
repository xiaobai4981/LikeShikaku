using System.Collections.Generic;
using UnityEngine;

namespace Common.Level
{
    [CreateAssetMenu(fileName = "LevelDatabase", menuName = "Shikaku/关卡列表")]
    public class LevelDatabase : ScriptableObject
    {
        [SerializeField]
        private List<LevelConfig> m_levels = new();
        public IReadOnlyList<LevelConfig> Levels => m_levels;

        public void SetLevels(List<LevelConfig> levels)
        {
            m_levels = levels;
        }

        public bool AddLevel(LevelConfig level)
        {
            if (level == null || m_levels.Contains(level))
            {
                return false;
            }

            m_levels.Add(level);
            return true;
        }
    }
}
