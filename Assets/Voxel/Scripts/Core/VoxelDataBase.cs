using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 複数のVoxelDataを一元管理し、IDベースでのアクセスを提供
    /// </summary>
    [CreateAssetMenu(fileName = "VoxelDataBase", menuName = "VoxelWorld/VoxelDataBase")]
    public class VoxelDataBase : ScriptableObject
    {
        [Header("ボクセルデータ管理")]
        //管理するVoxelDataのリスト
        [SerializeField] private List<VoxelData> m_voxelDataList = new List<VoxelData>();

        // プロパティ
        //登録されているVoxelData
        public int Count => m_voxelDataList.Count;
        
        //すべてのVoxelDataを取得
        public IReadOnlyList<VoxelData> AllVoxelData => m_voxelDataList.AsReadOnly();

        /// <summary>
        /// 指定されたIDのVoxelDataを取得
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <returns>VoxelData、見つからない場合はnull</returns>
        public VoxelData GetVoxelData(int voxelId)
        {
            // 空ボクセルの場合はnullを返す
            if (voxelId == VoxelConstants.EMPTY_VOXEL_ID)
            {
                return null;
            }

            // キャッシュがあればそれを使う（O(1)）
            if (m_voxelDataCache != null && m_voxelDataCache.TryGetValue(voxelId, out var cachedData))
            {
                return cachedData;
            }

            // キャッシュがない場合はLinqで検索（初回またはエディタモード用）
            return m_voxelDataList.FirstOrDefault(data => data != null && data.VoxelId == voxelId);
        }

        /// <summary>
        /// 指定されたIDのVoxelDataが存在するかチェック
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <returns>存在する場合true</returns>
        public bool HasVoxelData(int voxelId)
        {
            if (voxelId == VoxelConstants.EMPTY_VOXEL_ID)
            {
                return true; // 空ボクセルは常に存在するものとして扱う
            }

            return m_voxelDataList.Any(data => data != null && data.VoxelId == voxelId);
        }


        //ResourcesフォルダのVoxelDataBase固定名
        private const string VOXEL_DATABASE_RESOURCE_NAME = "VoxelDataBase";

        //キャッシュされたVoxelDataBase
        private static VoxelDataBase s_cachedInstance = null;

        // パフォーマンス最適化用：VoxelID → VoxelData のDictionaryキャッシュ
        private Dictionary<int, VoxelData> m_voxelDataCache = null;

        // パフォーマンス最適化用：VoxelID → Color のDictionaryキャッシュ
        private Dictionary<int, Color> m_colorCache = null;

        /// <summary>
        /// VoxelDataBaseのインスタンスを取得
        /// </summary>
        /// <returns>VoxelDataBase、見つからない場合はnull</returns>
        public static VoxelDataBase Instance
        {
            get
            {
                if (s_cachedInstance == null)
                {
                    s_cachedInstance = Resources.Load<VoxelDataBase>(VOXEL_DATABASE_RESOURCE_NAME);

                    if (s_cachedInstance == null)
                    {
                        Debug.LogError($"[VoxelDataBase] {VOXEL_DATABASE_RESOURCE_NAME} がResourcesフォルダに見つかりません。");
                    }
                    else
                    {
                        Debug.Log($"[VoxelDataBase] {VOXEL_DATABASE_RESOURCE_NAME} を正常にロードしました。");
                        // キャッシュを初期化
                        s_cachedInstance.InitializeCache();
                    }
                }

                return s_cachedInstance;
            }
        }

        /// <summary>
        /// パフォーマンス最適化のためのキャッシュを初期化
        /// </summary>
        private void InitializeCache()
        {
            if (m_voxelDataCache == null)
            {
                m_voxelDataCache = new Dictionary<int, VoxelData>();
                m_colorCache = new Dictionary<int, Color>();

                foreach (var data in m_voxelDataList)
                {
                    if (data != null)
                    {
                        m_voxelDataCache[data.VoxelId] = data;
                        m_colorCache[data.VoxelId] = data.Color;
                    }
                }

                Debug.Log($"[VoxelDataBase] {m_voxelDataCache.Count}個のボクセルデータをキャッシュしました。");
            }
        }

        /// <summary>
        /// 指定されたIDのVoxelDataを静的に取得
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <returns>VoxelData、見つからない場合はnull</returns>
        public static VoxelData GetVoxelDataStatic(int voxelId)
        {
            var instance = Instance;
            if (instance == null)
            {
                Debug.LogWarning($"[VoxelDataBase] データベースが利用できません。ID: {voxelId}");
                return null;
            }

            return instance.GetVoxelData(voxelId);
        }

        /// <summary>
        /// ボクセルの色を静的に取得（高速化：Dictionaryキャッシュ使用）
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <returns>ボクセルの色、見つからない場合は白</returns>
        public static Color GetVoxelColorStatic(int voxelId)
        {
            // 空ボクセルの場合は透明を返す
            if (voxelId == VoxelConstants.EMPTY_VOXEL_ID)
            {
                return Color.clear;
            }

            var instance = Instance;
            if (instance == null)
            {
                return Color.white;
            }

            // 色キャッシュから直接取得（O(1)）
            if (instance.m_colorCache != null && instance.m_colorCache.TryGetValue(voxelId, out var color))
            {
                return color;
            }

            // キャッシュになければVoxelDataから取得（フォールバック）
            var voxelData = GetVoxelDataStatic(voxelId);
            return voxelData != null ? voxelData.Color : Color.white;
        }

        /// <summary>
        /// ボクセルが破壊可能か静的にチェック
        /// </summary>
        /// <param name="voxelId">ボクセルID</param>
        /// <param name="attackPower">攻撃力</param>
        /// <returns>破壊可能な場合true</returns>
        public static bool CanDestroyVoxelStatic(int voxelId, float attackPower)
        {
            // 空ボクセルは破壊できない（すでに空なので）
            if (voxelId == VoxelConstants.EMPTY_VOXEL_ID)
            {
                return false;
            }

            var voxelData = GetVoxelDataStatic(voxelId);
            return voxelData != null && voxelData.CanBeDestroyed(attackPower);
        }

        /// <summary>
        /// データベースが利用可能かチェック
        /// </summary>
        /// <returns>利用可能な場合true</returns>
        public static bool IsAvailable()
        {
            return Instance != null;
        }
    }
}