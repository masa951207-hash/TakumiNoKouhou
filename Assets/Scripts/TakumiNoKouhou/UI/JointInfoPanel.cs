using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 選択中の継手・接続済みスロットの詳細情報を表示する教育的パネル。
    /// 継手の力学的説明・評価スコアを和紙パネルで表示する。
    /// </summary>
    public class JointInfoPanel : WashiPaperPanel
    {
        [Header("継手情報表示")]
        [Tooltip("継手名テキスト")]
        [SerializeField] private TextMeshProUGUI jointNameText;

        [Tooltip("継手説明テキスト")]
        [SerializeField] private TextMeshProUGUI descriptionText;

        [Tooltip("継手アイコン")]
        [SerializeField] private Image jointIcon;

        [Header("力学特性バー")]
        [SerializeField] private Slider compressionBar;
        [SerializeField] private Slider tensionBar;
        [SerializeField] private Slider shearBar;
        [SerializeField] private Slider bendingBar;
        [SerializeField] private Slider dampingBar;

        [Tooltip("各バーのラベルテキスト")]
        [SerializeField] private TextMeshProUGUI compressionLabel;
        [SerializeField] private TextMeshProUGUI tensionLabel;
        [SerializeField] private TextMeshProUGUI shearLabel;
        [SerializeField] private TextMeshProUGUI bendingLabel;
        [SerializeField] private TextMeshProUGUI dampingLabel;

        [Header("評価スコア表示（接続時）")]
        [Tooltip("精度スコアテキスト")]
        [SerializeField] private TextMeshProUGUI precisionScoreText;

        [Tooltip("適合スコアテキスト")]
        [SerializeField] private TextMeshProUGUI compatibilityScoreText;

        [Tooltip("評価コメントテキスト")]
        [SerializeField] private TextMeshProUGUI evaluationCommentText;

        [Tooltip("スコア表示グループ（接続なしの場合は非表示）")]
        [SerializeField] private CanvasGroup scoreGroup;

        [Header("工人の一言")]
        [Tooltip("継手についての工人コメント（ScriptableObjectから引用）")]
        [SerializeField] private TextMeshProUGUI craftmanCommentText;

        protected override void Awake()
        {
            base.Awake();
            if (scoreGroup != null)
            {
                scoreGroup.alpha = 0f;
                scoreGroup.interactable = false;
            }
        }

        /// <summary>継手データを表示する（スロット未接続時）</summary>
        public void ShowJointData(JointTypeData data)
        {
            if (data == null) return;

            if (jointNameText != null) jointNameText.text = data.jointName;
            if (descriptionText != null) descriptionText.text = data.description;
            if (jointIcon != null && data.icon != null) jointIcon.sprite = data.icon;

            SetBar(compressionBar, compressionLabel, data.compressionStrength, "圧縮強度");
            SetBar(tensionBar, tensionLabel, data.tensionStrength, "引張強度");
            SetBar(shearBar, shearLabel, data.shearStrength, "せん断強度");
            SetBar(bendingBar, bendingLabel, data.bendingFlexibility, "しなり");
            SetBar(dampingBar, dampingLabel, data.dampingCoefficient, "減衰");

            if (scoreGroup != null) scoreGroup.alpha = 0f;
            if (craftmanCommentText != null) craftmanCommentText.text = BuildCraftmanComment(data);
        }

        /// <summary>接続評価結果を表示する（スロット接続時）</summary>
        public void ShowEvaluation(JointEvaluation eval)
        {
            if (eval == null) return;

            // 継手データを先に表示
            if (eval.jointTypeData != null) ShowJointData(eval.jointTypeData);

            if (precisionScoreText != null)
                precisionScoreText.text = $"精度：{eval.precisionScore * 100f:F0}点";

            if (compatibilityScoreText != null)
                compatibilityScoreText.text = $"適合：{eval.compatibilityScore * 100f:F0}点";

            if (evaluationCommentText != null)
                evaluationCommentText.text = eval.notes;

            if (scoreGroup != null) scoreGroup.alpha = 1f;
        }

        private void SetBar(Slider slider, TextMeshProUGUI label, float value, string name)
        {
            if (slider != null) slider.value = value;
            if (label != null) label.text = $"{name}：{value * 10f:F1}";
        }

        private string BuildCraftmanComment(JointTypeData data)
        {
            return data.shapeType switch
            {
                JointShapeType.Mortise =>
                    "「ほぞは圧縮の友。柱と梁をしっかりと抱き合わせよ」",
                JointShapeType.Dovetail =>
                    "「蟻の形は引張に強い。横から引かれる力を逃がさぬ工夫ぞ」",
                JointShapeType.Sickle =>
                    "「鎌継ぎは長い部材を繋ぐ知恵。引張とせん断を同時に受け止める」",
                JointShapeType.Splice =>
                    "「相欠きは手早い継手。まず力の流れを確かめよ」",
                _ => "「木を組むとは、力の流れを読む技なり」"
            };
        }
    }
}
