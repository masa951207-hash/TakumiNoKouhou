using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 墨付け帳のような和風ステージ選択画面を管理するコンポーネント。
    /// 各ステージをカード形式で表示し、解放状況・過去評価を反映する。
    /// </summary>
    public class StageSelectScreen : WashiPaperPanel
    {
        [Header("参照")]
        [Tooltip("ステージデータの一覧（Inspector登録）")]
        [SerializeField] private PuzzleStageData[] allStages;

        [Tooltip("ステージカードのプレハブ")]
        [SerializeField] private StageCard stageCardPrefab;

        [Tooltip("ステージカードを並べるコンテナ")]
        [SerializeField] private Transform cardContainer;

        [Header("詳細パネル")]
        [Tooltip("選択中ステージの詳細表示パネル")]
        [SerializeField] private CanvasGroup detailPanel;

        [Tooltip("詳細パネルのステージ名テキスト")]
        [SerializeField] private TextMeshProUGUI detailStageName;

        [Tooltip("詳細パネルの説明テキスト")]
        [SerializeField] private TextMeshProUGUI detailDescription;

        [Tooltip("詳細パネルの評価ランクテキスト")]
        [SerializeField] private TextMeshProUGUI detailBestGrade;

        [Tooltip("ステージ開始ボタン")]
        [SerializeField] private Button startButton;

        [Tooltip("タイトルへ戻るボタン")]
        [SerializeField] private Button backButton;

        [Header("ゲームプレイシーン名")]
        [SerializeField] private string gameplaySceneName = "Gameplay";

        private PuzzleStageData _selectedStage;
        private List<StageCard> _cards = new();

        protected override void Awake()
        {
            base.Awake();

            if (detailPanel != null)
            {
                detailPanel.alpha = 0f;
                detailPanel.interactable = false;
                detailPanel.blocksRaycasts = false;
            }

            if (startButton != null)
                startButton.onClick.AddListener(OnStartButtonClicked);

            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("Title"));
        }

        void Start()
        {
            BuildCards();
        }

        private void BuildCards()
        {
            if (cardContainer == null) return;

            foreach (Transform child in cardContainer)
                Destroy(child.gameObject);

            _cards.Clear();

            foreach (var stage in allStages)
            {
                if (stage == null) continue;

                bool isUnlocked = PuzzleClearSystem.IsStageUnlocked(stage);
                StructuralGrade savedGrade = PuzzleClearSystem.LoadSavedGrade(stage.stageNumber);

                if (stageCardPrefab != null)
                {
                    var card = Instantiate(stageCardPrefab, cardContainer);
                    card.Setup(stage, isUnlocked, savedGrade, OnCardClicked);
                    _cards.Add(card);
                }
                else
                {
                    // プレハブなしフォールバック：シンプルなボタンをランタイム生成
                    var btnGO = new GameObject($"Stage{stage.stageNumber}Card");
                    btnGO.transform.SetParent(cardContainer, false);

                    var rt = btnGO.AddComponent<RectTransform>();
                    rt.sizeDelta = new Vector2(0f, 90f);

                    var img = btnGO.AddComponent<Image>();
                    img.color = isUnlocked
                        ? new Color(0.22f, 0.18f, 0.12f)
                        : new Color(0.12f, 0.10f, 0.07f, 0.6f);

                    var btn = btnGO.AddComponent<Button>();
                    btn.interactable = isUnlocked;

                    var textGO = new GameObject("Text");
                    textGO.transform.SetParent(btnGO.transform, false);
                    var textRt = textGO.AddComponent<RectTransform>();
                    textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
                    textRt.offsetMin = new Vector2(16f, 0f); textRt.offsetMax = new Vector2(-16f, 0f);
                    var tmp = textGO.AddComponent<TextMeshProUGUI>();
                    tmp.text = isUnlocked
                        ? $"第{stage.stageNumber}問　{stage.stageName}"
                        : $"第{stage.stageNumber}問　???（前問をクリアして解放）";
                    tmp.fontSize = 26f;
                    tmp.color = isUnlocked ? new Color(0.92f, 0.88f, 0.78f) : new Color(0.45f, 0.42f, 0.36f);
                    tmp.alignment = TextAlignmentOptions.MidlineLeft;

                    var capturedStage = stage;
                    btn.onClick.AddListener(() => OnCardClicked(capturedStage));
                }
            }
        }

        private void OnCardClicked(PuzzleStageData stage)
        {
            if (!PuzzleClearSystem.IsStageUnlocked(stage)) return;

            _selectedStage = stage;
            ShowDetail(stage);
        }

        private void ShowDetail(PuzzleStageData stage)
        {
            if (detailPanel == null) return;

            if (detailStageName != null)
                detailStageName.text = $"第{stage.stageNumber}問　{stage.stageName}";

            if (detailDescription != null)
                detailDescription.text = stage.stageDescription;

            StructuralGrade savedGrade = PuzzleClearSystem.LoadSavedGrade(stage.stageNumber);
            if (detailBestGrade != null)
            {
                string gradeStr = savedGrade == StructuralGrade.F && !HasBeenPlayed(stage.stageNumber)
                    ? "未挑戦"
                    : GradeToJapanese(savedGrade);
                detailBestGrade.text = $"最高評価：{gradeStr}";
            }

            if (startButton != null)
                startButton.interactable = PuzzleClearSystem.IsStageUnlocked(stage);

            detailPanel.alpha = 1f;
            detailPanel.interactable = true;
            detailPanel.blocksRaycasts = true;
        }

        private void OnStartButtonClicked()
        {
            if (_selectedStage == null) return;

            // 選択ステージをシーン間で引き渡す
            SelectedStageHolder.Stage = _selectedStage;
            SceneManager.LoadScene(gameplaySceneName);
        }

        private bool HasBeenPlayed(int stageNumber)
        {
            return PlayerPrefs.HasKey($"Stage_{stageNumber}_Grade");
        }

        private string GradeToJapanese(StructuralGrade grade)
        {
            return grade switch
            {
                StructuralGrade.S => "S（匠）",
                StructuralGrade.A => "A（上手）",
                StructuralGrade.B => "B（良）",
                StructuralGrade.C => "C（可）",
                StructuralGrade.D => "D（不可）",
                StructuralGrade.F => "F（倒壊）",
                _ => "—"
            };
        }
    }

    /// <summary>シーン間でステージデータを引き渡す静的ホルダー</summary>
    public static class SelectedStageHolder
    {
        public static PuzzleStageData Stage { get; set; }
    }

    /// <summary>ステージ選択カード1枚のUIコンポーネント</summary>
    public class StageCard : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI stageNumberText;
        [SerializeField] private TextMeshProUGUI stageNameText;
        [SerializeField] private Image thumbnail;
        [SerializeField] private TextMeshProUGUI gradeText;
        [SerializeField] private GameObject lockOverlay;
        [SerializeField] private Button button;

        public void Setup(
            PuzzleStageData data,
            bool isUnlocked,
            StructuralGrade savedGrade,
            System.Action<PuzzleStageData> onClick)
        {
            if (stageNumberText != null) stageNumberText.text = $"第{data.stageNumber}問";
            if (stageNameText != null) stageNameText.text = data.stageName;
            if (thumbnail != null && data.thumbnail != null) thumbnail.sprite = data.thumbnail;

            if (gradeText != null)
                gradeText.text = isUnlocked && PlayerPrefs.HasKey($"Stage_{data.stageNumber}_Grade")
                    ? savedGrade.ToString()
                    : "—";

            if (lockOverlay != null) lockOverlay.SetActive(!isUnlocked);

            button.interactable = isUnlocked;
            button.onClick.AddListener(() => onClick?.Invoke(data));
        }
    }
}
