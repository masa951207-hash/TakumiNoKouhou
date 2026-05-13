using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 力の流れを木材ピース・接続ライン上の色で可視化するコンポーネント。
    /// 圧縮=暖色（赤・橙）、引張=寒色（青・水色）、安全=緑 で表現する。
    /// </summary>
    public class StressVisualizer : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;
        [SerializeField] private ForceFlowCalculator forceCalculator;

        [Header("色設定（和の配色）")]
        [Tooltip("圧縮力：最大時の色（深紅）")]
        [SerializeField] private Color compressionMax = new Color(0.75f, 0.1f, 0.05f, 1f);

        [Tooltip("圧縮力：中程度（朱色）")]
        [SerializeField] private Color compressionMid = new Color(0.9f, 0.45f, 0.1f, 1f);

        [Tooltip("引張力：最大時の色（深藍）")]
        [SerializeField] private Color tensionMax = new Color(0.05f, 0.15f, 0.6f, 1f);

        [Tooltip("引張力：中程度（藍色）")]
        [SerializeField] private Color tensionMid = new Color(0.2f, 0.5f, 0.85f, 1f);

        [Tooltip("安全範囲の色（松葉緑）")]
        [SerializeField] private Color safeColor = new Color(0.2f, 0.5f, 0.25f, 1f);

        [Tooltip("接続なし・未計算の色（木材の素の色）")]
        [SerializeField] private Color neutralColor = new Color(0.7f, 0.5f, 0.3f, 1f);

        [Header("閾値設定")]
        [Tooltip("この力以上を「危険」と判定する閾値（kN）")]
        [SerializeField] private float dangerThreshold = 30f;

        [Tooltip("この力以下を「安全」と判定する閾値（kN）")]
        [SerializeField] private float safeThreshold = 5f;

        [Header("接続ライン")]
        [Tooltip("スロット接続を示すラインの太さ")]
        [SerializeField] private float connectionLineWidth = 0.015f;

        [Tooltip("接続ラインを表示するか")]
        [SerializeField] private bool showConnectionLines = true;

        // ピース → RendererのキャッシュとOriginal色
        private Dictionary<PlacedPiece, List<Renderer>> _pieceRenderers = new();
        private Dictionary<PlacedPiece, Color> _originalColors = new();

        // 接続ライン用LineRenderer
        private Dictionary<SlotConnection, LineRenderer> _connectionLines = new();

        void OnEnable()
        {
            if (forceCalculator != null)
                forceCalculator.OnForceFlowUpdated += Refresh;
        }

        void OnDisable()
        {
            if (forceCalculator != null)
                forceCalculator.OnForceFlowUpdated -= Refresh;
        }

        public void Initialize(PuzzleGrid puzzleGrid, ForceFlowCalculator calculator)
        {
            grid = puzzleGrid;
            forceCalculator = calculator;
            forceCalculator.OnForceFlowUpdated += Refresh;
        }

        /// <summary>力の流れを全ピース・全接続ラインに反映する</summary>
        public void Refresh()
        {
            ResetAll();

            var results = forceCalculator?.ForceResults;
            if (results == null || results.Count == 0) return;

            foreach (var kv in results)
            {
                var conn = kv.Key;
                var force = kv.Value;
                if (conn == null || force == null) continue;

                Color c = CalcStressColor(force);

                // ピースが未登録の場合は自動登録
                if (!_pieceRenderers.ContainsKey(conn.PieceA)) RegisterPiece(conn.PieceA);
                if (!_pieceRenderers.ContainsKey(conn.PieceB)) RegisterPiece(conn.PieceB);

                ApplyColorToPiece(conn.PieceA, c);
                ApplyColorToPiece(conn.PieceB, c);

                if (showConnectionLines)
                    UpdateConnectionLine(conn, c);
            }
        }

        /// <summary>全ピースを元の色に戻す</summary>
        public void ResetAll()
        {
            foreach (var kv in _originalColors)
                ApplyColorToPiece(kv.Key, kv.Value);
        }

        /// <summary>ピースのRendererを登録（WoodPieceControllerから呼び出す）</summary>
        public void RegisterPiece(PlacedPiece piece)
        {
            if (piece.gameObject == null) return;

            var renderers = new List<Renderer>(piece.gameObject.GetComponentsInChildren<Renderer>());
            _pieceRenderers[piece] = renderers;

            if (renderers.Count > 0)
                _originalColors[piece] = renderers[0].material.color;
        }

        public void UnregisterPiece(PlacedPiece piece)
        {
            _pieceRenderers.Remove(piece);
            _originalColors.Remove(piece);
        }

        // ─────────────────── 内部メソッド ───────────────────

        private Color CalcStressColor(ForceResult force)
        {
            if (force.totalForce <= safeThreshold) return safeColor;

            float t = Mathf.InverseLerp(safeThreshold, dangerThreshold, force.totalForce);

            return force.DominantType == ForceType.Compression
                ? Color.Lerp(compressionMid, compressionMax, t)
                : Color.Lerp(tensionMid, tensionMax, t);
        }

        private void ApplyColorToPiece(PlacedPiece piece, Color color)
        {
            if (!_pieceRenderers.TryGetValue(piece, out var renderers)) return;

            foreach (var rend in renderers)
            {
                if (rend == null) continue;
                rend.material.color = color;
            }
        }

        private void UpdateConnectionLine(SlotConnection conn, Color color)
        {
            if (!_connectionLines.TryGetValue(conn, out var lr))
            {
                var go = new GameObject("ConnectionLine");
                go.transform.parent = transform;
                lr = go.AddComponent<LineRenderer>();
                lr.material = new Material(Shader.Find("Sprites/Default"));
                lr.startWidth = connectionLineWidth;
                lr.endWidth = connectionLineWidth;
                lr.positionCount = 2;
                _connectionLines[conn] = lr;
            }

            lr.startColor = color;
            lr.endColor = color;
            lr.SetPosition(0, conn.SlotA.worldPosition);
            lr.SetPosition(1, conn.SlotB.worldPosition);
        }

        public void ClearConnectionLines()
        {
            foreach (var kv in _connectionLines)
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            _connectionLines.Clear();
        }
    }
}
