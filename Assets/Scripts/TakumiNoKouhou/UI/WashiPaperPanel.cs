using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 和紙テクスチャを持つUIパネルの基底クラス。
    /// 全てのUIパネルはこのクラスを継承し、統一された和風デザインを持つ。
    /// フェードイン・アウト・スライドインアニメーションを提供する。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class WashiPaperPanel : MonoBehaviour
    {
        [Header("和紙デザイン")]
        [Tooltip("和紙テクスチャを適用するImageコンポーネント")]
        [SerializeField] protected Image backgroundImage;

        [Tooltip("和紙テクスチャ")]
        [SerializeField] protected Texture2D washiTexture;

        [Tooltip("墨線の枠線Image")]
        [SerializeField] protected Image borderImage;

        [Tooltip("和紙の基本色（黄みがかった白）")]
        [SerializeField] protected Color washiColor = new Color(0.97f, 0.94f, 0.86f, 0.95f);

        [Header("アニメーション設定")]
        [SerializeField] protected float fadeSpeed = 4f;

        [Tooltip("スライドイン方向")]
        [SerializeField] protected SlideDirection slideDirection = SlideDirection.None;

        [Tooltip("スライド距離（ピクセル）")]
        [SerializeField] protected float slideDistance = 60f;

        protected CanvasGroup _canvasGroup;
        protected RectTransform _rectTransform;
        private Vector2 _originalAnchoredPosition;
        private bool _isVisible;

        protected virtual void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
            _rectTransform = GetComponent<RectTransform>();
            _originalAnchoredPosition = _rectTransform.anchoredPosition;

            if (backgroundImage != null)
                backgroundImage.color = washiColor;
        }

        /// <summary>パネルをフェードインで表示する</summary>
        public virtual void Show(bool animate = true)
        {
            gameObject.SetActive(true);
            _isVisible = true;

            if (animate)
                StartCoroutine(FadeIn());
            else
            {
                _canvasGroup.alpha = 1f;
                _canvasGroup.interactable = true;
                _canvasGroup.blocksRaycasts = true;
            }
        }

        /// <summary>パネルをフェードアウトで非表示にする</summary>
        public virtual void Hide(bool animate = true)
        {
            _isVisible = false;

            if (animate)
                StartCoroutine(FadeOut());
            else
            {
                _canvasGroup.alpha = 0f;
                _canvasGroup.interactable = false;
                _canvasGroup.blocksRaycasts = false;
                gameObject.SetActive(false);
            }
        }

        public bool IsVisible => _isVisible;

        protected virtual IEnumerator FadeIn()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = true;

            Vector2 startPos = slideDirection switch
            {
                SlideDirection.FromBottom => _originalAnchoredPosition + Vector2.down * slideDistance,
                SlideDirection.FromTop => _originalAnchoredPosition + Vector2.up * slideDistance,
                SlideDirection.FromLeft => _originalAnchoredPosition + Vector2.left * slideDistance,
                SlideDirection.FromRight => _originalAnchoredPosition + Vector2.right * slideDistance,
                _ => _originalAnchoredPosition
            };

            _rectTransform.anchoredPosition = startPos;
            _canvasGroup.alpha = 0f;

            float elapsed = 0f;
            float duration = 1f / fadeSpeed;

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t * t * (3f - 2f * t);
                _canvasGroup.alpha = eased;
                _rectTransform.anchoredPosition = Vector2.Lerp(startPos, _originalAnchoredPosition, eased);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _canvasGroup.alpha = 1f;
            _rectTransform.anchoredPosition = _originalAnchoredPosition;
            _canvasGroup.interactable = true;
        }

        protected virtual IEnumerator FadeOut()
        {
            _canvasGroup.interactable = false;
            _canvasGroup.blocksRaycasts = false;

            float startAlpha = _canvasGroup.alpha;
            float elapsed = 0f;
            float duration = 1f / fadeSpeed;

            while (elapsed < duration)
            {
                _canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _canvasGroup.alpha = 0f;
            gameObject.SetActive(false);
        }
    }

    public enum SlideDirection { None, FromBottom, FromTop, FromLeft, FromRight }
}
