using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// WebGL ビルド用スクリプト。
    /// GitHub Actions (GameCI): -executeMethod TakumiNoKouhou.Editor.WebGLBuilder.Build
    /// ローカル:                匠の工法 → WebGL ビルド（デスクトップ出力）
    /// </summary>
    public static class WebGLBuilder
    {
        private static readonly string[] Scenes =
        {
            "Assets/Scenes/Title.unity",
            "Assets/Scenes/StageSelect.unity",
            "Assets/Scenes/Gameplay.unity",
        };

        // ─────────────────── GameCI 用（コマンドライン実行）───────────────────

        public static void Build()
        {
            string buildPath = GetArg("-buildPath") ?? "Builds/WebGL/TakumiNoKouhou";
            string version   = GetArg("-buildVersion") ?? Application.version;

            ApplyWebGLSettings(version);

            var opts = new BuildPlayerOptions
            {
                scenes           = Scenes,
                locationPathName = buildPath,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None,
            };

            Debug.Log($"[WebGLBuilder] ビルド開始 → {buildPath}  version={version}");
            var report = BuildPipeline.BuildPlayer(opts);

            if (report.summary.result == BuildResult.Succeeded)
                Debug.Log($"[WebGLBuilder] ビルド成功: {report.summary.totalSize / 1024 / 1024} MB");
            else
                throw new Exception($"WebGL build failed: {report.summary.result}");
        }

        // ─────────────────── ローカルビルド（メニューから実行）───────────────────

        [MenuItem("匠の工法/WebGL ビルド（デスクトップ出力）", priority = 11)]
        public static void BuildLocal()
        {
            var desktop   = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            var outputDir = Path.Combine(desktop, "匠の工法_WebGL");

            ApplyWebGLSettings(Application.version);

            var opts = new BuildPlayerOptions
            {
                scenes           = Scenes,
                locationPathName = outputDir,
                target           = BuildTarget.WebGL,
                options          = BuildOptions.None,
            };

            Debug.Log($"[匠の工法] WebGL ビルド開始: {outputDir}");
            var report = BuildPipeline.BuildPlayer(opts);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[匠の工法] WebGL ビルド成功 → {outputDir}");
                EditorUtility.DisplayDialog(
                    "WebGL ビルド完了",
                    $"デスクトップに出力しました。\n\n{outputDir}\n\n" +
                    "ブラウザで開く場合はローカルサーバーが必要です。\n" +
                    "例: python -m http.server 8000",
                    "OK");
            }
            else
            {
                Debug.LogError($"[匠の工法] WebGL ビルド失敗: {report.summary.result}");
                EditorUtility.DisplayDialog("ビルド失敗",
                    $"WebGL ビルドに失敗しました。\nConsole を確認してください。\n\n{report.summary.result}",
                    "OK");
            }
        }

        // ─────────────────── 共通設定 ───────────────────

        private static void ApplyWebGLSettings(string version)
        {
            PlayerSettings.bundleVersion = version;
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.WebGL, "jp.takumi.kougou");
            PlayerSettings.productName = "匠の工法";

            // 圧縮なし → GitHub Pages は Brotli/Gzip の自動解凍に対応していないため
            PlayerSettings.WebGL.compressionFormat = WebGLCompressionFormat.Disabled;

            // 例外サポート：None でパフォーマンス優先（デバッグ時は ExplicitlyThrownExceptionsOnly に変更）
            PlayerSettings.WebGL.exceptionSupport = WebGLExceptionSupport.None;

            // データキャッシュ（再訪問時のロード高速化）
            PlayerSettings.WebGL.dataCaching = true;

            // WebGL は IL2CPP のみ
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.WebGL, ScriptingImplementation.IL2CPP);

            // グラフィクス API（WebGL は WebGL2 自動）
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.WebGL, true);
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }
    }
}
