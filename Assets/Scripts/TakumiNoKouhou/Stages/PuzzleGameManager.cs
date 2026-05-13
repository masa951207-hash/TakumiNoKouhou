using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// パズルゲームプレイシーン全体のライフサイクルと各システムの初期化を管理するコンポーネント。
    /// SelectedStageHolderからステージデータを受け取り、全サブシステムを連携させる。
    /// </summary>
    public class PuzzleGameManager : MonoBehaviour
    {
        [Header("システム参照")]
        [SerializeField] private PuzzleGrid grid;
        [SerializeField] private SlotSystem slotSystem;
        [SerializeField] private JointCompatibilityChecker compatibilityChecker;
        [SerializeField] private ForceFlowCalculator forceCalculator;
        [SerializeField] private StressVisualizer stressVisualizer;
        [SerializeField] private ResonanceSimulator resonanceSimulator;
        [SerializeField] private BendingSimulator bendingSimulator;
        [SerializeField] private SeismicLoadApplicator seismicApplicator;
        [SerializeField] private StructuralEvaluator structuralEvaluator;
        [SerializeField] private PuzzleClearSystem clearSystem;

        [Header("UI参照")]
        [SerializeField] private PuzzleHUD hud;
        [SerializeField] private JointInfoPanel jointInfoPanel;
        [SerializeField] private ResultScreen resultScreen;
        [SerializeField] private TutorialSystem tutorialSystem;

        [Header("継手データ")]
        [Tooltip("このシーンで使用できる継手タイプの一覧")]
        [SerializeField] private JointTypeData[] availableJointTypes;

        [Header("フォールバック")]
        [Tooltip("SelectedStageHolderにデータがない場合のデフォルトステージ")]
        [SerializeField] private PuzzleStageData fallbackStage;

        private PuzzleStageData _currentStage;

        void Start()
        {
            // ステージデータを取得
            _currentStage = SelectedStageHolder.Stage ?? fallbackStage;
            if (_currentStage == null)
            {
                Debug.LogError("[PuzzleGameManager] ステージデータが設定されていません");
                return;
            }

            InitializeAllSystems();
        }

        private void InitializeAllSystems()
        {
            if (grid == null) { Debug.LogError("[PuzzleGameManager] grid が未設定です"); return; }

            grid.Initialize(_currentStage.gridSize);

            slotSystem?.Initialize(grid);
            stressVisualizer?.Initialize(grid, forceCalculator);
            clearSystem?.LoadStage(_currentStage);

            PlaceAnchorPieces();
            SpawnAvailablePieces();

            if (hud != null)
            {
                hud.Initialize(
                    _currentStage,
                    availableJointTypes,
                    stressVisualizer,
                    clearSystem,
                    resonanceSimulator
                );
                hud.OnTestRequested += OnTestRequested;
            }

            if (clearSystem != null)
            {
                clearSystem.OnClearJudged += OnClearJudged;
                clearSystem.OnTestStarted += OnTestStarted;
            }

            forceCalculator?.Calculate(_currentStage.verticalLoad, 0f);
            resonanceSimulator?.Calculate();
        }

        // ─────────────────── アンカーピース配置 ───────────────────

        private void PlaceAnchorPieces()
        {
            if (_currentStage.anchorPieces == null) return;

            foreach (var entry in _currentStage.anchorPieces)
            {
                if (entry.pieceData == null) continue;

                var go = entry.pieceData.piecePrefab != null
                    ? Instantiate(entry.pieceData.piecePrefab, grid.GridToWorld(entry.gridPosition), Quaternion.Euler(0f, entry.rotationStep * 90f, 0f))
                    : CreateDefaultPieceObject(entry.pieceData, entry.gridPosition, entry.rotationStep);

                var placed = new PlacedPiece
                {
                    data = entry.pieceData,
                    gameObject = go,
                    anchorCell = entry.gridPosition,
                    rotationStep = entry.rotationStep,
                    isFixed = entry.isFixed
                };

                var controller = go.AddComponent<WoodPieceController>();
                controller.Initialize(placed, grid, entry.isFixed);

                // ドラッグイベントをHUD・スロットシステムに接続
                controller.OnPickedUp += c => hud.RegisterDraggingPiece(c);
                controller.OnDragEnded += c =>
                {
                    hud.ClearDraggingPiece();
                    slotSystem.RefreshConnections(placed);
                    stressVisualizer.RegisterPiece(placed);
                    forceCalculator.Calculate();
                    resonanceSimulator.Calculate();
                };

                // セル登録
                var cells = controller.CalcOccupiedCells(entry.gridPosition, entry.rotationStep);
                placed.SetOccupiedCells(cells);
                grid.RegisterPiece(placed, cells);
                slotSystem.RefreshConnections(placed);
                stressVisualizer.RegisterPiece(placed);
            }
        }

        // ─────────────────── 手持ちピース生成（ステージングエリア） ───────────────────

        private void SpawnAvailablePieces()
        {
            if (_currentStage.availablePieces == null || _currentStage.availablePieces.Length == 0)
                return;

            // グリッド手前（カメラ側）に横並びで配置
            float stagingZ = grid.transform.position.z - grid.CellSize * 2.5f;
            float startX   = grid.transform.position.x;
            float spacingX = grid.CellSize * 1.5f;

            int spawnIndex = 0;
            foreach (var entry in _currentStage.availablePieces)
            {
                if (entry.pieceData == null) continue;
                int count = entry.count < 0 ? 2 : entry.count;

                for (int i = 0; i < count; i++)
                {
                    var worldPos = new Vector3(startX + spawnIndex * spacingX, 0f, stagingZ);
                    var go = entry.pieceData.piecePrefab != null
                        ? Instantiate(entry.pieceData.piecePrefab, worldPos, Quaternion.identity)
                        : CreateDefaultPieceObjectAtWorld(entry.pieceData, worldPos, 0);

                    // PlacedPiece はグリッド外なので anchorCell=(-1, spawnIndex) とする
                    var placed = new PlacedPiece
                    {
                        data        = entry.pieceData,
                        gameObject  = go,
                        anchorCell  = new Vector2Int(-1, spawnIndex),
                        rotationStep = 0,
                        isFixed     = false
                    };

                    var controller = go.AddComponent<WoodPieceController>();
                    controller.Initialize(placed, grid, false);

                    controller.OnPickedUp  += c => hud.RegisterDraggingPiece(c);
                    controller.OnDragEnded += c =>
                    {
                        hud.ClearDraggingPiece();
                        slotSystem.RefreshConnections(placed);
                        stressVisualizer.RegisterPiece(placed);
                        forceCalculator.Calculate();
                        resonanceSimulator.Calculate();
                    };

                    spawnIndex++;
                }
            }
        }

        private GameObject CreateDefaultPieceObjectAtWorld(WoodPieceData data, Vector3 worldPos, int rotStep)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = data.pieceName;

            float scaleX = data.length * grid.CellSize;
            float scaleY = data.height * 0.303f;
            float scaleZ = data.width  * grid.CellSize;

            go.transform.position    = worldPos + Vector3.up * (scaleY * 0.5f);
            go.transform.rotation    = Quaternion.Euler(0f, rotStep * 90f, 0f);
            go.transform.localScale  = new Vector3(scaleX, scaleY, scaleZ);

            if (data.woodMaterial != null)
                go.GetComponent<Renderer>().material = data.woodMaterial;

            return go;
        }

        private GameObject CreateDefaultPieceObject(WoodPieceData data, Vector2Int cell, int rotStep)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = data.pieceName;

            // 寸法：尺→メートル（1尺=0.303m）。長さ・幅はセル数相当なのでCellSizeを掛ける。
            // 高さは尺→メートル直接換算。
            float scaleX = data.length * grid.CellSize;
            float scaleY = data.height * 0.303f;
            float scaleZ = data.width * grid.CellSize;

            go.transform.position = grid.GridToWorld(cell) + Vector3.up * (scaleY * 0.5f);
            go.transform.rotation = Quaternion.Euler(0f, rotStep * 90f, 0f);
            go.transform.localScale = new Vector3(scaleX, scaleY, scaleZ);

            if (data.woodMaterial != null)
                go.GetComponent<Renderer>().material = data.woodMaterial;

            return go;
        }

        // ─────────────────── イベントハンドラ ───────────────────

        private void OnTestRequested()
        {
            GameAudioManager.Instance?.PlaySeismicStart();
            resonanceSimulator?.Calculate();
            clearSystem?.RunTest();
        }

        private void OnTestStarted()
        {
            // ピース操作を無効化（地震テスト中は配置変更不可）
            foreach (var ctrl in FindObjectsByType<WoodPieceController>(FindObjectsSortMode.None))
                ctrl.enabled = false;
        }

        private void OnClearJudged(PuzzleClearResult result)
        {
            foreach (var ctrl in FindObjectsByType<WoodPieceController>(FindObjectsSortMode.None))
                ctrl.enabled = true;

            bool passed = result.evaluationResult != null &&
                          (int)result.evaluationResult.grade >= (int)StructuralGrade.C; // C以上で合格
            if (passed) GameAudioManager.Instance?.PlayClear();
            else        GameAudioManager.Instance?.PlayFail();

            hud?.OnTestFinished();
            resultScreen?.ShowResult(result);

            StageProgressionManager.Instance?.SaveResult(
                _currentStage.stageNumber,
                result.evaluationResult.grade,
                result.evaluationResult.totalScore
            );
        }
    }
}
