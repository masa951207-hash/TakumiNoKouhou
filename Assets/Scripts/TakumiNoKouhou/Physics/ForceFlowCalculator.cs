using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 荷重から各継手接続を通じた圧縮・引張力の分散を計算するクラス。
    /// グラフ探索（BFS）で力の流れを追跡し、各スロット接続に力値を付与する。
    /// </summary>
    public class ForceFlowCalculator : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;
        [SerializeField] private SlotSystem slotSystem;

        [Header("荷重設定")]
        [Tooltip("鉛直方向の外力荷重（kN）")]
        [SerializeField] private float verticalLoad = 20f;

        [Tooltip("水平方向の外力荷重（kN）。地震・風荷重。")]
        [SerializeField] private float horizontalLoad = 0f;

        [Tooltip("地盤として扱うグリッド行（Y=0が基礎）")]
        [SerializeField] private int groundRow = 0;

        // 最後に計算した力フロー結果
        private Dictionary<SlotConnection, ForceResult> _forceResults = new();

        public IReadOnlyDictionary<SlotConnection, ForceResult> ForceResults => _forceResults;

        // 変更通知イベント（StressVisualizerが購読）
        public System.Action OnForceFlowUpdated;

        /// <summary>
        /// 現在のグリッド状態から力の流れを計算する。
        /// ピース配置・荷重変更のたびに呼び出す。
        /// </summary>
        public void Calculate(float vertLoad, float horizLoad)
        {
            verticalLoad = vertLoad;
            horizontalLoad = horizLoad;
            _forceResults.Clear();

            var connections = slotSystem.GetAllConnections();
            if (connections == null || connections.Count == 0) return;

            // 各ピースの合力を計算（簡易静力学モデル）
            var pieceForces = CalcPieceForces(connections);

            // 接続毎の力を配分
            foreach (var conn in connections)
            {
                var result = new ForceResult();

                // スロット方向と外力の内積で圧縮/引張を判定
                float vDot = Vector3.Dot(conn.SlotA.worldDirection, Vector3.up);
                float hDot = Vector3.Dot(conn.SlotA.worldDirection, Vector3.right);

                float axialForce = vDot * verticalLoad + hDot * horizontalLoad;

                if (axialForce >= 0f)
                {
                    result.compressionForce = axialForce;
                    result.tensionForce = 0f;
                }
                else
                {
                    result.compressionForce = 0f;
                    result.tensionForce = -axialForce;
                }

                // ピース合力から分配された横力（せん断）
                if (pieceForces.TryGetValue(conn.PieceA, out var pf))
                    result.shearForce = pf.shear * 0.5f;

                // 継手の強度係数で実効力を補正
                if (conn.SlotA.definition.acceptedShape != JointShapeType.Generic)
                {
                    // 力の損失（精度が低い継手ほど力を逃がせない → 応力集中）
                    result.stressConcentration = 1f; // JointEvaluationと連携してPhase4で調整
                }

                result.totalForce = Mathf.Sqrt(
                    result.compressionForce * result.compressionForce +
                    result.tensionForce * result.tensionForce +
                    result.shearForce * result.shearForce);

                _forceResults[conn] = result;
            }

            OnForceFlowUpdated?.Invoke();
        }

        public void Calculate() => Calculate(verticalLoad, horizontalLoad);

        private Dictionary<PlacedPiece, PieceForce> CalcPieceForces(List<SlotConnection> connections)
        {
            var result = new Dictionary<PlacedPiece, PieceForce>();
            int heightRange = Mathf.Max(1, grid.Rows - groundRow);

            foreach (var piece in grid.GetAllPieces())
            {
                float weight = piece.data != null ? piece.data.Weight : 0f;

                // anchorCell.y が groundRow を下回る場合も Clamp01 で0に丸める
                float heightRatio = Mathf.Clamp01(
                    (float)(piece.anchorCell.y - groundRow) / heightRange);

                result[piece] = new PieceForce
                {
                    axial = verticalLoad * heightRatio + weight,
                    shear = horizontalLoad * heightRatio
                };
            }

            return result;
        }
    }

    // ─────────────────── 結果データ ───────────────────

    /// <summary>1つの継手接続に作用する力の計算結果</summary>
    public class ForceResult
    {
        /// <summary>圧縮力（kN）</summary>
        public float compressionForce;

        /// <summary>引張力（kN）</summary>
        public float tensionForce;

        /// <summary>せん断力（kN）</summary>
        public float shearForce;

        /// <summary>応力集中係数（1.0=正常、>1.0=集中）</summary>
        public float stressConcentration = 1f;

        /// <summary>合成力（kN）</summary>
        public float totalForce;

        /// <summary>主に圧縮か引張かを返す</summary>
        public ForceType DominantType =>
            compressionForce > tensionForce ? ForceType.Compression : ForceType.Tension;
    }

    public enum ForceType { Compression, Tension, Shear }

    internal struct PieceForce
    {
        public float axial;
        public float shear;
    }
}
