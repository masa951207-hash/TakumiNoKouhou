using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 木材ピースのドラッグ・スナップ・回転・配置を制御するコンポーネント。
    /// EventSystem IPointerHandler を使用し、PC/モバイル両対応。
    /// PhysicsRaycaster がカメラにアタッチされている必要がある。
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class WoodPieceController : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        [Header("参照")]
        [SerializeField] private PuzzleGrid grid;

        [Tooltip("ゴーストプレビュー用マテリアル（半透明）")]
        [SerializeField] private Material ghostMaterial;

        [Header("配置設定")]
        [Tooltip("ドラッグ中のY方向オフセット")]
        [SerializeField] private float dragHeight = 0.5f;

        [Tooltip("スナップ時のスムージング速度")]
        [SerializeField] private float snapSpeed = 20f;

        [Header("音響")]
        [SerializeField] private AudioClip pickupSfx;
        [SerializeField] private AudioClip placeSfx;
        [SerializeField] private AudioClip failSfx;

        // ── 内部状態 ──
        private PlacedPiece _placedPiece;
        private bool _isDragging;
        private Vector3 _targetPosition;
        private GameObject _ghostObject;
        private Camera _mainCamera;
        private AudioSource _audioSource;
        private bool _isFixed;

        // ドラッグ中のスクリーン座標（タッチ/マウス共通）
        private Vector2 _currentScreenPos;

        // イベント
        public System.Action<WoodPieceController> OnPlacedSuccessfully;
        public System.Action<WoodPieceController> OnPickedUp;
        public System.Action<WoodPieceController> OnDragEnded;

        void Awake()
        {
            _mainCamera = Camera.main;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.spatialBlend = 1f;
            _audioSource.volume = 0.7f;
        }

        public void Initialize(PlacedPiece piece, PuzzleGrid puzzleGrid, bool isFixed)
        {
            _placedPiece = piece;
            grid = puzzleGrid;
            _isFixed = isFixed;
            _targetPosition = transform.position;
        }

        void Update()
        {
            // キーボード R でドラッグ中に回転（PC向け）
            if (_isDragging && Keyboard.current != null &&
                Keyboard.current.rKey.wasPressedThisFrame)
            {
                RotatePiece();
            }

            if (!_isDragging) SmoothSnapToTarget();
        }

        // ─────────────────── EventSystem ───────────────────

        public void OnPointerDown(PointerEventData eventData)
        {
            if (_isFixed || _placedPiece == null) return;
            _currentScreenPos = eventData.position;
            BeginDrag();
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_isDragging) return;
            _currentScreenPos = eventData.position;
            UpdateDragPosition();
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (_isDragging) EndDrag();
        }

        // ─────────────────── ドラッグロジック ───────────────────

        private void BeginDrag()
        {
            _isDragging = true;
            grid.UnregisterPiece(_placedPiece);
            _ghostObject = CreateGhost();
            PlayPickupSound();
            OnPickedUp?.Invoke(this);
        }

        private void EndDrag()
        {
            _isDragging = false;

            var targetCell = GetGridCellAtScreen(_currentScreenPos);
            var cells = CalcOccupiedCells(targetCell, _placedPiece.rotationStep);

            if (grid.CanPlace(cells))
            {
                _placedPiece.anchorCell = targetCell;
                _placedPiece.SetOccupiedCells(cells);
                grid.RegisterPiece(_placedPiece, cells);
                _targetPosition = grid.GridToWorld(targetCell);
                _targetPosition.y = 0f;
                PlayPlaceSound();
                OnPlacedSuccessfully?.Invoke(this);
            }
            else
            {
                // 元の位置へ戻す
                // anchorCell.x<0 はステージングエリア（グリッド未登録）のピース
                if (_placedPiece.anchorCell.x >= 0 &&
                    _placedPiece.OccupiedCells != null &&
                    _placedPiece.OccupiedCells.Count > 0)
                {
                    grid.RegisterPiece(_placedPiece, _placedPiece.OccupiedCells);
                    _targetPosition = grid.GridToWorld(_placedPiece.anchorCell);
                    _targetPosition.y = 0f;
                }
                // anchorCell<0 の場合は _targetPosition = Initialize時の元の世界座標に戻る
                PlayFailSound();
            }

            DestroyGhost();
            OnDragEnded?.Invoke(this);
        }

        private void UpdateDragPosition()
        {
            Ray ray = _mainCamera.ScreenPointToRay(_currentScreenPos);
            Plane plane = new Plane(Vector3.up, Vector3.up * dragHeight);

            if (plane.Raycast(ray, out float enter))
            {
                transform.position = ray.GetPoint(enter);

                if (_ghostObject != null)
                {
                    var snapCell = GetGridCellAtScreen(_currentScreenPos);
                    var cells = CalcOccupiedCells(snapCell, _placedPiece.rotationStep);
                    bool canPlace = grid.CanPlace(cells);
                    _ghostObject.transform.position = grid.GridToWorld(snapCell);
                    SetGhostColor(canPlace);
                }
            }
        }

        private void SmoothSnapToTarget()
        {
            if (Vector3.Distance(transform.position, _targetPosition) > 0.001f)
                transform.position = Vector3.Lerp(transform.position, _targetPosition, Time.deltaTime * snapSpeed);
        }

        // ─────────────────── 回転 ───────────────────

        private void RotatePiece()
        {
            _placedPiece.rotationStep = (_placedPiece.rotationStep + 1) % 4;
            transform.rotation = Quaternion.Euler(0f, _placedPiece.rotationStep * 90f, 0f);
        }

        /// <summary>モバイルUIボタンから回転を呼ぶ（PuzzleHUDから呼び出す）</summary>
        public void RotateFromUI()
        {
            if (_isDragging) RotatePiece();
        }

        // ─────────────────── ユーティリティ ───────────────────

        private Vector2Int GetGridCellAtScreen(Vector2 screenPos)
        {
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            Plane plane = new Plane(Vector3.up, Vector3.zero);
            if (plane.Raycast(ray, out float enter))
                return grid.WorldToGrid(ray.GetPoint(enter));
            return _placedPiece?.anchorCell ?? Vector2Int.zero;
        }

        /// <summary>アンカーセルと回転ステップからこのピースが占有するセル一覧を計算する</summary>
        public List<Vector2Int> CalcOccupiedCells(Vector2Int anchor, int rotStep)
        {
            var result = new List<Vector2Int>();
            if (_placedPiece?.data == null) return result;

            // 尺単位の長さ・幅をセル数（切り上げ1以上）に変換
            int lengthCells = Mathf.Max(1, Mathf.RoundToInt(_placedPiece.data.length));
            int widthCells  = Mathf.Max(1, Mathf.RoundToInt(_placedPiece.data.width));

            for (int x = 0; x < lengthCells; x++)
            {
                for (int z = 0; z < widthCells; z++)
                {
                    Vector2Int offset = rotStep switch
                    {
                        1 => new Vector2Int(-z, x),
                        2 => new Vector2Int(-x, -z),
                        3 => new Vector2Int(z, -x),
                        _ => new Vector2Int(x, z)
                    };
                    result.Add(anchor + offset);
                }
            }
            return result;
        }

        private GameObject CreateGhost()
        {
            var ghost = Instantiate(gameObject, transform.position, transform.rotation);
            ghost.name = "Ghost_" + gameObject.name;

            foreach (var col in ghost.GetComponentsInChildren<Collider>())
                col.enabled = false;

            if (ghost.TryGetComponent<WoodPieceController>(out var ctrl))
                Destroy(ctrl);

            // EventSystemハンドラーも削除
            foreach (var comp in ghost.GetComponents<MonoBehaviour>())
            {
                if (comp is IPointerDownHandler || comp is IDragHandler || comp is IPointerUpHandler)
                    Destroy(comp);
            }

            if (ghostMaterial != null)
                foreach (var rend in ghost.GetComponentsInChildren<Renderer>())
                    rend.material = ghostMaterial;

            return ghost;
        }

        private void SetGhostColor(bool canPlace)
        {
            if (_ghostObject == null) return;
            Color c = canPlace
                ? new Color(0.2f, 0.8f, 0.2f, 0.4f)
                : new Color(0.8f, 0.2f, 0.2f, 0.4f);

            foreach (var rend in _ghostObject.GetComponentsInChildren<Renderer>())
            {
                if (rend.material != null)
                    rend.material.color = c;
            }
        }

        private void DestroyGhost()
        {
            if (_ghostObject != null) { Destroy(_ghostObject); _ghostObject = null; }
        }

        private void PlaySfx(AudioClip clip)
        {
            if (clip != null) _audioSource.PlayOneShot(clip);
        }

        private void PlayPickupSound()
        {
            if (pickupSfx != null) _audioSource.PlayOneShot(pickupSfx);
            else GameAudioManager.Instance?.PlayPickup();
        }

        private void PlayPlaceSound()
        {
            if (placeSfx != null) _audioSource.PlayOneShot(placeSfx);
            else GameAudioManager.Instance?.PlayPlaceSuccess();
        }

        private void PlayFailSound()
        {
            if (failSfx != null) _audioSource.PlayOneShot(failSfx);
            else GameAudioManager.Instance?.PlayPlaceFail();
        }
    }
}
