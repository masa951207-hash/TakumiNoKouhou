using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 1つのパズルステージを定義するScriptableObject。
    /// 使用できる木材・配置済みアンカー・クリア条件を保持する。
    /// </summary>
    [CreateAssetMenu(fileName = "PuzzleStageData", menuName = "匠の工法/パズルステージデータ", order = 3)]
    public class PuzzleStageData : ScriptableObject
    {
        [Header("ステージ基本情報")]
        [Tooltip("ステージ番号")]
        public int stageNumber;

        [Tooltip("ステージ名称（例：第一問 柱と梁の接合）")]
        public string stageName;

        [Tooltip("ステージの解説文（工人の独り言風に記述）")]
        [TextArea(3, 8)]
        public string stageDescription;

        [Tooltip("このステージのサムネイル画像")]
        public Sprite thumbnail;

        [Header("解放条件")]
        [Tooltip("このステージを解放するために必要な前のステージ番号（0=最初から解放）")]
        public int unlockAfterStage = 0;

        [Tooltip("解放に必要な評価ランク（前ステージがこの評価以上でないと開放されない）")]
        public StructuralGrade requiredGrade = StructuralGrade.C;

        [Header("パズル構成")]
        [Tooltip("プレイヤーが使用できる木材ピースの一覧")]
        public WoodPieceInventoryEntry[] availablePieces;

        [Tooltip("最初から固定配置されているアンカーピース（動かせない基礎材）")]
        public AnchorPieceEntry[] anchorPieces;

        [Tooltip("グリッドのサイズ（X×Y セル数）")]
        public Vector2Int gridSize = new Vector2Int(5, 5);

        [Header("荷重条件")]
        [Tooltip("ステージの外力荷重（kN）。屋根荷重・積雪等。")]
        [Range(0f, 100f)]
        public float verticalLoad = 20f;

        [Tooltip("地震動の強度（0〜1）。Phase4 の SeismicLoadApplicator に渡す。")]
        [Range(0f, 1f)]
        public float seismicIntensity = 0.3f;

        [Tooltip("地震の継続時間（秒）")]
        [Range(5f, 60f)]
        public float seismicDuration = 20f;

        [Header("クリア条件")]
        [Tooltip("クリアに必要な最低評価ランク")]
        public StructuralGrade minimumPassGrade = StructuralGrade.C;

        [Tooltip("ステージのクリア解説文")]
        [TextArea(2, 5)]
        public string clearMessage;

        [Tooltip("失敗時の解説文（ヒント含む）")]
        [TextArea(2, 5)]
        public string failMessage;

        [Header("チュートリアルヒント")]
        [Tooltip("このステージで表示するヒントの一覧（順番に表示）")]
        public string[] hints;

        [Header("BGM")]
        [Tooltip("ステージ中のBGMクリップ（nullの場合デフォルト工房BGM）")]
        public AudioClip stageBgm;
    }

    /// <summary>プレイヤーが使える木材ピースの在庫エントリ</summary>
    [System.Serializable]
    public struct WoodPieceInventoryEntry
    {
        [Tooltip("使用できる木材ピースデータ")]
        public WoodPieceData pieceData;

        [Tooltip("使用できる枚数（-1=無制限）")]
        public int count;
    }

    /// <summary>最初から固定配置されているアンカーピースのエントリ</summary>
    [System.Serializable]
    public struct AnchorPieceEntry
    {
        [Tooltip("固定配置するピースデータ")]
        public WoodPieceData pieceData;

        [Tooltip("グリッド座標（セル単位）")]
        public Vector2Int gridPosition;

        [Tooltip("配置回転（0/90/180/270度）")]
        [Range(0, 3)]
        public int rotationStep;

        [Tooltip("このピースをプレイヤーが動かせないよう固定する")]
        public bool isFixed;
    }

    /// <summary>構造評価ランク（StructuralEvaluatorで使用）</summary>
    public enum StructuralGrade
    {
        S = 5, // 最優秀（完璧な木組み）
        A = 4, // 優秀
        B = 3, // 良好
        C = 2, // 合格
        D = 1, // 不合格（修復可能）
        F = 0  // 倒壊
    }
}
