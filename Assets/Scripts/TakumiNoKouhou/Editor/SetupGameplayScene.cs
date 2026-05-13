using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem.UI;
using TMPro;

namespace TakumiNoKouhou.Editor
{
    /// <summary>
    /// Gameplayシーンの全ヒエラルキーとコンポーネント参照を自動セットアップするEditorスクリプト。
    /// メニュー → 匠の工法 → Gameplayシーンセットアップ から実行する。
    /// FirstRunSetup からは SetupSilent() を呼ぶこと（ダイアログなし版）。
    /// </summary>
    public static class SetupGameplayScene
    {
        // SetupSilent が生成した主要コンポーネントをまとめて返す構造体
        public struct SetupResult
        {
            public PuzzleGrid           grid;
            public SlotSystem           slotSystem;
            public JointCompatibilityChecker compatibilityChecker;
            public ForceFlowCalculator  forceCalculator;
            public StressVisualizer     stressVisualizer;
            public ResonanceSimulator   resonanceSimulator;
            public BendingSimulator     bendingSimulator;
            public SeismicLoadApplicator seismicApplicator;
            public StructuralEvaluator  structuralEvaluator;
            public PuzzleClearSystem    clearSystem;
            public PuzzleGameManager    gameManager;
            public PuzzleHUD            hud;
            public JointInfoPanel       jointInfoPanel;
            public ResultScreen         resultScreen;
            public TutorialSystem       tutorialSystem;
            public StageProgressionManager progressionManager;
        }

        [MenuItem("匠の工法/Gameplayシーンセットアップ", priority = 2)]
        public static void Setup()
        {
            if (!EditorUtility.DisplayDialog("シーンセットアップ",
                "現在のシーンにGameplayの全オブジェクトを生成します。\n既存のオブジェクトは削除されません。\n続行しますか？", "実行", "キャンセル"))
                return;

            SetupSilent();

            EditorUtility.DisplayDialog("完了",
                "シーンのセットアップが完了しました。\nInspectorで残りのSerializeFieldを確認してください。", "OK");
        }

        /// <summary>ダイアログなしのサイレントセットアップ（FirstRunSetupから呼ぶ）</summary>
        public static SetupResult SetupSilent()
        {
            SetupCamera();
            var grid = SetupGrid();
            SetupSystems(grid);
            SetupCanvas();
            SetupEventSystem();
            SetupLighting();

            Debug.Log("[匠の工法] Gameplayシーンセットアップ完了");

            return CollectResult();
        }

        private static SetupResult CollectResult()
        {
            var systemsObj = GameObject.Find("Systems");
            var canvasObj  = GameObject.Find("Canvas");

            return new SetupResult
            {
                grid                = GameObject.Find("PuzzleGrid")?.GetComponent<PuzzleGrid>(),
                slotSystem          = systemsObj?.GetComponent<SlotSystem>(),
                compatibilityChecker = systemsObj?.GetComponent<JointCompatibilityChecker>(),
                forceCalculator     = systemsObj?.GetComponent<ForceFlowCalculator>(),
                stressVisualizer    = systemsObj?.GetComponent<StressVisualizer>(),
                resonanceSimulator  = systemsObj?.GetComponent<ResonanceSimulator>(),
                bendingSimulator    = systemsObj?.GetComponent<BendingSimulator>(),
                seismicApplicator   = systemsObj?.GetComponent<SeismicLoadApplicator>(),
                structuralEvaluator = systemsObj?.GetComponent<StructuralEvaluator>(),
                clearSystem         = systemsObj?.GetComponent<PuzzleClearSystem>(),
                gameManager         = GameObject.Find("PuzzleGameManager")?.GetComponent<PuzzleGameManager>(),
                hud                 = canvasObj?.transform.Find("PuzzleHUD")?.GetComponent<PuzzleHUD>(),
                jointInfoPanel      = canvasObj?.transform.Find("JointInfoPanel")?.GetComponent<JointInfoPanel>(),
                resultScreen        = canvasObj?.transform.Find("ResultScreen")?.GetComponent<ResultScreen>(),
                tutorialSystem      = canvasObj?.transform.Find("TutorialSystem")?.GetComponent<TutorialSystem>(),
                progressionManager  = GameObject.Find("StageProgressionManager")?.GetComponent<StageProgressionManager>()
            };
        }

        // ─────────────────── カメラ ───────────────────

