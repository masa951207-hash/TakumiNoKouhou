using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 工房の雰囲気（照明・霧・木屑パーティクル）を制御するコンポーネント。
    /// URP Global Volumeのパラメータを遷移させることで時間帯変化を表現する。
    /// </summary>
    public class WorkshopAtmosphereController : MonoBehaviour
    {
        [Header("URP Volume参照")]
        [SerializeField] private Volume globalVolume;

        [Header("朝日ライト")]
        [Tooltip("工房に差し込む朝日のDirectional Light")]
        [SerializeField] private Light morningDirectionalLight;

        [Tooltip("暗時の光量")]
        [SerializeField] private float darkLightIntensity = 0.05f;

        [Tooltip("朝日最大光量")]
        [SerializeField] private float morningLightIntensity = 2.5f;

        [Tooltip("朝日の色（暖かいオレンジ）")]
        [SerializeField] private Color morningLightColor = new Color(1.0f, 0.78f, 0.45f, 1f);

        [Tooltip("暗時の環境光色")]
        [SerializeField] private Color darkAmbientColor = new Color(0.04f, 0.04f, 0.06f, 1f);

        [Header("木屑パーティクル")]
        [Tooltip("工房に漂う木屑・埃パーティクル")]
        [SerializeField] private ParticleSystem woodDustParticle;

        [Tooltip("朝日の光筋パーティクル（光の粒子）")]
        [SerializeField] private ParticleSystem lightRayParticle;

        [Header("Bloom設定")]
        [Tooltip("暗時のBloom強度")]
        [SerializeField] private float darkBloomIntensity = 0.2f;

        [Tooltip("朝日時のBloom強度（光が溢れる表現）")]
        [SerializeField] private float morningBloomIntensity = 1.2f;

        // URP PostProcessing コンポーネント
        private Bloom _bloom;
        private ColorAdjustments _colorAdjustments;
        private Vignette _vignette;

        void Awake()
        {
            if (globalVolume != null && globalVolume.profile != null)
            {
                globalVolume.profile.TryGet(out _bloom);
                globalVolume.profile.TryGet(out _colorAdjustments);
                globalVolume.profile.TryGet(out _vignette);
            }
        }

        /// <summary>暗い工房の初期状態を即座に設定する</summary>
        public void SetDark()
        {
            StopAllCoroutines();

            if (morningDirectionalLight != null)
            {
                morningDirectionalLight.intensity = darkLightIntensity;
                morningDirectionalLight.color = new Color(0.6f, 0.6f, 0.7f);
            }

            RenderSettings.ambientLight = darkAmbientColor;

            if (_bloom != null) _bloom.intensity.value = darkBloomIntensity;
            if (_vignette != null) _vignette.intensity.value = 0.55f;
            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.value = new Color(0.7f, 0.7f, 0.8f);
                _colorAdjustments.postExposure.value = -1.5f;
            }

            // 木屑パーティクルは微量で漂わせる
            if (woodDustParticle != null)
            {
                var emission = woodDustParticle.emission;
                emission.rateOverTime = 3f;
                woodDustParticle.Play();
            }

            if (lightRayParticle != null) lightRayParticle.Stop();
        }

        /// <summary>朝日差し込みシーケンスをアニメーション再生する</summary>
        public void PlayMorningLightSequence(float duration)
        {
            StartCoroutine(MorningLightCoroutine(duration));
        }

        /// <summary>朝日状態を即座に設定する（スキップ用）</summary>
        public void SetMorningLight()
        {
            StopAllCoroutines();
            ApplyMorningState(1f);

            if (lightRayParticle != null)
            {
                var emission = lightRayParticle.emission;
                emission.rateOverTime = 15f;
                lightRayParticle.Play();
            }
        }

        private IEnumerator MorningLightCoroutine(float duration)
        {
            float elapsed = 0f;

            // 光線パーティクルを開始
            if (lightRayParticle != null)
            {
                var emission = lightRayParticle.emission;
                emission.rateOverTime = 0f;
                lightRayParticle.Play();
            }

            while (elapsed < duration)
            {
                float t = elapsed / duration;
                float eased = t * t * (3f - 2f * t); // smoothstep

                ApplyMorningState(eased);

                // 光線パーティクルの量を徐々に増やす
                if (lightRayParticle != null)
                {
                    var emission = lightRayParticle.emission;
                    emission.rateOverTime = eased * 20f;
                }

                // 木屑を朝日で増やす（埃が舞う）
                if (woodDustParticle != null)
                {
                    var emission = woodDustParticle.emission;
                    emission.rateOverTime = 3f + eased * 15f;
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            ApplyMorningState(1f);
        }

        private void ApplyMorningState(float t)
        {
            if (morningDirectionalLight != null)
            {
                morningDirectionalLight.intensity = Mathf.Lerp(darkLightIntensity, morningLightIntensity, t);
                morningDirectionalLight.color = Color.Lerp(new Color(0.6f, 0.6f, 0.7f), morningLightColor, t);
            }

            RenderSettings.ambientLight = Color.Lerp(darkAmbientColor, new Color(0.3f, 0.25f, 0.15f), t);

            if (_bloom != null)
                _bloom.intensity.value = Mathf.Lerp(darkBloomIntensity, morningBloomIntensity, t);

            if (_vignette != null)
                _vignette.intensity.value = Mathf.Lerp(0.55f, 0.3f, t);

            if (_colorAdjustments != null)
            {
                _colorAdjustments.colorFilter.value = Color.Lerp(
                    new Color(0.7f, 0.7f, 0.8f),
                    new Color(1.0f, 0.92f, 0.78f),
                    t);
                _colorAdjustments.postExposure.value = Mathf.Lerp(-1.5f, 0f, t);
            }
        }
    }
}
