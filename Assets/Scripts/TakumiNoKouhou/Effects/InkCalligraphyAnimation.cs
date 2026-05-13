using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 『匠の工法』のロゴを墨が走るように表示するアニメーションコンポーネント。
    /// TextMeshProのアルファ・スケール・墨飛沫エフェクトを組み合わせて演出する。
    /// </summary>
    public class InkCalligraphyAnimation : MonoBehaviour
    {
        [Header("ロゴテキスト参照")]
        [Tooltip("メインタイトルテキスト『匠の工法』")]
        [SerializeField] private TextMeshProUGUI titleText;

        [Tooltip("サブタイトルテキスト（英語表記など）")]
        [SerializeField] private TextMeshProUGUI subTitleText;

        [Header("墨エフェクト")]
        [Tooltip("墨が走る時に出すパーティクル")]
        [SerializeField] private ParticleSystem inkSplatterParticle;

        [Tooltip("墨線を引くエフェクト用Image（左から右にスライドするマスク用）")]
        [SerializeField] private RectTransform inkMaskTransform;

        [Header("アニメーション設定")]
        [Tooltip("墨が走る速度（文字全体を通過するのにかかる秒数）")]
        [SerializeField] private float inkStrokeSpeed = 0.5f;

        [Tooltip("文字が現れた後の停留時間")]
        [SerializeField] private float holdDuration = 0.15f;

        [Tooltip("サブタイトルのフェードイン時間")]
        [SerializeField] private float subTitleFadeDuration = 0.3f;

        [Header("色設定")]
        [Tooltip("タイトル文字の色（墨色）")]
        [SerializeField] private Color inkColor = new Color(0.1f, 0.08f, 0.05f, 1f);

        [Tooltip("サブタイトルの色")]
        [SerializeField] private Color subTitleColor = new Color(0.3f, 0.25f, 0.15f, 0.8f);

        private bool _isPlaying;

        void Awake()
        {
            // 初期状態：非表示
            if (titleText != null)
            {
                titleText.color = new Color(inkColor.r, inkColor.g, inkColor.b, 0f);
                titleText.transform.localScale = Vector3.one * 0.95f;
            }
            if (subTitleText != null)
                subTitleText.color = new Color(subTitleColor.r, subTitleColor.g, subTitleColor.b, 0f);

            if (inkMaskTransform != null)
                inkMaskTransform.anchorMax = new Vector2(0f, 1f);
        }

        /// <summary>墨書きアニメーションを再生し、完了まで待機するCoroutineを返す</summary>
        public IEnumerator PlayAnimation()
        {
            if (_isPlaying) yield break;
            _isPlaying = true;

            // ─── メインタイトル：墨が走る ───
            yield return StrokeTitle();

            // ─── 停留 ───
            yield return new WaitForSeconds(holdDuration);

            // ─── サブタイトルフェードイン ───
            if (subTitleText != null)
                yield return FadeInSubTitle();

            _isPlaying = false;
        }

        /// <summary>アニメーションなしで即座にロゴを表示する（スキップ用）</summary>
        public void ShowImmediately()
        {
            StopAllCoroutines();
            _isPlaying = false;

            if (titleText != null)
            {
                titleText.color = inkColor;
                titleText.transform.localScale = Vector3.one;
            }

            if (subTitleText != null)
                subTitleText.color = subTitleColor;

            if (inkMaskTransform != null)
                inkMaskTransform.anchorMax = new Vector2(1f, 1f);
        }

        private IEnumerator StrokeTitle()
        {
            if (titleText == null) yield break;

            float elapsed = 0f;

            // 墨が左→右に走るマスクアニメーション
            while (elapsed < inkStrokeSpeed)
            {
                float t = elapsed / inkStrokeSpeed;
                float eased = Mathf.SmoothStep(0f, 1f, t);

                // マスクを左から右にスライド
                if (inkMaskTransform != null)
                    inkMaskTransform.anchorMax = new Vector2(eased, 1f);

                // 文字アルファとスケールを同期
                float alpha = Mathf.Clamp01(t * 1.5f);
                float scale = Mathf.Lerp(0.95f, 1f, eased);

                titleText.color = new Color(inkColor.r, inkColor.g, inkColor.b, alpha);
                titleText.transform.localScale = Vector3.one * scale;

                // 墨飛沫パーティクルを文字先端付近で発生
                if (inkSplatterParticle != null && t < 0.95f)
                {
                    var shape = inkSplatterParticle.shape;
                    // マスク右端のローカル位置に合わせてパーティクル位置を追従
                    if (inkMaskTransform != null)
                    {
                        var rect = (inkMaskTransform.parent as RectTransform)?.rect ?? Rect.zero;
                        float xOffset = rect.width * eased - rect.width * 0.5f;
                        inkSplatterParticle.transform.localPosition =
                            new Vector3(xOffset, inkSplatterParticle.transform.localPosition.y, 0f);
                    }
                    inkSplatterParticle.Emit(1);
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            // 完全表示
            titleText.color = inkColor;
            titleText.transform.localScale = Vector3.one;

            if (inkMaskTransform != null)
                inkMaskTransform.anchorMax = new Vector2(1f, 1f);
        }

        private IEnumerator FadeInSubTitle()
        {
            if (subTitleText == null) yield break;

            float elapsed = 0f;
            while (elapsed < subTitleFadeDuration)
            {
                float alpha = elapsed / subTitleFadeDuration;
                subTitleText.color = new Color(subTitleColor.r, subTitleColor.g, subTitleColor.b, alpha);
                elapsed += Time.deltaTime;
                yield return null;
            }
            subTitleText.color = subTitleColor;
        }
    }
}
