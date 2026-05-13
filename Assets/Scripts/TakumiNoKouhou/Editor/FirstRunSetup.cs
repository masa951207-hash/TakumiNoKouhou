using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Rendering.Universal;
using UnityEditor.Build;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// プロジェクトを Unity Hub で開いた瞬間に一度だけ実行される完全自動セットアップ。
    ///
    /// 実行順序:
    ///   1. URP パイプラインアセット生成 → GraphicsSettings に適用
    ///   2. Android PlayerSettings 設定
    ///   3. Assets/Scenes/ に Title / StageSelect / Gameplay を生成
    ///   4. Build Settings にシーンを登録
    ///   5. ScriptableObject を一括生成（JointTypeData / WoodPieceData / PuzzleStageData）
    ///   6. Gameplay シーンを開いてヒエラルキーを自動構築
    ///   7. PuzzleGameManager / PuzzleClearSystem 等の SerializeField を自動接続
    ///   8. チュートリアルステップを自動定義
    ///   9. シーンを保存して完了ダイアログを表示
    /// </summary>
    [InitializeOnLoad]
    public static class FirstRunSetup
    {
        private const string PREFS_KEY = "TakumiNoKouhou_FirstRunComplete";

        static FirstRunSetup()
        {
            if (EditorPrefs.GetBool(PREFS_KEY, false)) return;
            // コンパイル直後に実行するため delayCall を使う
            EditorApplication.delayCall += Run;
        }

        [MenuItem("匠の工法/初回セットアップを再実行", priority = 99)]
        public static void ReRun()
        {
            EditorPrefs.DeleteKey(PREFS_KEY);
            Run();
        }

        // ═══════════════════════════════════════════════════════════════
        //  メイン処理
        // ═══════════════════════════════════════════════════════════════

        private static void Run()
        {
            EditorApplication.delayCall -= Run;

            Debug.Log("[匠の工法] 初回セットアップ開始...");

            try
            {
                Step1_SetupURP();
                Step2_AndroidPlayerSettings();
                Step3_CreateScenes();
                Step4_RegisterBuildSettings();
                Step5_CreateScriptableObjects();
                SetupFontsAndAudio.SetupAll();   // フォント + SE + BGM 生成
                Step6_SetupAllScenes();
                // シーン変更後に参照接続を実行
                EditorApplication.delayCall += Step7_WireReferences;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[匠の工法] セットアップ中にエラーが発生しました: {e}");
            }
        }

        // ─────────────────── Step 1: URP ───────────────────

        private static void Step1_SetupURP()
        {
            const string settingsDir = "Assets/Settings";
            if (!AssetDatabase.IsValidFolder(settingsDir))
                AssetDatabase.CreateFolder("Assets", "Settings");

            const string urpAssetPath     = settingsDir + "/UniversalRenderPipelineAsset.asset";
            const string urpRendererPath  = settingsDir + "/UniversalRendererData.asset";

            // Renderer Data
            UniversalRendererData rendererData;
            rendererData = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(urpRendererPath);
            if (rendererData == null)
            {
                rendererData = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(rendererData, urpRendererPath);
            }

            // URP Asset
            UniversalRenderPipelineAsset urpAsset;
            urpAsset = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(urpAssetPath);
            if (urpAsset == null)
            {
                urpAsset = UniversalRenderPipelineAsset.Create(rendererData);
                urpAsset.msaaSampleCount = 2;
                urpAsset.shadowDistance  = 30f;
                AssetDatabase.CreateAsset(urpAsset, urpAssetPath);
            }

            // Graphics Settings に適用
            GraphicsSettings.defaultRenderPipeline = urpAsset;
            // Quality Settings にも適用（全レベル）
            for (int i = 0; i < QualitySettings.names.Length; i++)
                QualitySettings.SetQualityLevel(i);

            QualitySettings.renderPipeline = urpAsset;

            EditorUtility.SetDirty(urpAsset);
            AssetDatabase.SaveAssets();
            Debug.Log("[Step 1] URP アセット生成・適用完了");
        }

        // ─────────────────── Step 2: Android PlayerSettings ───────────────────

        private static void Step2_AndroidPlayerSettings()
        {
            PlayerSettings.companyName   = "TakumiStudio";
            PlayerSettings.productName   = "匠の工法";
            PlayerSettings.bundleVersion = "1.0.0";

            PlayerSettings.SetApplicationIdentifier(
                NamedBuildTarget.Android, "jp.takumi.kougou");

            PlayerSettings.Android.minSdkVersion     = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.Android.targetSdkVersion  = AndroidSdkVersions.AndroidApiLevel34;
            PlayerSettings.Android.bundleVersionCode = 1;

            // IL2CPP + ARM64
            PlayerSettings.SetScriptingBackend(
                NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;

            // m_ActiveInputHandler は ProjectSettings.asset で既に 2 (Both) に設定済み。
            // ここで SerializedObject 経由で再書き込みすると InputSystem 初期化クラッシュの
            // 原因になるため、意図的に省略する。

            Debug.Log("[Step 2] Android PlayerSettings 設定完了");
        }

        // ─────────────────── Step 3: シーン生成 ───────────────────

        private static void Step3_CreateScenes()
        {
            const string scenesDir = "Assets/Scenes";
            if (!AssetDatabase.IsValidFolder(scenesDir))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            CreateSceneIfNotExists(scenesDir + "/Title.unity",      NewSceneSetup.EmptyScene);
            CreateSceneIfNotExists(scenesDir + "/StageSelect.unity", NewSceneSetup.EmptyScene);
            CreateSceneIfNotExists(scenesDir + "/Gameplay.unity",   NewSceneSetup.DefaultGameObjects);

            AssetDatabase.Refresh();
            Debug.Log("[Step 3] シーン生成完了");
        }

        private static void CreateSceneIfNotExists(string path, NewSceneSetup setup)
        {
            if (File.Exists(Path.Combine(Application.dataPath, "..", path))) return;

            var scene = EditorSceneManager.NewScene(setup, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, path);
        }

        // ─────────────────── Step 4: Build Settings ───────────────────

        private static void Step4_RegisterBuildSettings()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/Title.unity",      true),
                new EditorBuildSettingsScene("Assets/Scenes/StageSelect.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/Gameplay.unity",   true)
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[Step 4] Build Settings 登録完了");
        }

        // ─────────────────── Step 5: ScriptableObject 生成 ───────────────────

        private static void Step5_CreateScriptableObjects()
        {
            CreateGameAssets.CreateAll();
            Debug.Log("[Step 5] ScriptableObject 生成完了");
        }

        // ─────────────────── Step 6: 全シーン構築 ───────────────────

        private static void Step6_SetupAllScenes()
        {
            // ── Title シーン ──
            EditorSceneManager.OpenScene("Assets/Scenes/Title.unity");
            SetupTitleScene.SetupSilent();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // ── StageSelect シーン ──
            EditorSceneManager.OpenScene("Assets/Scenes/StageSelect.unity");
            SetupStageSelectScene.SetupSilent();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            // ── Gameplay シーン（最後に開いておく → Step7 が操作する対象） ──
            EditorSceneManager.OpenScene("Assets/Scenes/Gameplay.unity");
            SetupGameplayScene.SetupSilent();
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());

            Debug.Log("[Step 6] 全シーン構築完了");
        }

        // ─────────────────── Step 7: SerializeField 自動接続 ───────────────────

        private static void Step7_WireReferences()
        {
            EditorApplication.delayCall -= Step7_WireReferences;

            try
            {
                Step7_WireReferencesImpl();
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[匠の工法] Step7 参照接続中にエラー: {e}");
            }
        }

        private static void Step7_WireReferencesImpl()
        {
            // ── Systems GameObject からコンポーネントを取得 ──
            var systemsGO   = GameObject.Find("Systems");
            var gridGO      = GameObject.Find("PuzzleGrid");
            var managerGO   = GameObject.Find("PuzzleGameManager");
            var canvasGO    = GameObject.Find("Canvas");

            if (systemsGO == null || gridGO == null || managerGO == null)
            {
                Debug.LogWarning("[Step 7] 必要なGameObjectが見つかりません。シーンセットアップを再実行してください。");
                return;
            }

            var grid                = gridGO.GetComponent<PuzzleGrid>();
            var slotSystem          = systemsGO.GetComponent<SlotSystem>();
            var compatChecker       = systemsGO.GetComponent<JointCompatibilityChecker>();
            var forceCalc           = systemsGO.GetComponent<ForceFlowCalculator>();
            var stressVis           = systemsGO.GetComponent<StressVisualizer>();
            var resonanceSim        = systemsGO.GetComponent<ResonanceSimulator>();
            var bendingSim          = systemsGO.GetComponent<BendingSimulator>();
            var seismicApplicator   = systemsGO.GetComponent<SeismicLoadApplicator>();
            var structEvaluator     = systemsGO.GetComponent<StructuralEvaluator>();
            var clearSystem         = systemsGO.GetComponent<PuzzleClearSystem>();

            var hud          = canvasGO?.transform.Find("PuzzleHUD")?.GetComponent<PuzzleHUD>();
            var infoPanel    = canvasGO?.transform.Find("JointInfoPanel")?.GetComponent<JointInfoPanel>();
            var resultScreen = canvasGO?.transform.Find("ResultScreen")?.GetComponent<ResultScreen>();

            // ── PuzzleGameManager の参照接続 ──
            var gameManager = managerGO.GetComponent<PuzzleGameManager>();
            Wire(gameManager, so =>
            {
                Set(so, "grid",               grid);
                Set(so, "slotSystem",          slotSystem);
                Set(so, "compatibilityChecker", compatChecker);
                Set(so, "forceCalculator",     forceCalc);
                Set(so, "stressVisualizer",    stressVis);
                Set(so, "resonanceSimulator",  resonanceSim);
                Set(so, "bendingSimulator",    bendingSim);
                Set(so, "seismicApplicator",   seismicApplicator);
                Set(so, "structuralEvaluator", structEvaluator);
                Set(so, "clearSystem",         clearSystem);
                Set(so, "hud",                 hud);
                Set(so, "jointInfoPanel",      infoPanel);
                Set(so, "resultScreen",        resultScreen);
                SetArray(so, "availableJointTypes", LoadAllJoints());
            });

            // ── PuzzleClearSystem の参照接続 ──
            Wire(clearSystem, so =>
            {
                Set(so, "seismicApplicator",   seismicApplicator);
                Set(so, "structuralEvaluator", structEvaluator);
                Set(so, "resonanceSimulator",  resonanceSim);
                Set(so, "forceCalculator",     forceCalc);
                Set(so, "bendingSimulator",    bendingSim);
            });

            // ── ForceFlowCalculator ──
            Wire(forceCalc, so =>
            {
                Set(so, "grid",       grid);
                Set(so, "slotSystem", slotSystem);
            });

            // ── StressVisualizer ──
            Wire(stressVis, so =>
            {
                Set(so, "grid",            grid);
                Set(so, "forceCalculator", forceCalc);
            });

            // ── ResonanceSimulator ──
            Wire(resonanceSim, so =>
            {
                Set(so, "grid",                grid);
                Set(so, "slotSystem",          slotSystem);
                Set(so, "compatibilityChecker", compatChecker);
            });

            // ── BendingSimulator ──
            Wire(bendingSim, so =>
            {
                Set(so, "grid",            grid);
                Set(so, "forceCalculator", forceCalc);
            });

            // ── StructuralEvaluator ──
            Wire(structEvaluator, so =>
            {
                Set(so, "slotSystem",           slotSystem);
                Set(so, "compatibilityChecker", compatChecker);
                Set(so, "resonanceSimulator",   resonanceSim);
                Set(so, "bendingSimulator",     bendingSim);
                Set(so, "forceCalculator",      forceCalc);
            });

            // ── SeismicLoadApplicator ──
            Wire(seismicApplicator, so =>
            {
                Set(so, "forceCalculator", forceCalc);
                Set(so, "bendingSimulator", bendingSim);
            });

            // ── JointCompatibilityChecker ──
            Wire(compatChecker, so =>
            {
                SetArray(so, "jointTypeDatabase", LoadAllJoints());
            });

            // ── SlotSystem ──
            Wire(slotSystem, so => Set(so, "grid", grid));

            // ── StageSelectScreen（StageSelectシーンはここでは接続しない） ──

            // ── Step 8: チュートリアルステップ定義 ──
            Step8_SetupTutorial(canvasGO);

            // ── Step 9: PuzzleGameManager にフォールバックステージを設定 ──
            var fallbackStage = LoadStage(1);
            if (fallbackStage != null)
                Wire(gameManager, so => Set(so, "fallbackStage", fallbackStage));

            // ── シーン保存 ──
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();

            EditorPrefs.SetBool(PREFS_KEY, true);

            Debug.Log("[Step 7] SerializeField 自動接続完了");
            EditorUtility.DisplayDialog(
                "匠の工法 セットアップ完了",
                "全ての初期化が完了しました！\n\n" +
                "▶ ボタンを押してゲームを実行できます。\n\n" +
                "残り作業：\n" +
                "・Assets/TakumiNoKouhou/ScriptableObjects/\n" +
                "  に生成された各SO に icon/prefab/sound を設定\n" +
                "・木材のマテリアル・テクスチャを作成",
                "了解！");
        }

        // ─────────────────── Step 8: チュートリアルステップ ───────────────────

        private static void Step8_SetupTutorial(GameObject canvasGO)
        {
            var tutorialGO = canvasGO?.transform.Find("TutorialSystem")?.gameObject;
            if (tutorialGO == null) return;

            var tutorial = tutorialGO.GetComponent<TutorialSystem>();
            if (tutorial == null) return;

            // TutorialStep の配列を SerializedObject 経由で設定
            var so = new SerializedObject(tutorial);
            var stepsProp = so.FindProperty("steps");
            if (stepsProp == null) return;

            var messages = new[]
            {
                "ようこそ、匠の工房へ。\nここでは木組みの技を学ぶ。\n「次へ」で進もう。",
                "木材ピースを長押し（PC：クリック）してドラッグすると移動できる。\n格子の上で離すと配置される。",
                "ドラッグ中に「回転」ボタンを押すと\nピースを90度回転できる。",
                "継手スロット（金色の点）が合わさると\n自動的に接合される。",
                "力の流れボタンを押すと\n圧縮（赤）・引張（青）の分布が見える。",
                "構造が完成したら「地震試験」ボタンを押そう。\n評価結果でS〜Fのランクが付く。",
                "以上が基本操作だ。\nさあ、木を組め！"
            };

            stepsProp.arraySize = messages.Length;
            for (int i = 0; i < messages.Length; i++)
            {
                var elem = stepsProp.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("message").stringValue = messages[i];
                elem.FindPropertyRelative("waitForClick").boolValue = true;
                elem.FindPropertyRelative("autoAdvanceDelay").floatValue = 4f;
            }

            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tutorial);
        }

        // ─────────────────── ユーティリティ ───────────────────

        private static void Wire(Object target, System.Action<SerializedObject> configure)
        {
            if (target == null) return;
            var so = new SerializedObject(target);
            configure(so);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(target);
        }

        private static void Set(SerializedObject so, string propName, Object value)
        {
            var prop = so.FindProperty(propName);
            if (prop == null)
            {
                Debug.LogWarning($"[Wire] プロパティが見つかりません: {propName} ({so.targetObject?.name})");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetArray(SerializedObject so, string propName, Object[] values)
        {
            var prop = so.FindProperty(propName);
            if (prop == null) return;

            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++)
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        }

        private static JointTypeData[] LoadAllJoints()
        {
            var guids = AssetDatabase.FindAssets("t:JointTypeData",
                new[] { "Assets/TakumiNoKouhou/ScriptableObjects/Joints" });

            var result = new JointTypeData[guids.Length];
            for (int i = 0; i < guids.Length; i++)
                result[i] = AssetDatabase.LoadAssetAtPath<JointTypeData>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
            return result;
        }

        private static PuzzleStageData LoadStage(int number)
        {
            var guids = AssetDatabase.FindAssets("t:PuzzleStageData",
                new[] { "Assets/TakumiNoKouhou/ScriptableObjects/Stages" });

            foreach (var guid in guids)
            {
                var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(
                    AssetDatabase.GUIDToAssetPath(guid));
                if (stage != null && stage.stageNumber == number) return stage;
            }
            return null;
        }
    }
}
