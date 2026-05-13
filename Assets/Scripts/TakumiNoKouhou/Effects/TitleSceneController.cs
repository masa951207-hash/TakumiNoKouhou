using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TakumiNoKouhou
{
    /// <summary>
    /// タイトル画面の演出フロー全体を制御するコンポーネント。
    /// 「暗い工房 → 朝日差し込み → 墨書きロゴ → メニュー表示」の順で進行する。
    /// </summary>
    public class TitleSceneController : MonoBehaviour
    {
        [Header("演出コンポーネント参照")]
        [SerializeField] private InkCalligraphyAnimation inkAnimation;
        [SerializeField] private WorkshopAtmosphereController atmosphereController;
        [SerializeField] private TitleAudioManager audioManager;

        [Header("UIパネル参照")]
        [Tooltip("メインメニューパネル（初期は非表示）")]
        [SerializeField] private CanvasGroup mainMenuPanel;

        [Tooltip("スキップボタンのCanvasGroup")]
        [SerializeField] private CanvasGroup skipButtonGroup;

        [Header("演出タイミング（秒）")]
        [Tooltip("初期の暗転時間")]
        [SerializeField] private float initialDarkDuration = 0.3f;

        [Tooltip("朝日演出の時間")]
        [SerializeField] private float morningLightDuration = 1.0f;

        [Tooltip("墨書き開始前の静寂時間")]
        [SerializeField] private float beforeInkDelay = 0.1f;

        [Tooltip("メニュー表示フェードイン時間")]
        [SerializeField] private float menuFadeInDuration = 0.4f;

        [Header("シーン遷移")]
        [Tooltip("ステージ選択シーン名")]
        [SerializeField] private string stageSelectSceneName = "StageSelect";

        [Header("ボタン参照")]
        [Tooltip("終了ボタン（WebGLでは非表示）")]
        [SerializeField] private UnityEngine.UI.Button quitButton;

        private bool _isIntroPlaying;
        private bool _skipRequested;

        void Start()
        {
            if (mainMenuPanel != null)
            {
                mainMenuPanel.alpha = 0f;
                mainMenuPanel.interactable = false;
                mainMenuPanel.blocksRaycasts = false;
            }

            if (skipButtonGroup != null)
            {
                skipButtonGroup.alpha = 0f;
                skipButtonGroup.interactable = false;
            }

            // 終了ボタン：WebGLでは非表示、それ以外は Application.Quit を登録
#if UNITY_WEBGL && !UNITY_EDITOR
            if (quitButton != null) quitButton.gameObject.SetActive(false);
#else
            if (quitButton != null)
                quitButton.onClick.AddListener(Application.Quit);
#endif

            // アニメーション系コンポーネントが未割当の場合は即メニュー表示
            if (inkAnimation == null && atmosphereController == null && audioManager == null)
            {
                StartCoroutine(FadeInMenu());
                return;
            }

            StartCoroutine(PlayIntro());
        }

        void Update()
        {
            // スペース・Enterキーでイントロスキップ
            if (_isIntroPlaying &&
                (UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame ||
                 UnityEngine.InputSystem.Keyboard.current.enterKey.wasPressedThisFrame))
            {
                _skipRequested = true;
            }
        }

        private IEnumerator PlayIntro()
        {
            _isIntroPlaying = true;

            // ─── フェーズ1：暗転 ───
            atmosphereController?.SetDark();
            audioManager?.PlayWorkshopAmbience();
            yield return new WaitForSeconds(initialDarkDuration);

            if (_skipRequested) { SkipToMenu(); yield break; }

            // ─── フェーズ2：朝日差し込み ───
            StartCoroutine(ShowSkipButton());
            atmosphereController?.PlayMorningLightSequence(morningLightDuration);
            audioManager?.PlayMorningBirdSfx();
            yield return new WaitForSeconds(morningLightDuration);

            if (_skipRequested) { SkipToMenu(); yield break; }

            // ─── フェーズ3：静寂 ───
            audioManager?.PlayWoodCreakSfx();
            yield return new WaitForSeconds(beforeInkDelay);

            if (_skipRequested) { SkipToMenu(); yield break; }

            // ─── フェーズ4：墨書きロゴ ───
            audioManager?.PlayBrushStrokeSfx();
            if (inkAnimation != null) yield return inkAnimation.PlayAnimation();

            if (_skipRequested) { SkipToMenu(); yield break; }

            // ─── フェーズ5：木槌音 ───
            audioManager?.PlayMalletSfx();
            yield return new WaitForSeconds(0.2f);

            // ─── フェーズ6：メニュー表示 ───
            yield return FadeInMenu();

            _isIntroPlaying = false;
            HideSkipButton();
        }

        private void SkipToMenu()
        {
            StopAllCoroutines();
            atmosphereController?.SetMorningLight();
            inkAnimation?.ShowImmediately();
            StartCoroutine(FadeInMenu());
            _isIntroPlaying = false;
            HideSkipButton();
        }

        private IEnumerator FadeInMenu()
        {
            if (mainMenuPanel == null) yield break;

            mainMenuPanel.interactable = true;
            mainMenuPanel.blocksRaycasts = true;

            float elapsed = 0f;
            while (elapsed < menuFadeInDuration)
            {
                mainMenuPanel.alpha = elapsed / menuFadeInDuration;
                elapsed += Time.deltaTime;
                yield return null;
            }
            mainMenuPanel.alpha = 1f;
        }

        private IEnumerator ShowSkipButton()
        {
            if (skipButtonGroup == null) yield break;
            yield return new WaitForSeconds(0.5f);
            skipButtonGroup.alpha = 0.6f;
            skipButtonGroup.interactable = true;
        }

        private void HideSkipButton()
        {
            if (skipButtonGroup == null) return;
            skipButtonGroup.alpha = 0f;
            skipButtonGroup.interactable = false;
        }

        // ─────────────────── メニューボタン ───────────────────

        public void OnStartButtonPressed()
        {
            audioManager?.PlayMenuSelectSfx();
            GameAudioManager.Instance?.PlayButtonClick();
            SceneManager.LoadScene(stageSelectSceneName);
        }

        public void OnSkipButtonPressed()
        {
            if (_isIntroPlaying) _skipRequested = true;
        }
    }
}
