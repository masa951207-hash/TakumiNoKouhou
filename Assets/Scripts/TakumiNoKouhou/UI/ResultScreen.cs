using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 地震試験後の評価結果を表示するパネル。
    /// 等級・各スコア・工人のコメントを和紙パネルで表示する。
    /// </summary>
    public class ResultScreen : WashiPaperPanel
    {
        [Header("等級表示")]
        [Tooltip("等級文字テキスト（S / A / B / C / D / F）")]
        [SerializeField] private TextMeshProUGUI gradeText;

        [Tooltip("等級の大きいアイコン or 印鑑イメージ")]
        [SerializeField] private Image gradeStampImage;

        [Tooltip("等級スタンプのスプライト（S/A/B/C/D/F順）")]
        [SerializeField] private Sprite[] gradeStampSprites;

        [Header("スコア内訳")]
        [SerializeField] private Slider jointPrecisionSlider;
        [SerializeField] private Slider forceDistSlider;
        [SerializeField] private Slider resonanceSlider;
        [SerializeField] private Slider bendingSlider;
        [SerializeField] private Slider connectionSlider;

        [SerializeField] private TextMeshProUGUI jointPrecisionText;
        [SerializeField] private TextMeshProUGUI forceDistText;
        [SerializeField] private TextMeshProUGUI resonanceText;
        [SerializeField] private TextMeshProUGUI bendingText;
        [SerializeField] private TextMeshProUGUI connectionText;

        [Header("コメント")]
        [Tooltip("クリア/失敗メッセージ")]
        [SerializeField] private TextMeshProUGUI resultMessageText;

        [Tooltip("工人の評価コメント")]
        [SerializeField] private TextMeshProUGUI gradeCommentText;

        [Tooltip("共振診断テキスト")]
        [SerializeField] private TextMeshProUGUI resonanceDiagText;

        [Header("ボタン")]
        [SerializeField] private Button retryButton;
        [SerializeField] private Button nextStageButton;
        [SerializeField] private Button stageSelectButton;

        [Header("スタンプアニメーション")]
        [Tooltip("スタンプが押される時間")]
        [SerializeField] private float stampDuration = 0.3f;

        [Tooltip("スタンプSFX")]
        [SerializeField] private AudioClip stampSfx;

        private AudioSource _audioSource;

        protected override void Awake()
        {
            base.Awake();
            _audioSource = gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            if (retryButton != null)
                retryButton.onClick.AddListener(() => SceneManager.LoadScene(SceneManager.GetActiveScene().name));
            if (stageSelectButton != null)
                stageSelectButton.onClick.AddListener(() => SceneManager.LoadScene("StageSelect"));
        }

        /// <summary>評価結果を受けてパネルを表示する（PuzzleClearSystemから呼ぶ）</summary>
        public void ShowResult(PuzzleClearResult clearResult)
        {
            if (clearResult == null || clearResult.evaluationResult == null)
            {
                Debug.LogWarning("[ResultScreen] clearResult または evaluationResult が null です");
                return;
            }
            Show();
            StartCoroutine(PopulateResultCoroutine(clearResult));
        }

        private IEnumerator PopulateResultCoroutine(PuzzleClearResult clearResult)
        {
            var eval = clearResult.evaluationResult;

            // ─── コメント ───
            if (resultMessageText != null) resultMessageText.text = clearResult.resultMessage;
            if (gradeCommentText != null) gradeCommentText.text = eval.gradeComment;

            // ─── 等級テキスト ───
            if (gradeText != null)
            {
                gradeText.text = eval.grade.ToString();
                gradeText.color = GradeColor(eval.grade);
            }

            // ─── スコアスライダーをアニメーション ───
            yield return AnimateScores(eval);

            // ─── スタンプ演出 ───
            yield return new WaitForSeconds(0.3f);
            yield return StampAnimation(eval.grade);

            // ─── 次ステージボタン ───
            if (nextStageButton != null)
                nextStageButton.gameObject.SetActive(clearResult.isPassed);

            // ─── 共振診断 ───
            if (resonanceDiagText != null && clearResult.seismicSummary != null)
            {
                resonanceDiagText.text =
                    $"最大加速度：{clearResult.seismicSummary.pgaAchieved:F2}G  " +
                    $"最大水平荷重：{clearResult.seismicSummary.maxHorizontalLoad:F1} kN";
            }
        }

        private IEnumerator AnimateScores(StructuralEvaluationResult eval)
        {
            float duration = 1.2f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);

                SetScore(jointPrecisionSlider, jointPrecisionText, eval.jointPrecisionScore * t, "継手精度");
                SetScore(forceDistSlider, forceDistText, eval.forceDistributionScore * t, "力分散");
                SetScore(resonanceSlider, resonanceText, eval.resonanceScore * t, "共振耐性");
                SetScore(bendingSlider, bendingText, eval.bendingScore * t, "しなり適正");
                SetScore(connectionSlider, connectionText, eval.connectionCompletenessScore * t, "接合完成度");

                elapsed += Time.deltaTime;
                yield return null;
            }

            SetScore(jointPrecisionSlider, jointPrecisionText, eval.jointPrecisionScore, "継手精度");
            SetScore(forceDistSlider, forceDistText, eval.forceDistributionScore, "力分散");
            SetScore(resonanceSlider, resonanceText, eval.resonanceScore, "共振耐性");
            SetScore(bendingSlider, bendingText, eval.bendingScore, "しなり適正");
            SetScore(connectionSlider, connectionText, eval.connectionCompletenessScore, "接合完成度");
        }

        private IEnumerator StampAnimation(StructuralGrade grade)
        {
            if (gradeStampImage == null) yield break;

            int idx = (int)StructuralGrade.S - (int)grade;
            if (gradeStampSprites != null && idx < gradeStampSprites.Length)
                gradeStampImage.sprite = gradeStampSprites[idx];

            gradeStampImage.transform.localScale = Vector3.one * 2.5f;
            gradeStampImage.color = new Color(1f, 1f, 1f, 0f);

            if (stampSfx != null) _audioSource.PlayOneShot(stampSfx);

            float elapsed = 0f;
            while (elapsed < stampDuration)
            {
                float t = elapsed / stampDuration;
                float eased = 1f - Mathf.Pow(1f - t, 3f); // ease out cubic
                gradeStampImage.transform.localScale = Vector3.one * Mathf.Lerp(2.5f, 1f, eased);
                gradeStampImage.color = new Color(1f, 1f, 1f, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            gradeStampImage.transform.localScale = Vector3.one;
            gradeStampImage.color = Color.white;
        }

        private void SetScore(Slider slider, TextMeshProUGUI label, float value, string name)
        {
            if (slider != null) slider.value = value;
            if (label != null) label.text = $"{name}：{value * 100f:F0}";
        }

        private Color GradeColor(StructuralGrade grade)
        {
            return grade switch
            {
                StructuralGrade.S => new Color(0.8f, 0.65f, 0.1f),  // 金色
                StructuralGrade.A => new Color(0.6f, 0.6f, 0.65f),  // 銀色
                StructuralGrade.B => new Color(0.6f, 0.35f, 0.15f), // 銅色
                StructuralGrade.C => new Color(0.2f, 0.4f, 0.2f),   // 緑（合格）
                StructuralGrade.D => new Color(0.6f, 0.4f, 0.1f),   // 橙（不合格）
                StructuralGrade.F => new Color(0.6f, 0.1f, 0.1f),   // 赤（倒壊）
                _ => Color.white
            };
        }
    }
}
