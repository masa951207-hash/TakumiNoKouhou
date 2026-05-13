using System.Collections;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// タイトル画面専用のオーディオ管理コンポーネント。
    /// 工房BGM・木槌音・墨書き音・ドアの軋み音などを管理する。
    /// </summary>
    public class TitleAudioManager : MonoBehaviour
    {
        [Header("BGM")]
        [Tooltip("工房の静寂BGM（ループ）")]
        [SerializeField] private AudioClip workshopAmbienceBgm;

        [Tooltip("BGMのフェードイン時間")]
        [SerializeField] private float bgmFadeInDuration = 3f;

        [Header("SFX一覧")]
        [Tooltip("朝の鳥のさえずり")]
        [SerializeField] private AudioClip morningBirdSfx;

        [Tooltip("木の軋む音")]
        [SerializeField] private AudioClip woodCreakSfx;

        [Tooltip("墨が走る音（筆の音）")]
        [SerializeField] private AudioClip brushStrokeSfx;

        [Tooltip("木槌を打つ音")]
        [SerializeField] private AudioClip malletSfx;

        [Tooltip("メニュー選択音")]
        [SerializeField] private AudioClip menuSelectSfx;

        [Header("音量")]
        [Range(0f, 1f)]
        [SerializeField] private float bgmVolume = 0.45f;

        [Range(0f, 1f)]
        [SerializeField] private float sfxVolume = 0.8f;

        private AudioSource _bgmSource;
        private AudioSource _sfxSource;

        void Awake()
        {
            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.volume = 0f;
            _bgmSource.playOnAwake = false;

            _sfxSource = gameObject.AddComponent<AudioSource>();
            _sfxSource.loop = false;
            _sfxSource.volume = sfxVolume;
            _sfxSource.playOnAwake = false;
        }

        /// <summary>工房アンビエンスBGMをフェードインで再生する</summary>
        public void PlayWorkshopAmbience()
        {
            if (workshopAmbienceBgm == null) return;
            _bgmSource.clip = workshopAmbienceBgm;
            _bgmSource.Play();
            StartCoroutine(FadeInBgm());
        }

        /// <summary>朝の鳥SFXを再生する</summary>
        public void PlayMorningBirdSfx() => PlaySfx(morningBirdSfx);

        /// <summary>木の軋みSFXを再生する</summary>
        public void PlayWoodCreakSfx() => PlaySfx(woodCreakSfx);

        /// <summary>墨書きSFXを再生する</summary>
        public void PlayBrushStrokeSfx() => PlaySfx(brushStrokeSfx);

        /// <summary>木槌SFXを再生する</summary>
        public void PlayMalletSfx() => PlaySfx(malletSfx);

        /// <summary>メニュー選択SFXを再生する</summary>
        public void PlayMenuSelectSfx() => PlaySfx(menuSelectSfx);

        /// <summary>BGMをフェードアウトして停止する</summary>
        public void StopBgm(float fadeOut = 1.5f)
        {
            StartCoroutine(FadeOutBgm(fadeOut));
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip == null) return;
            _sfxSource.PlayOneShot(clip, sfxVolume);
        }

        private IEnumerator FadeInBgm()
        {
            float elapsed = 0f;
            while (elapsed < bgmFadeInDuration)
            {
                _bgmSource.volume = (elapsed / bgmFadeInDuration) * bgmVolume;
                elapsed += Time.deltaTime;
                yield return null;
            }
            _bgmSource.volume = bgmVolume;
        }

        private IEnumerator FadeOutBgm(float duration)
        {
            float startVol = _bgmSource.volume;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                _bgmSource.volume = Mathf.Lerp(startVol, 0f, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            _bgmSource.Stop();
            _bgmSource.volume = bgmVolume;
        }
    }
}
