using System;
using System.Collections.Generic;
using System.Threading;
using Common.Level;
using Core;
using Cysharp.Threading.Tasks;
using MainScene.BattlePart;
using MainScene.BattlePart.Configs;
using MainScene.BattlePart.Manager;
using MainScene.BattlePart.Shikaku;
using MainScene.BattlePart.UI;
using UnityEngine;

namespace MainScene
{
    public class MainSceneGameMode : GameModeBase
    {
        private BattleManager m_battleManager;
        public BattleManager BattleManager => m_battleManager;

        [SerializeField]
        private BattleSettings m_battleSettings = null;
        public BattleSettings BattleSettings => m_battleSettings;

        [SerializeField]
        private ShikakuController m_shikakuController = null;
        public ShikakuController ShikakuController => m_shikakuController;

        [SerializeField]
        private LevelConfig m_testLevelConfig = null;
        [SerializeField]
        private LevelConfig[] m_levels = null;
        [SerializeField]
        private bool m_autoResume = true;
        private List<ShikakuLevelData> m_campaign;
        private CancellationTokenSource m_lifetime;
        private BattleHUD m_hud;
        public IReadOnlyList<ShikakuLevelData> Campaign => m_campaign;
        public int CurrentLevelIndex { get; private set; }
        public bool IsPlaytesting { get; private set; }

        public bool IsValidationRun
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.SessionState.GetBool("Shikaku.ValidationRunning", false);
#else
                return false;
#endif
            }
        }

        private bool m_isStarting;
        public override async UniTask Init()
        {
            m_lifetime = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            await UniTask.Yield(m_lifetime.Token);
            m_campaign = new List<ShikakuLevelData>();
            LevelConfig playtest = null;
#if UNITY_EDITOR
            string playtestPath = UnityEditor.SessionState.GetString("Shikaku.PlaytestLevel", "");
            if (!string.IsNullOrEmpty(playtestPath))
            {
                playtest = UnityEditor.AssetDatabase.LoadAssetAtPath<LevelConfig>(playtestPath);
            }

#endif
            IsPlaytesting = playtest != null;
            if (playtest != null)
            {
                m_campaign.Add(playtest.ToData());
            }
            else if (m_levels != null && m_levels.Length > 0)
            {
                foreach (var level in m_levels)
                {
                    if (!level)
                    {
                        throw new ArgumentException("关卡列表包含空资源。");
                    }

                    m_campaign.Add(level.ToData());
                }
            }
            else if (m_testLevelConfig && m_testLevelConfig.HasOrders)
            {
                m_campaign.Add(m_testLevelConfig.ToData());
            }
            else
            {
                var database = Resources.Load<LevelDatabase>("Shikaku/LevelDatabase");
                if (database != null && database.Levels.Count > 0)
                {
                    foreach (var level in database.Levels)
                    {
                        if (level == null)
                        {
                            throw new ArgumentException("关卡列表包含空资源。");
                        }

                        m_campaign.Add(level.ToData());
                    }
                }
                else
                {
                    m_campaign = ShikakuCampaign.CreateDefault();
                }
            }

            var ids = new HashSet<string>();
            foreach (var level in m_campaign)
            {
                if (!ids.Add(level.id))
                {
                    throw new ArgumentException("关卡 ID 重复：" + level.id);
                }
            }

            m_battleManager = new BattleManager();
            m_battleManager.Init(this);
            if (m_battleSettings && m_battleSettings.ShowFallbackHUD)
            {
                m_hud = gameObject.AddComponent<BattleHUD>();
                m_hud.Bind(this);
            }

            CurrentLevelIndex = 0;
            bool hasResume = false;
            if (m_autoResume && m_battleManager.Progress.Data.resume != null)
            {
                int index = m_campaign.FindIndex(x => x.id == m_battleManager.Progress.Data.resume.levelId);
                if (index >= 0 && IsUnlocked(index))
                {
                    CurrentLevelIndex = index;
                    hasResume = true;
                }
            }

            if (m_autoResume && !hasResume)
            {
                for (int i = 0; i < m_campaign.Count; i++)
                {
                    if (!IsUnlocked(i))
                    {
                        break;
                    }

                    CurrentLevelIndex = i;
                    if (m_battleManager.Progress.Stars(m_campaign[i].id) == 0)
                    {
                        break;
                    }
                }
            }

            await StartLevelAsync(CurrentLevelIndex, m_autoResume && !IsPlaytesting);
            if (!IsPlaytesting)
            {
                m_hud?.ShowHome();
            }
        }

        public void NormalGameStart()
        {
            StartLevel(CurrentLevelIndex, false);
        }

        public void StartLevel(int index, bool resume = false)
        {
            StartLevelAsync(index, resume).Forget(exception => Debug.LogException(exception, this));
        }

        private async UniTask StartLevelAsync(int index, bool resume)
        {
            if (m_isStarting || m_battleManager == null || index < 0 || index >= m_campaign.Count || !IsUnlocked(index))
            {
                return;
            }

            m_isStarting = true;
            try
            {
                CurrentLevelIndex = index;
                await m_battleManager.NormalGameStart(m_campaign[index], m_shikakuController, m_lifetime.Token, resume);
                m_hud?.Refresh();
            }
            finally
            {
                m_isStarting = false;
            }
        }

        public bool IsUnlocked(int index)
        {
            return m_campaign != null && index >= 0 && index < m_campaign.Count && m_battleManager.Progress.IsUnlocked(m_campaign, index);
        }

        public void RestartLevel()
        {
            StartLevel(CurrentLevelIndex, false);
        }

        public void NextLevel()
        {
            if (m_battleManager.ShikakuModel?.Status == E_ShikakuPlayStatus.Won)
            {
                StartLevel(CurrentLevelIndex + 1);
            }
        }

        public void TogglePause()
        {
            if (m_battleManager.ShikakuModel?.Status == E_ShikakuPlayStatus.Paused)
            {
                m_battleManager.EnterGameplay();
            }
            else
            {
                m_battleManager.OpenMenu();
            }
        }

        public override void Tick()
        {
            m_battleManager?.Tick();
        }

        public override void Cleanup()
        {
            if (m_hud)
            {
                m_hud.Unbind();
                Destroy(m_hud);
                m_hud = null;
            }

            m_battleManager?.Clear();
            if (m_lifetime != null)
            {
                m_lifetime.Cancel();
                m_lifetime.Dispose();
                m_lifetime = null;
            }

            MainGameInstance.MessagingSystem?.UnsubscribeAll(this);
        }

        private void Start()
        {
            if (MainGameInstance.Instance == null)
            {
                UnityEngine.SceneManagement.SceneManager.LoadScene("BootScene");
            }
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                m_battleManager?.OpenMenu();
            }
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                m_battleManager?.OpenMenu();
            }
        }

        private void OnApplicationQuit()
        {
            m_battleManager?.Save();
        }
    }
}
