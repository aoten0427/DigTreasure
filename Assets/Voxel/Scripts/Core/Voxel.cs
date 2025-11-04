using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// 個々のボクセル情報を保持する構造体
    /// VoxelDataとの連携によりボクセルの状態を管理
    /// </summary>
    [System.Serializable]
    public class Voxel
    {
        //識別ID
        [SerializeField] private int m_voxelId;
        public int VoxelId => m_voxelId;

        //現在の耐久度
        [SerializeField] private float m_durability;
        public float Durability => m_durability;

        //空ボクセルか判定
        public bool IsEmpty => m_voxelId == VoxelConstants.EMPTY_VOXEL_ID;
        //ボクセルが有効かどうか
        public bool IsValid => !IsEmpty;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        public Voxel(int voxelId)
        {
            m_voxelId = voxelId;

            // VoxelDataから最大耐久度を取得して初期化
            var voxelData = VoxelDataBase.GetVoxelDataStatic(voxelId);
            m_durability = voxelData != null ? voxelData.MaxDurability : VoxelConstants.DEFAULT_MAX_DURABILITY;
        }

        /// <summary>
        /// VoxelDataを取得
        /// </summary>
        /// <returns>対応するVoxelData、空ボクセルの場合はnull</returns>
        public VoxelData GetVoxelData()
        {
            return VoxelDataBase.GetVoxelDataStatic(m_voxelId);
        }

        /// <summary>
        /// ボクセルの色を取得
        /// </summary>
        /// <returns>ボクセルの色</returns>
        public Color GetColor()
        {
            return VoxelDataBase.GetVoxelColorStatic(m_voxelId);
        }

        /// <summary>
        /// ボクセルの硬度を取得
        /// </summary>
        /// <returns>ボクセルの硬度、空の場合は0</returns>
        public float GetHardness()
        {
            if (IsEmpty)
            {
                return 0.0f;
            }

            var voxelData = GetVoxelData();
            return voxelData != null ? voxelData.Hardness : VoxelConstants.DEFAULT_TEST_HARDNESS;
        }


        /// <summary>
        /// 指定された攻撃力でボクセルが破壊可能かチェック
        /// 攻撃力が硬度以上なら耐久度を減少させ、耐久度が0以下になったら破壊可能と判定
        /// </summary>
        /// <param name="attackPower">攻撃力</param>
        /// <param name="updatedVoxel">耐久度が更新されたVoxel（out parameter）</param>
        /// <returns>耐久度が0以下になった場合true</returns>
        public bool CanBeDestroyed(float attackPower)
        {
            // 硬度チェック：攻撃力が硬度未満なら何も起こらない
            if (!VoxelDataBase.CanDestroyVoxelStatic(m_voxelId, attackPower))
            {
                return false;
            }

            // 耐久度を減少
            m_durability -= attackPower;

            Debug.Log($"耐久度: {m_durability}");

            // 耐久度が0以下なら破壊可能
            return m_durability <= 0.0f;
        }

        /// <summary>
        /// ボクセルを空に設定
        /// </summary>
        /// <returns>空に設定されたVoxel</returns>
        public Voxel SetEmpty()
        {
            return new Voxel(VoxelConstants.EMPTY_VOXEL_ID);
        }

        /// <summary>
        /// ボクセルIDを変更
        /// </summary>
        /// <param name="newVoxelId">新しいボクセルID</param>
        /// <returns>変更されたVoxel</returns>
        public Voxel SetVoxelId(int newVoxelId)
        {
            return new Voxel(newVoxelId);
        }

        /// <summary>
        /// 等価比較
        /// </summary>
        /// <param name="other">比較対象</param>
        /// <returns>等しい場合true</returns>
        public bool Equals(Voxel other)
        {
            return m_voxelId == other.m_voxelId;
        }


        // 静的メソッド
        /// <summary>
        /// 空のボクセルを取得
        /// </summary>
        public static Voxel Empty => new Voxel(VoxelConstants.EMPTY_VOXEL_ID);

        /// <summary>
        /// 指定されたIDのボクセルを作成
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <returns>作成されたVoxel</returns>
        public static Voxel Create(int voxelId)
        {
            return new Voxel(voxelId);
        }


        /// <summary>
        /// intからVoxelへの変換
        /// </summary>
        public static implicit operator Voxel(int voxelId)
        {
            return new Voxel(voxelId);
        }

        /// <summary>
        /// Voxelからintへの変換
        /// </summary>
        public static implicit operator int(Voxel voxel)
        {
            return voxel.m_voxelId;
        }
    }
}