        private static void SetupCamera()
        {
            var camObj = FindOrCreate("Main Camera");
            camObj.tag = "MainCamera";

            var cam = camObj.GetOrAddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.08f, 0.07f, 0.05f);
            // グリッド中心(≈0,0,0) + 手前ステージングエリア(Z≈-1.5) を上方斜め前から見渡す
            cam.transform.position = new Vector3(0.6f, 4.5f, -4.0f);
            cam.transform.rotation = Quaternion.Euler(42f, 0f, 0f);
            cam.fieldOfView = 55f;

            // URP用カメラデータ
            var urpData = camObj.GetOrAddComponent<UniversalAdditionalCameraData>();
            urpData.renderPostProcessing = true;
            urpData.antialiasing = AntialiasingMode.FastApproximateAntialiasing;

            // モバイル用PhysicsRaycaster（ピース選択に必要）
            camObj.GetOrAddComponent<PhysicsRaycaster>();
        }

        // ─────────────────── グリッド ───────────────────

        private static PuzzleGrid SetupGrid()
        {
            var gridObj = FindOrCreate("PuzzleGrid");
            gridObj.transform.position = Vector3.zero;
            var grid = gridObj.GetOrAddComponent<PuzzleGrid>();
            return grid;
        }

        // ─────────────────── 全システムコンポーネント ───────────────────

        private static void SetupSystems(PuzzleGrid grid)
        {
            // 全システムを1つのGameObjectにまとめる
            var systemsObj = FindOrCreate("Systems");

            var slotSystem          = systemsObj.GetOrAddComponent<SlotSystem>();
            var compatChecker       = systemsObj.GetOrAddComponent<JointCompatibilityChecker>();
            var forceCalc           = systemsObj.GetOrAddComponent<ForceFlowCalculator>();
            var stressVis           = systemsObj.GetOrAddComponent<StressVisualizer>();
            var resonanceSim        = systemsObj.GetOrAddComponent<ResonanceSimulator>();
            var bendingSim          = systemsObj.GetOrAddComponent<BendingSimulator>();
            var seismicApplicator   = systemsObj.GetOrAddComponent<SeismicLoadApplicator>();
            var structEvaluator     = systemsObj.GetOrAddComponent<StructuralEvaluator>();
            var clearSystem         = systemsObj.GetOrAddComponent<PuzzleClearSystem>();

            // PuzzleGameManager（SystemsにアタッチするかManagerとして別オブジェクトにしてもよい）
            var managerObj = FindOrCreate("PuzzleGameManager");
            var manager = managerObj.GetOrAddComponent<PuzzleGameManager>();

            // ステージ進行マネージャー
            var progressionObj = FindOrCreate("StageProgressionManager");
            progressionObj.GetOrAddComponent<StageProgressionManager>();

            Debug.Log("[Setup] Systems生成完了 — InspectorでSerializeFieldに各アセットをアサインしてください。");
        }

        // ─────────────────── キャンバス ───────────────────

        private static void SetupCanvas()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/TMP_Japanese.asset");

            var canvasObj = FindOrCreate("Canvas");
            var canvas = canvasObj.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            canvasObj.GetOrAddComponent<GraphicRaycaster>();

