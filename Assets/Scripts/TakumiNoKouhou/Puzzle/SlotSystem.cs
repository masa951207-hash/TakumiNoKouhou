using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// グリッド上のピース間で継手スロットの接続関係を管理するシステム。
    /// 隣接ピースのスロットを照合し、接続可能なペアを登録・解除する。
    /// </summary>
    public class SlotSystem : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;

        [Tooltip("スロット接続の検索半径（グリッドセル単位）")]
        [SerializeField] private float connectionSearchRadius = 0.1f;

        [Header("表示設定")]
        [Tooltip("接続済みスロットのギズモ色")]
        [SerializeField] private Color connectedColor = new Color(0.1f, 0.8f, 0.1f, 0.8f);

        [Tooltip("未接続必須スロットのギズモ色")]
        [SerializeField] private Color requiredUnconnectedColor = new Color(0.9f, 0.1f, 0.1f, 0.8f);

        // 全接続ペアのリスト
        private List<SlotConnection> _connections = new();

        // ピース → そのピースが持つスロット実体のキャッシュ
        private Dictionary<PlacedPiece, List<RuntimeSlot>> _pieceSlots = new();

        public IReadOnlyList<SlotConnection> Connections => _connections;

        public void Initialize(PuzzleGrid puzzleGrid)
        {
            grid = puzzleGrid;
        }

        /// <summary>
        /// ピースが新たに配置された・移動した時に呼び出す。
        /// 隣接ピースのスロットとの接続を再計算する。
        /// </summary>
        public void RefreshConnections(PlacedPiece movedPiece)
        {
            // このピースの旧接続を全解除
            DisconnectAll(movedPiece);

            // このピースのスロット実体を再生成
            _pieceSlots[movedPiece] = BuildRuntimeSlots(movedPiece);

            // 全ピースと照合して接続可能なペアを登録
            foreach (var otherPiece in grid.GetAllPieces())
            {
                if (otherPiece == movedPiece) continue;
                if (!_pieceSlots.TryGetValue(otherPiece, out var otherSlots))
                {
                    otherSlots = BuildRuntimeSlots(otherPiece);
                    _pieceSlots[otherPiece] = otherSlots;
                }

                TryConnect(_pieceSlots[movedPiece], otherSlots);
            }
        }

        /// <summary>指定ピースの全接続を解除し、スロットキャッシュも削除する</summary>
        public void DisconnectAll(PlacedPiece piece)
        {
            _connections.RemoveAll(c => c.PieceA == piece || c.PieceB == piece);

            // 他ピースのスロットの isConnected フラグを再評価する（stale防止）
            foreach (var kv in _pieceSlots)
            {
                if (kv.Key == piece) continue;
                foreach (var slot in kv.Value)
                    slot.isConnected = _connections.Exists(c => c.SlotA == slot || c.SlotB == slot);
            }

            // 移動したピース自身のスロットキャッシュを削除（次の RefreshConnections で再生成）
            _pieceSlots.Remove(piece);
        }

        /// <summary>
        /// 必須スロットが全て接続済みかどうかを返す。
        /// StructuralEvaluatorが呼び出してクリア可否を判断する。
        /// </summary>
        public bool AllRequiredSlotsConnected()
        {
            foreach (var kv in _pieceSlots)
            {
                foreach (var slot in kv.Value)
                {
                    if (slot.definition.isRequired && !slot.isConnected)
                        return false;
                }
            }
            return true;
        }

        /// <summary>全スロット接続を返す（ForceFlowCalculatorが使用）</summary>
        public List<SlotConnection> GetAllConnections() => _connections;

        // ─────────────────── 内部ロジック ───────────────────

        private void TryConnect(List<RuntimeSlot> slotsA, List<RuntimeSlot> slotsB)
        {
            foreach (var sa in slotsA)
            {
                if (sa.isConnected) continue;
                foreach (var sb in slotsB)
                {
                    if (sb.isConnected) continue;
                    if (CanConnect(sa, sb))
                    {
                        var conn = new SlotConnection(sa, sb);
                        _connections.Add(conn);
                        sa.isConnected = true;
                        sb.isConnected = true;
                        return;
                    }
                }
            }
        }

        private bool CanConnect(RuntimeSlot a, RuntimeSlot b)
        {
            // 物理的に近接しているか
            float dist = Vector3.Distance(a.worldPosition, b.worldPosition);
            if (dist > connectionSearchRadius * grid.CellSize) return false;

            // 役割が互いに補完しているか（Tenon ↔ Mortise または Either ↔ Either）
            bool rolesMatch =
                (a.definition.role == SlotRole.Tenon && b.definition.role == SlotRole.Mortise) ||
                (a.definition.role == SlotRole.Mortise && b.definition.role == SlotRole.Tenon) ||
                (a.definition.role == SlotRole.Either && b.definition.role == SlotRole.Either);

            if (!rolesMatch) return false;

            // 形状が互いに受け入れ可能か
            bool shapeMatch =
                a.definition.acceptedShape == b.definition.acceptedShape ||
                a.definition.acceptedShape == JointShapeType.Generic ||
                b.definition.acceptedShape == JointShapeType.Generic;

            return shapeMatch;
        }

        private List<RuntimeSlot> BuildRuntimeSlots(PlacedPiece piece)
        {
            var result = new List<RuntimeSlot>();
            if (piece.data?.slots == null) return result;

            float rotAngle = piece.rotationStep * 90f;
            var rotQ = Quaternion.Euler(0f, rotAngle, 0f);

            Vector3 pieceWorldPos = piece.gameObject != null
                ? piece.gameObject.transform.position
                : grid.GridToWorld(piece.anchorCell);

            foreach (var slotDef in piece.data.slots)
            {
                // ローカルスロット位置を尺→メートル変換して回転を適用
                Vector3 localPosMeters = slotDef.localPosition * 0.303f;
                Vector3 worldPos = pieceWorldPos + rotQ * localPosMeters;

                result.Add(new RuntimeSlot
                {
                    owner = piece,
                    definition = slotDef,
                    worldPosition = worldPos,
                    worldDirection = rotQ * Quaternion.Euler(slotDef.localEulerAngles) * Vector3.forward,
                    isConnected = false
                });
            }

            return result;
        }

        void OnDrawGizmos()
        {
            foreach (var kv in _pieceSlots)
            {
                foreach (var slot in kv.Value)
                {
                    Gizmos.color = slot.isConnected
                        ? connectedColor
                        : (slot.definition.isRequired ? requiredUnconnectedColor : Color.yellow);

                    Gizmos.DrawSphere(slot.worldPosition, 0.03f);
                    Gizmos.DrawRay(slot.worldPosition, slot.worldDirection * 0.1f);
                }
            }

            Gizmos.color = connectedColor;
            foreach (var conn in _connections)
                Gizmos.DrawLine(conn.SlotA.worldPosition, conn.SlotB.worldPosition);
        }
    }

    // ─────────────────── データクラス ───────────────────

    /// <summary>実行時のスロット実体（ワールド座標付き）</summary>
    public class RuntimeSlot
    {
        public PlacedPiece owner;
        public JointSlotDefinition definition;
        public Vector3 worldPosition;
        public Vector3 worldDirection;
        public bool isConnected;
    }

    /// <summary>2つのスロット間の接続を表すクラス</summary>
    public class SlotConnection
    {
        public RuntimeSlot SlotA { get; }
        public RuntimeSlot SlotB { get; }

        public PlacedPiece PieceA => SlotA.owner;
        public PlacedPiece PieceB => SlotB.owner;

        public JointShapeType JointShape => SlotA.definition.acceptedShape;

        public SlotConnection(RuntimeSlot a, RuntimeSlot b)
        {
            SlotA = a;
            SlotB = b;
        }
    }
}
