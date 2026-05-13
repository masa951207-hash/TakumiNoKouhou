using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// パズルのクリア判定・スコア記録・次ステージアンロックを統括するコンポーネント。
    /// 地震シミュレーション終了後にStructuralEvaluatorの結果を受けてクリア判定を行う。
    /// </summary>
    public class PuzzleClearSystem : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private SeismicLoadApplicator seismicApplicator;
        [SerializeField] private StructuralEvaluator structuralEvaluator;
        [SerializeField] private ResonanceSimulator resonanceSimulator;
        [SerializeField] private ForceFlowCalculator forceCalculator;
        [SerializeField] private BendingSimulator bendingSimulator;

        [Header("現在のステージ")]
        [SerializeField] private PuzzleStageData currentStage;

        // クリア状態
        private bool _hasTested;
        private StructuralEvaluationResult _lastEvalResult;

        // イベント
        public System.Action<PuzzleClearResult> OnClearJudged;
        public System.Action OnTestStarted;

        void OnEnable()
        {
            if (seismicApplicator != null)
                seismicApplicator.OnSeismicFinished += HandleSeismicFinished;
        }

        void OnDisable()
        {
            if (seismicApplicator != null)
                seismicApplicator.OnSeismicFinished -= HandleSeismicFinished;
        }

        /// <summary>ステージデータを設定して初期化する（GameManagerが呼ぶ）</summary>
        public void LoadStage(PuzzleStageData stage)
        {
            currentStage = stage;
            _hasTested = false;
            _lastEvalResult = null;

            seismicApplicator.Initialize(
                stage.verticalLoad,
                0f,
                stage.seismicIntensity * 0.5f
            );
        }

        /// <summary>地震テストを実行する（UIボタンから呼ぶ）</summary>
        public void RunTest()
        {
            if (seismicApplicator.IsRunning) return;

            // 共振計算を事前実行
            resonanceSimulator.Calculate();

            OnTestStarted?.Invoke();
            seismicApplicator.StartSeismic(currentStage.seismicDuration);
        }

        private void HandleSeismicFinished(SeismicSummary summary)
        {
            _hasTested = true;

            // 構造評価
            _lastEvalResult = structuralEvaluator.Evaluate(summary);

            // クリア判定
            bool isPassed = _lastEvalResult.grade >= currentStage.minimumPassGrade;

            var clearResult = new PuzzleClearResult
            {
                stageData = currentStage,
                evaluationResult = _lastEvalResult,
                seismicSummary = summary,
                isPassed = isPassed,
                resultMessage = isPassed ? currentStage.clearMessage : currentStage.failMessage
            };

            // クリアならセーブ
            if (isPassed)
                StageProgressionSave(currentStage.stageNumber, _lastEvalResult.grade);

            OnClearJudged?.Invoke(clearResult);
        }

        /// <summary>
        /// 手動でテスト結果を取得する（ResultScreenからの再表示用）
        /// </summary>
        public StructuralEvaluationResult GetLastEvaluation() => _lastEvalResult;

        public bool HasTested => _hasTested;

        // ─────────────────── セーブ ───────────────────

        private void StageProgressionSave(int stageNumber, StructuralGrade grade)
        {
            string key = $"Stage_{stageNumber}_Grade";
            int saved = PlayerPrefs.GetInt(key, -1);

            // 過去最高評価のみ保存
            if ((int)grade > saved)
            {
                PlayerPrefs.SetInt(key, (int)grade);
                PlayerPrefs.Save();
            }
        }

        public static StructuralGrade LoadSavedGrade(int stageNumber)
        {
            int saved = PlayerPrefs.GetInt($"Stage_{stageNumber}_Grade", -1);
            return saved >= 0 ? (StructuralGrade)saved : StructuralGrade.F;
        }

        public static bool IsStageUnlocked(PuzzleStageData stage)
        {
            if (stage.unlockAfterStage == 0) return true;

            StructuralGrade savedGrade = LoadSavedGrade(stage.unlockAfterStage);
            return savedGrade >= stage.requiredGrade;
        }
    }

    // ─────────────────── 結果クラス ───────────────────

    public class PuzzleClearResult
    {
        public PuzzleStageData stageData;
        public StructuralEvaluationResult evaluationResult;
        public SeismicSummary seismicSummary;
        public bool isPassed;
        public string resultMessage;
    }
}
