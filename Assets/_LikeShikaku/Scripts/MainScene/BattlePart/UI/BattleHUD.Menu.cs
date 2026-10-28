using MainScene.BattlePart.Shikaku;
using UnityEngine;
using UnityEngine.UI;

namespace MainScene.BattlePart.UI
{
    public sealed partial class BattleHUD
    {
        private GameObject m_homeOverlay;
        private GameObject m_pauseOverlay;
        private GameObject m_resultOverlay;
        private GameObject m_helpOverlay;
        private bool m_showingHome;
        public void ShowHome()
        {
            if (m_mode == null)
            {
                return;
            }

            m_showingHome = true;
            ClosePicker(false);
            ClearOverlay(ref m_pauseOverlay);
            ClearOverlay(ref m_resultOverlay);
            ClearOverlay(ref m_homeOverlay);
            m_mode.BattleManager.OpenMenu();
            var content = CreateMenu("Home", "八阵图", "一阵一令，诸军相援。", m_mode.BattleSettings.advisorPortrait, out m_homeOverlay);
            var model = m_mode.BattleManager.ShikakuModel;
            string action = model != null && (model.CoveredArea > 0 || model.Errors > 0) ? "继续布阵" : "开始布阵";
            MenuButton(content, action, StartPlaying);
            MenuButton(content, "选择关卡", () =>
            {
                HideHome();
                OpenPicker();
            });
            MenuButton(content, "布阵须知", ShowHelp);
            Label(content, "由校场操练入门，研习中军、两翼、纵深、环护、交错、雁行、合围、连营八种布局。\n一阵一令，面积相符；协同关卡还需让弓阵得到步阵或枪阵护卫。", 23);
            if (m_mode.IsPlaytesting)
            {
                Label(content, "编辑器试玩 · 不写入正式进度", 21);
            }
        }

        public void StartPlaying()
        {
            HideHome();
            var model = m_mode.BattleManager.ShikakuModel;
            if (model == null)
            {
                return;
            }

            if (model.Status == E_ShikakuPlayStatus.Lost)
            {
                m_mode.RestartLevel();
            }
            else if (model.Status == E_ShikakuPlayStatus.Won && m_mode.CurrentLevelIndex + 1 < m_mode.Campaign.Count)
            {
                m_mode.NextLevel();
            }
            else if (model.Status == E_ShikakuPlayStatus.Won)
            {
                m_mode.RestartLevel();
            }
            else
            {
                m_mode.BattleManager.EnterGameplay();
            }

            Refresh();
        }

        private void HideHome()
        {
            m_showingHome = false;
            ClearOverlay(ref m_homeOverlay);
            ClearOverlay(ref m_helpOverlay);
        }

        private void RefreshMenus()
        {
            if (m_showingHome || m_mode == null)
            {
                return;
            }

            var model = m_mode.BattleManager.ShikakuModel;
            if (model == null)
            {
                return;
            }

            bool finished = model.Status == E_ShikakuPlayStatus.Won || model.Status == E_ShikakuPlayStatus.Lost;
            if (finished && m_resultOverlay == null)
            {
                ClearOverlay(ref m_pauseOverlay);
                bool won = model.Status == E_ShikakuPlayStatus.Won;
                string heading = won ? (model.Level.formation == E_FormationTheme.操练 ? "操练完成" : model.Level.formation + "阵 · 阵成") : "军令用尽";
                string detail = won ? $"获得 {model.Stars} 星 · 失误 {model.Errors} 次 · 提示 {model.HintsUsed} 次" : "整顿旗鼓，再试一次。";
                var content = CreateMenu("Result", heading, detail, won ? m_mode.BattleSettings.advisorVictory : m_mode.BattleSettings.advisorPortrait, out m_resultOverlay);
                if (won)
                {
                    Label(content, ShikakuNames.FormationIdea(model.Level.formation) + "\n" + ShikakuRules.FormationProgress(model.Level, model.Regions), 23);
                }
                if (won && m_mode.CurrentLevelIndex + 1 < m_mode.Campaign.Count)
                {
                    MenuButton(content, "下一关", () =>
                    {
                        ClearOverlay(ref m_resultOverlay);
                        m_mode.NextLevel();
                    });
                }

                MenuButton(content, "重新布阵", () =>
                {
                    ClearOverlay(ref m_resultOverlay);
                    m_mode.RestartLevel();
                });
                MenuButton(content, "返回主界面", ShowHome);
            }
            else if (!finished)
            {
                ClearOverlay(ref m_resultOverlay);
            }

            if (!finished && !m_picker && model.Status == E_ShikakuPlayStatus.Paused && m_pauseOverlay == null)
            {
                var content = CreateMenu("Pause", "暂歇片刻", model.Level.title, null, out m_pauseOverlay);
                MenuButton(content, "继续布阵", () => m_mode.BattleManager.EnterGameplay());
                MenuButton(content, "重新布阵", () =>
                {
                    ClearOverlay(ref m_pauseOverlay);
                    m_mode.RestartLevel();
                });
                MenuButton(content, "返回主界面", ShowHome);
            }
            else if (model.Status != E_ShikakuPlayStatus.Paused || m_picker)
            {
                ClearOverlay(ref m_pauseOverlay);
            }
        }

