using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;
using MainScene.BattlePart.Shikaku;

namespace MainScene.BattlePart.UI
{
    /// <summary>战斗操作界面，通过 BattleManager 操作对局。</summary>
    public sealed partial class BattleHUD : MonoBehaviour
    {
        private MainSceneGameMode m_mode;
        private GameObject m_canvasObject;
        private GameObject m_picker;
        private GameObject m_eventObject;
        private RectTransform m_safe;
        private RectTransform m_battleViewport;
        private RectTransform m_extraActions;
        private Text m_title;
        private Text m_status;
        private Text m_formation;
        private readonly Text[] m_armyLegend = new Text[4];
        private Text m_lesson;
        private Text m_message;
        private Button m_undo;
        private Button m_hint;
        private Button m_pause;
        private Button m_next;
        private Button m_skip;
        private Font m_font;
        private Rect m_lastSafe;
        private bool m_pickerShouldResume;
        private bool m_wasSelecting;
        private readonly Color m_ink = new Color32(48, 41, 30, 255);
        public void Bind(MainSceneGameMode gameMode)
        {
            m_mode = gameMode;
            m_font = LoadFont();
            m_canvasObject = new GameObject("BattleHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            m_canvasObject.transform.SetParent(transform, false);
            var canvas = m_canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = m_canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(900, 1200);
            scaler.matchWidthOrHeight = .5f;
            m_safe = Rect("SafeArea", m_canvasObject.transform);
            Full(m_safe);
            var panel = Image("BattlePanel", m_safe, new Color32(240, 226, 194, 250));
            Anchor(panel.rectTransform, Vector2.zero, new Vector2(1, .34f));
            panel.rectTransform.offsetMin = new Vector2(10, 6);
            panel.rectTransform.offsetMax = new Vector2(-10, -4);
            var viewport = Rect("Viewport", panel.transform);
            Full(viewport);
            m_battleViewport = viewport;
            viewport.offsetMin = new Vector2(0, 120);
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
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 6;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = panel.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            m_title = Label(content, "八阵图", 27);
            m_status = Label(content, "", 23);
            m_formation = Label(content, "", 22);
            var legendRow = Rect("ArmyLegend", content);
            Height(legendRow.gameObject, 28);
            var legendLayout = legendRow.gameObject.AddComponent<HorizontalLayoutGroup>();
            legendLayout.childControlWidth = legendLayout.childControlHeight = true;
            legendLayout.childForceExpandWidth = true;
            for (int i = 0; i < m_armyLegend.Length; i++)
            {
                m_armyLegend[i] = Label(legendRow, ShikakuNames.Unit((E_ArmyType)(i + 1)) + "兵", 21);
            }
            m_lesson = Label(content, "", 23);
            m_message = Label(content, "", 23);
            var row = Rect("Actions", panel.transform);
            Anchor(row, Vector2.zero, new Vector2(1, 0));
            row.offsetMin = new Vector2(12, 12);
            row.offsetMax = new Vector2(-12, 62);
            Height(row.gameObject, 50);
            var horizontal = row.gameObject.AddComponent<HorizontalLayoutGroup>();
            horizontal.childControlWidth = true;
            horizontal.childControlHeight = true;
            horizontal.childForceExpandWidth = true;
            horizontal.childForceExpandHeight = true;
            horizontal.spacing = 6;
            m_undo = Button(row, "撤销", () => m_mode.BattleManager.Undo());
            m_hint = Button(row, "提示", () => m_mode.BattleManager.Hint());
            Button(row, "重试", () =>
            {
                ClosePicker(false);
                m_mode.RestartLevel();
            });
            m_pause = Button(row, "暂停", () => m_mode.TogglePause());
            Button(row, "选关", OpenPicker);
            var extras = Rect("TeachingActions", panel.transform);
            m_extraActions = extras;
            Anchor(extras, Vector2.zero, new Vector2(1, 0));
            extras.offsetMin = new Vector2(12, 70);
            extras.offsetMax = new Vector2(-12, 116);
            Height(extras.gameObject, 46);
            var extraLayout = extras.gameObject.AddComponent<HorizontalLayoutGroup>();
            extraLayout.childControlWidth = true;
            extraLayout.childControlHeight = true;
            extraLayout.childForceExpandWidth = true;
            extraLayout.childForceExpandHeight = true;
            extraLayout.spacing = 8;
            m_skip = Button(extras, "跳过教学", () => m_mode.BattleManager.SkipTutorial());
            m_next = Button(extras, "下一关", () => m_mode.NextLevel());
            Label(content, "一阵一令 · 数字为格数 · 点击军阵可撤回\nI 步兵 / S 枪兵 / A 弓兵 / C 骑兵（无中文棋盘字体时）", 19);
            if (!EventSystem.current)
            {
                m_eventObject = new GameObject("BattleEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
                m_eventObject.transform.SetParent(transform, false);
                m_eventObject.GetComponent<InputSystemUIInputModule>().AssignDefaultActions();
            }

            m_mode.BattleManager.Changed += OnChanged;
            RefreshSafeArea();
            Refresh();
        }

        private static Font LoadFont()
        {
            try
            {
                var names = Font.GetOSInstalledFontNames();
                foreach (var candidate in new[]
                {
                    "Microsoft YaHei",
                    "Noto Sans CJK SC",
                    "PingFang SC",
                    "SimHei",
                    "Arial Unicode MS"
                }

                )
                {
                    if (Array.IndexOf(names, candidate) >= 0)
                    {
                        return Font.CreateDynamicFontFromOSFont(candidate, 26);
                    }
                }
            }
            catch
            { /* Platforms without OS fonts require assigning a bundled Font below. */
            }

            return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        private void OnChanged(BattleChangedEvent value)
        {
            Refresh();
        }

        public void Refresh()
        {
            if (!m_mode || m_mode.BattleManager.ShikakuModel == null || !m_title)
            {
                return;
            }

            var model = m_mode.BattleManager.ShikakuModel;
            m_title.text = model.Level.title;
            for (int i = 0; i < m_armyLegend.Length; i++)
            {
                m_armyLegend[i].color = m_mode.BattleSettings.UnitColor((E_ArmyType)(i + 1));
            }
            m_formation.text = ShikakuNames.FormationIdea(model.Level.formation) + "\n" + ShikakuRules.FormationProgress(model.Level, model.Regions);
            m_status.text = $"部署 {model.CoveredArea}/{model.Level.AvailableArea} 格 · 将令 {model.Regions.Count}/{model.Level.clues.Count}" + (model.Level.maximumErrors > 0 ? $" · 容错 {Math.Max(0, model.Level.maximumErrors - model.Errors)}/{model.Level.maximumErrors}" : " · 不限错");
            m_lesson.gameObject.SetActive(model.InLesson);
            m_lesson.text = model.InLesson ? "孔明：" + model.CurrentLesson.text : "";
            m_message.text = model.Status == E_ShikakuPlayStatus.Won ? $"布阵成功！本关 {model.Stars} 星。" : model.Status == E_ShikakuPlayStatus.Lost ? "军令用尽，本局结束。可以重试或选关。" : model.Status == E_ShikakuPlayStatus.Paused ? "已暂停，点击继续布阵。" : model.LastMessage;
            if (!string.IsNullOrEmpty(m_mode.BattleManager.Progress.LastWarning))
            {
                m_message.text += "\n" + m_mode.BattleManager.Progress.LastWarning;
            }

            bool playable = model.Status == E_ShikakuPlayStatus.Playing && !m_picker;
            m_undo.interactable = model.CanUndo && !m_picker;
            m_hint.interactable = playable;
            m_pause.interactable = !m_picker && (playable || model.Status == E_ShikakuPlayStatus.Paused);
            m_pause.GetComponentInChildren<Text>().text = model.Status == E_ShikakuPlayStatus.Paused ? "继续" : "暂停";
            m_skip.gameObject.SetActive(model.InLesson && playable);
            m_next.gameObject.SetActive(model.Status == E_ShikakuPlayStatus.Won && m_mode.CurrentLevelIndex + 1 < m_mode.Campaign.Count);
            bool extraVisible = m_skip.gameObject.activeSelf || m_next.gameObject.activeSelf;
            m_extraActions.gameObject.SetActive(extraVisible);
            m_battleViewport.offsetMin = new Vector2(0, extraVisible ? 120 : 70);
            RefreshMenus();
        }

        private void Update()
        {
            if (!m_mode || !m_safe)
            {
                return;
            }

            if (m_lastSafe != Screen.safeArea)
            {
                RefreshSafeArea();
            }

            UpdateMenuLayout();
            var model = m_mode.BattleManager?.ShikakuModel;
            if (model == null)
            {
                return;
            }

            if (model.IsSelecting)
            {
                var rect = ShikakuRect.Between(new GridCell(model.StartCell.x, model.StartCell.y), new GridCell(model.EndCell.x, model.EndCell.y));
                string info = $"试划 {rect.Height} 行 × {rect.Width} 列，共 {rect.Area} 格。";

                m_message.text = info;
                m_wasSelecting = true;
            }
            else if (m_wasSelecting)
            {
                m_wasSelecting = false;
                Refresh();
            }
        }

        private void RefreshSafeArea()
        {
            m_lastSafe = Screen.safeArea;
            if (Screen.width <= 0 || Screen.height <= 0)
            {
                return;
            }

            m_safe.anchorMin = new Vector2(m_lastSafe.xMin / Screen.width, m_lastSafe.yMin / Screen.height);
            m_safe.anchorMax = new Vector2(m_lastSafe.xMax / Screen.width, m_lastSafe.yMax / Screen.height);
        }

        private void OpenPicker()
        {
            if (m_picker || m_mode.BattleManager.ShikakuModel == null)
            {
                return;
            }

            m_pickerShouldResume = m_mode.BattleManager.ShikakuModel.Status == E_ShikakuPlayStatus.Playing;
            m_mode.BattleManager.OpenMenu();
            var background = Image("LevelPicker", m_safe, new Color32(235, 220, 185, 255));
            Full(background.rectTransform);
            m_picker = background.gameObject;
            var close = Button(background.transform, "返回布阵", () => ClosePicker(true));
            Anchor((RectTransform)close.transform, new Vector2(0, 1), Vector2.one);
            ((RectTransform)close.transform).offsetMin = new Vector2(16, -60);
            ((RectTransform)close.transform).offsetMax = new Vector2(-16, -10);
            var viewportImage = Image("Viewport", background.transform, new Color32(240, 230, 205, 255));
            var viewport = viewportImage.rectTransform;
            Full(viewport);
            viewport.offsetMin = new Vector2(16, 16);
            viewport.offsetMax = new Vector2(-16, -70);
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = Rect("Levels", viewport);
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
            layout.spacing = 8;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            var scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = false;
            for (int i = 0; i < m_mode.Campaign.Count; i++)
            {
                int index = i;
                bool unlocked = m_mode.IsUnlocked(i);
                string label = m_mode.Campaign[i].title + (unlocked ? "  " + new string ('*', m_mode.BattleManager.Progress.Stars(m_mode.Campaign[i].id)) : "  [未解锁]");
                var button = Button(content, label, () =>
                {
                    ClosePicker(false);
                    m_mode.StartLevel(index);
                });
                button.interactable = unlocked;
                Height(button.gameObject, 58);
            }

            Refresh();
        }

        private void ClosePicker(bool resume)
        {
            if (m_picker)
            {
                m_picker.SetActive(false);
                Destroy(m_picker);
                m_picker = null;
            }

            if (resume && m_pickerShouldResume)
            {
                m_mode.BattleManager.EnterGameplay();
            }

            Refresh();
        }

        public void Unbind()
        {
            if (m_mode && m_mode.BattleManager != null)
            {
                m_mode.BattleManager.Changed -= OnChanged;
            }

            if (m_canvasObject)
            {
                m_canvasObject.SetActive(false);
                Destroy(m_canvasObject);
                m_canvasObject = null;
            }

            if (m_eventObject)
            {
                m_eventObject.SetActive(false);
                Destroy(m_eventObject);
                m_eventObject = null;
            }

            m_mode = null;
        }

        private void OnDestroy()
        {
            Unbind();
        }

        private RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name, typeof(RectTransform));
            obj.transform.SetParent(parent, false);
            return (RectTransform)obj.transform;
        }

        private Image Image(string name, Transform parent, Color color)
        {
            var rect = Rect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            image.color = color;
            return image;
        }

        private Text Label(Transform parent, string text, int size)
        {
            var rect = Rect("Text", parent);
            var label = rect.gameObject.AddComponent<Text>();
            label.font = m_mode && m_mode.BattleSettings && m_mode.BattleSettings.hudFont ? m_mode.BattleSettings.hudFont : m_font;
            label.text = text;
            label.color = m_ink;
            label.fontSize = size;
            label.supportRichText = false;
            label.raycastTarget = false;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            return label;
        }

        private Button Button(Transform parent, string text, UnityEngine.Events.UnityAction callback)
        {
            var image = Image(text, parent, new Color32(212, 194, 153, 255));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var label = Label(image.transform, text, 24);
            Full(label.rectTransform);
            label.alignment = TextAnchor.MiddleCenter;
            button.onClick.AddListener(callback);
            return button;
        }

        private static void Height(GameObject obj, float height)
        {
            var layout = obj.AddComponent<LayoutElement>();
            layout.minHeight = layout.preferredHeight = height;
        }

        private static void Full(RectTransform rect)
        {
            Anchor(rect, Vector2.zero, Vector2.one);
        }

        private static void Anchor(RectTransform rect, Vector2 min, Vector2 max)
        {
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }
    }
}
