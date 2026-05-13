using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TakumiNoKouhou
{
    /// <summary>
    /// 継手種別選択ボタン1つ分のUIコンポーネント。
    /// JointTypeDataを表示し、クリック時にコールバックを呼ぶ。
    /// </summary>
    public class JointTypeButton : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private Button button;
        [SerializeField] private Image selectionHighlight;

        private JointTypeData _data;
        private System.Action<JointTypeData> _onClick;

        public void Setup(JointTypeData data, System.Action<JointTypeData> onClick)
        {
            _data = data;
            _onClick = onClick;

            if (iconImage != null && data.icon != null) iconImage.sprite = data.icon;
            if (nameLabel != null) nameLabel.text = data.jointName;
            if (selectionHighlight != null) selectionHighlight.enabled = false;

            button.onClick.AddListener(() => _onClick?.Invoke(_data));
        }

        public void SetSelected(bool selected)
        {
            if (selectionHighlight != null) selectionHighlight.enabled = selected;
        }
    }

    /// <summary>
    /// 在庫エントリUI1つ分のコンポーネント。
    /// </summary>
    public class InventoryEntryUI : MonoBehaviour
    {
        [SerializeField] private Image pieceIcon;
        [SerializeField] private TextMeshProUGUI pieceNameLabel;
        [SerializeField] private TextMeshProUGUI countLabel;

        public WoodPieceData PieceData { get; private set; }

        public void Setup(WoodPieceData data, int count)
        {
            PieceData = data;
            if (pieceIcon != null && data.pieceIcon != null) pieceIcon.sprite = data.pieceIcon;
            if (pieceNameLabel != null) pieceNameLabel.text = data.pieceName;
            SetCount(count);
        }

        public void SetCount(int count)
        {
            if (countLabel == null) return;
            countLabel.text = count < 0 ? "∞" : $"× {count}";
            countLabel.color = count == 0 ? new Color(0.5f, 0.5f, 0.5f) : Color.white;
        }
    }
}
