using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TakumiNoKouhou
{
    /// <summary>
    /// BGM・SE を一元管理するシングルトン。
    /// Title シーンで生成され DontDestroyOnLoad で全シーンに引き継がれる。
    /// </summary>
    public class GameAudioManager : MonoBehaviour
    {
        public static GameAudioManager Instance { get; private set; }

        [Header("BGM")]
        [SerializeField] private AudioClip titleBgm;
        [SerializeField] private AudioClip gameplayBgm;
        [SerializeField][Range(0f, 1f)] private float bgmVolume = 0.35f;
        [SerializeField] private float bgmFadeDuration = 1.2f;

        [Header("SE")]
        [SerializeField] private AudioClip sePickup;
        [SerializeField] private AudioClip sePlaceSuccess;
        [SerializeField] private AudioClip sePlaceFail;
        [SerializeField] private AudioClip seJointConnect;
        [SerializeField] private AudioClip seSeismicStart;
        [SerializeField] private AudioClip seClear;
        [SerializeField] private AudioClip seFail;
        [SerializeField] private AudioClip seButtonClick;
        [SerializeField][Range(0f, 1f)] private float seVolume = 0.75f;

        private AudioSource _bgmSource;
        private AudioSource _seSource;
        private Coroutine _fadeCoroutine;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _bgmSource = gameObject.AddComponent<AudioSource>();
            _bgmSource.loop = true;
            _bgmSource.volume = bgmVolume;
            _bgmSource.spatialBlend = 0f;

            _seSource = gameObject.AddComponent<AudioSource>();
            _seSource.loop = false;
            _seSource.volume = seVolume;
            _seSource.spatialBlend = 0f;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDestroy()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            switch (scene.name)
            {
                case "Title":      PlayBgm(titleBgm);    break;
                case "StageSelect":PlayBgm(titleBgm);    break;
                case "Gameplay":   PlayBgm(gameplayBgm); break;
            }
        }

        // ─── BGM ───

        public void PlayBgm(AudioClip clip)
        {
            if (clip == null) return;
            if (_bgmSource.clip == clip && _bgmSource.isPlaying) return;

            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(CrossFadeBgm(clip));
        }

        private IEnumerator CrossFadeBgm(AudioClip next)
        {
            float halfDur = bgmFadeDuration * 0.5f;
            float start = _bgmSource.volume;

            for (float t = 0; t < halfDur; t += Time.unscaledDeltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(start, 0f, t / halfDur);
                yield return null;
            }

            _bgmSource.clip = next;
            _bgmSource.Play();

            for (float t = 0; t < halfDur; t += Time.unscaledDeltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(0f, bgmVolume, t / halfDur);
                yield return null;
            }
            _bgmSource.volume = bgmVolume;
        }

        public void StopBgm()
        {
            if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
            _fadeCoroutine = StartCoroutine(FadeOutBgm());
        }

        private IEnumerator FadeOutBgm()
        {
            float start = _bgmSource.volume;
            for (float t = 0; t < bgmFadeDuration; t += Time.unscaledDeltaTime)
            {
                _bgmSource.volume = Mathf.Lerp(start, 0f, t / bgmFadeDuration);
                yield return null;
            }
            _bgmSource.Stop();
            _bgmSource.volume = bgmVolume;
        }

        // ─── SE ───

        public void PlayPickup()       => PlaySe(sePickup);
        public void PlayPlaceSuccess() => PlaySe(sePlaceSuccess);
        public void PlayPlaceFail()    => PlaySe(sePlaceFail);
        public void PlayJointConnect() => PlaySe(seJointConnect);
        public void PlaySeismicStart() => PlaySe(seSeismicStart);
        public void PlayClear()        => PlaySe(seClear);
        public void PlayFail()         => PlaySe(seFail);
        public void PlayButtonClick()  => PlaySe(seButtonClick);

        private void PlaySe(AudioClip clip)
        {
            if (clip == null) return;
            _seSource.PlayOneShot(clip, seVolume);
        }

        // ─── ボリューム ───

        public void SetBgmVolume(float v)
        {
            bgmVolume = v;
            _bgmSource.volume = v;
        }

        public void SetSeVolume(float v)
        {
            seVolume = v;
        }
    }
}
