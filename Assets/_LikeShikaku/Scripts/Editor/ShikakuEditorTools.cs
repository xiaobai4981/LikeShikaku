using System;
using System.Collections.Generic;
using System.IO;
using Common.Level;
using MainScene.BattlePart.Configs;
using MainScene.BattlePart.Shikaku;
using UnityEditor;
using UnityEngine;

namespace LikeShikaku.Editor
{
    public static class ShikakuEditorTools
    {
        private const string Source = "E:/Desktop/小游戏相关资料/军师八阵图小游戏/KingdomFormation_Unity2D_ArtPack/KingdomFormationAssets";
        [MenuItem("Tools/军师八阵图/生成二十关模板（不覆盖已有资源）")]
        public static void Generate()
        {
            const string folder = "Assets/_LikeShikaku/Content/MainScene/Data/KingdomCampaign";
            Directory.CreateDirectory(folder);
            AssetDatabase.Refresh();
            var assets = new List<UnityEngine.Object>();
            foreach (var data in ShikakuCampaign.CreateDefault())
            {
                var asset = ScriptableObject.CreateInstance<LevelConfig>();
                asset.SetData(data);
                var path = AssetDatabase.GenerateUniqueAssetPath(folder + "/" + data.id + ".asset");
                AssetDatabase.CreateAsset(asset, path);
                assets.Add(asset);
            }

            AssetDatabase.SaveAssets();
            Selection.objects = assets.ToArray();
            Debug.Log("已生成并选中二十关。可将这些 LevelConfig 按文件名顺序拖到 MainSceneGameMode 的 Levels 数组。未配置时运行内置题库。");
        }

        [MenuItem("Tools/军师八阵图/检查默认二十关")]
        public static void CheckDefaults()
        {
            foreach (var data in ShikakuCampaign.CreateDefault())
            {
                Check(data);
            }
        }

        [MenuItem("Tools/军师八阵图/检查选中的 LevelConfig")]
        public static void CheckSelected()
        {
            foreach (var obj in Selection.objects)
            {
                var level = obj as LevelConfig;
                if (!level)
                {
                    continue;
                }

                try
                {
                    Check(level.ToData());
                }
                catch (Exception e)
                {
                    Debug.LogError(e.Message, level);
                }
            }
        }

        private static void Check(ShikakuLevelData data)
        {
            var errors = data.Inspect();
            if (errors.Count > 0)
            {
                Debug.LogError(data.id + "：" + string.Join("；", errors));
                return;
            }

            var solved = ShikakuSolver.Solve(data, null, 2, 200000);
            string verdict = solved.solutions.Count >= 2 ? "存在多解（均可接受）" : solved.budgetExceeded ? "搜索达到上限，无法确认唯一性" : solved.solutions.Count == 1 ? "唯一解" : "无解，必须修复";
            if (solved.solutions.Count == 0 && !solved.budgetExceeded)
            {
                Debug.LogError(data.id + "：" + verdict);
            }
            else
            {
                Debug.Log(data.id + "：" + verdict);
            }
        }

        [MenuItem("Tools/军师八阵图/将美术配置到选中的 BattleSettings")]
        public static void ImportArt()
        {
            var settings = Selection.activeObject as BattleSettings;
            if (!settings)
            {
                Debug.LogWarning("请先选中原工程使用的 BattleSettings 资源。");
                return;
            }

            string folder = Directory.Exists(Source) ? Source : EditorUtility.OpenFolderPanel("选择 KingdomFormationAssets", "", "");
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Undo.RecordObject(settings, "Configure Kingdom Formation Art");
            settings.groundSprite = Import(folder, "04_Board/Terrain/sand.png");
            settings.infantrySprite = Import(folder, "02_Units/infantry/infantry_01.png");
            settings.spearmanSprite = Import(folder, "02_Units/spearman/spearman_01.png");
            settings.archerSprite = Import(folder, "02_Units/archer/archer_01.png");
            settings.cavalrySprite = Import(folder, "02_Units/cavalry/cavalry_01.png");
            settings.infantryFlag = Import(folder, "03_Banners/banner_infantry_red.png");
            settings.spearmanFlag = Import(folder, "03_Banners/banner_spearman_blue.png");
            settings.archerFlag = Import(folder, "03_Banners/banner_archer_green.png");
            settings.cavalryFlag = Import(folder, "03_Banners/banner_cavalry_gold.png");
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            Debug.Log("已配置原 BattleSettings 的新增美术字段。源图片与原格子 Prefab 不变。");
        }

        private static Sprite Import(string folder, string relative)
        {
            string source = Path.Combine(folder, relative);
            if (!File.Exists(source))
            {
                Debug.LogWarning("缺少美术：" + source);
                return null;
            }

            string target = "Assets/_LikeShikaku/Art/KingdomFormation/" + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(target));
            AssetDatabase.Refresh();
            if (!File.Exists(target))
            {
                File.Copy(source, target);
            }

            AssetDatabase.ImportAsset(target, ImportAssetOptions.ForceSynchronousImport);
            var importer = AssetImporter.GetAtPath(target) as TextureImporter;
            if (!importer)
            {
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            var textureSettings = new TextureImporterSettings();
            importer.ReadTextureSettings(textureSettings);
            textureSettings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(textureSettings);
            importer.spritePixelsPerUnit = 100;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(target);
        }
    }
}