            // ── HUD（上部バー）──
            var hudObj = CreateUIPanel(canvasObj, "PuzzleHUD",
                new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -35f), new Vector2(0f, 70f));
            hudObj.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.05f, 0.90f);
            var hudComp = hudObj.GetOrAddComponent<PuzzleHUD>();

            // 力の流れトグルボタン
            CreateButton(hudObj, "ForceFlowToggleButton", "力の流れ：表示",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(180f, 52f), new Vector2(100f, 0f), font);

            // 地震テストボタン
            var testBtn = CreateButton(hudObj, "TestButton", "地震試験",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(180f, 52f), new Vector2(-100f, 0f), font);
            testBtn.GetComponent<Image>().color = new Color(0.65f, 0.15f, 0.10f);

            // ── JointInfoPanel（左下）──
            var infoObj = CreateUIPanel(canvasObj, "JointInfoPanel",
                new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(200f, 160f), new Vector2(340f, 300f));
            infoObj.GetComponent<Image>().color = new Color(0.14f, 0.10f, 0.06f, 0.92f);
            var infoComp = infoObj.GetOrAddComponent<JointInfoPanel>();
            var infoGroup = infoObj.GetOrAddComponent<CanvasGroup>();
            infoGroup.alpha = 0f;

            // ── ResultScreen（中央）──
            var resultObj = CreateUIPanel(canvasObj, "ResultScreen",
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(700f, 560f));
            resultObj.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.05f, 0.96f);
            var resultComp = resultObj.GetOrAddComponent<ResultScreen>();
            var resultGroup = resultObj.GetOrAddComponent<CanvasGroup>();
            resultGroup.alpha = 0f;
            resultGroup.interactable = false;

            // ── TutorialSystem ──
            var tutorialObj = FindOrCreate("TutorialSystem", canvasObj);
            var tutComp = tutorialObj.GetOrAddComponent<TutorialSystem>();

            // ── 各コンポーネント内部UI構築 ──
            BuildHUDContent(hudObj, hudComp, font);
            BuildJointInfoContent(infoObj, infoComp, font);
            BuildResultScreenContent(resultObj, resultComp, font);
            BuildTutorialContent(tutorialObj, tutComp, font);
        }

        // ─────────────────── HUD 内部UI ───────────────────

        private static void BuildHUDContent(GameObject hudObj, PuzzleHUD hud, TMP_FontAsset font)
        {
            var cream = new Color(0.96f, 0.90f, 0.78f);

            // 回転ボタン（モバイル用）
            CreateButton(hudObj, "RotateButton", "回転 R",
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(120f, 52f), new Vector2(310f, 0f), font);

            // 選択中継手ラベル（中央）
            var jlObj = FindOrCreate("SelectedJointLabel", hudObj);
            var jlRt = jlObj.GetOrAddComponent<RectTransform>();
            jlRt.anchorMin = new Vector2(0.5f, 0.5f); jlRt.anchorMax = new Vector2(0.5f, 0.5f);
            jlRt.anchoredPosition = new Vector2(-120f, 0f); jlRt.sizeDelta = new Vector2(260f, 50f);
            var jlTmp = jlObj.GetOrAddComponent<TextMeshProUGUI>();
            jlTmp.text = "継手：─"; jlTmp.fontSize = 20f;
            jlTmp.alignment = TextAlignmentOptions.Center; jlTmp.color = cream;
            if (font != null) jlTmp.font = font;

            // 共振リスクバッジ
            var rbObj = FindOrCreate("ResonanceRiskBadge", hudObj);
            var rbRt = rbObj.GetOrAddComponent<RectTransform>();
            rbRt.anchorMin = new Vector2(0.5f, 0.5f); rbRt.anchorMax = new Vector2(0.5f, 0.5f);
            rbRt.anchoredPosition = new Vector2(110f, 0f); rbRt.sizeDelta = new Vector2(160f, 44f);
            rbObj.GetOrAddComponent<Image>().color = new Color(0.2f, 0.45f, 0.2f);
            var rbTextObj = FindOrCreate("Text", rbObj);
            var rbTextRt = rbTextObj.GetOrAddComponent<RectTransform>();
            rbTextRt.anchorMin = Vector2.zero; rbTextRt.anchorMax = Vector2.one;
            rbTextRt.offsetMin = Vector2.zero; rbTextRt.offsetMax = Vector2.zero;
            var rbTmp = rbTextObj.GetOrAddComponent<TextMeshProUGUI>();
            rbTmp.text = "共振：安全"; rbTmp.fontSize = 18f;
            rbTmp.alignment = TextAlignmentOptions.Center; rbTmp.color = cream;
            if (font != null) rbTmp.font = font;

            // 戻るボタン
            var backBtnObj = CreateButton(hudObj, "BackButton", "← 課題選択",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(160f, 52f), new Vector2(-310f, 0f), font);
            backBtnObj.GetComponent<Image>().color = new Color(0.20f, 0.15f, 0.07f);

            // SerializeField接続
            var so = new SerializedObject(hud);
            var ffTf   = hudObj.transform.Find("ForceFlowToggleButton");
            var tBtnTf = hudObj.transform.Find("TestButton");
            var rotTf  = hudObj.transform.Find("RotateButton");
            var bkTf   = hudObj.transform.Find("BackButton");
            WireProp(so, "forceFlowToggleButton", ffTf?.GetComponent<Button>());
            WireProp(so, "forceFlowButtonLabel",  ffTf?.Find("Text")?.GetComponent<TextMeshProUGUI>());
            WireProp(so, "testButton",            tBtnTf?.GetComponent<Button>());
            WireProp(so, "testButtonLabel",       tBtnTf?.Find("Text")?.GetComponent<TextMeshProUGUI>());
            WireProp(so, "rotateButton",          rotTf?.GetComponent<Button>());
            WireProp(so, "backButton",            bkTf?.GetComponent<Button>());
            WireProp(so, "selectedJointLabel",    jlTmp);
            WireProp(so, "resonanceRiskBadge",    rbObj.GetComponent<Image>());
            WireProp(so, "resonanceRiskText",     rbTmp);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(hud);
        }

        // ─────────────────── ResultScreen 内部UI ───────────────────

        private static void BuildResultScreenContent(GameObject resultObj, ResultScreen result, TMP_FontAsset font)
        {
            var cream  = new Color(0.96f, 0.90f, 0.78f);
            var warm   = new Color(0.78f, 0.64f, 0.40f);
            var dim    = new Color(0.55f, 0.45f, 0.30f);
            var amber  = new Color(0.62f, 0.42f, 0.12f);

            // パネルタイトル
            MakeText(FindOrCreate("ResultTitle", resultObj),
                new Vector2(0f, 0.91f), new Vector2(1f, 1f), new Vector2(16f, 0f), new Vector2(-16f, 0f),
                "─ 評 定 ─", 22f, amber, TextAlignmentOptions.Center, font);

            // 結果メッセージ
            var msgTmp = MakeText(FindOrCreate("ResultMessage", resultObj),
                new Vector2(0f, 0.83f), new Vector2(0.62f, 0.91f), new Vector2(16f, 0f), new Vector2(-8f, 0f),
                "─", 24f, cream, TextAlignmentOptions.MidlineLeft, font);

            // 等級テキスト（大文字）
            var gradeTmp = MakeText(FindOrCreate("GradeText", resultObj),
                new Vector2(0f, 0.68f), new Vector2(0.32f, 0.84f), new Vector2(16f, 0f), new Vector2(-8f, 0f),
                "─", 60f, new Color(0.8f, 0.65f, 0.1f), TextAlignmentOptions.Center, font);

            // 評価コメント
            var commentTmp = MakeText(FindOrCreate("GradeComment", resultObj),
                new Vector2(0f, 0.60f), new Vector2(1f, 0.68f), new Vector2(16f, 0f), new Vector2(-16f, 0f),
                "─", 18f, warm, TextAlignmentOptions.MidlineLeft, font);
            commentTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // 区切り線
            MakeDivider(resultObj, "ScoreDivider", new Vector2(0f, 0.595f), new Vector2(1f, 0.595f), amber);

            // ── スコア行 x 5 ──
            var so = new SerializedObject(result);
            string[] sliderFields = { "jointPrecisionSlider", "forceDistSlider", "resonanceSlider", "bendingSlider", "connectionSlider" };
            string[] textFields   = { "jointPrecisionText",   "forceDistText",   "resonanceText",   "bendingText",   "connectionText" };
            string[] rowLabels    = { "継手精度", "力分散", "共振耐性", "しなり適正", "接合完成度" };
            Color    barColor     = new Color(0.52f, 0.33f, 0.10f);

            float rowH = 0.40f / 5f;
            for (int i = 0; i < 5; i++)
            {
                float yMax = 0.595f - i * rowH;
                float yMin = yMax - rowH;

                var row = FindOrCreate($"ScoreRow{i}", resultObj);
                var rowRt = row.GetOrAddComponent<RectTransform>();
                rowRt.anchorMin = new Vector2(0f, yMin); rowRt.anchorMax = new Vector2(1f, yMax);
                rowRt.offsetMin = new Vector2(16f, 2f);  rowRt.offsetMax = new Vector2(-16f, -2f);

                var lbl = FindOrCreate("Label", row);
                var lblRt = lbl.GetOrAddComponent<RectTransform>();
                lblRt.anchorMin = Vector2.zero; lblRt.anchorMax = new Vector2(0.26f, 1f);
                lblRt.offsetMin = Vector2.zero; lblRt.offsetMax = Vector2.zero;
                var lblTmp = lbl.GetOrAddComponent<TextMeshProUGUI>();
                lblTmp.text = rowLabels[i]; lblTmp.fontSize = 16f;
                lblTmp.alignment = TextAlignmentOptions.MidlineRight; lblTmp.color = dim;
                if (font != null) lblTmp.font = font;

                var slider = BuildSlider(row, "Slider", new Vector2(0.28f, 0.2f), new Vector2(0.75f, 0.8f), barColor);

                var stObj = FindOrCreate("ScoreText", row);
                var stRt = stObj.GetOrAddComponent<RectTransform>();
                stRt.anchorMin = new Vector2(0.77f, 0f); stRt.anchorMax = Vector2.one;
                stRt.offsetMin = Vector2.zero; stRt.offsetMax = Vector2.zero;
                var stTmp = stObj.GetOrAddComponent<TextMeshProUGUI>();
                stTmp.text = $"{rowLabels[i]}：0"; stTmp.fontSize = 15f;
                stTmp.alignment = TextAlignmentOptions.MidlineLeft; stTmp.color = cream;
                if (font != null) stTmp.font = font;

                WireProp(so, sliderFields[i], slider);
                WireProp(so, textFields[i],   stTmp);
            }

            // 区切り線・共振診断
            MakeDivider(resultObj, "ResDivider", new Vector2(0f, 0.20f), new Vector2(1f, 0.20f), amber);
            var resDiagTmp = MakeText(FindOrCreate("ResonanceDiag", resultObj),
                new Vector2(0f, 0.20f), new Vector2(1f, 0.27f), new Vector2(16f, 0f), new Vector2(-16f, 0f),
                "─", 14f, dim, TextAlignmentOptions.MidlineLeft, font);

            // ボタン行
            var retryBtn  = MakeResultButton(resultObj, "RetryButton",       "もう一度",
                new Vector2(0.02f, 0.02f), new Vector2(0.33f, 0.18f), new Color(0.22f, 0.16f, 0.08f), font);
            var nextBtn   = MakeResultButton(resultObj, "NextStageButton",   "次の課題へ",
                new Vector2(0.35f, 0.02f), new Vector2(0.65f, 0.18f), new Color(0.18f, 0.38f, 0.14f), font);
            var selectBtn = MakeResultButton(resultObj, "StageSelectButton", "課題一覧へ",
                new Vector2(0.67f, 0.02f), new Vector2(0.98f, 0.18f), new Color(0.22f, 0.16f, 0.08f), font);

            // SerializeField接続
            WireProp(so, "gradeText",         gradeTmp);
            WireProp(so, "resultMessageText", msgTmp);
            WireProp(so, "gradeCommentText",  commentTmp);
            WireProp(so, "resonanceDiagText", resDiagTmp);
            WireProp(so, "retryButton",       retryBtn.GetComponent<Button>());
            WireProp(so, "nextStageButton",   nextBtn.GetComponent<Button>());
            WireProp(so, "stageSelectButton", selectBtn.GetComponent<Button>());
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(result);
        }

        // ─────────────────── JointInfoPanel 内部UI ───────────────────

        private static void BuildJointInfoContent(GameObject infoObj, JointInfoPanel info, TMP_FontAsset font)
        {
            var cream = new Color(0.96f, 0.90f, 0.78f);
            var dim   = new Color(0.55f, 0.45f, 0.30f);
            var amber = new Color(0.62f, 0.42f, 0.12f);

            // 継手名
            var nameTmp = MakeText(FindOrCreate("JointName", infoObj),
                new Vector2(0f, 0.83f), new Vector2(1f, 1f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                "─", 21f, cream, TextAlignmentOptions.MidlineLeft, font);

            // 説明
            var descTmp = MakeText(FindOrCreate("Description", infoObj),
                new Vector2(0f, 0.67f), new Vector2(1f, 0.83f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                "─", 13f, new Color(0.78f, 0.70f, 0.54f), TextAlignmentOptions.TopLeft, font);
            descTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // バー 5本
            string[] barFields  = { "compressionBar",   "tensionBar",   "shearBar",   "bendingBar",   "dampingBar"   };
            string[] lblFields  = { "compressionLabel", "tensionLabel", "shearLabel", "bendingLabel", "dampingLabel" };
            string[] lblTexts   = { "圧縮", "引張", "せん断", "しなり", "減衰" };
            var barColor = new Color(0.52f, 0.33f, 0.10f);
            float rowH = 0.52f / 5f;
            var so = new SerializedObject(info);

            for (int i = 0; i < 5; i++)
            {
                float yMax = 0.66f - i * rowH;
                float yMin = yMax - rowH;

                var row = FindOrCreate($"BarRow{i}", infoObj);
                var rRt = row.GetOrAddComponent<RectTransform>();
                rRt.anchorMin = new Vector2(0f, yMin); rRt.anchorMax = new Vector2(1f, yMax);
                rRt.offsetMin = new Vector2(12f, 2f);  rRt.offsetMax = new Vector2(-12f, -2f);

                var lbl = FindOrCreate("Label", row);
                var lRt = lbl.GetOrAddComponent<RectTransform>();
                lRt.anchorMin = Vector2.zero; lRt.anchorMax = new Vector2(0.32f, 1f);
                lRt.offsetMin = Vector2.zero; lRt.offsetMax = Vector2.zero;
                var lTmp = lbl.GetOrAddComponent<TextMeshProUGUI>();
                lTmp.text = lblTexts[i]; lTmp.fontSize = 13f;
                lTmp.alignment = TextAlignmentOptions.MidlineRight; lTmp.color = dim;
                if (font != null) lTmp.font = font;

                var slider = BuildSlider(row, "Slider", new Vector2(0.34f, 0.2f), new Vector2(1f, 0.8f), barColor);

                WireProp(so, barFields[i], slider);
                WireProp(so, lblFields[i], lTmp);
            }

            // スコアグループ
            var sgObj = FindOrCreate("ScoreGroup", infoObj);
            var sgRt  = sgObj.GetOrAddComponent<RectTransform>();
            sgRt.anchorMin = new Vector2(0f, 0.10f); sgRt.anchorMax = new Vector2(1f, 0.20f);
            sgRt.offsetMin = new Vector2(12f, 0f);   sgRt.offsetMax = new Vector2(-12f, 0f);
            var sgCg = sgObj.GetOrAddComponent<CanvasGroup>();
            sgCg.alpha = 0f;

            var precTmp = MakeText(FindOrCreate("PrecisionScore", sgObj),
                Vector2.zero, new Vector2(0.5f, 1f), Vector2.zero, Vector2.zero,
                "精度：─", 13f, cream, TextAlignmentOptions.MidlineLeft, font);
            var compTmp = MakeText(FindOrCreate("CompatibilityScore", sgObj),
                new Vector2(0.5f, 0f), Vector2.one, Vector2.zero, Vector2.zero,
                "適合：─", 13f, cream, TextAlignmentOptions.MidlineLeft, font);

            // 工人コメント
            var craftTmp = MakeText(FindOrCreate("CraftmanComment", infoObj),
                new Vector2(0f, 0f), new Vector2(1f, 0.10f), new Vector2(12f, 0f), new Vector2(-12f, 0f),
                "「木を組むとは、力の流れを読む技なり」", 11f,
                new Color(0.45f, 0.36f, 0.20f), TextAlignmentOptions.Center, font);
            craftTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // 評価コメント（スコアグループ内）
            var evalObj = FindOrCreate("EvaluationComment", sgObj);
            var evalRt  = evalObj.GetOrAddComponent<RectTransform>();
            evalRt.anchorMin = new Vector2(0f, -1.2f); evalRt.anchorMax = new Vector2(1f, -0.1f);
            evalRt.offsetMin = Vector2.zero; evalRt.offsetMax = Vector2.zero;
            var evalTmp = evalObj.GetOrAddComponent<TextMeshProUGUI>();
            evalTmp.text = "─"; evalTmp.fontSize = 12f;
            evalTmp.alignment = TextAlignmentOptions.MidlineLeft; evalTmp.color = dim;
            evalTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            if (font != null) evalTmp.font = font;

            WireProp(so, "jointNameText",          nameTmp);
            WireProp(so, "descriptionText",        descTmp);
            WireProp(so, "craftmanCommentText",    craftTmp);
            WireProp(so, "precisionScoreText",     precTmp);
            WireProp(so, "compatibilityScoreText", compTmp);
            WireProp(so, "evaluationCommentText",  evalTmp);
            WireProp(so, "scoreGroup",             sgCg);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(info);
        }

        // ─────────────────── TutorialSystem 内部UI ───────────────────

        private static void BuildTutorialContent(GameObject tutObj, TutorialSystem tut, TMP_FontAsset font)
        {
            if (tut == null) return;
            var cream     = new Color(0.96f, 0.90f, 0.78f);
            var panelDark = new Color(0.10f, 0.08f, 0.05f, 0.94f);
            var amber     = new Color(0.62f, 0.42f, 0.12f);

            // TutorialSystem 全体のRectTransform（全画面）
            var tutRt = tutObj.GetOrAddComponent<RectTransform>();
            tutRt.anchorMin = Vector2.zero; tutRt.anchorMax = Vector2.one;
            tutRt.offsetMin = Vector2.zero; tutRt.offsetMax = Vector2.zero;

            // 吹き出しパネル
            var tpObj = FindOrCreate("TooltipPanel", tutObj);
            var tpRt  = tpObj.GetOrAddComponent<RectTransform>();
            tpRt.anchorMin = new Vector2(0.5f, 0f); tpRt.anchorMax = new Vector2(0.5f, 0f);
            tpRt.anchoredPosition = new Vector2(0f, 210f); tpRt.sizeDelta = new Vector2(560f, 180f);
            tpObj.GetOrAddComponent<Image>().color = panelDark;

            // 吹き出しテキスト
            var ttTmp = MakeText(FindOrCreate("TooltipText", tpObj),
                new Vector2(0f, 0.35f), Vector2.one, new Vector2(20f, 0f), new Vector2(-20f, 0f),
                "継手をグリッドに配置しましょう。", 20f, cream, TextAlignmentOptions.TopLeft, font);
            ttTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;

            // スキップボタン
            var skipObj = FindOrCreate("SkipButton", tpObj);
            var skipRt  = skipObj.GetOrAddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(0.04f, 0.05f); skipRt.anchorMax = new Vector2(0.36f, 0.32f);
            skipRt.offsetMin = Vector2.zero; skipRt.offsetMax = Vector2.zero;
            skipObj.GetOrAddComponent<Image>().color = new Color(0.15f, 0.12f, 0.08f);
            skipObj.GetOrAddComponent<Button>();
            MakeText(FindOrCreate("Text", skipObj), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "スキップ", 16f, new Color(0.65f, 0.52f, 0.32f), TextAlignmentOptions.Center, font);

            // 次へボタン
            var nextObj = FindOrCreate("NextButton", tpObj);
            var nextRt  = nextObj.GetOrAddComponent<RectTransform>();
            nextRt.anchorMin = new Vector2(0.64f, 0.05f); nextRt.anchorMax = new Vector2(0.96f, 0.32f);
            nextRt.offsetMin = Vector2.zero; nextRt.offsetMax = Vector2.zero;
            nextObj.GetOrAddComponent<Image>().color = new Color(0.22f, 0.16f, 0.08f);
            nextObj.GetOrAddComponent<Button>();
            MakeText(FindOrCreate("Text", nextObj), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "次へ →", 18f, cream, TextAlignmentOptions.Center, font);

            // 矢印
            var arrowObj = FindOrCreate("ArrowTransform", tutObj);
            var arrowRt  = arrowObj.GetOrAddComponent<RectTransform>();
            arrowRt.anchorMin = new Vector2(0.5f, 0.5f); arrowRt.anchorMax = new Vector2(0.5f, 0.5f);
            arrowRt.anchoredPosition = Vector2.zero; arrowRt.sizeDelta = new Vector2(40f, 40f);
            arrowObj.GetOrAddComponent<Image>().color = amber;
            arrowObj.SetActive(false);

            // SerializeField接続
            var so = new SerializedObject(tut);
            WireProp(so, "tooltipPanel",  tpRt);
            WireProp(so, "tooltipText",   ttTmp);
            WireProp(so, "nextButton",    nextObj.GetComponent<Button>());
            WireProp(so, "skipButton",    skipObj.GetComponent<Button>());
            WireProp(so, "arrowTransform", arrowRt);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(tut);
        }

        // ─────────────────── UIヘルパー (内部) ───────────────────

        private static TextMeshProUGUI MakeText(GameObject obj,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax,
            string text, float size, Color color, TextAlignmentOptions align, TMP_FontAsset font)
        {
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            var tmp = obj.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text = text; tmp.fontSize = size; tmp.color = color; tmp.alignment = align;
            if (font != null) tmp.font = font;
            return tmp;
        }

        private static void MakeDivider(GameObject parent, string name, Vector2 ancMin, Vector2 ancMax, Color color)
        {
            var obj = FindOrCreate(name, parent);
            var rt  = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.anchoredPosition = Vector2.zero; rt.sizeDelta = new Vector2(0f, 1f);
            obj.GetOrAddComponent<Image>().color = new Color(color.r, color.g, color.b, 0.4f);
        }

        private static GameObject MakeResultButton(GameObject parent, string name, string label,
            Vector2 ancMin, Vector2 ancMax, Color btnColor, TMP_FontAsset font)
        {
            var obj = FindOrCreate(name, parent);
            var rt  = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = new Vector2(4f, 4f); rt.offsetMax = new Vector2(-4f, -4f);
            obj.GetOrAddComponent<Image>().color = btnColor;
            obj.GetOrAddComponent<Button>();
            MakeText(FindOrCreate("Text", obj), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                label, 22f, new Color(0.96f, 0.90f, 0.78f), TextAlignmentOptions.Center, font);
            return obj;
        }

        private static Slider BuildSlider(GameObject parent, string name,
            Vector2 ancMin, Vector2 ancMax, Color fillColor)
        {
            var slObj = FindOrCreate(name, parent);
            var slRt  = slObj.GetOrAddComponent<RectTransform>();
            slRt.anchorMin = ancMin; slRt.anchorMax = ancMax;
            slRt.offsetMin = Vector2.zero; slRt.offsetMax = Vector2.zero;

            var bgObj = FindOrCreate("Background", slObj);
            var bgRt  = bgObj.GetOrAddComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
            bgObj.GetOrAddComponent<Image>().color = new Color(0.15f, 0.12f, 0.08f);

            var faObj = FindOrCreate("Fill Area", slObj);
            var faRt  = faObj.GetOrAddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.25f); faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = new Vector2(5f, 0f); faRt.offsetMax = new Vector2(-5f, 0f);

            var fillObj = FindOrCreate("Fill", faObj);
            var fillRt  = fillObj.GetOrAddComponent<RectTransform>();
            fillRt.anchorMin = Vector2.zero; fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.sizeDelta = Vector2.zero; fillRt.anchoredPosition = Vector2.zero;
            fillObj.GetOrAddComponent<Image>().color = fillColor;

            var sl = slObj.GetOrAddComponent<Slider>();
            sl.fillRect = fillRt;
            sl.direction = Slider.Direction.LeftToRight;
            sl.minValue = 0f; sl.maxValue = 1f; sl.value = 0f;
            return sl;
        }

        private static void WireProp(SerializedObject so, string name, Object val)
        {
            var p = so.FindProperty(name);
            if (p != null) p.objectReferenceValue = val;
        }

        // ─────────────────── EventSystem ───────────────────

        private static void SetupEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                // 旧 StandaloneInputModule が残っていたら差し替える
                var old = existing.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);
                existing.gameObject.GetOrAddComponent<InputSystemUIInputModule>();
                return;
            }

            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            // New Input System 用モジュール（StandaloneInputModule はドラッグ不可になる）
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        // ─────────────────── ライティング ───────────────────

        private static void SetupLighting()
        {
            // 工房アンビエント（暗め）
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.13f, 0.10f);

            // Directional Light
            var lightObj = FindOrCreate("DirectionalLight");
            var light = lightObj.GetOrAddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1.0f, 0.88f, 0.65f);
            light.intensity = 1.2f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            lightObj.GetOrAddComponent<UniversalAdditionalLightData>();
        }

        // ─────────────────── UIヘルパー ───────────────────

        private static GameObject CreateUIPanel(GameObject parent, string name,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 anchoredPos, Vector2 size)
        {
            var obj = FindOrCreate(name, parent);
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            var img = obj.GetOrAddComponent<Image>();
            img.color = new Color(0.96f, 0.93f, 0.85f, 0.92f);
            return obj;
        }

        private static GameObject CreateButton(GameObject parent, string name, string label,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 size, Vector2 anchoredPos,
            TMP_FontAsset font = null)
        {
            var obj = FindOrCreate(name, parent);
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;

            obj.GetOrAddComponent<Image>().color = new Color(0.22f, 0.16f, 0.08f);
            obj.GetOrAddComponent<Button>();

            var textObj = FindOrCreate("Text", obj);
            var txtRt = textObj.GetOrAddComponent<RectTransform>();
            txtRt.anchorMin = Vector2.zero;
            txtRt.anchorMax = Vector2.one;
            txtRt.offsetMin = Vector2.zero;
            txtRt.offsetMax = Vector2.zero;

            var tmp = textObj.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 22;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = new Color(0.96f, 0.90f, 0.78f);
            if (font != null) tmp.font = font;

            return obj;
        }

        // ─────────────────── GameObject ヘルパー ───────────────────

        private static GameObject FindOrCreate(string name, GameObject parent = null)
        {
            // 親指定がある場合は子から探す
            if (parent != null)
            {
                var child = parent.transform.Find(name);
                if (child != null) return child.gameObject;
                var newChild = new GameObject(name);
                newChild.transform.SetParent(parent.transform, false);
                return newChild;
            }

            var found = GameObject.Find(name);
            if (found != null) return found;
            return new GameObject(name);
        }
    }

    // ─────────────────── 拡張メソッド ───────────────────

    internal static class GameObjectExtensions
    {
        internal static T GetOrAddComponent<T>(this GameObject go) where T : Component
        {
            var c = go.GetComponent<T>();
            return c != null ? c : go.AddComponent<T>();
        }
    }
}
