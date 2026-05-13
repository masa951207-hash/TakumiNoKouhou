using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// Windows スタンドアロンビルドをデスクトップへ出力するツール。
    /// メニュー → 匠の工法 → Windows ビルド（デスクトップ） から実行。
    /// </summary>
    public static class BuildTools
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Title.unity",
            "Assets/Scenes/StageSelect.unity",
            "Assets/Scenes/Gameplay.unity",
        };

        [MenuItem("匠の工法/Windows ビルド（デスクトップ）", priority = 10)]
        public static void BuildWindows()
        {
            var desktop   = System.Environment.GetFolderPath(
                                System.Environment.SpecialFolder.Desktop);
            var outputDir = Path.Combine(desktop, "匠の工法");
            var exePath   = Path.Combine(outputDir, "匠の工法.exe");

            Directory.CreateDirectory(outputDir);

            var opts = new BuildPlayerOptions
            {
                scenes             = Scenes,
                locationPathName   = exePath,
                target             = BuildTarget.StandaloneWindows64,
                options            = BuildOptions.None,
            };

            Debug.Log($"[匠の工法] ビルド開始: {exePath}");

            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[匠の工法] ビルド成功 → {exePath}");
                EditorUtility.DisplayDialog(
                    "ビルド完了",
                    $"デスクトップに出力しました。\n\n{exePath}",
                    "OK");
            }
            else
            {
                Debug.LogError($"[匠の工法] ビルド失敗: {report.summary.result}");
                EditorUtility.DisplayDialog(
                    "ビルド失敗",
                    $"ビルドに失敗しました。\nConsole を確認してください。\n\n{report.summary.result}",
                    "OK");
            }
        }
    }
}
