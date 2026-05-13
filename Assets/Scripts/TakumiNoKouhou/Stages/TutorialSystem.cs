using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 初回プレイヤーへの操作案内チュートリアルシステム。
    /// ハイライト・吹き出し・矢印を順番に表示してパズルの基本を教える。
    /// </summary>
    public class TutorialSystem : MonoBehaviour
    {
        [Header("チュートリアルUI")]
        [Tooltip("吹き出しパネルのRectTransform")]
        [SerializeField] private RectTransform tooltipPanel;

        [Tooltip("吹き出し内テキスト")]
        [SerializeField] private TextMeshProUGUI tooltipText;

        [Tooltip("次へボタン")]
        [SerializeField] private Button nextButton;

        [Tooltip("スキップボタン")]
        [SerializeField] private Button skipButton;

        [Tooltip("ハイライト用オーバーレイ（暗い背景＋穴開き表現用）")]
        [SerializeField] private Image highlightOverlay;

        [Tooltip("矢印アニメーション")]
        [SerializeField] private RectTransform arrowTransform;

        [Header("ステップ定義")]
        [SerializeField] private TutorialStep[] steps;

        [Header("設定")]
        [Tooltip("チュートリアルを完了済みにするPlayerPrefsキー")]
        [SerializeField] private string completedKey = "Tutorial_Completed";

        [Tooltip("デバッグ用：チュートリアルを強制再表示する")]
        [SerializeField] private bool forceShow;

        private int _currentStepIndex = -1;
        private bool _isRunning;
        private Coroutine _arrowCoroutine;

        public System.Action OnTutorialCompleted;

        void Start()
        {
            if (!forceShow && PlayerPrefs.GetInt(completedKey, 0) == 1)
            {
                gameObject.SetActive(false);
                return;
            }

            SetupButtons();
            StartTutorial();
        }

        private void SetupButtons()
        {
            if (nextButton != null) nextButton.onClick.AddListener(AdvanceStep);
            if (skipButton != null) skipButton.onClick.AddListener(CompleteTutorial);
        }

        /// <summary>チュートリアルを最初から開始する</summary>
        public void StartTutorial()
        {
            _isRunning = true;
            _currentStepIndex = -1;
            gameObject.SetActive(true);
            AdvanceStep();
        }

        private void AdvanceStep()
        {
            _currentStepIndex++;

            if (_currentStepIndex >= steps.Length)
            {
                CompleteTutorial();
                return;
            }

            ShowStep(steps[_currentStepIndex]);
        }

        private void ShowStep(TutorialStep step)
        {
            // ─── テキスト ───
            if (tooltipText != null) tooltipText.text = step.message;

            // ─── 吹き出しパネル位置 ───
            if (tooltipPanel != null && step.tooltipAnchor != null)
            {
                tooltipPanel.position = step.tooltipAnchor.position;
            }

            // ─── ハイライト ───
            if (highlightOverlay != null)
                highlightOverlay.gameObject.SetActive(step.highlightTarget != null);

            // ─── 矢印アニメーション ───
            if (_arrowCoroutine != null) StopCoroutine(_arrowCoroutine);
            if (arrowTransform != null && step.arrowTarget != null)
            {
                arrowTransform.gameObject.SetActive(true);
                arrowTransform.position = step.arrowTarget.position;
                _arrowCoroutine = StartCoroutine(BounceArrow(step.arrowDirection));
            }
            else if (arrowTransform != null)
            {
                arrowTransform.gameObject.SetActive(false);
            }

            // ─── 自動進行（waitForClick=falseの場合）───
            if (!step.waitForClick)
                StartCoroutine(AutoAdvance(step.autoAdvanceDelay));
        }

        private IEnumerator AutoAdvance(float delay)
        {
            if (nextButton != null) nextButton.interactable = false;
            yield return new WaitForSeconds(delay);
            if (nextButton != null) nextButton.interactable = true;
            AdvanceStep();
        }

        private IEnumerator BounceArrow(Vector2 direction)
        {
            float t = 0f;
            Vector3 startPos = arrowTransform.localPosition;

            while (true)
            {
                float offset = Mathf.Sin(t * 3f) * 12f;
                arrowTransform.localPosition = startPos + (Vector3)(direction.normalized * offset);
                t += Time.deltaTime;
                yield return null;
            }
        }

        private void CompleteTutorial()
        {
            _isRunning = false;
            PlayerPrefs.SetInt(completedKey, 1);
            PlayerPrefs.Save();

            if (arrowTransform != null) arrowTransform.gameObject.SetActive(false);
            if (highlightOverlay != null) highlightOverlay.gameObject.SetActive(false);

            gameObject.SetActive(false);
            OnTutorialCompleted?.Invoke();
        }

        public bool IsRunning => _isRunning;
    }

    // ─────────────────── データクラス ───────────────────

    [System.Serializable]
    public class TutorialStep
    {
        [Tooltip("表示する説明文")]
        [TextArea(2, 5)]
        public string message;

        [Tooltip("吹き出しを配置するUI上のアンカーTransform")]
        public RectTransform tooltipAnchor;

        [Tooltip("ハイライトするUI要素（nullなら非表示）")]
        public RectTransform highlightTarget;

        [Tooltip("矢印が指すターゲット（nullなら非表示）")]
        public RectTransform arrowTarget;

        [Tooltip("矢印のバウンス方向")]
        public Vector2 arrowDirection = Vector2.down;

        [Tooltip("クリックを待つか")]
        public bool waitForClick = true;

        [Tooltip("waitForClick=falseの場合の自動進行秒数")]
        public float autoAdvanceDelay = 3f;
    }
}
