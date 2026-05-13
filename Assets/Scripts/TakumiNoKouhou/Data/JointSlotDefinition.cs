using UnityEngine;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 木材ピース上の継手スロット1つを定義する構造体。
    /// WoodPieceData.slots[] に配列として保持される。
    /// </summary>
    [System.Serializable]
    public struct JointSlotDefinition
    {
        [Tooltip("このスロットのID（同一ピース内で一意）")]
        public string slotId;

        [Tooltip("スロットの役割")]
        public SlotRole role;

        [Tooltip("スロットの接合方向")]
        public SlotDirection direction;

        [Tooltip("このスロットが受け入れられる継手形状")]
        public JointShapeType acceptedShape;

        [Tooltip("木材ピースのローカル座標でのスロット位置（尺単位）")]
        public Vector3 localPosition;

        [Tooltip("スロットの向き（ローカル回転）")]
        public Vector3 localEulerAngles;

        [Tooltip("スロットの深さ（尺単位）")]
        [Range(0.05f, 1f)]
        public float depth;

        [Tooltip("スロットの幅（尺単位）")]
        [Range(0.05f, 1f)]
        public float slotWidth;

        [Tooltip("このスロットへの接合が必須かどうか。必須スロットが未接続だとクリア不可。")]
        public bool isRequired;
    }

    /// <summary>スロットの役割：突起側か穴側か</summary>
    public enum SlotRole
    {
        Tenon,   // ほぞ（突起側・差し込む側）
        Mortise, // ほぞ穴（穴側・受け入れる側）
        Either   // どちらでも接続可能（相欠き継ぎ等）
    }

    /// <summary>スロットが力を伝達する方向</summary>
    public enum SlotDirection
    {
        PosX,  // +X方向
        NegX,  // -X方向
        PosY,  // +Y方向（上方）
        NegY,  // -Y方向（下方）
        PosZ,  // +Z方向
        NegZ   // -Z方向
    }
}
