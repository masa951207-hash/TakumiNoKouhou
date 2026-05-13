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
    /// ステージ選択シーンの全ヒエラルキーを自動セットアップするEditorスクリプト。
    /// </summary>
    public static class SetupStageSelectScene
    {
        private const string StageDir     = "Assets/TakumiNoKouhou/ScriptableObjects/Stages";
        private const string FontAssetPath = "Assets/Fonts/TMP_Japanese.asset";

        // ─── 木工大工カラーパレット ───
        private static readonly Color BgDeep      = new Color(0.10f, 0.07f, 0.04f);
        private static readonly Color PanelWood   = new Color(0.14f, 0.10f, 0.06f, 0.88f);
        private static readonly Color PanelDark   = new Color(0.10f, 0.08f, 0.05f, 0.92f);
        private static readonly Color AccentAmber = new Color(0.62f, 0.42f, 0.12f);
        private static readonly Color TextCream   = new Color(0.96f, 0.90f, 0.78f);
        private static readonly Color TextWarm    = new Color(0.78f, 0.64f, 0.40f);
        private static readonly Color BtnStart    = new Color(0.52f, 0.33f, 0.10f);

        [MenuItem("匠の工法/ステージ選択シーンセットアップ", priority = 4)]
        public static void Setup()
        {
            SetupSilent();
            EditorUtility.DisplayDialog("完了", "ステージ選択シーンのセットアップが完了しました。", "OK");
        }

        public static void SetupSilent()
        {
            SetupCamera();
            SetupLighting();
            SetupCanvas();
            SetupEventSystem();
            Debug.Log("[匠の工法] ステージ選択シーンセットアップ完了");
        }

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

        private static void SetupLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.15f, 0.12f, 0.09f);
            var lightObj = FindOrCreate("DirectionalLight");
            var light = lightObj.GetOrAddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.88f, 0.65f);
            light.intensity = 0.8f;
            lightObj.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        }

        private static void SetupCanvas()
        {
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);

            var canvasObj = FindOrCreate("Canvas");
            var canvas = canvasObj.GetOrAddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasObj.GetOrAddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f); // 横画面
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.GetOrAddComponent<GraphicRaycaster>();

            // ── 全体背景 ──
            SetImg(FindOrCreate("Background", canvasObj),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero, BgDeep);

            // ── 上端・下端アクセントライン ──
            BuildHLine(canvasObj, "TopAccent",    new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -5f),  10f, AccentAmber);
            BuildHLine(canvasObj, "BottomAccent", new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f,  5f),  10f, AccentAmber);

            // ── ヘッダー ──
            var headerObj = FindOrCreate("Header", canvasObj);
            var headerRt = headerObj.GetOrAddComponent<RectTransform>();
            headerRt.anchorMin = new Vector2(0f, 1f); headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -40f);
            headerRt.sizeDelta = new Vector2(0f, 80f);
            headerObj.GetOrAddComponent<Image>().color = PanelWood;

            BuildText(FindOrCreate("HeaderText", headerObj),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "稽 古 題  ─  鍛 錬 の 場", 34f, TextCream, font);

            // ヘッダー下端ライン
            BuildHLine(headerObj, "HeaderLine",
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, -1f), 2f, AccentAmber);

            // ── メインエリア（ヘッダー下～フッター上）──
            var screenObj = FindOrCreate("StageSelectScreen", canvasObj);
            var screenRt = screenObj.GetOrAddComponent<RectTransform>();
            screenRt.anchorMin = new Vector2(0f, 0.08f); screenRt.anchorMax = new Vector2(1f, 0.93f);
            screenRt.offsetMin = new Vector2(20f, 0f); screenRt.offsetMax = new Vector2(-20f, 0f);
            screenObj.GetOrAddComponent<CanvasGroup>().alpha = 1f;
            screenObj.GetOrAddComponent<Image>().color = new Color(0, 0, 0, 0); // 透明
            var screen = screenObj.GetOrAddComponent<StageSelectScreen>();

            // ── カードコンテナ（左40%）──
            var cardContainerObj = FindOrCreate("CardContainer", screenObj);
            var cardRt = cardContainerObj.GetOrAddComponent<RectTransform>();
            cardRt.anchorMin = new Vector2(0f, 0f); cardRt.anchorMax = new Vector2(0.38f, 1f);
            cardRt.offsetMin = new Vector2(10f, 10f); cardRt.offsetMax = new Vector2(-10f, -10f);
            cardContainerObj.GetOrAddComponent<Image>().color = PanelDark;

            var cardInner = FindOrCreate("CardList", cardContainerObj);
            var cardInnerRt = cardInner.GetOrAddComponent<RectTransform>();
            cardInnerRt.anchorMin = new Vector2(0f, 0f); cardInnerRt.anchorMax = new Vector2(1f, 1f);
            cardInnerRt.offsetMin = new Vector2(10f, 10f); cardInnerRt.offsetMax = new Vector2(-10f, -10f);
            var vl = cardInner.GetOrAddComponent<VerticalLayoutGroup>();
            vl.spacing = 12f;
            vl.childAlignment = TextAnchor.UpperCenter;
            vl.childControlWidth = true;
            vl.childControlHeight = false;
            vl.childForceExpandWidth = true;

            // カードリスト見出し
            BuildText(FindOrCreate("CardListTitle", cardContainerObj),
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, -18f), new Vector2(0f, 36f),
                "─ 課 題 一 覧 ─", 18f, AccentAmber, font);

            // ── 詳細パネル（右60%）──
            var detailObj = FindOrCreate("DetailPanel", screenObj);
            var detailRt = detailObj.GetOrAddComponent<RectTransform>();
            detailRt.anchorMin = new Vector2(0.40f, 0f); detailRt.anchorMax = new Vector2(1f, 1f);
            detailRt.offsetMin = new Vector2(10f, 10f); detailRt.offsetMax = new Vector2(-10f, -10f);
            detailObj.GetOrAddComponent<Image>().color = PanelWood;
            var detailCg = detailObj.GetOrAddComponent<CanvasGroup>();
            detailCg.alpha = 0f; detailCg.interactable = false; detailCg.blocksRaycasts = false;

            // 詳細パネル左アクセント縦線
            var detailLineObj = FindOrCreate("DetailAccent", detailObj);
            var detailLineRt = detailLineObj.GetOrAddComponent<RectTransform>();
            detailLineRt.anchorMin = new Vector2(0f, 0.05f); detailLineRt.anchorMax = new Vector2(0f, 0.95f);
            detailLineRt.anchoredPosition = new Vector2(5f, 0f);
            detailLineRt.sizeDelta = new Vector2(5f, 0f);
            detailLineObj.GetOrAddComponent<Image>().color = AccentAmber;

            var stageNameObj = FindOrCreate("StageName", detailObj);
            BuildText(stageNameObj, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -50f), new Vector2(0f, 90f), "─  課 題 名  ─", 32f, TextCream, font);

            var descObj = FindOrCreate("Description", detailObj);
            BuildText(descObj, new Vector2(0f, 0.35f), new Vector2(1f, 0.88f),
                Vector2.zero, Vector2.zero, "", 20f,
                new Color(0.82f, 0.76f, 0.62f), font);
            descObj.GetComponent<TextMeshProUGUI>().textWrappingMode = TMPro.TextWrappingModes.Normal;
            descObj.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.TopLeft;

            var gradeObj = FindOrCreate("BestGrade", detailObj);
            BuildText(gradeObj, new Vector2(0f, 0.15f), new Vector2(1f, 0.32f),
                Vector2.zero, Vector2.zero, "最高評価：─", 24f, TextWarm, font);

            var startBtnObj = FindOrCreate("StartButton", detailObj);
            var startBtnRt = startBtnObj.GetOrAddComponent<RectTransform>();
            startBtnRt.anchorMin = new Vector2(0.1f, 0.03f); startBtnRt.anchorMax = new Vector2(0.9f, 0.13f);
            startBtnRt.offsetMin = Vector2.zero; startBtnRt.offsetMax = Vector2.zero;
            startBtnObj.GetOrAddComponent<Image>().color = BtnStart;
            startBtnObj.GetOrAddComponent<Button>();
            BuildText(FindOrCreate("Text", startBtnObj),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "この問を解く", 30f, TextCream, font);

            // ── 戻るボタン ──
            var backObj = FindOrCreate("BackButton", canvasObj);
            var backRt = backObj.GetOrAddComponent<RectTransform>();
            backRt.anchorMin = new Vector2(0f, 1f); backRt.anchorMax = new Vector2(0f, 1f);
            backRt.anchoredPosition = new Vector2(90f, -40f);
            backRt.sizeDelta = new Vector2(150f, 55f);
            backObj.GetOrAddComponent<Image>().color = new Color(0.22f, 0.16f, 0.08f, 0.9f);
            backObj.GetOrAddComponent<Button>();
            BuildText(FindOrCreate("Text", backObj),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero,
                "← 戻る", 22f, TextCream, font);

            // ── StageSelectScreen SerializeField 接続 ──
            var so = new SerializedObject(screen);
            WireProp(so, "cardContainer",     cardInner.transform);
            WireProp(so, "detailPanel",       detailCg);
            WireProp(so, "detailStageName",   stageNameObj.GetComponent<TextMeshProUGUI>());
            WireProp(so, "detailDescription", descObj.GetComponent<TextMeshProUGUI>());
            WireProp(so, "detailBestGrade",   gradeObj.GetComponent<TextMeshProUGUI>());
            WireProp(so, "startButton",       startBtnObj.GetComponent<Button>());
            WireProp(so, "backButton",        backObj.GetComponent<Button>());
            SetStageArray(so, "allStages");
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(screen);
        }

        // ─────────────────── ヘルパー ───────────────────

        private static void BuildText(GameObject obj,
            Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, Vector2 sizeDelta,
            string text, float fontSize, Color color, TMP_FontAsset font)
        {
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            if (sizeDelta != Vector2.zero) rt.sizeDelta = sizeDelta;
            if (ancPos    != Vector2.zero) rt.anchoredPosition = ancPos;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            var tmp = obj.GetOrAddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            if (font != null) tmp.font = font;
        }

        private static void SetImg(GameObject obj,
            Vector2 ancMin, Vector2 ancMax, Vector2 offMin, Vector2 offMax, Color col)
        {
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.offsetMin = offMin; rt.offsetMax = offMax;
            obj.GetOrAddComponent<Image>().color = col;
        }

        private static void BuildHLine(GameObject parent, string name,
            Vector2 ancMin, Vector2 ancMax, Vector2 ancPos, float height, Color col)
        {
            var obj = FindOrCreate(name, parent);
            var rt = obj.GetOrAddComponent<RectTransform>();
            rt.anchorMin = ancMin; rt.anchorMax = ancMax;
            rt.anchoredPosition = ancPos;
            rt.sizeDelta = new Vector2(0f, height);
            obj.GetOrAddComponent<Image>().color = col;
        }

        private static void SetStageArray(SerializedObject so, string propName)
        {
            var guids = AssetDatabase.FindAssets("t:PuzzleStageData", new[] { StageDir });
            var prop = so.FindProperty(propName);
            if (prop == null || guids.Length == 0) return;
            prop.arraySize = guids.Length;
            for (int i = 0; i < guids.Length; i++)
            {
                var stage = AssetDatabase.LoadAssetAtPath<PuzzleStageData>(
                    AssetDatabase.GUIDToAssetPath(guids[i]));
                prop.GetArrayElementAtIndex(i).objectReferenceValue = stage;
            }
        }

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

        private static void WireProp(SerializedObject so, string name, Object val)
        {
            var p = so.FindProperty(name);
            if (p != null) p.objectReferenceValue = val;
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
