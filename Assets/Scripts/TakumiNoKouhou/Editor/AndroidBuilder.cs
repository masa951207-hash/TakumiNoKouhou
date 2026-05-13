using System;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// GitHub Actions (GameCI) からコマンドラインで呼ばれるAndroidビルドメソッド。
    /// -executeMethod TakumiNoKouhou.Editor.AndroidBuilder.Build
    /// </summary>
    public static class AndroidBuilder
    {
        public static void Build()
        {
            // ── ビルド出力パス（GameCI が環境変数で渡す）──
            string buildPath = GetArg("-buildPath") ?? "Builds/Android/TakumiNoKouhou.apk";

            // ── バージョン番号を git タグから設定 ──
            string version = GetArg("-buildVersion") ?? Application.version;
            PlayerSettings.bundleVersion = version;
            PlayerSettings.Android.bundleVersionCode = CalcVersionCode(version);

            // ── パッケージ名 ──
            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "jp.takumi.kougou");

            // ── Android SDK 設定 ──
            PlayerSettings.Android.minSdkVersion  = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel34;

            // ── IL2CPP（スマホは ARM64 + IL2CPP 推奨）──
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // ── グラフィクス API ──
            PlayerSettings.SetUseDefaultGraphicsAPIs(BuildTarget.Android, false);
            PlayerSettings.SetGraphicsAPIs(BuildTarget.Android,
                new[] { UnityEngine.Rendering.GraphicsDeviceType.OpenGLES3 });

            // ── ビルドシーン一覧 ──
            var scenes = new[]
            {
                "Assets/Scenes/Title.unity",
                "Assets/Scenes/StageSelect.unity",
                "Assets/Scenes/Gameplay.unity"
            };

            var buildOptions = new BuildPlayerOptions
            {
                scenes             = scenes,
                locationPathName   = buildPath,
                target             = BuildTarget.Android,
                options            = BuildOptions.None
            };

            Debug.Log($"[AndroidBuilder] ビルド開始 → {buildPath}  version={version}");

            var report = BuildPipeline.BuildPlayer(buildOptions);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[AndroidBuilder] ビルド成功: {report.summary.totalSize / 1024 / 1024} MB");
            }
            else
            {
                Debug.LogError($"[AndroidBuilder] ビルド失敗: {report.summary.result}");
                // GameCI がこのプロセスの終了コードを見るため例外で失敗を通知
                throw new Exception($"Build failed: {report.summary.result}");
            }
        }

        private static string GetArg(string name)
        {
            var args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
                if (args[i] == name) return args[i + 1];
            return null;
        }

        private static int CalcVersionCode(string version)
        {
            // "1.2.3" → 10203
            var parts = version.Split('.');
            int major = parts.Length > 0 ? int.Parse(parts[0]) : 1;
            int minor = parts.Length > 1 ? int.Parse(parts[1]) : 0;
            int patch = parts.Length > 2 ? int.Parse(parts[2]) : 0;
            return major * 10000 + minor * 100 + patch;
        }
    }
}
