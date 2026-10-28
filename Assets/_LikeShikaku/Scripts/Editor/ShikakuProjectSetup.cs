using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Common.Level;
using MainScene.BattlePart.Configs;
using MainScene.BattlePart.Shikaku;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace LikeShikaku.Editor
{
    [InitializeOnLoad]
    public static class ShikakuProjectSetup
    {
        public const string DatabasePath = "Assets/_LikeShikaku/Resources/Shikaku/LevelDatabase.asset";
        private const string SettingsPath = "Assets/_LikeShikaku/Content/MainScene/BattlePart/Config/BattleSettings.asset";
        private const string ArtSource = "E:/Desktop/小游戏相关资料/军师八阵图小游戏/KingdomFormation_Unity2D_ArtPack/KingdomFormationAssets";
        static ShikakuProjectSetup()
        {
            EditorApplication.delayCall += Initialize;
        }

        private static void Initialize()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || SessionState.GetBool("Shikaku.AssetsReady.v3", false))
            {
                return;
            }

            SessionState.SetBool("Shikaku.AssetsReady.v3", true);
            EnsureAssets();
        }

        [MenuItem("Tools/军师八阵图/检查并补齐游戏资源")]
        public static void EnsureAssets()
        {
            Directory.CreateDirectory("Assets/_LikeShikaku/Resources/Shikaku/Levels");
            AssetDatabase.Refresh();
            var database = AssetDatabase.LoadAssetAtPath<LevelDatabase>(DatabasePath);
            if (database == null)
            {
                database = ScriptableObject.CreateInstance<LevelDatabase>();
                var configs = new List<LevelConfig>();
                foreach (var data in ShikakuCampaign.CreateDefault())
                {
                    string path = "Assets/_LikeShikaku/Resources/Shikaku/Levels/" + data.id + ".asset";
                    var config = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                    if (config == null)
                    {
                        config = ScriptableObject.CreateInstance<LevelConfig>();
                        config.SetData(data);
                        AssetDatabase.CreateAsset(config, path);
                    }

                    configs.Add(config);
                }

                database.SetLevels(configs);
                AssetDatabase.CreateAsset(database, DatabasePath);
            }

            var settings = AssetDatabase.LoadAssetAtPath<BattleSettings>(SettingsPath);
            if (settings != null)
            {
                var font = AssetDatabase.LoadAssetAtPath<Font>("Assets/Res/Fonts/NewFont.ttf");
                if (settings.hudFont == null)
                {
                    settings.hudFont = font;
                }

                if (settings.boardFont == null && font != null)
                {
                    settings.boardFont = CreateBoardFont(font);
                }

                if (Directory.Exists(ArtSource))
                {
                    settings.groundSprite = settings.groundSprite ? settings.groundSprite : ImportSprite("04_Board/Terrain/sand.png");
                    settings.infantrySprite = settings.infantrySprite ? settings.infantrySprite : ImportSprite("02_Units/infantry/infantry_01.png");
                    settings.spearmanSprite = settings.spearmanSprite ? settings.spearmanSprite : ImportSprite("02_Units/spearman/spearman_01.png");
                    settings.archerSprite = settings.archerSprite ? settings.archerSprite : ImportSprite("02_Units/archer/archer_01.png");
                    settings.cavalrySprite = settings.cavalrySprite ? settings.cavalrySprite : ImportSprite("02_Units/cavalry/cavalry_01.png");
                    settings.infantryFlag = settings.infantryFlag ? settings.infantryFlag : ImportSprite("03_Banners/banner_infantry_red.png");
                    settings.spearmanFlag = settings.spearmanFlag ? settings.spearmanFlag : ImportSprite("03_Banners/banner_spearman_blue.png");
                    settings.archerFlag = settings.archerFlag ? settings.archerFlag : ImportSprite("03_Banners/banner_archer_green.png");
                    settings.cavalryFlag = settings.cavalryFlag ? settings.cavalryFlag : ImportSprite("03_Banners/banner_cavalry_gold.png");
                    settings.backgroundSprite = settings.backgroundSprite ? settings.backgroundSprite : ImportSprite("07_Backgrounds/training_ground_day.png");
                    settings.advisorPortrait = settings.advisorPortrait ? settings.advisorPortrait : ImportSprite("01_Characters/ZhugeLiang/zhuge_think.png");
                    settings.advisorVictory = settings.advisorVictory ? settings.advisorVictory : ImportSprite("01_Characters/ZhugeLiang/zhuge_victory.png");
                }

                EditorUtility.SetDirty(settings);
            }

            var scenes = EditorBuildSettings.scenes.ToList();
            foreach (string scene in new[]
            {
                "Assets/BootScene.unity",
                "Assets/_LikeShikaku/Content/MainScene/MainScene.unity"
            }

            )
            {
                if (!scenes.Exists(x => x.path == scene))
                {
                    scenes.Add(new EditorBuildSettingsScene(scene, true));
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            AssetDatabase.SaveAssets();
        }

        private static TMP_FontAsset CreateBoardFont(Font font)
        {
            const string path = "Assets/_LikeShikaku/Resources/Shikaku/BoardFont.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (asset != null)
            {
                return asset;
            }

            asset = TMP_FontAsset.CreateFontAsset(font, 48, 5, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            asset.name = "MiSans Board";
            AssetDatabase.CreateAsset(asset, path);
            // 预填棋盘需要的字符；其余汉字由随包字体动态补充。
            asset.TryAddCharacters("0123456789步枪弓骑引导军令", out string missing);
            if (asset.material != null)
            {
                AssetDatabase.AddObjectToAsset(asset.material, asset);
            }

            foreach (var texture in asset.atlasTextures)
            {
                if (texture != null && !AssetDatabase.Contains(texture))
                {
                    AssetDatabase.AddObjectToAsset(texture, asset);
                }
            }

            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static Sprite ImportSprite(string relative)
        {
            string source = Path.Combine(ArtSource, relative);
            if (!File.Exists(source))
            {
                return null;
            }

            string destination = "Assets/_LikeShikaku/Art/KingdomFormation/" + relative;
            Directory.CreateDirectory(Path.GetDirectoryName(destination));
            if (!File.Exists(destination))
            {
                File.Copy(source, destination);
            }

            AssetDatabase.ImportAsset(destination, ImportAssetOptions.ForceSynchronousImport);
            var importer = (TextureImporter)AssetImporter.GetAtPath(destination);
            if (importer.textureType != TextureImporterType.Sprite || importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = true;
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(destination);
        }
    }
}
