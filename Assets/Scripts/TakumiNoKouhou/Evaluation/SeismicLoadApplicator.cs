using System.Collections;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 構造にP波・S波を模した地震荷重を時系列で加え、
    /// ForceFlowCalculatorを逐次更新することで動的応答を模擬するコンポーネント。
    /// </summary>
    public class SeismicLoadApplicator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private ForceFlowCalculator forceCalculator;
        [SerializeField] private BendingSimulator bendingSimulator;

        [Header("地震パラメータ")]
        [Tooltip("地震の最大加速度（G単位）")]
        [SerializeField] private float peakGroundAcceleration = 0.3f;

        [Tooltip("P波の周波数（Hz）。縦揺れ。")]
        [SerializeField] private float pWaveFrequency = 5f;

        [Tooltip("S波の周波数（Hz）。横揺れ。")]
        [SerializeField] private float sWaveFrequency = 1.5f;

        [Tooltip("P波到達後、S波が始まるまでの時間（秒）")]
        [SerializeField] private float sWaveDelay = 3f;

        [Tooltip("地震継続時間（秒）")]
        [SerializeField] private float duration = 20f;

        [Tooltip("P波の強度比（S波を1.0としたときの比率）")]
        [SerializeField] private float pWaveRelativeIntensity = 0.3f;

        // 基準荷重（ステージデータから設定）
        private float _baseVerticalLoad;
        private float _baseHorizontalLoad;

        // 地震シミュレーション中か
        private bool _isRunning;
        private float _elapsedTime;

        // 最大応答値の記録（StructuralEvaluatorが参照）
        public float MaxVerticalLoad { get; private set; }
        public float MaxHorizontalLoad { get; private set; }
        public float MaxTotalLoad { get; private set; }

        // イベント
        public System.Action OnSeismicStarted;
        public System.Action<SeismicSnapshot> OnSeismicSnapshot;
        public System.Action<SeismicSummary> OnSeismicFinished;

        public bool IsRunning => _isRunning;

        /// <summary>外部から初期化（PuzzleClearSystemが呼ぶ）</summary>
        public void Initialize(float baseVertLoad, float baseHorizLoad, float pgaOverride = -1f)
        {
            _baseVerticalLoad = baseVertLoad;
            _baseHorizontalLoad = baseHorizLoad;
            if (pgaOverride > 0f) peakGroundAcceleration = pgaOverride;
        }

        /// <summary>地震シミュレーションを開始する</summary>
        public void StartSeismic(float durationOverride = -1f)
        {
            if (_isRunning) return;
            if (durationOverride > 0f) duration = durationOverride;
            StartCoroutine(SeismicCoroutine());
        }

        /// <summary>地震を強制停止する</summary>
        public void StopSeismic()
        {
            _isRunning = false;
            StopAllCoroutines();
            // 荷重をベースラインに戻す
            forceCalculator.Calculate(_baseVerticalLoad, _baseHorizontalLoad);
        }

        private IEnumerator SeismicCoroutine()
        {
            _isRunning = true;
            _elapsedTime = 0f;
            MaxVerticalLoad = _baseVerticalLoad;
            MaxHorizontalLoad = _baseHorizontalLoad;
            MaxTotalLoad = 0f;

            OnSeismicStarted?.Invoke();

            while (_elapsedTime < duration)
            {
                float t = _elapsedTime;

                // ─── P波（縦揺れ）───
                float pAmp = t < sWaveDelay
                    ? Mathf.Sin(2f * Mathf.PI * pWaveFrequency * t) * pWaveRelativeIntensity
                    : 0f;

                // ─── S波（横揺れ）———
                float sEnvelope = 0f;
                if (t >= sWaveDelay)
                {
                    float sElapsed = t - sWaveDelay;
                    float sDuration = duration - sWaveDelay;
                    // エンベロープ：台形形状（立ち上がり2秒→最大→減衰）
                    sEnvelope = sElapsed < 2f
                        ? sElapsed / 2f
                        : sElapsed > sDuration - 4f
                            ? Mathf.Max(0f, 1f - (sElapsed - (sDuration - 4f)) / 4f)
                            : 1f;
                }

                float sAmp = sEnvelope *
                    Mathf.Sin(2f * Mathf.PI * sWaveFrequency * t) *
                    peakGroundAcceleration;

                // 荷重に換算（加速度 × 等価質量 × G ≒ kN）
                float vertLoad = _baseVerticalLoad + pAmp * 9.81f * 10f;
                float horizLoad = _baseHorizontalLoad + sAmp * 9.81f * 10f;

                // ForceFlowを更新
                forceCalculator.Calculate(vertLoad, horizLoad);
                bendingSimulator.Calculate();

                // 最大値を記録
                MaxVerticalLoad = Mathf.Max(MaxVerticalLoad, vertLoad);
                MaxHorizontalLoad = Mathf.Max(MaxHorizontalLoad, Mathf.Abs(horizLoad));
                float total = Mathf.Sqrt(vertLoad * vertLoad + horizLoad * horizLoad);
                MaxTotalLoad = Mathf.Max(MaxTotalLoad, total);

                // スナップショットを通知（ResultScreenが使用）
                OnSeismicSnapshot?.Invoke(new SeismicSnapshot
                {
                    time = t,
                    verticalLoad = vertLoad,
                    horizontalLoad = horizLoad,
                    isInPWave = t < sWaveDelay,
                    isInSWave = t >= sWaveDelay
                });

                _elapsedTime += Time.deltaTime;
                yield return null;
            }

            // 終了：荷重をベースに戻す
            forceCalculator.Calculate(_baseVerticalLoad, _baseHorizontalLoad);

            _isRunning = false;

            OnSeismicFinished?.Invoke(new SeismicSummary
            {
                maxVerticalLoad = MaxVerticalLoad,
                maxHorizontalLoad = MaxHorizontalLoad,
                maxTotalLoad = MaxTotalLoad,
                pgaAchieved = peakGroundAcceleration
            });
        }
    }

    // ─────────────────── データクラス ───────────────────

    public class SeismicSnapshot
    {
        public float time;
        public float verticalLoad;
        public float horizontalLoad;
        public bool isInPWave;
        public bool isInSWave;
    }

    public class SeismicSummary
    {
        public float maxVerticalLoad;
        public float maxHorizontalLoad;
        public float maxTotalLoad;
        public float pgaAchieved;
    }
}
