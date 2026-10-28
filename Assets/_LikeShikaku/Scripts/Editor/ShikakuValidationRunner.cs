using System;
using System.IO;
using System.Linq;
using Common.Level;
using MainScene;
using MainScene.BattlePart.Shikaku;
using MainScene.BattlePart.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Reflection;
using MainScene.BattlePart;
using MainScene.BattlePart.Input;

namespace LikeShikaku.Editor
{
    // 编辑器内执行场景冒烟检查，不覆盖正式进度，不修改用户关卡。
    [InitializeOnLoad]
    public static class ShikakuValidationRunner
    {
        private const string RequestPath = "Temp/ShikakuValidation.request";
        private const string ResultPath = "Temp/ShikakuValidation.json";
        private const string ActiveKey = "Shikaku.ValidationRunning";
        private static bool m_leavingPlayMode;
        [Serializable]
        private class Result
        {
            public bool success;
            public int completedLevels;
            public string message;
            public string errors;
        }

        static ShikakuValidationRunner()
        {
            EditorApplication.update += Tick;
            EditorApplication.playModeStateChanged += OnPlayModeChanged;
            Application.logMessageReceived += OnLog;
        }

        [MenuItem("Tools/军师八阵图/运行场景冒烟检查")]
        public static void Request()
        {
            Directory.CreateDirectory("Temp");
            File.WriteAllText(RequestPath, "smoke");
        }

