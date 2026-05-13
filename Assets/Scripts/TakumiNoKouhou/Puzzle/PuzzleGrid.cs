using System.Collections.Generic;
using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// パズルボードのグリッドを管理するコンポーネント。
    /// セル座標 ↔ ワールド座標の変換と、ピース占有状態の管理を行う。
    /// </summary>
    public class PuzzleGrid : MonoBehaviour
    {
        [Header("グリッド設定")]
        [Tooltip("1セルの大きさ（メートル）。1尺=0.303mなので、デフォルト0.303m。")]
        [SerializeField] private float cellSize = 0.303f;

        [Tooltip("グリッドの列数（X方向）")]
        [SerializeField] private int columns = 5;

        [Tooltip("グリッドの行数（Y方向）")]
        [SerializeField] private int rows = 5;

        [Header("表示設定")]
        [Tooltip("グリッド線の表示")]
        [SerializeField] private bool showGrid = true;

        [Tooltip("グリッド線の色（和紙に墨線のイメージ）")]
        [SerializeField] private Color gridLineColor = new Color(0.2f, 0.15f, 0.05f, 0.4f);

        [Tooltip("占有セルのハイライト色")]
        [SerializeField] private Color occupiedColor = new Color(0.6f, 0.3f, 0.1f, 0.3f);

        [Tooltip("配置可能セルのハイライト色")]
        [SerializeField] private Color availableColor = new Color(0.1f, 0.5f, 0.1f, 0.3f);

        // セル座標 → 配置済みピースのマッピング
        private Dictionary<Vector2Int, PlacedPiece> _occupiedCells = new();

        // グリッド表示用LineRenderer
        private LineRenderer _lineRenderer;

        public float CellSize => cellSize;
        public int Columns => columns;
        public int Rows => rows;

        void Awake()
        {
            _lineRenderer = gameObject.AddComponent<LineRenderer>();
            _lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            _lineRenderer.startColor = gridLineColor;
            _lineRenderer.endColor = gridLineColor;
            _lineRenderer.startWidth = 0.01f;
            _lineRenderer.endWidth = 0.01f;
        }

        void Start()
        {
            if (showGrid) DrawGridLines();
        }

        /// <summary>グリッド座標をワールド座標に変換する（セル中央）</summary>
        public Vector3 GridToWorld(Vector2Int cell)
        {
            float x = transform.position.x + (cell.x + 0.5f) * cellSize;
            float y = transform.position.y;
            float z = transform.position.z + (cell.y + 0.5f) * cellSize;
            return new Vector3(x, y, z);
        }

        /// <summary>ワールド座標を最近傍のグリッド座標に変換する</summary>
        public Vector2Int WorldToGrid(Vector3 worldPos)
        {
            float localX = worldPos.x - transform.position.x;
            float localZ = worldPos.z - transform.position.z;
            int col = Mathf.FloorToInt(localX / cellSize);
            int row = Mathf.FloorToInt(localZ / cellSize);
            return new Vector2Int(col, row);
        }

        /// <summary>指定セルがグリッド範囲内かどうかを返す</summary>
        public bool IsInBounds(Vector2Int cell)
        {
            return cell.x >= 0 && cell.x < columns && cell.y >= 0 && cell.y < rows;
        }

        /// <summary>指定セルが空きかどうかを返す</summary>
        public bool IsEmpty(Vector2Int cell)
        {
            return !_occupiedCells.ContainsKey(cell);
        }

        /// <summary>
        /// 指定セル群が全て配置可能か（範囲内かつ空き）を検証する。
        /// </summary>
        public bool CanPlace(IEnumerable<Vector2Int> cells, PlacedPiece excludePiece = null)
        {
            foreach (var cell in cells)
            {
                if (!IsInBounds(cell)) return false;
                if (_occupiedCells.TryGetValue(cell, out var existing) && existing != excludePiece)
                    return false;
            }
            return true;
        }

        /// <summary>指定セル群にピースを登録する</summary>
        public void RegisterPiece(PlacedPiece piece, IEnumerable<Vector2Int> cells)
        {
            foreach (var cell in cells)
                _occupiedCells[cell] = piece;
        }

        /// <summary>指定ピースの全セル登録を解除する</summary>
        public void UnregisterPiece(PlacedPiece piece)
        {
            var toRemove = new List<Vector2Int>();
            foreach (var kv in _occupiedCells)
                if (kv.Value == piece) toRemove.Add(kv.Key);
            foreach (var cell in toRemove)
                _occupiedCells.Remove(cell);
        }

        /// <summary>グリッド全体の占有状態をリセットする</summary>
        public void Clear()
        {
            _occupiedCells.Clear();
        }

        /// <summary>指定セルを占有しているピースを返す（なければnull）</summary>
        public PlacedPiece GetPieceAt(Vector2Int cell)
        {
            return _occupiedCells.TryGetValue(cell, out var p) ? p : null;
        }

        /// <summary>配置済み全ピースを返す（スナップショット — 反復中の変更に安全）</summary>
        public List<PlacedPiece> GetAllPieces()
        {
            var seen = new HashSet<PlacedPiece>();
            var result = new List<PlacedPiece>();
            foreach (var kv in _occupiedCells)
                if (seen.Add(kv.Value)) result.Add(kv.Value);
            return result;
        }

        /// <summary>外部からグリッドサイズをステージデータに合わせて初期化する</summary>
        public void Initialize(Vector2Int size, float newCellSize = 0f)
        {
            columns = size.x;
            rows = size.y;
            if (newCellSize > 0f) cellSize = newCellSize;
            _occupiedCells.Clear();
            if (showGrid) DrawGridLines();
        }

        private void DrawGridLines()
        {
            // LineRendererを使ったグリッド線は簡易実装のため Gizmos で代替
            // 実際のビルドではShaderGraphのPlane＋Tiling Textureを推奨
        }

        void OnDrawGizmos()
        {
            if (!showGrid) return;

            Gizmos.color = gridLineColor;
            Vector3 origin = transform.position;

            for (int x = 0; x <= columns; x++)
            {
                Vector3 start = origin + new Vector3(x * cellSize, 0f, 0f);
                Vector3 end = start + new Vector3(0f, 0f, rows * cellSize);
                Gizmos.DrawLine(start, end);
            }

            for (int z = 0; z <= rows; z++)
            {
                Vector3 start = origin + new Vector3(0f, 0f, z * cellSize);
                Vector3 end = start + new Vector3(columns * cellSize, 0f, 0f);
                Gizmos.DrawLine(start, end);
            }

            // 占有セルを色付き表示
            Gizmos.color = occupiedColor;
            foreach (var kv in _occupiedCells)
            {
                Vector3 center = GridToWorld(kv.Key);
                Gizmos.DrawCube(center, new Vector3(cellSize * 0.9f, 0.01f, cellSize * 0.9f));
            }
        }
    }

    /// <summary>グリッド上に配置されたピースを表すデータクラス</summary>
    [System.Serializable]
    public class PlacedPiece
    {
        public WoodPieceData data;
        public GameObject gameObject;
        public Vector2Int anchorCell;    // 配置の基準セル（左下）
        public int rotationStep;          // 0/1/2/3 → 0/90/180/270度
        public bool isFixed;              // アンカーピース（動かせない）

        public List<Vector2Int> OccupiedCells { get; private set; } = new();

        public void SetOccupiedCells(List<Vector2Int> cells)
        {
            OccupiedCells = cells;
        }
    }
}
