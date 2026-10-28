using System;
using System.Collections.Generic;
using UnityEngine;

namespace MainScene.BattlePart.Shikaku
{
    [Serializable]
    public sealed class ShikakuLevelRecord
    {
        public string levelId;
        public int bestStars;
    }

    [Serializable]
    public sealed class ShikakuProgressData
    {
        public int version = 1;
        public List<ShikakuLevelRecord> records = new List<ShikakuLevelRecord>();
        public ShikakuResume resume;
    }

    public sealed class ShikakuProgressStore
    {
        // 关卡进度使用独立存档键，与玩家基础数据分开。
        private const string Key = "Shikaku.KingdomFormation.Save.v1";
        public ShikakuProgressData Data { get; private set; } = new ShikakuProgressData();
        public string LastWarning { get; private set; }

        private readonly bool m_isTemporary;
        public ShikakuProgressStore(bool isTemporary = false)
        {
            m_isTemporary = isTemporary;
            if (m_isTemporary)
            {
                return;
            }

            try
            {
                var json = PlayerPrefs.GetString(Key, "");
                if (string.IsNullOrEmpty(json))
                {
                    json = PlayerPrefs.GetString("KingdomFormation.Standalone.Save.v1", "");
                }

                if (!string.IsNullOrEmpty(json))
                {
                    var loaded = JsonUtility.FromJson<ShikakuProgressData>(json);
                    if (loaded != null && loaded.version == 1 && loaded.records != null)
                    {
                        loaded.records.RemoveAll(x => x == null || string.IsNullOrEmpty(x.levelId) || x.bestStars < 1 || x.bestStars > 3);
                        Data = loaded;
                    }
                    else
                    {
                        LastWarning = "存档格式不兼容，已从默认进度开始。";
                    }
                }
            }
            catch (Exception e)
            {
                LastWarning = "读取存档失败，已从默认进度开始：" + e.Message;
            }
        }

        public int Stars(string id)
        {
            var record = Data.records.Find(x => x.levelId == id);
            return record == null ? 0 : record.bestStars;
        }

        public bool IsUnlocked(IReadOnlyList<ShikakuLevelData> levels, int index)
        {
            return index == 0 || Stars(levels[index - 1].id) > 0;
        }

        public void Save(ShikakuModel session)
        {
            if (session != null)
            {
                Data.resume = session.Capture();
                if (session.Status == E_ShikakuPlayStatus.Won)
                {
                    var record = Data.records.Find(x => x.levelId == session.Level.id);
                    if (record == null)
                    {
                        Data.records.Add(new ShikakuLevelRecord { levelId = session.Level.id, bestStars = session.Stars });
                    }
                    else
                    {
                        record.bestStars = Math.Max(record.bestStars, session.Stars);
                    }
                }
            }

            if (m_isTemporary)
            {
                return;
            }

            try
            {
                PlayerPrefs.SetString(Key, JsonUtility.ToJson(Data));
                PlayerPrefs.Save();
                LastWarning = null;
            }
            catch (Exception e)
            {
                LastWarning = "保存失败：" + e.Message;
                Debug.LogWarning(LastWarning);
            }
        }
    }
}