        private static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false))
            {
                if (!File.Exists(RequestPath) || EditorApplication.isCompiling || EditorApplication.isUpdating)
                {
                    return;
                }

                bool singleLevel = File.ReadAllText(RequestPath).Trim() == "playtest";
                File.Delete(RequestPath);
                if (EditorApplication.isPlayingOrWillChangePlaymode || EditorSceneManager.GetActiveScene().isDirty)
                {
                    Finish(false, "请先退出运行并保存当前场景，再进行冒烟检查。");
                    return;
                }

                try
                {
                    ShikakuProjectSetup.EnsureAssets();
                    CheckEditorTools();
                    var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(ShikakuProjectSetup.DatabasePath);
                    if (database == null || database.Levels.Count == 0)
                    {
                        throw new Exception("未生成可编辑题库。");
                    }

                    foreach (var config in database.Levels)
                    {
                        var solved = ShikakuSolver.Solve(config.ToData(), null, 1);
                        if (solved.solutions.Count == 0)
                        {
                            throw new Exception("关卡无解：" + config.name);
                        }
                    }

                    SessionState.SetString("Shikaku.ValidationOriginalScene", EditorSceneManager.GetActiveScene().path);
                    SessionState.SetBool("Shikaku.ValidationSingle", singleLevel);
                    SessionState.SetString("Shikaku.PlaytestLevel", singleLevel ? AssetDatabase.GetAssetPath(database.Levels[Math.Min(14, database.Levels.Count - 1)]) : "");
                    SessionState.SetString("Shikaku.ValidationErrors", "");
                    SessionState.SetInt("Shikaku.ValidationLevel", 0);
                    SessionState.SetInt("Shikaku.ValidationPhase", 0);
                    SessionState.SetBool("Shikaku.GameplayCaptured", false);
                    SessionState.SetFloat("Shikaku.ValidationStart", (float)EditorApplication.timeSinceStartup);
                    SessionState.SetBool(ActiveKey, true);
                    EditorSceneManager.OpenScene("Assets/BootScene.unity");
                    EditorApplication.isPlaying = true;
                }
                catch (Exception exception)
                {
                    Finish(false, exception.ToString());
                }

                return;
            }

            if (m_leavingPlayMode || !EditorApplication.isPlaying)
            {
                return;
            }

            try
            {
                if (EditorApplication.timeSinceStartup - SessionState.GetFloat("Shikaku.ValidationStart", 0) > 120)
                {
                    throw new Exception("场景检查超时。");
                }

                var mode = UnityEngine.Object.FindAnyObjectByType<MainSceneGameMode>();
                if (mode == null || mode.BattleManager == null || mode.BattleManager.ShikakuModel == null)
                {
                    return;
                }

                if (SessionState.GetBool("Shikaku.ValidationSingle", false) && (!mode.IsPlaytesting || mode.Campaign.Count != 1))
                {
                    throw new Exception("编辑器试玩未隔离为单关。");
                }

                if (SessionState.GetInt("Shikaku.ValidationPhase", 0) == 0)
                {
                    if (!SessionState.GetBool("Shikaku.ValidationSingle", false))
                    {
                        ScreenCapture.CaptureScreenshot("Temp/Shikaku-Home.png");
                    }

                    SessionState.SetFloat("Shikaku.CaptureTime", (float)EditorApplication.timeSinceStartup);
                    SessionState.SetInt("Shikaku.ValidationPhase", 5);
                    return;
                }

                if (SessionState.GetInt("Shikaku.ValidationPhase", 0) == 5)
                {
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat("Shikaku.CaptureTime", 0) < 1)
                    {
                        return;
                    }

                    var hud = mode.GetComponent<BattleHUD>();
                    if (hud == null)
                    {
                        throw new Exception("未创建操作界面。");
                    }

                    hud.StartPlaying();
                    if (mode.BattleManager.ShikakuModel.Status != E_ShikakuPlayStatus.Playing)
                    {
                        return;
                    }

                    SessionState.SetInt("Shikaku.ValidationPhase", 1);
                }

                int index = SessionState.GetInt("Shikaku.ValidationLevel", 0);
                if (index >= mode.Campaign.Count)
                {
                    string errors = SessionState.GetString("Shikaku.ValidationErrors", "");
                    if (!SessionState.GetBool("Shikaku.ValidationSingle", false))
                    {
                        ScreenCapture.CaptureScreenshot("Temp/Shikaku-Smoke.png");
                    }

                    Finish(string.IsNullOrEmpty(errors), "已从 BootScene 启动并完成 " + index + " 关，检查撤销、暂停、界面和结算。");
                    return;
                }

                if (mode.CurrentLevelIndex != index)
                {
                    mode.StartLevel(index);
                    return;
                }

                var model = mode.BattleManager.ShikakuModel;
                if (model.Level.id != mode.Campaign[index].id)
                {
                    return;
                }

                var solution = ShikakuSolver.Solve(model.Level, null, 1).solutions[0];
                var first = solution[0];
                if (index == 14 && !SessionState.GetBool("Shikaku.GameplayCaptured", false))
                {
                    model.Submit(first);
                    mode.BattleManager.Tick();
                    ScreenCapture.CaptureScreenshot("Temp/Shikaku-Gameplay.png");
                    SessionState.SetBool("Shikaku.GameplayCaptured", true);
                    SessionState.SetFloat("Shikaku.CaptureTime", (float)EditorApplication.timeSinceStartup);
                    return;
                }

                if (index == 14)
                {
                    if (EditorApplication.timeSinceStartup - SessionState.GetFloat("Shikaku.CaptureTime", 0) < 1)
                    {
                        return;
                    }

                    if (model.Regions.Count > 0)
                    {
                        model.Undo();
                    }
                }

                model.Submit(first);
                if (model.Status == E_ShikakuPlayStatus.Playing)
                {
                    if (!model.Undo() || model.Regions.Count != 0)
                    {
                        throw new Exception("撤销失败。");
                    }

                    mode.TogglePause();
                    if (model.Status != E_ShikakuPlayStatus.Paused)
                    {
                        throw new Exception("暂停失败。");
                    }

                    mode.TogglePause();
                }

                foreach (var rect in solution)
                {
                    if (model.Regions.Contains(rect))
                    {
                        continue;
                    }

                    var simulation = mode.BattleManager.ShikakuSimulation;
                    var controller = mode.ShikakuController;
                    var resolver = new ShikakuInputTransform(controller.GameCamera, controller.MapRoot, controller.CellLayer);
                    Vector3 CellWorld(int row, int col) => controller.MapRoot.TransformPoint(new Vector3((col - (model.ColCount - 1) * .5f) * mode.BattleSettings.BlockSpacing, ((model.RowCount - 1) * .5f - row) * mode.BattleSettings.BlockSpacing, 0));
                    var down = resolver.Resolve(new BattleInputFrame { PressDown = true, ScreenPosition = controller.GameCamera.WorldToScreenPoint(CellWorld(rect.top, rect.left)) });
                    var up = resolver.Resolve(new BattleInputFrame { PressUp = true, ScreenPosition = controller.GameCamera.WorldToScreenPoint(CellWorld(rect.bottom, rect.right)) });
                    if (!down.HasCell || !up.HasCell)
                    {
                        throw new Exception("相机射线未命中棋盘格。");
                    }

                    simulation.Tick(down, .016f);
                    simulation.Tick(up, .016f);
                }

                mode.BattleManager.Tick();
                if (model.Status != E_ShikakuPlayStatus.Won)
                {
                    throw new Exception("无法完成关卡 " + model.Level.id);
                }

                var restored = new ShikakuModel(model.Level);
                var resume = JsonUtility.FromJson<ShikakuResume>(JsonUtility.ToJson(model.Capture()));
                if (!restored.Restore(resume) || restored.Status != E_ShikakuPlayStatus.Won)
                {
                    throw new Exception("局面序列化失败。");
                }

                SessionState.SetInt("Shikaku.ValidationLevel", index + 1);
                if (index + 1 < mode.Campaign.Count)
                {
                    mode.NextLevel();
                }
            }
            catch (Exception exception)
            {
                Finish(false, exception.ToString());
            }
        }

        private static void CheckEditorTools()
        {
            var config = ScriptableObject.CreateInstance<LevelConfig>();
            config.SetData(ShikakuCampaign.CreateDefault()[4]);
            var window = ScriptableObject.CreateInstance<ShikakuLevelEditor>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            string path = "Assets/__ShikakuEditorRoundTrip.asset";
            try
            {
                window.SetLevel(config);
                typeof(ShikakuLevelEditor).GetField("m_number", flags).SetValue(window, 6);
                var brushType = typeof(ShikakuLevelEditor).GetNestedType("Brush", BindingFlags.NonPublic);
                var paint = typeof(ShikakuLevelEditor).GetMethod("Paint", flags);
                void Paint(string brush) => paint.Invoke(window, new object[] { new GridCell(0, 0), Enum.Parse(brushType, brush) });
                Paint("将令");
                if (!Array.Exists(config.cellDatas, x => x.position == Vector2Int.zero && x.number == 6))
                {
                    throw new Exception("关卡画板将令写入失败。");
                }

                Undo.FlushUndoRecordObjects();
                Undo.PerformUndo();
                if (!Array.Exists(config.cellDatas, x => x.position == Vector2Int.zero && x.number == 4))
                {
                    throw new Exception("关卡画板撤销失败。");
                }

                Paint("擦除");
                if (Array.Exists(config.cellDatas, x => x.position == Vector2Int.zero))
                {
                    throw new Exception("将令擦除失败。");
                }

                config.SetData(ShikakuCampaign.CreateDefault()[6]);
                config.levelId = "editor-round-trip";
                AssetDatabase.CreateAsset(config, path);
                AssetDatabase.SaveAssets();
                var restored = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (restored == null || restored.ToData().id != "editor-round-trip" || !restored.requireArcherSupport || restored.formation != E_FormationTheme.中军 || ShikakuSolver.Solve(restored.ToData(), null, 1).solutions.Count == 0)
                {
                    throw new Exception("关卡保存读取失败。");
                }
            }
            finally
            {
                window.SetLevel(null);
                UnityEngine.Object.DestroyImmediate(window);
                Undo.ClearUndo(config);
                if (File.Exists(path))
                {
                    AssetDatabase.DeleteAsset(path);
                }
                else
                {
                    UnityEngine.Object.DestroyImmediate(config);
                }
            }
        }

        private static void Finish(bool success, string message)
        {
            Directory.CreateDirectory("Temp");
            string path = SessionState.GetBool("Shikaku.ValidationSingle", false) ? "Temp/ShikakuPlaytestValidation.json" : ResultPath;
            File.WriteAllText(path, JsonUtility.ToJson(new Result { success = success, completedLevels = SessionState.GetInt("Shikaku.ValidationLevel", 0), message = message, errors = SessionState.GetString("Shikaku.ValidationErrors", "") }, true));
            if (EditorApplication.isPlaying)
            {
                m_leavingPlayMode = true;
                EditorApplication.delayCall += () => EditorApplication.isPlaying = false;
            }
            else
            {
                SessionState.SetBool(ActiveKey, false);
            }
        }

        private static void OnLog(string message, string stack, LogType type)
        {
            if (SessionState.GetBool(ActiveKey, false) && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert))
            {
                SessionState.SetString("Shikaku.ValidationErrors", SessionState.GetString("Shikaku.ValidationErrors", "") + message + "\n" + stack + "\n");
            }
        }

        private static void OnPlayModeChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredEditMode)
            {
                return;
            }

            bool wasValidation = SessionState.GetBool(ActiveKey, false);
            SessionState.SetBool(ActiveKey, false);
            SessionState.SetString("Shikaku.PlaytestLevel", "");
            m_leavingPlayMode = false;
            if (!wasValidation)
            {
                return;
            }

            string scene = SessionState.GetString("Shikaku.ValidationOriginalScene", "");
            if (!string.IsNullOrEmpty(scene) && File.Exists(scene))
            {
                EditorApplication.delayCall += () => EditorSceneManager.OpenScene(scene);
            }
        }
    }
}
