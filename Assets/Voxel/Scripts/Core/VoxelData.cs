using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// 個別ボクセル情報を定義するScriptableObject
    /// ID、色、硬度などのボクセル固有データを管理
    /// </summary>
    [CreateAssetMenu(fileName = "New VoxelData", menuName = "VoxelWorld/VoxelData")]
    public class VoxelData : ScriptableObject
    {
        [Header("基本情報")]
        //識別ID
        [SerializeField] private byte m_voxelId;
        
        //表示名
        [SerializeField] private string m_displayName = "Default VoxelID";

        [Header("視覚設定")]
        //基本色
        [SerializeField] private Color m_color = Color.white;

        [Header("物理特性")]
        //硬度
        [SerializeField, Range(0.1f, 100f)] private float m_hardness = 1.0f;
        //最大耐久度
        [SerializeField, Range(1f, 1000f)] private short m_maxDurability = 100;
        //破壊可能かどうか
        [SerializeField] private bool m_isDestructible = true;

        // プロパティ
        //ボクセルID
        public byte VoxelId { get => m_voxelId; set => m_voxelId = value; }
        //表示名
        public string DisplayName { get => m_displayName; set => m_displayName = value; }
        //基本色
        public Color Color { get => m_color; set => m_color = value; }
        //硬度
        public float Hardness { get => m_hardness; set => m_hardness = value; }
        //最大耐久度
        public short MaxDurability { get => m_maxDurability; set => m_maxDurability = value; }
        //破壊可能フラグ
        public bool IsDestructible { get => m_isDestructible; set => m_isDestructible = value; }

        /// <summary>
        /// 指定された攻撃力でボクセルが破壊可能かチェック
        /// </summary>
        /// <param name="attackPower">攻撃力</param>
        /// <returns>破壊可能な場合true</returns>
        public bool CanBeDestroyed(float attackPower)
        {
            return m_isDestructible && attackPower >= m_hardness;
        }

        /// <summary>
        /// ボクセルデータの妥当性をチェック
        /// </summary>
        /// <returns>妥当性チェック結果</returns>
        public bool IsValid()
        {
            return m_voxelId >= VoxelConstants.BASE_VOXEL_ID_START && 
                   m_voxelId < VoxelConstants.MAX_VOXEL_TYPES &&
                   m_hardness > 0f &&
                   !string.IsNullOrEmpty(m_displayName);
        }
    }
}