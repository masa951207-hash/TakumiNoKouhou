using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 木材ピースの物性・外観・スロット構成を定義するScriptableObject。
    /// パズルで使用する1つの木材部材を表す。
    /// </summary>
    [CreateAssetMenu(fileName = "WoodPieceData", menuName = "匠の工法/木材ピースデータ", order = 2)]
    public class WoodPieceData : ScriptableObject
    {
        [Header("基本情報")]
        [Tooltip("木材の名称（例：桁・梁・柱・束）")]
        public string pieceName;

        [Tooltip("木材の説明")]
        [TextArea(2, 4)]
        public string description;

        [Header("寸法（尺単位）")]
        [Tooltip("長さ（尺）")]
        [Range(0.5f, 10f)]
        public float length = 2f;

        [Tooltip("幅（尺）")]
        [Range(0.3f, 3f)]
        public float width = 0.5f;

        [Tooltip("高さ（尺）")]
        [Range(0.3f, 3f)]
        public float height = 0.5f;

        [Header("木材種別")]
        public WoodSpecies species;

        [Header("物性値")]
        [Tooltip("木材の密度（kg/m³）")]
        [Range(300f, 900f)]
        public float density = 450f;

        [Tooltip("ヤング率（弾性係数）MPa。曲げ剛性に使用。")]
        [Range(5000f, 15000f)]
        public float youngsModulus = 9000f;

        [Tooltip("曲げ強度（MPa）。破壊荷重の計算に使用。")]
        [Range(20f, 80f)]
        public float bendingStrength = 40f;

        [Tooltip("圧縮強度（MPa）。軸力方向の耐力。")]
        [Range(15f, 60f)]
        public float compressionStrength = 30f;

        [Tooltip("自重（kg）。ゲーム内の荷重計算用。")]
        public float Weight => density * (length * 0.303f) * (width * 0.303f) * (height * 0.303f);

        [Header("スロット定義")]
        [Tooltip("この木材ピースが持つ継手スロットの一覧")]
        public JointSlotDefinition[] slots;

        [Header("外観")]
        [Tooltip("木材のマテリアル")]
        public Material woodMaterial;

        [Tooltip("パズルUI上で使用するアイコン")]
        public Sprite pieceIcon;

        [Tooltip("3Dモデル用プレハブ")]
        public GameObject piecePrefab;

        [Header("音響")]
        [Tooltip("この木材を叩いた時のSFXクリップ")]
        public AudioClip knockSound;

        [Tooltip("配置時のSFXクリップ")]
        public AudioClip placeSound;
    }

    /// <summary>木材の樹種</summary>
    public enum WoodSpecies
    {
        Sugi,    // 杉（軽量・しなりやすい）
        Hinoki,  // 檜（耐久性・香り高い）
        Matsu,   // 松（硬質・重い）
        Keyaki,  // 欅（高強度・伝統的装飾材）
        Kiri     // 桐（超軽量・湿度調整）
    }
}
