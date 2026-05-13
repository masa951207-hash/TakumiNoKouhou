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
    /// タイトルシーンの全ヒエラルキーを自動セットアップするEditorスクリプト。
    /// メニュー → 匠の工法 → タイトルシーンセットアップ から実行。
    /// </summary>
    public static class SetupTitleScene
    {
        private const string FontAssetPath = "Assets/Fonts/TMP_Japanese.asset";

        // ─── 木工大工カラーパレット ───
        private static readonly Color BgDeep       = new Color(0.10f, 0.07f, 0.04f); // 深い漆黒木目
        private static readonly Color PanelWood    = new Color(0.14f, 0.10f, 0.06f, 0.85f); // ダークオーク
        private static readonly Color AccentAmber  = new Color(0.62f, 0.42f, 0.12f); // 琥珀・ケヤキ
        private static readonly Color TextCream    = new Color(0.96f, 0.90f, 0.78f); // 和紙クリーム
        private static readonly Color TextWarm     = new Color(0.78f, 0.64f, 0.40f); // 暖色テキスト
        private static readonly Color TextDim      = new Color(0.50f, 0.40f, 0.22f); // 暗色テキスト
        private static readonly Color BtnStart     = new Color(0.52f, 0.33f, 0.10f); // スタート茶橙
        private static readonly Color BtnQuit      = new Color(0.22f, 0.16f, 0.08f); // 終了ダーク

        [MenuItem("匠の工法/タイトルシーンセットアップ", priority = 3)]
        public static void Setup()
        {
            SetupSilent();
            EditorUtility.DisplayDialog("完了", "タイトルシーンのセットアップが完了しました。", "OK");
        }

        public static void SetupSilent()
        {
            SetupCamera();
            SetupLighting();
            SetupCanvas();
            SetupEventSystem();
            SetupController();
            SetupAudioManager();
            Debug.Log("[匠の工法] タイトルシーンセットアップ完了");
        }

        private static void SetupAudioManager()
        {
            var go = FindOrCreate("GameAudioManager");
            SetupFontsAndAudio.WireAudioToManager(go);
        }

        // ─────────────────── カメラ ───────────────────

        private static void SetupCamera()
        {
            var camObj = FindOrCreate("Main Camera");
            camObj.tag = "MainCamera";
            var cam = camObj.GetOrAddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = BgDeep;
            cam.transform.position = new Vector3(0f, 0f, -10f);
            cam.transform.rotation = Quaternion.identity;
            camObj.GetOrAddComponent<UniversalAdditionalCameraData>();
        }

        // ─────────────────── ライティング ───────────────────

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.18f, 0.14f, 0.08f);
            var lightObj = FindOrCreate("DirectionalLight");
            var light = lightObj.GetOrAddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.65f);
            light.intensity = 0.8f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        // ─────────────────── キャンバス ───────────────────

        private static void SetupCanvas()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            var canvasObj = FindOrCreate("Canvas");
            var canvas = canvasObj.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;

            var scaler = canvasObj.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 横画面（PC/デスクトップ）
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.GetOrAddComponent<GraphicRaycaster>();

            BuildBackground(canvasObj);
            BuildLeftPanel(canvasObj, font);    // タイトル + ルール
            BuildRightPanel(canvasObj, font);   // メニューボタン
            BuildFooter(canvasObj, font);       // 操作方法
            BuildSkipButton(canvasObj, font);
        }

        // ── 背景装飾 ──────────────────────────────────────────

        private static void BuildBackground(GameObject canvas)
        {
            // 全体背景
            SetImg(FindOrCreate("Background", canvas),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, BgDeep);

            // 上端アクセントライン
            var topObj = FindOrCreate("TopAccent", canvas);
            var topRt = topObj.GetOrAddComponent<RectTransform>();
            topRt.anchorMin = new Vector2(0f, 1f); topRt.anchorMax = new Vector2(1f, 1f);
            topRt.anchoredPosition = new Vector2(0f, -5f);
            topRt.sizeDelta = new Vector2(0f, 10f);
            topObj.GetOrAddComponent<Image>().color = AccentAmber;

            // 下端アクセントライン
            var botObj = FindOrCreate("BottomAccent", canvas);
            var botRt = botObj.GetOrAddComponent<RectTransform>();
            botRt.anchorMin = new Vector2(0f, 0f); botRt.anchorMax = new Vector2(1f, 0f);
            botRt.anchoredPosition = new Vector2(0f, 5f);
            botRt.sizeDelta = new Vector2(0f, 10f);
            botObj.GetOrAddComponent<Image>().color = AccentAmber;

            // 左右中央の縦区切り線（大工の墨線イメージ）
            var divObj = FindOrCreate("CenterDivider", canvas);
            var divRt = divObj.GetOrAddComponent<RectTransform>();
            divRt.anchorMin = new Vector2(0.5f, 0.12f); divRt.anchorMax = new Vector2(0.5f, 0.93f);
            divRt.anchoredPosition = Vector2.zero;
            divRt.sizeDelta = new Vector2(2f, 0f);
            divObj.GetOrAddComponent<Image>().color = new Color(AccentAmber.r, AccentAmber.g, AccentAmber.b, 0.45f);
        }

        // ── 左パネル: タイトル + ルール説明 ──────────────────

        private static void BuildLeftPanel(GameObject canvas, TMP_FontAsset font)
        {
            var panel = FindOrCreate("LogoArea", canvas);  // InkCalligraphyAnimation の参照名を維持
            var rt = panel.GetOrAddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0.12f); rt.anchorMax = new Vector2(0.48f, 0.95f);
            rt.offsetMin = new Vector2(50f, 0f); rt.offsetMax = new Vector2(-20f, 0f);

            // ── タイトル ──
            var titleObj = FindOrCreate("TitleText", panel);
            var titleRt = titleObj.GetOrAddComponent<RectTransform>();
            titleRt.anchorMin = new Vector2(0f, 0.72f); titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.offsetMin = Vector2.zero; titleRt.offsetMax = Vector2.zero;
            var titleTmp = titleObj.GetOrAddComponent<TextMeshProUGUI>();
            titleTmp.text = "匠の工法";
            titleTmp.fontSize = 100;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = TextCream;
            if (font != null) titleTmp.font = font;

            // ── 副題 ──
            var subObj = FindOrCreate("SubTitleText", panel);
            var subRt = subObj.GetOrAddComponent<RectTransform>();
            subRt.anchorMin = new Vector2(0f, 0.60f); subRt.anchorMax = new Vector2(1f, 0.71f);
            subRt.offsetMin = Vector2.zero; subRt.offsetMax = Vector2.zero;
            var subTmp = subObj.GetOrAddComponent<TextMeshProUGUI>();
            subTmp.text = "柱を立て、梁を渡し、継手を刻め。";
            subTmp.fontSize = 24;
            subTmp.alignment = TextAlignmentOptions.Left;
            subTmp.color = TextWarm;
            if (font != null) subTmp.font = font;

            // InkCalligraphyAnimation はロゴエリアに付与
            panel.GetOrAddComponent<InkCalligraphyAnimation>();

            // ── 区切り線 ──
            var lineObj = FindOrCreate("RuleSeparator", panel);
            var lineRt = lineObj.GetOrAddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 0.58f); lineRt.anchorMax = new Vector2(0.9f, 0.58f);
            lineRt.anchoredPosition = Vector2.zero;
            lineRt.sizeDelta = new Vector2(0f, 2f);
            lineObj.GetOrAddComponent<Image>().color = AccentAmber;

            // ── ルール説明パネル ──
            var rulesPanel = FindOrCreate("RulesPanel", panel);
            var rulesPanelRt = rulesPanel.GetOrAddComponent<RectTransform>();
            rulesPanelRt.anchorMin = new Vector2(0f, 0.03f); rulesPanelRt.anchorMax = new Vector2(1f, 0.56f);
            rulesPanelRt.offsetMin = Vector2.zero; rulesPanelRt.offsetMax = Vector2.zero;
            rulesPanel.GetOrAddComponent<Image>().color = PanelWood;

            // 左端の木目アクセント縦線
            var woodLineObj = FindOrCreate("WoodAccentLine", rulesPanel);
            var woodLineRt = woodLineObj.GetOrAddComponent<RectTransform>();
            woodLineRt.anchorMin = new Vector2(0f, 0.05f); woodLineRt.anchorMax = new Vector2(0f, 0.95f);
            woodLineRt.anchoredPosition = new Vector2(6f, 0f);
            woodLineRt.sizeDelta = new Vector2(5f, 0f);
            woodLineObj.GetOrAddComponent<Image>().color = AccentAmber;

            var ruleTitleObj = FindOrCreate("RulesTitle", rulesPanel);
            var ruleTitleRt = ruleTitleObj.GetOrAddComponent<RectTransform>();
            ruleTitleRt.anchorMin = new Vector2(0f, 0.84f); ruleTitleRt.anchorMax = new Vector2(1f, 1f);
            ruleTitleRt.offsetMin = new Vector2(22f, 0f); ruleTitleRt.offsetMax = new Vector2(-12f, 0f);
            var ruleTitleTmp = ruleTitleObj.GetOrAddComponent<TextMeshProUGUI>();
            ruleTitleTmp.text = "─── 遊 び 方 ───";
            ruleTitleTmp.fontSize = 19;
            ruleTitleTmp.alignment = TextAlignmentOptions.Left;
            ruleTitleTmp.color = AccentAmber;
            if (font != null) ruleTitleTmp.font = font;

            var rulesTextObj = FindOrCreate("RulesText", rulesPanel);
            var rulesTextRt = rulesTextObj.GetOrAddComponent<RectTransform>();
            rulesTextRt.anchorMin = new Vector2(0f, 0f); rulesTextRt.anchorMax = new Vector2(1f, 0.83f);
            rulesTextRt.offsetMin = new Vector2(22f, 10f); rulesTextRt.offsetMax = new Vector2(-12f, 0f);
            var rulesTextTmp = rulesTextObj.GetOrAddComponent<TextMeshProUGUI>();
            rulesTextTmp.text =
                "木材をグリッドに配置して建物の骨組みを完成させましょう。\n\n" +
                "継手（つなぎ目）が正確なほど構造の強度が増します。\n\n" +
                "「地震テスト」で荷重に耐えたらクリア！\n" +
                "評価は  S・A・B・C・D  の五段階。名工を目指せ！";
            rulesTextTmp.fontSize = 18;
            rulesTextTmp.lineSpacing = 4f;
            rulesTextTmp.alignment = TextAlignmentOptions.TopLeft;
            rulesTextTmp.color = new Color(0.85f, 0.78f, 0.64f);
            rulesTextTmp.textWrappingMode = TMPro.TextWrappingModes.Normal;
            if (font != null) rulesTextTmp.font = font;
        }

        // ── 右パネル: メニューボタン ──────────────────────────

        private static void BuildRightPanel(GameObject canvas, TMP_FontAsset font)
        {
            var menuPanelObj = FindOrCreate("MainMenuPanel", canvas);
            var menuRt = menuPanelObj.GetOrAddComponent<RectTransform>();
            menuRt.anchorMin = new Vector2(0.52f, 0.22f); menuRt.anchorMax = new Vector2(1f, 0.95f);
            menuRt.offsetMin = new Vector2(20f, 0f); menuRt.offsetMax = new Vector2(-60f, 0f);

            var menuGroup = menuPanelObj.GetOrAddComponent<CanvasGroup>();
            menuGroup.alpha = 0f;
            menuGroup.interactable = false;
            menuGroup.blocksRaycasts = false;

            // 右パネル背景（透明）
            var panelImg = menuPanelObj.GetOrAddComponent<Image>();
            panelImg.color = new Color(0, 0, 0, 0);

            // 装飾テキスト「木組みの道」
            var decorObj = FindOrCreate("PanelDecorLabel", menuPanelObj);
            var decorRt = decorObj.GetOrAddComponent<RectTransform>();
            decorRt.anchorMin = new Vector2(0f, 0.82f); decorRt.anchorMax = new Vector2(1f, 1f);
            decorRt.offsetMin = Vector2.zero; decorRt.offsetMax = Vector2.zero;
            var decorTmp = decorObj.GetOrAddComponent<TextMeshProUGUI>();
            decorTmp.text = "木  組  み  の  道";
            decorTmp.fontSize = 26;
            decorTmp.alignment = TextAlignmentOptions.Center;
            decorTmp.color = TextDim;
            if (font != null) decorTmp.font = font;

            // ボタンコンテナ
            var btnContainer = FindOrCreate("ButtonContainer", menuPanelObj);
            var btnContRt = btnContainer.GetOrAddComponent<RectTransform>();
            btnContRt.anchorMin = new Vector2(0.05f, 0.25f); btnContRt.anchorMax = new Vector2(0.95f, 0.80f);
            btnContRt.offsetMin = Vector2.zero; btnContRt.offsetMax = Vector2.zero;

            var vl = btnContainer.GetOrAddComponent<VerticalLayoutGroup>();
            vl.spacing = 30f;
            vl.childAlignment = TextAnchor.MiddleCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;

            CreateMenuButton(btnContainer, "StartButton", "木 を 組 む", BtnStart, font);
            CreateMenuButton(btnContainer, "QuitButton",  "立  つ",       BtnQuit, font);

            // 職人格言（下部）
            var quoteObj = FindOrCreate("CraftQuote", menuPanelObj);
            var quoteRt = quoteObj.GetOrAddComponent<RectTransform>();
            quoteRt.anchorMin = new Vector2(0f, 0f); quoteRt.anchorMax = new Vector2(1f, 0.22f);
            quoteRt.offsetMin = new Vector2(10f, 0f); quoteRt.offsetMax = new Vector2(-10f, 0f);
            var quoteTmp = quoteObj.GetOrAddComponent<TextMeshProUGUI>();
            quoteTmp.text = "「 木 は 正 直 に 組 め 。 力 は 嘘 を つ か ぬ 」";
            quoteTmp.fontSize = 15;
            quoteTmp.alignment = TextAlignmentOptions.Center;
            quoteTmp.color = new Color(0.42f, 0.33f, 0.18f);
            if (font != null) quoteTmp.font = font;
        }

        // ── フッター: 操作方法 ────────────────────────────────

        private static void BuildFooter(GameObject canvas, TMP_FontAsset font)
        {
            var footerObj = FindOrCreate("FooterPanel", canvas);
            var footerRt = footerObj.GetOrAddComponent<RectTransform>();
            footerRt.anchorMin = new Vector2(0f, 0f); footerRt.anchorMax = new Vector2(1f, 0.12f);
            footerRt.offsetMin = Vector2.zero; footerRt.offsetMax = Vector2.zero;
            footerObj.GetOrAddComponent<Image>().color = new Color(0.12f, 0.09f, 0.05f, 0.92f);

            // フッター上端ライン
            var lineObj = FindOrCreate("FooterTopLine", footerObj);
            var lineRt = lineObj.GetOrAddComponent<RectTransform>();
            lineRt.anchorMin = new Vector2(0f, 1f); lineRt.anchorMax = new Vector2(1f, 1f);
            lineRt.anchoredPosition = new Vector2(0f, -1f);
            lineRt.sizeDelta = new Vector2(0f, 2f);
            lineObj.GetOrAddComponent<Image>().color = AccentAmber;

            // 操作ラベル見出し
            var ctrlTitleObj = FindOrCreate("ControlsLabel", footerObj);
            var ctrlTitleRt = ctrlTitleObj.GetOrAddComponent<RectTransform>();
            ctrlTitleRt.anchorMin = new Vector2(0f, 0.55f); ctrlTitleRt.anchorMax = new Vector2(1f, 1f);
            ctrlTitleRt.offsetMin = new Vector2(40f, 0f); ctrlTitleRt.offsetMax = new Vector2(-40f, 0f);
            var ctrlTitleTmp = ctrlTitleObj.GetOrAddComponent<TextMeshProUGUI>();
            ctrlTitleTmp.text =
                "【 操 作 方 法 】　" +
                "ドラッグ＆ドロップ : 木材を配置　／　" +
                "右クリック / R : 木材を回転　／　" +
                "テストボタン : 地震テスト開始　／　" +
                "ESC : タイトルに戻る";
            ctrlTitleTmp.fontSize = 17;
            ctrlTitleTmp.alignment = TextAlignmentOptions.Center;
            ctrlTitleTmp.color = TextWarm;
            if (font != null) ctrlTitleTmp.font = font;

            // バージョン表記
            var verObj = FindOrCreate("VersionText", footerObj);
            var verRt = verObj.GetOrAddComponent<RectTransform>();
            verRt.anchorMin = new Vector2(1f, 0f); verRt.anchorMax = new Vector2(1f, 0.5f);
            verRt.anchoredPosition = new Vector2(-20f, 0f);
            verRt.sizeDelta = new Vector2(200f, 0f);
            var verTmp = verObj.GetOrAddComponent<TextMeshProUGUI>();
            verTmp.text = "ver 0.1.0";
            verTmp.fontSize = 14;
            verTmp.alignment = TextAlignmentOptions.Right;
            verTmp.color = TextDim;
            if (font != null) verTmp.font = font;
        }

        // ── スキップボタン ────────────────────────────────────

        private static void BuildSkipButton(GameObject canvas, TMP_FontAsset font)
        {
            var skipGroupObj = FindOrCreate("SkipButtonGroup", canvas);
            var skipRt = skipGroupObj.GetOrAddComponent<RectTransform>();
            skipRt.anchorMin = new Vector2(0f, 0f); skipRt.anchorMax = new Vector2(0f, 0f);
            skipRt.anchoredPosition = new Vector2(120f, 80f);
            skipRt.sizeDelta = new Vector2(200f, 55f);

            var skipGroup = skipGroupObj.GetOrAddComponent<CanvasGroup>();
            skipGroup.alpha = 0f;
            skipGroup.interactable = false;

            skipGroupObj.GetOrAddComponent<Button>();
            skipGroupObj.GetOrAddComponent<Image>().color = new Color(0.18f, 0.13f, 0.07f, 0.75f);

            var skipTextObj = FindOrCreate("Text", skipGroupObj);
            var skipTextRt = skipTextObj.GetOrAddComponent<RectTransform>();
            skipTextRt.anchorMin = Vector2.zero; skipTextRt.anchorMax = Vector2.one;
            skipTextRt.offsetMin = Vector2.zero; skipTextRt.offsetMax = Vector2.zero;
            var skipTmp = skipTextObj.GetOrAddComponent<TextMeshProUGUI>();
            skipTmp.text = "スキップ";
            skipTmp.fontSize = 22;
            skipTmp.alignment = TextAlignmentOptions.Center;
            skipTmp.color = TextWarm;
            if (font != null) skipTmp.font = font;
        }

        // ─────────────────── ボタン生成ヘルパー ───────────────────

        private static GameObject CreateMenuButton(GameObject parent, string name, string label,
            Color color, TMP_FontAsset font = null)
        {
            var obj = FindOrCreate(name, parent);
            obj.GetOrAddComponent<RectTransform>().sizeDelta = new Vector2(0f, 90f);
            obj.GetOrAddComponent<Image>().color = color;
            obj.GetOrAddComponent<Button>();

            var textObj = FindOrCreate("Text", obj);
            var textRt = textObj.GetOrAddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = Vector2.zero; textRt.offsetMax = Vector2.zero;
            var tmp = textObj.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 38;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = TextCream;
            if (font != null) tmp.font = font;
            return obj;
        }

        private static void SetImg(GameObject obj,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Color col)
        {
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            obj.GetOrAddComponent<Image>().color = col;
        }

        // ─────────────────── EventSystem ───────────────────

        private static void SetupEventSystem()
        {
            var existing = Object.FindAnyObjectByType<EventSystem>();
            if (existing != null)
            {
                var old = existing.GetComponent<StandaloneInputModule>();
                if (old != null) Object.DestroyImmediate(old);
                existing.gameObject.GetOrAddComponent<InputSystemUIInputModule>();
                return;
            }
            var esObj = new GameObject("EventSystem");
            esObj.AddComponent<EventSystem>();
            esObj.AddComponent<InputSystemUIInputModule>();
        }

        // ─────────────────── TitleSceneController ───────────────────

        private static void SetupController()
        {
            var ctrlObj = FindOrCreate("TitleController");
            var ctrl = ctrlObj.GetOrAddComponent<TitleSceneController>();
            var canvasObj = GameObject.Find("Canvas");
            if (canvasObj == null) return;

            var so = new SerializedObject(ctrl);
            var menuPanelGO = canvasObj.transform.Find("MainMenuPanel")?.GetComponent<CanvasGroup>();
            var skipGroupGO = canvasObj.transform.Find("SkipButtonGroup")?.GetComponent<CanvasGroup>();
            var inkAnimGO   = canvasObj.transform.Find("LogoArea")?.GetComponent<InkCalligraphyAnimation>();

            SetProp(so, "mainMenuPanel",   menuPanelGO);
            SetProp(so, "skipButtonGroup", skipGroupGO);
            SetProp(so, "inkAnimation",    inkAnimGO);

            // タイミング強制上書き（既存シーンの古い値を更新する）
            SetFloat(so, "initialDarkDuration",  0.3f);
            SetFloat(so, "morningLightDuration",  1.0f);
            SetFloat(so, "beforeInkDelay",        0.1f);
            SetFloat(so, "menuFadeInDuration",    0.4f);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);

            // InkCalligraphyAnimation のタイミングも強制上書き
            if (inkAnimGO != null)
            {
                var inkSo = new SerializedObject(inkAnimGO);
                SetFloat(inkSo, "inkStrokeSpeed",       0.5f);
                SetFloat(inkSo, "holdDuration",         0.15f);
                SetFloat(inkSo, "subTitleFadeDuration", 0.3f);
                inkSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(inkAnimGO);
            }

            // StartButton: MainMenuPanel/ButtonContainer/StartButton
            var startBtnGO = canvasObj.transform
                .Find("MainMenuPanel/ButtonContainer/StartButton")?.GetComponent<Button>();
            if (startBtnGO != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    startBtnGO.onClick, ctrl.OnStartButtonPressed);
                EditorUtility.SetDirty(startBtnGO);
            }

            var skipBtnGO = canvasObj.transform.Find("SkipButtonGroup")?.GetComponent<Button>();
            if (skipBtnGO != null)
            {
                UnityEditor.Events.UnityEventTools.AddPersistentListener(
                    skipBtnGO.onClick, ctrl.OnSkipButtonPressed);
                EditorUtility.SetDirty(skipBtnGO);
            }

            // QuitButton: SerializeField接続（WebGLでは実行時に自動非表示）
            var quitBtnGO = canvasObj.transform
                .Find("MainMenuPanel/ButtonContainer/QuitButton")?.GetComponent<Button>();
            SetProp(so, "quitButton", quitBtnGO);
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(ctrl);
        }

        // ─────────────────── ヘルパー ───────────────────

        private static void SetProp(SerializedObject so, string name, Object val)
        {
            var p = so.FindProperty(name);
            if (p != null) p.objectReferenceValue = val;
        }

        private static void SetFloat(SerializedObject so, string name, float val)
        {
            var p = so.FindProperty(name);
            if (p != null) p.floatValue = val;
        }

        private static GameObject FindOrCreate(string name, GameObject parent = null)
        {
            if (parent != null)
            {
                var child = parent.transform.Find(name);
                if (child != null) return child.gameObject;
                var nc = new GameObject(name);
                nc.transform.SetParent(parent.transform, false);
                return nc;
            }
            var found = GameObject.Find(name);
            return found != null ? found : new GameObject(name);
        }
    }
}
