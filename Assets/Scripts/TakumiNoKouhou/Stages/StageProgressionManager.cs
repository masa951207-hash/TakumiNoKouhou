using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// ステージのアンロック・クリア記録・進行状況のセーブ/ロードを管理するシングルトン。
    /// PlayerPrefsを使用してデバイスにデータを保存する。
    /// </summary>
    public class StageProgressionManager : MonoBehaviour
    {
        private static StageProgressionManager _instance;
        public static StageProgressionManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    var go = new GameObject("StageProgressionManager");
                    _instance = go.AddComponent<StageProgressionManager>();
                    DontDestroyOnLoad(go);
                }
                return _instance;
            }
        }

        [Header("全ステージデータ")]
        [Tooltip("全ステージデータの一覧（Resources等から設定）")]
        [SerializeField] private PuzzleStageData[] allStages;

        private const string KEY_PREFIX = "Takumi_Stage_";
        private const string KEY_GRADE = "_Grade";
        private const string KEY_PLAYED = "_Played";
        private const string KEY_TOTAL_SCORE = "_TotalScore";

        // メモリキャッシュ
        private Dictionary<int, StageRecord> _records = new();

        void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
            DontDestroyOnLoad(gameObject);
            LoadAll();
        }

        // ─────────────────── 読み書き ───────────────────

        /// <summary>ステージのクリア結果を保存する</summary>
        public void SaveResult(int stageNumber, StructuralGrade grade, float totalScore)
        {
            string keyGrade = KEY_PREFIX + stageNumber + KEY_GRADE;
            string keyPlayed = KEY_PREFIX + stageNumber + KEY_PLAYED;
            string keyScore = KEY_PREFIX + stageNumber + KEY_TOTAL_SCORE;

            // 既存のデータより良い評価のみ上書き
            int savedGrade = PlayerPrefs.GetInt(keyGrade, -1);
            if ((int)grade > savedGrade)
            {
                PlayerPrefs.SetInt(keyGrade, (int)grade);
                PlayerPrefs.SetFloat(keyScore, totalScore);
            }

            PlayerPrefs.SetInt(keyPlayed, 1);
            PlayerPrefs.Save();

            // キャッシュ更新
            _records[stageNumber] = new StageRecord
            {
                stageNumber = stageNumber,
                bestGrade = (StructuralGrade)PlayerPrefs.GetInt(keyGrade, (int)StructuralGrade.F),
                hasPlayed = true,
                bestTotalScore = PlayerPrefs.GetFloat(keyScore, 0f)
            };
        }

        /// <summary>指定ステージの記録を返す</summary>
        public StageRecord GetRecord(int stageNumber)
        {
            if (_records.TryGetValue(stageNumber, out var record)) return record;

            return new StageRecord
            {
                stageNumber = stageNumber,
                bestGrade = StructuralGrade.F,
                hasPlayed = false,
                bestTotalScore = 0f
            };
        }

        /// <summary>指定ステージが解放済みか返す</summary>
        public bool IsUnlocked(PuzzleStageData stage)
        {
            if (stage.unlockAfterStage == 0) return true;
            var record = GetRecord(stage.unlockAfterStage);
            return record.hasPlayed && record.bestGrade >= stage.requiredGrade;
        }

        /// <summary>クリア済みステージ数を返す</summary>
        public int GetClearedCount()
        {
            int count = 0;
            foreach (var kv in _records)
                if (kv.Value.hasPlayed && kv.Value.bestGrade >= StructuralGrade.C)
                    count++;
            return count;
        }

        /// <summary>全データをリセットする（デバッグ用）</summary>
        public void ResetAll()
        {
            if (allStages == null) return;
            foreach (var stage in allStages)
            {
                PlayerPrefs.DeleteKey(KEY_PREFIX + stage.stageNumber + KEY_GRADE);
                PlayerPrefs.DeleteKey(KEY_PREFIX + stage.stageNumber + KEY_PLAYED);
                PlayerPrefs.DeleteKey(KEY_PREFIX + stage.stageNumber + KEY_TOTAL_SCORE);
            }
            PlayerPrefs.Save();
            _records.Clear();
        }

        private void LoadAll()
        {
            if (allStages == null) return;
            foreach (var stage in allStages)
            {
                int sn = stage.stageNumber;
                bool played = PlayerPrefs.GetInt(KEY_PREFIX + sn + KEY_PLAYED, 0) == 1;
                int grade = PlayerPrefs.GetInt(KEY_PREFIX + sn + KEY_GRADE, (int)StructuralGrade.F);
                float score = PlayerPrefs.GetFloat(KEY_PREFIX + sn + KEY_TOTAL_SCORE, 0f);

                _records[sn] = new StageRecord
                {
                    stageNumber = sn,
                    bestGrade = (StructuralGrade)grade,
                    hasPlayed = played,
                    bestTotalScore = score
                };
            }
        }
    }

    // ─────────────────── データクラス ───────────────────

    public class StageRecord
    {
        public int stageNumber;
        public StructuralGrade bestGrade;
        public bool hasPlayed;
        public float bestTotalScore;
    }
}
