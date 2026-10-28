using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace LikeShikaku.Editor
{
    public static class ShikakuBuildTools
    {
        [MenuItem("Tools/军师八阵图/构建当前平台游玩版")]
        public static void Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
            {
                Debug.LogWarning("请先退出运行，等待编译完成。");
                return;
            }

            ShikakuProjectSetup.EnsureAssets();
            var target = EditorUserBuildSettings.activeBuildTarget;
            if (!BuildPipeline.IsBuildTargetSupported(BuildPipeline.GetBuildTargetGroup(target), target))
            {
                Debug.LogError("尚未安装当前目标平台的 Unity 构建模块。");
                return;
            }

            string folder = EditorUtility.SaveFolderPanel("保存游玩版", "Builds", target.ToString());
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            Directory.CreateDirectory(folder);
            // 原框架通过 Addressables 加载界面和主场景，构建时同时生成对应内容。
            AddressableAssetSettings.BuildPlayerContent(out var content);
            if (!string.IsNullOrEmpty(content.Error))
            {
                Debug.LogError("Addressables 构建失败：" + content.Error);
                return;
            }

            string location = target == BuildTarget.StandaloneWindows64 || target == BuildTarget.StandaloneWindows ? Path.Combine(folder, "KingdomFormation.exe") : target == BuildTarget.Android ? Path.Combine(folder, "KingdomFormation.apk") : target == BuildTarget.StandaloneOSX ? Path.Combine(folder, "KingdomFormation.app") : folder;
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions { scenes = EditorBuildSettings.scenes.Where(x => x.enabled).Select(x => x.path).ToArray(), locationPathName = location, target = target, options = BuildOptions.None });
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log("游玩版构建成功：" + location);
            }
            else
            {
                Debug.LogError("构建失败，详见 Console：" + report.summary.result);
            }
        }
    }
}
