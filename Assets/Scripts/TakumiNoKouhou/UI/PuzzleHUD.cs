using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// パズルプレイ中のHUDを管理するコンポーネント。
    /// 選択中の継手種別・残り部材数・力の流れON/OFF・地震テストボタンを提供する。
    /// </summary>
    public class PuzzleHUD : WashiPaperPanel
    {
        [Header("継手選択UI")]
        [Tooltip("継手種別ボタンのグループ")]
        [SerializeField] private Transform jointButtonContainer;

        [Tooltip("継手種別ボタンのプレハブ")]
        [SerializeField] private JointTypeButton jointButtonPrefab;

        [Tooltip("現在選択中の継手種別ラベル")]
        [SerializeField] private TextMeshProUGUI selectedJointLabel;

        [Tooltip("現在選択中の継手アイコン")]
        [SerializeField] private Image selectedJointIcon;

        [Header("在庫表示")]
        [Tooltip("残り部材数テキスト（例：桁 × 3）")]
        [SerializeField] private Transform inventoryContainer;

        [Tooltip("在庫エントリプレハブ")]
        [SerializeField] private InventoryEntryUI inventoryEntryPrefab;

        [Header("トグルボタン")]
        [Tooltip("力の流れ可視化ON/OFFボタン")]
        [SerializeField] private Button forceFlowToggleButton;

        [Tooltip("力の流れ表示中のボタンラベルテキスト")]
        [SerializeField] private TextMeshProUGUI forceFlowButtonLabel;

        [Header("回転ボタン（モバイル用）")]
        [Tooltip("ドラッグ中のピースを90度回転するボタン")]
        [SerializeField] private Button rotateButton;

        [Header("戻るボタン")]
        [Tooltip("課題選択画面に戻るボタン")]
        [SerializeField] private Button backButton;

        [Header("地震テストボタン")]
        [Tooltip("地震テスト実行ボタン")]
        [SerializeField] private Button testButton;

        [Tooltip("地震テストボタンラベル")]
        [SerializeField] private TextMeshProUGUI testButtonLabel;

        [Header("共振リスク表示")]
        [Tooltip("共振リスクバッジ")]
        [SerializeField] private Image resonanceRiskBadge;

        [Tooltip("共振リスクテキスト")]
        [SerializeField] private TextMeshProUGUI resonanceRiskText;

        [Header("色設定（墨・朱・緑）")]
        [SerializeField] private Color safeIndicatorColor = new Color(0.2f, 0.45f, 0.2f);
        [SerializeField] private Color warnIndicatorColor = new Color(0.7f, 0.55f, 0.1f);
        [SerializeField] private Color dangerIndicatorColor = new Color(0.7f, 0.15f, 0.1f);

        // 外部参照
        private StressVisualizer _stressVisualizer;
        private PuzzleClearSystem _clearSystem;
        private ResonanceSimulator _resonanceSimulator;

        private bool _forceFlowEnabled = true;
        private JointTypeData _selectedJointType;

        public System.Action<JointTypeData> OnJointTypeSelected;
        public System.Action OnTestRequested;

        /// <summary>HUDを初期化する（PuzzleGameManagerから呼ぶ）</summary>
        public void Initialize(
            PuzzleStageData stageData,
            JointTypeData[] availableJoints,
            StressVisualizer visualizer,
            PuzzleClearSystem clearSystem,
            ResonanceSimulator resonanceSimulator)
        {
            _stressVisualizer = visualizer;
            _clearSystem = clearSystem;
            _resonanceSimulator = resonanceSimulator;

            BuildJointButtons(availableJoints);
            if (stageData?.availablePieces != null) BuildInventory(stageData.availablePieces);
            SetupListeners();

            if (resonanceSimulator != null)
                resonanceSimulator.OnResonanceCalculated += UpdateResonanceDisplay;
        }

        void OnDestroy()
        {
            if (_resonanceSimulator != null)
                _resonanceSimulator.OnResonanceCalculated -= UpdateResonanceDisplay;
        }

        // ─────────────────── 継手選択 ───────────────────

        private void BuildJointButtons(JointTypeData[] joints)
        {
            if (jointButtonPrefab == null || jointButtonContainer == null) return;

            foreach (Transform child in jointButtonContainer)
                Destroy(child.gameObject);

            foreach (var joint in joints)
            {
                var btn = Instantiate(jointButtonPrefab, jointButtonContainer);
                btn.Setup(joint, OnJointButtonClicked);
            }

            if (joints.Length > 0) SelectJoint(joints[0]);
        }

        private void OnJointButtonClicked(JointTypeData jointData)
        {
            SelectJoint(jointData);
        }

        private void SelectJoint(JointTypeData jointData)
        {
            _selectedJointType = jointData;

            if (selectedJointLabel != null) selectedJointLabel.text = jointData.jointName;
            if (selectedJointIcon != null && jointData.icon != null)
                selectedJointIcon.sprite = jointData.icon;

            OnJointTypeSelected?.Invoke(jointData);
        }

        // ─────────────────── 在庫表示 ───────────────────

        private void BuildInventory(WoodPieceInventoryEntry[] entries)
        {
            if (inventoryEntryPrefab == null || inventoryContainer == null) return;

            foreach (Transform child in inventoryContainer)
                Destroy(child.gameObject);

            foreach (var entry in entries)
            {
                var ui = Instantiate(inventoryEntryPrefab, inventoryContainer);
                ui.Setup(entry.pieceData, entry.count);
            }
        }

        public void UpdateInventoryCount(WoodPieceData piece, int remaining)
        {
            foreach (var entry in inventoryContainer.GetComponentsInChildren<InventoryEntryUI>())
            {
                if (entry.PieceData == piece)
                    entry.SetCount(remaining);
            }
        }

        // ─────────────────── ボタンリスナー ───────────────────

        // ドラッグ中のピースを追跡（回転ボタン用）
        private WoodPieceController _draggingPiece;

        public void RegisterDraggingPiece(WoodPieceController ctrl) => _draggingPiece = ctrl;
        public void ClearDraggingPiece() => _draggingPiece = null;

        private void SetupListeners()
        {
            if (forceFlowToggleButton != null)
                forceFlowToggleButton.onClick.AddListener(ToggleForceFlow);

            if (testButton != null)
                testButton.onClick.AddListener(OnTestButtonClicked);

            // モバイル用回転ボタン
            if (rotateButton != null)
                rotateButton.onClick.AddListener(() => _draggingPiece?.RotateFromUI());

            if (backButton != null)
                backButton.onClick.AddListener(() => SceneManager.LoadScene("StageSelect"));
        }

        private void ToggleForceFlow()
        {
            _forceFlowEnabled = !_forceFlowEnabled;

            if (_forceFlowEnabled)
            {
                _stressVisualizer?.Refresh();
                if (forceFlowButtonLabel != null) forceFlowButtonLabel.text = "力の流れ：表示";
            }
            else
            {
                _stressVisualizer?.ResetAll();
                if (forceFlowButtonLabel != null) forceFlowButtonLabel.text = "力の流れ：非表示";
            }
        }

        private void OnTestButtonClicked()
        {
            if (testButton != null) testButton.interactable = false;
            if (testButtonLabel != null) testButtonLabel.text = "試験中...";
            OnTestRequested?.Invoke();
        }

        public void OnTestFinished()
        {
            if (testButton != null) testButton.interactable = true;
            if (testButtonLabel != null) testButtonLabel.text = "地震試験";
        }

        // ─────────────────── 共振リスク表示 ───────────────────

        private void UpdateResonanceDisplay(ResonanceResult result)
        {
            if (resonanceRiskText == null || resonanceRiskBadge == null) return;

            Color badgeColor = result.resonanceRisk switch
            {
                ResonanceRisk.Safe => safeIndicatorColor,
                ResonanceRisk.Moderate => warnIndicatorColor,
                ResonanceRisk.Critical => dangerIndicatorColor,
                _ => safeIndicatorColor
            };

            resonanceRiskBadge.color = badgeColor;
            resonanceRiskText.text = result.resonanceRisk switch
            {
                ResonanceRisk.Safe => "共振：安全",
                ResonanceRisk.Moderate => "共振：注意",
                ResonanceRisk.Critical => "共振：危険",
                _ => ""
            };
        }
    }
}