        private void ShowHelp()
        {
            ClearOverlay(ref m_helpOverlay);
            var content = CreateMenu("Help", "布阵须知", "按住起点，拖到对角后松手部署。", null, out m_helpOverlay);
            Label(content, "每块矩形恰好包含一道将令，面积等于将令数字，最终覆盖全盘。\n\n弓阵护卫关卡中，每支弓阵必须与至少一支步阵或枪阵共边，角接不算。一支护卫可接应多支弓阵；骑兵不算护卫。青色连线表示接应成立。\n\n可以先布弓阵再补护卫；全盘覆盖后仍缺护卫时可撤回调整，不扣错。\n\n八种阵名表示布局思路，不要求照抄固定阵图，也不限制某兵种只能横排或竖排。\n\n点击已部署军阵可以撤回。撤回不扣错，撤销不返还已用容错。提示分为推理方向、关键将令、可行矩形三级。", 24);
            MenuButton(content, "返回", () => ClearOverlay(ref m_helpOverlay));
        }

        private Transform CreateMenu(string name, string heading, string detail, Sprite portrait, out GameObject overlay)
        {
            var shade = Image(name, m_safe, new Color32(41, 44, 35, 255));
            Full(shade.rectTransform);
            overlay = shade.gameObject;
            if (m_mode.BattleSettings.backgroundSprite != null)
            {
                var background = Image("CampBackground", shade.transform, new Color(1, 1, 1, .3f));
                background.sprite = m_mode.BattleSettings.backgroundSprite;
                Full(background.rectTransform);
                background.raycastTarget = false;
            }

            var panel = Image("Parchment", shade.transform, new Color32(241, 226, 194, 255));
            panel.rectTransform.anchorMin = panel.rectTransform.anchorMax = new Vector2(.5f, .5f);
            panel.rectTransform.pivot = new Vector2(.5f, .5f);
            panel.rectTransform.sizeDelta = new Vector2(m_safe.rect.width * .76f, Mathf.Min(m_safe.rect.height * .88f, 620));
            var viewport = Rect("Viewport", panel.transform);
            Full(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Content", viewport);
            content.anchorMin = new Vector2(0, 1);
            content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f, 1);
            content.sizeDelta = Vector2.zero;
            content.anchoredPosition = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            layout.padding = new RectOffset(22, 22, 22, 22);
            layout.spacing = 16;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            var caption = Label(content, heading, 38);
            caption.alignment = TextAnchor.MiddleCenter;
            if (portrait != null)
            {
                var image = Image("Advisor", content, Color.white);
                image.sprite = portrait;
                image.preserveAspect = true;
                Height(image.gameObject, 140);
            }

            var subtitle = Label(content, detail, 24);
            subtitle.alignment = TextAnchor.MiddleCenter;
            return content;
        }

        private void MenuButton(Transform parent, string text, UnityEngine.Events.UnityAction action)
        {
            var button = Button(parent, text, action);
            Height(button.gameObject, 58);
        }

        private void UpdateMenuLayout()
        {
            foreach (var overlay in new[]
            {
                m_homeOverlay,
                m_pauseOverlay,
                m_resultOverlay,
                m_helpOverlay
            }

            )
            {
                if (overlay == null)
                {
                    continue;
                }

                var panel = overlay.transform.Find("Parchment") as RectTransform;
                if (panel == null)
                {
                    continue;
                }

                var size = new Vector2(m_safe.rect.width * .76f, Mathf.Min(m_safe.rect.height * .88f, 620));
                if (panel.sizeDelta != size)
                {
                    panel.sizeDelta = size;
                }
            }
        }

        private void ClearOverlay(ref GameObject overlay)
        {
            if (overlay == null)
            {
                return;
            }

            overlay.SetActive(false);
            Destroy(overlay);
            overlay = null;
        }
    }
}
