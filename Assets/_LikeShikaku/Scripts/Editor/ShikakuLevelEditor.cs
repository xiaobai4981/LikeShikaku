using System;
using System.Collections.Generic;
using System.Linq;
using Common.Level;
using MainScene.BattlePart;
using MainScene.BattlePart.Shikaku;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LikeShikaku.Editor
{
    public class ShikakuLevelEditor : EditorWindow
    {
        private enum Brush
        {
            选择,
            将令,
            擦除
        }

        private LevelConfig m_level;
        private Brush m_brush;
        private Vector2 m_scroll;
        private Vector2 m_boardScroll;
        private GridCell m_selected;
        private GridCell? m_lastPainted;
        private int m_number = 4;
        private E_ArmyType m_army = E_ArmyType.Infantry;
        private int m_rows = 5;
        private int m_cols = 5;
        private string m_validation;
        private MessageType m_validationType;
        private List<ShikakuRect> m_solution;
        private bool m_showSolution;
        private bool m_showLessons;
        [MenuItem("Tools/军师八阵图/关卡编辑器 %#l")]
        public static void Open()
        {
            var window = GetWindow<ShikakuLevelEditor>("八阵图 · 关卡编辑器");
            window.minSize = new Vector2(780, 540);
            if (Selection.activeObject is LevelConfig config)
            {
                window.SetLevel(config);
            }
        }

        private void OnEnable()
        {
            Undo.undoRedoPerformed += OnUndo;
        }

        private void OnDisable()
        {
            Undo.undoRedoPerformed -= OnUndo;
        }

        private void OnUndo()
        {
            m_solution = null;
            Repaint();
        }

        public void SetLevel(LevelConfig config)
        {
            m_level = config;
            if (config == null)
            {
                return;
            }

            m_rows = config.determinant.x;
            m_cols = config.determinant.y;
            m_solution = null;
            m_validation = "";
            Repaint();
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var selected = (LevelConfig)EditorGUILayout.ObjectField(m_level, typeof(LevelConfig), false, GUILayout.MinWidth(180));
            if (selected != m_level)
            {
                SetLevel(selected);
            }

            if (GUILayout.Button("新建", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                CreateLevel();
            }

            if (GUILayout.Button("保存", EditorStyles.toolbarButton, GUILayout.Width(48)))
            {
                AssetDatabase.SaveAssets();
            }

            if (GUILayout.Button("关卡列表", EditorStyles.toolbarButton, GUILayout.Width(72)))
            {
                Selection.activeObject = AssetDatabase.LoadAssetAtPath<LevelDatabase>(ShikakuProjectSetup.DatabasePath);
            }

            EditorGUILayout.EndHorizontal();
            if (m_level == null)
            {
                EditorGUILayout.HelpBox("选择或双击一个 LevelConfig，或新建关卡。左侧配置笔刷，右侧点击/拖动绘制。支持 Ctrl+Z。", MessageType.Info);
                DrawDatabaseList();
                return;
            }

            EditorGUILayout.BeginHorizontal();
            m_scroll = EditorGUILayout.BeginScrollView(m_scroll, GUILayout.Width(280));
            DrawSettings();
            EditorGUILayout.EndScrollView();
            m_boardScroll = EditorGUILayout.BeginScrollView(m_boardScroll);
            DrawBoard();
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawSettings()
        {
            EditorGUILayout.LabelField("关卡信息", EditorStyles.boldLabel);
            var serialized = new SerializedObject(m_level);
            serialized.Update();
            EditorGUILayout.PropertyField(serialized.FindProperty("levelId"), new GUIContent("唯一 ID"));
            EditorGUILayout.PropertyField(serialized.FindProperty("title"), new GUIContent("关卡名称"));
            EditorGUILayout.PropertyField(serialized.FindProperty("briefing"), new GUIContent("开局说明"));
            EditorGUILayout.PropertyField(serialized.FindProperty("formation"), new GUIContent("阵法主题"));
            EditorGUILayout.PropertyField(serialized.FindProperty("requireArcherSupport"), new GUIContent("弓阵护卫"));
            EditorGUILayout.HelpBox("弓阵护卫：每支弓阵与任意步阵或枪阵共边，角接不算。一支护卫可保护多支弓阵。主题只描述布局意图，不限制固定形状。", MessageType.None);
            EditorGUILayout.PropertyField(serialized.FindProperty("maximumErrors"), new GUIContent("容错次数（0 不限）"));
            if (serialized.ApplyModifiedProperties())
            {
                Invalidate();
            }

            m_rows = EditorGUILayout.IntSlider("行数", m_rows, 1, 20);
            m_cols = EditorGUILayout.IntSlider("列数", m_cols, 1, 20);
            if (GUILayout.Button("应用尺寸（保留范围内数据）"))
            {
                ResizeLevel();
            }

            EditorGUILayout.Space();
            m_brush = (Brush)EditorGUILayout.EnumPopup("绘制工具", m_brush);
            EditorGUILayout.LabelField($"选中：第 {m_selected.row + 1} 行，第 {m_selected.col + 1} 列");
            m_number = Mathf.Max(1, EditorGUILayout.IntField("将令格数", m_number));
            m_army = (E_ArmyType)EditorGUILayout.EnumPopup("将令兵种", m_army);
            if (m_army == E_ArmyType.Any)
            {
                m_army = E_ArmyType.Infantry;
            }

            if (GUILayout.Button("将令写入选中格"))
            {
                Paint(m_selected, Brush.将令);
            }

            if (GUILayout.Button("清除选中格"))
            {
                Paint(m_selected, Brush.擦除);
            }

            EditorGUILayout.Space();
            if (GUILayout.Button("检查配置与解的数量", GUILayout.Height(30)))
            {
                ValidateLevel();
            }

            if (!string.IsNullOrEmpty(m_validation))
            {
                EditorGUILayout.HelpBox(m_validation, m_validationType);
            }

            m_showSolution = EditorGUILayout.Toggle("显示参考划分", m_showSolution);
            if (GUILayout.Button("从 BootScene 试玩本关", GUILayout.Height(32)))
            {
                Playtest();
            }

            if (GUILayout.Button("加入正式关卡列表"))
            {
                AddToDatabase();
            }

            m_showLessons = EditorGUILayout.Foldout(m_showLessons, "教学步骤（按动作推进）", true);
            if (m_showLessons)
            {
                serialized.Update();
                EditorGUILayout.PropertyField(serialized.FindProperty("lessons"), new GUIContent("步骤"), true);
                if (serialized.ApplyModifiedProperties())
                {
                    Invalidate();
                }
            }

            EditorGUILayout.HelpBox("坐标从左上角 (0,0) 开始。选择笔刷会读取该格的将令。保存只更新资源，不改运行时存档。", MessageType.None);
        }

        private void DrawBoard()
        {
            int rows = Mathf.Clamp(m_level.determinant.x, 1, 20);
            int cols = Mathf.Clamp(m_level.determinant.y, 1, 20);
            float size = Mathf.Clamp((position.width - 330) / cols, 30, 72);
            var area = GUILayoutUtility.GetRect(cols * size + 2, rows * size + 2, GUILayout.ExpandWidth(false));
            var data = m_level.cellDatas ?? Array.Empty<CellData>();
            var style = new GUIStyle(EditorStyles.boldLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = size >= 50 ? 18 : 12
            };
            style.normal.textColor = Color.black;
            for (int r = 0; r < rows; r++)
            {
                for (int c = 0; c < cols; c++)
                {
                    var cell = new GridCell(r, c);
                    var rect = new Rect(area.x + c * size, area.y + r * size, size - 2, size - 2);
                    var entry = Array.Find(data, x => x.position.x == r && x.position.y == c);
                    Color color = new Color(.86f, .79f, .64f);
                    if (m_showSolution && m_solution != null)
                    {
                        int index = m_solution.FindIndex(x => x.Contains(cell));
                        if (index >= 0)
                        {
                            color = Color.HSVToRGB((index * .19f) % 1, .24f, .86f);
                        }
                    }

                    if (cell.Equals(m_selected))
                    {
                        color = Color.Lerp(color, new Color(.4f, .72f, .9f), .55f);
                    }

                    EditorGUI.DrawRect(rect, color);
                    string label = entry.number > 0 ? ShikakuNames.Unit(entry.armyType) + " " + entry.number : "";
                    GUI.Label(rect, label, style);
                }
            }

            var current = Event.current;
            if (current.type == EventType.MouseDown)
            {
                m_lastPainted = null;
            }
            if (current.type == EventType.MouseUp)
            {
                m_lastPainted = null;
            }

            if ((current.type != EventType.MouseDown && current.type != EventType.MouseDrag) || current.button != 0 || !area.Contains(current.mousePosition))
            {
                return;
            }

            var target = new GridCell((int)((current.mousePosition.y - area.y) / size), (int)((current.mousePosition.x - area.x) / size));
            if (target.row >= rows || target.col >= cols)
            {
                return;
            }

            m_selected = target;
            if (!m_lastPainted.HasValue || !m_lastPainted.Value.Equals(target))
            {
                Paint(target, m_brush);
                m_lastPainted = target;
            }

            current.Use();
            Repaint();
        }

        private void Paint(GridCell cell, Brush brush)
        {
            if (cell.row < 0 || cell.col < 0 || cell.row >= m_level.determinant.x || cell.col >= m_level.determinant.y)
            {
                return;
            }

            var entries = new List<CellData>(m_level.cellDatas ?? Array.Empty<CellData>());
            var old = entries.Find(x => x.position.x == cell.row && x.position.y == cell.col);
            if (brush == Brush.选择)
            {
                if (old.number > 0)
                {
                    m_number = old.number;
                    m_army = old.armyType;
                }

                return;
            }

            Undo.RecordObject(m_level, "绘制八阵图关卡");
            entries.RemoveAll(x => x.position.x == cell.row && x.position.y == cell.col);
            if (brush == Brush.将令)
            {
                entries.Add(new CellData { position = new Vector2Int(cell.row, cell.col), number = m_number, armyType = m_army });
            }
            m_level.cellDatas = entries.ToArray();
            Invalidate();
        }

        private void ResizeLevel()
        {
            Undo.RecordObject(m_level, "调整关卡尺寸");
            m_level.determinant = new Vector2Int(m_rows, m_cols);
            bool InRange(GridCell c) => c.row >= 0 && c.col >= 0 && c.row < m_rows && c.col < m_cols;
            m_level.cellDatas = (m_level.cellDatas ?? Array.Empty<CellData>()).Where(x => InRange(new GridCell(x.position.x, x.position.y))).ToArray();
            m_level.lessons?.RemoveAll(x => x.hasTarget && !InRange(x.target));
            m_selected = new GridCell(0, 0);
            Invalidate();
        }

        private void Invalidate()
        {
            m_solution = null;
            m_validation = "修改后请重新检查。";
            m_validationType = MessageType.Info;
            EditorUtility.SetDirty(m_level);
            Repaint();
        }

        private void ValidateLevel()
        {
            try
            {
                var result = ShikakuSolver.Solve(m_level.ToData(), null, 2, 200000);
                m_solution = result.solutions.Count > 0 ? result.solutions[0] : null;
                m_validation = result.solutions.Count >= 2 ? "存在多解，运行时接受所有合法解。" : result.budgetExceeded ? "达到搜索预算，尚不能确认解的数量。" : result.solutions.Count == 1 ? "配置有效，唯一解。" : "无解，请调整将令或限制。";
                m_validationType = m_solution == null ? MessageType.Warning : MessageType.Info;
            }
            catch (Exception exception)
            {
                m_validation = exception.Message;
                m_validationType = MessageType.Error;
                m_solution = null;
            }
        }

        private void CreateLevel()
        {
            string path = EditorUtility.SaveFilePanelInProject("新建关卡", "custom-level", "asset", "保存新的 LevelConfig");
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            var level = CreateInstance<LevelConfig>();
            level.SetData(ShikakuCampaign.CreateDefault()[4]);
            level.levelId = "custom-" + Guid.NewGuid().ToString("N").Substring(0, 12);
            level.title = "新军阵";
            AssetDatabase.CreateAsset(level, path);
            SetLevel(level);
            Selection.activeObject = level;
        }

        private void Playtest()
        {
            ValidateLevel();
            if (m_solution == null || EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                return;
            }

            AssetDatabase.SaveAssets();
            SessionState.SetString("Shikaku.PlaytestLevel", AssetDatabase.GetAssetPath(m_level));
            EditorSceneManager.OpenScene("Assets/BootScene.unity");
            EditorApplication.isPlaying = true;
        }

        private void AddToDatabase()
        {
            ValidateLevel();
            if (m_solution == null)
            {
                return;
            }

            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(ShikakuProjectSetup.DatabasePath);
            if (database == null)
            {
                return;
            }

            if (database.Levels.Any(x => x != null && x != m_level && x.levelId == m_level.levelId))
            {
                m_validation = "列表中已存在相同 ID，请修改唯一 ID。";
                m_validationType = MessageType.Error;
                return;
            }

            Undo.RecordObject(database, "加入关卡列表");
            database.AddLevel(m_level);
            EditorUtility.SetDirty(database);
            AssetDatabase.SaveAssets();
            m_validation = "已加入正式闯关列表，退出试玩后从首页进入即可游玩。";
        }

        private void DrawDatabaseList()
        {
            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(ShikakuProjectSetup.DatabasePath);
            if (database == null)
            {
                return;
            }

            foreach (var config in database.Levels)
            {
                if (config != null && GUILayout.Button(config.title))
                {
                    SetLevel(config);
                }
            }
        }
    }
}
