using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 木材のしなり（曲げたわみ）を計算し、ビジュアルにフィードバックするコンポーネント。
    /// 梁の等分布荷重モデルに基づき、最大たわみとたわみ曲線を求める。
    /// </summary>
    public class BendingSimulator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;
        [SerializeField] private ForceFlowCalculator forceCalculator;

        [Header("たわみ表示")]
        [Tooltip("たわみの視覚的誇張係数（1.0=実寸、5.0=5倍誇張）")]
        [SerializeField] private float deflectionExaggeration = 5f;

        [Tooltip("たわみ線のアニメーション速度")]
        [SerializeField] private float animationSpeed = 3f;

        [Tooltip("危険なたわみ量（尺単位）。この値以上で赤表示。")]
        [SerializeField] private float dangerDeflection = 0.05f;

        // ピース → たわみ計算結果
        private Dictionary<PlacedPiece, BendingResult> _bendingResults = new();

        public IReadOnlyDictionary<PlacedPiece, BendingResult> BendingResults => _bendingResults;

        public System.Action OnBendingUpdated;

        void OnEnable()
        {
            if (forceCalculator != null)
                forceCalculator.OnForceFlowUpdated += Calculate;
        }

        void OnDisable()
        {
            if (forceCalculator != null)
                forceCalculator.OnForceFlowUpdated -= Calculate;
        }

        /// <summary>全ピースのしなりを計算する</summary>
        public void Calculate()
        {
            _bendingResults.Clear();

            foreach (var piece in grid.GetAllPieces())
            {
                if (piece.data == null) continue;

                var result = CalcBending(piece);
                _bendingResults[piece] = result;

                ApplyDeflectionVisual(piece, result);
            }

            OnBendingUpdated?.Invoke();
        }

        private BendingResult CalcBending(PlacedPiece piece)
        {
            var data = piece.data;

            // 寸法をメートルに変換
            float L = data.length * 0.303f;   // 梁長（m）
            float b = data.width * 0.303f;    // 断面幅（m）
            float h = data.height * 0.303f;   // 断面高（m）

            // 断面二次モーメント I = b*h³/12
            float I = b * Mathf.Pow(h, 3f) / 12f;

            // ヤング率 E（Pa換算）
            float E = data.youngsModulus * 1e6f;

            // 等分布荷重 w（N/m）= 自重 + 外力の分担
            float selfWeight = data.Weight * 9.81f; // N
            float w = selfWeight / Mathf.Max(L, 0.01f);

            // フォースフローから軸力を取得して横荷重に加算
            float additionalLoad = GetAdditionalLoad(piece);
            w += additionalLoad / Mathf.Max(L, 0.01f);

            // 両端支持梁の最大たわみ δ_max = 5wL⁴ / (384EI)
            float EI = E * I;
            float deflectionMeters = EI > 0f
                ? (5f * w * Mathf.Pow(L, 4f)) / (384f * EI)
                : 0f;

            // メートル→尺変換
            float deflectionShaku = deflectionMeters / 0.303f;

            // 断面係数 Z = b*h²/6
            float Z = b * h * h / 6f;

            // 最大曲げ応力 σ = wL²/(8Z)
            float bendingStress = Z > 0f ? (w * L * L) / (8f * Z) / 1e6f : 0f; // MPa

            // たわみ角（支点での傾き）θ = wL³/(24EI)
            float slopeAngle = EI > 0f
                ? Mathf.Rad2Deg * (w * Mathf.Pow(L, 3f)) / (24f * EI)
                : 0f;

            return new BendingResult
            {
                maxDeflection = deflectionShaku,
                maxBendingStress = bendingStress,
                slopeAngleDeg = slopeAngle,
                isDangerous = deflectionShaku > dangerDeflection,
                utilizationRatio = Mathf.Clamp01(bendingStress / Mathf.Max(data.bendingStrength, 1f))
            };
        }

        private float GetAdditionalLoad(PlacedPiece piece)
        {
            float load = 0f;
            foreach (var kv in forceCalculator.ForceResults)
            {
                if (kv.Key.PieceA == piece || kv.Key.PieceB == piece)
                    load += kv.Value.totalForce * 1000f; // kN→N
            }
            return load;
        }

        private void ApplyDeflectionVisual(PlacedPiece piece, BendingResult result)
        {
            if (piece.gameObject == null) return;

            // たわみをY方向の変位として視覚的に表現（誇張付き）
            float visualDelta = -result.maxDeflection * 0.303f * deflectionExaggeration;

            // Lerpで滑らかにアニメーション
            Vector3 current = piece.gameObject.transform.localPosition;
            Vector3 target = current + Vector3.up * visualDelta;
            piece.gameObject.transform.localPosition = Vector3.Lerp(
                current, target, Time.deltaTime * animationSpeed);
        }
    }

    // ─────────────────── 結果クラス ───────────────────

    public class BendingResult
    {
        /// <summary>最大たわみ（尺単位）</summary>
        public float maxDeflection;

        /// <summary>最大曲げ応力（MPa）</summary>
        public float maxBendingStress;

        /// <summary>支点でのたわみ角（度）</summary>
        public float slopeAngleDeg;

        /// <summary>危険なたわみか</summary>
        public bool isDangerous;

        /// <summary>曲げ強度に対する利用率（0〜1）</summary>
        public float utilizationRatio;
    }
}
