using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 地震試験後の構造全体を5カテゴリで採点し、S〜Fの6段階評価を下すクラス。
    /// JointCompatibilityChecker・ResonanceSimulator・BendingSimulatorの結果を統合する。
    /// </summary>
    public class StructuralEvaluator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private SlotSystem slotSystem;
        [SerializeField] private JointCompatibilityChecker compatibilityChecker;
        [SerializeField] private ResonanceSimulator resonanceSimulator;
        [SerializeField] private BendingSimulator bendingSimulator;
        [SerializeField] private ForceFlowCalculator forceCalculator;

        [Header("採点ウェイト")]
        [Tooltip("継手精度スコアのウェイト")]
        [Range(0f, 1f)]
        [SerializeField] private float jointPrecisionWeight = 0.30f;

        [Tooltip("力分散スコアのウェイト")]
        [Range(0f, 1f)]
        [SerializeField] private float forceDistributionWeight = 0.25f;

        [Tooltip("共振耐性スコアのウェイト")]
        [Range(0f, 1f)]
        [SerializeField] private float resonanceWeight = 0.20f;

        [Tooltip("しなり適正スコアのウェイト")]
        [Range(0f, 1f)]
        [SerializeField] private float bendingWeight = 0.15f;

        [Tooltip("必須スロット接続完了のウェイト")]
        [Range(0f, 1f)]
        [SerializeField] private float connectionCompletenessWeight = 0.10f;

        /// <summary>地震試験結果を受けて総合評価を計算する</summary>
        public StructuralEvaluationResult Evaluate(SeismicSummary seismicSummary)
        {
            var result = new StructuralEvaluationResult();

            // ─── 1. 継手精度スコア ───
            var connections = slotSystem.GetAllConnections();
            var evaluations = compatibilityChecker.EvaluateAllConnections(connections);

            float avgPrecision = 0f;
            float avgCompatibility = 0f;
            int validCount = 0;

            foreach (var eval in evaluations)
            {
                if (!eval.isCompatible) continue;
                avgPrecision += eval.precisionScore;
                avgCompatibility += eval.compatibilityScore;
                validCount++;
            }

            if (validCount > 0)
            {
                avgPrecision /= validCount;
                avgCompatibility /= validCount;
            }

            result.jointPrecisionScore = avgPrecision;
            result.jointCompatibilityScore = avgCompatibility;
            float jointScore = (avgPrecision + avgCompatibility) * 0.5f;

            // ─── 2. 力分散スコア ───
            float forceDistScore = CalcForceDistributionScore();
            result.forceDistributionScore = forceDistScore;

            // ─── 3. 共振耐性スコア ───
            float resonanceScore = CalcResonanceScore();
            result.resonanceScore = resonanceScore;

            // ─── 4. しなり適正スコア ───
            float bendScore = CalcBendingScore();
            result.bendingScore = bendScore;

            // ─── 5. 必須スロット接続スコア ───
            float connectionScore = slotSystem.AllRequiredSlotsConnected() ? 1.0f : 0f;
            result.connectionCompletenessScore = connectionScore;

            // ─── 地震最大荷重ペナルティ ───
            // 地震で過大荷重がかかった場合は減点
            float seismicPenalty = seismicSummary != null
                ? Mathf.Clamp01(seismicSummary.maxTotalLoad / 200f)
                : 0f;

            // ─── 加重平均 ───
            float totalScore =
                jointScore * jointPrecisionWeight +
                forceDistScore * forceDistributionWeight +
                resonanceScore * resonanceWeight +
                bendScore * bendingWeight +
                connectionScore * connectionCompletenessWeight;

            // ペナルティ適用
            totalScore = Mathf.Clamp01(totalScore * (1f - seismicPenalty * 0.3f));

            result.totalScore = totalScore;
            result.grade = ScoreToGrade(totalScore);
            result.gradeComment = BuildGradeComment(result);

            return result;
        }

        private float CalcForceDistributionScore()
        {
            var forceResults = forceCalculator.ForceResults;
            if (forceResults.Count == 0) return 0f;

            // 各接続の力のばらつきが少ないほど高スコア（均等分散が理想）
            var forces = new List<float>();
            foreach (var kv in forceResults)
                forces.Add(kv.Value.totalForce);

            if (forces.Count == 0) return 0f;

            float mean = 0f;
            foreach (var f in forces) mean += f;
            mean /= forces.Count;

            float variance = 0f;
            foreach (var f in forces) variance += (f - mean) * (f - mean);
            variance /= forces.Count;

            float cv = mean > 0f ? Mathf.Sqrt(variance) / mean : 1f;
            return Mathf.Clamp01(1f - cv);
        }

        private float CalcResonanceScore()
        {
            var res = resonanceSimulator.LastResult;
            if (res == null) return 0.5f;

            return res.resonanceRisk switch
            {
                ResonanceRisk.Safe => 1.0f,
                ResonanceRisk.Moderate => 0.5f,
                ResonanceRisk.Critical => 0.1f,
                _ => 0.5f
            };
        }

        private float CalcBendingScore()
        {
            var bendResults = bendingSimulator.BendingResults;
            if (bendResults.Count == 0) return 1f;

            float totalUtil = 0f;
            int count = 0;

            foreach (var kv in bendResults)
            {
                totalUtil += kv.Value.utilizationRatio;
                count++;
            }

            float avgUtil = count > 0 ? totalUtil / count : 0f;
            // 利用率が低いほど余裕がある → 高スコア
            return Mathf.Clamp01(1f - avgUtil);
        }

        private StructuralGrade ScoreToGrade(float score)
        {
            return score switch
            {
                >= 0.90f => StructuralGrade.S,
                >= 0.75f => StructuralGrade.A,
                >= 0.60f => StructuralGrade.B,
                >= 0.45f => StructuralGrade.C,
                >= 0.25f => StructuralGrade.D,
                _ => StructuralGrade.F
            };
        }

        private string BuildGradeComment(StructuralEvaluationResult r)
        {
            return r.grade switch
            {
                StructuralGrade.S => "見事な木組みです。力が均等に流れ、揺れをしなやかに受け止めました。",
                StructuralGrade.A => "優れた接合です。細部の精度をさらに磨けば完璧になります。",
                StructuralGrade.B => "良い構造です。継手の向きと力の流れをもう少し意識してみましょう。",
                StructuralGrade.C => "なんとか耐えましたが、改善の余地があります。",
                StructuralGrade.D => "構造が不安定です。必須の継手を確認してください。",
                StructuralGrade.F => "倒壊しました。力の流れを見直し、しなりのある継手を加えてください。",
                _ => ""
            };
        }
    }

    // ─────────────────── 結果クラス ───────────────────

    public class StructuralEvaluationResult
    {
        public float jointPrecisionScore;
        public float jointCompatibilityScore;
        public float forceDistributionScore;
        public float resonanceScore;
        public float bendingScore;
        public float connectionCompletenessScore;
        public float totalScore;
        public StructuralGrade grade;
        public string gradeComment;
    }
}
