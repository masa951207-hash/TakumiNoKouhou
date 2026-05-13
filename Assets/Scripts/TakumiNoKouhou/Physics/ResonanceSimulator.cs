using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 構造全体の固有振動数と共振リスクを計算するクラス。
    /// 継手の剛性・ダンピング係数・ピースの質量分布から固有振動数を推定する。
    /// </summary>
    public class ResonanceSimulator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;
        [SerializeField] private SlotSystem slotSystem;
        [SerializeField] private JointCompatibilityChecker compatibilityChecker;

        [Header("地震動設定")]
        [Tooltip("日本の一般的な地震の卓越周波数（Hz）。長周期：0.3Hz、短周期：2.0Hz")]
        [SerializeField] private float seismicFrequency = 1.0f;

        [Header("共振リスク判定")]
        [Tooltip("共振倍率がこの値以上で危険と判定")]
        [SerializeField] private float dangerAmplificationFactor = 3.0f;

        [Tooltip("共振倍率がこの値以下で安全と判定")]
        [SerializeField] private float safeAmplificationFactor = 1.5f;

        // 最後の計算結果
        private ResonanceResult _lastResult;

        public ResonanceResult LastResult => _lastResult;

        // 変更通知
        public System.Action<ResonanceResult> OnResonanceCalculated;

        /// <summary>現在の構造から共振特性を計算する</summary>
        public ResonanceResult Calculate()
        {
            var result = new ResonanceResult();

            var connections = slotSystem.GetAllConnections();
            var evaluations = compatibilityChecker.EvaluateAllConnections(connections);

            // ─── 等価剛性（k）の計算 ───
            float totalStiffness = 0f;
            float totalDamping = 0f;

            foreach (var eval in evaluations)
            {
                if (!eval.isCompatible || eval.jointTypeData == null) continue;

                // 継手剛性 ∝ 圧縮強度 × 引張強度 × 精度スコア
                float jointStiffness =
                    eval.jointTypeData.compressionStrength *
                    eval.jointTypeData.tensionStrength *
                    eval.precisionScore *
                    1000f; // kN/m換算係数

                // 継手ダンピング ∝ 減衰係数 × しなり係数
                float jointDamping =
                    eval.jointTypeData.dampingCoefficient *
                    eval.jointTypeData.bendingFlexibility *
                    eval.precisionScore;

                totalStiffness += jointStiffness;
                totalDamping += jointDamping;
            }

            // 接続がない場合は剛性ゼロ
            if (evaluations.Count == 0)
            {
                result.naturalFrequency = 0f;
                result.dampingRatio = 0f;
                result.amplificationFactor = 999f;
                result.resonanceRisk = ResonanceRisk.Critical;
                _lastResult = result;
                OnResonanceCalculated?.Invoke(result);
                return result;
            }

            float avgStiffness = totalStiffness / evaluations.Count;
            float avgDamping = totalDamping / evaluations.Count;

            // ─── 等価質量（m）の計算 ───
            float totalMass = 0f;
            foreach (var piece in grid.GetAllPieces())
                totalMass += piece.data?.Weight ?? 1f;

            totalMass = Mathf.Max(totalMass, 0.1f);

            // ─── 固有角振動数 ω = √(k/m) ───
            float omega = Mathf.Sqrt(avgStiffness / totalMass);
            result.naturalFrequency = omega / (2f * Mathf.PI);

            // ─── 減衰比 ζ ───
            float criticalDamping = 2f * Mathf.Sqrt(avgStiffness * totalMass);
            result.dampingRatio = Mathf.Clamp(avgDamping * evaluations.Count / criticalDamping, 0.01f, 1f);

            // ─── 動的増幅係数 DAF ───
            float frequencyRatio = seismicFrequency / Mathf.Max(result.naturalFrequency, 0.01f);
            float zeta = result.dampingRatio;

            float denom = Mathf.Sqrt(
                Mathf.Pow(1f - frequencyRatio * frequencyRatio, 2f) +
                Mathf.Pow(2f * zeta * frequencyRatio, 2f));

            result.amplificationFactor = denom > 0.001f ? 1f / denom : 999f;
            result.frequencyRatio = frequencyRatio;

            // ─── リスク判定 ───
            result.resonanceRisk = result.amplificationFactor >= dangerAmplificationFactor
                ? ResonanceRisk.Critical
                : result.amplificationFactor >= safeAmplificationFactor
                    ? ResonanceRisk.Moderate
                    : ResonanceRisk.Safe;

            result.summary = BuildSummary(result);

            _lastResult = result;
            OnResonanceCalculated?.Invoke(result);
            return result;
        }

        private string BuildSummary(ResonanceResult r)
        {
            return r.resonanceRisk switch
            {
                ResonanceRisk.Safe =>
                    $"固有振動数 {r.naturalFrequency:F2}Hz — 地震との共振リスクは低い（DAF={r.amplificationFactor:F2}）",
                ResonanceRisk.Moderate =>
                    $"固有振動数 {r.naturalFrequency:F2}Hz — 共振に注意が必要（DAF={r.amplificationFactor:F2}）",
                ResonanceRisk.Critical =>
                    $"固有振動数 {r.naturalFrequency:F2}Hz — 共振危険！鎌継ぎや蟻継ぎでしなりを加えること（DAF={r.amplificationFactor:F2}）",
                _ => ""
            };
        }
    }

    // ─────────────────── 結果クラス ───────────────────

    public class ResonanceResult
    {
        /// <summary>構造の固有振動数（Hz）</summary>
        public float naturalFrequency;

        /// <summary>減衰比（0〜1）</summary>
        public float dampingRatio;

        /// <summary>地震周波数との比（β = f_seismic / f_natural）</summary>
        public float frequencyRatio;

        /// <summary>動的増幅係数（DAF）</summary>
        public float amplificationFactor;

        /// <summary>共振リスク判定</summary>
        public ResonanceRisk resonanceRisk;

        /// <summary>評価サマリ文（JointInfoPanel等で表示）</summary>
        public string summary;
    }

    public enum ResonanceRisk
    {
        Safe,     // 安全（緑）
        Moderate, // 要注意（黄）
        Critical  // 危険（赤）
    }
}
