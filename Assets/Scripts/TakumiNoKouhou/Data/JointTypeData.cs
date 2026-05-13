using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 継手の種別を定義するScriptableObject。
    /// ほぞ継ぎ・蟻継ぎ・鎌継ぎそれぞれの力学特性を保持する。
    /// </summary>
    [CreateAssetMenu(fileName = "JointTypeData", menuName = "匠の工法/継手データ", order = 1)]
    public class JointTypeData : ScriptableObject
    {
        [Header("基本情報")]
        [Tooltip("継手の名称（例：ほぞ継ぎ、蟻継ぎ、鎌継ぎ）")]
        public string jointName;

        [Tooltip("継手の説明文（JointInfoPanelで表示）")]
        [TextArea(3, 6)]
        public string description;

        [Tooltip("継手のアイコン画像")]
        public Sprite icon;

        [Header("力学特性")]
        [Tooltip("圧縮強度係数 (0〜1)。1に近いほど圧縮力を受け止める。")]
        [Range(0f, 1f)]
        public float compressionStrength = 0.8f;

        [Tooltip("引張強度係数 (0〜1)。1に近いほど引張力に耐える。")]
        [Range(0f, 1f)]
        public float tensionStrength = 0.5f;

        [Tooltip("せん断強度係数 (0〜1)。横からの力への耐性。")]
        [Range(0f, 1f)]
        public float shearStrength = 0.6f;

        [Tooltip("しなり係数 (0〜1)。1に近いほどたわんで力を逃がす。")]
        [Range(0f, 1f)]
        public float bendingFlexibility = 0.3f;

        [Tooltip("共振減衰係数 (0〜1)。振動エネルギーを吸収する能力。")]
        [Range(0f, 1f)]
        public float dampingCoefficient = 0.4f;

        [Header("接合形状")]
        [Tooltip("この継手が使用するスロット形状の種別")]
        public JointShapeType shapeType;

        [Tooltip("接合に必要な木材の厚み（単位：尺、1尺=0.303m）")]
        [Range(0.1f, 2f)]
        public float requiredThickness = 0.5f;

        [Tooltip("接合難易度 (1〜5)。チュートリアルでの解放順序に使用。")]
        [Range(1, 5)]
        public int difficulty = 1;

        [Header("視覚効果")]
        [Tooltip("このジョイントタイプのビーム表示色（StressVisualizerが上書きする前の初期色）")]
        public Color baseColor = Color.white;

        [Tooltip("接合エフェクト用パーティクル")]
        public GameObject jointEffectPrefab;
    }

    /// <summary>継手スロットの形状種別</summary>
    public enum JointShapeType
    {
        Mortise,      // ほぞ（四角い突起＋穴）
        Dovetail,     // 蟻継ぎ（台形の突起）
        Sickle,       // 鎌継ぎ（鎌型の引張継手）
        Splice,       // 相欠き継ぎ（シンプルな段欠き）
        Generic       // 汎用（カスタム形状）
    }
}
