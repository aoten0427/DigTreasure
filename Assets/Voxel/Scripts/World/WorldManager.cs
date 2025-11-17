using UnityEngine;
using System.Collections.Generic;
using System;
using UniRx;
using System.Threading.Tasks;

namespace VoxelWorld
{
    /// <summary>
    /// ワールド管理の統合クラス
    /// </summary>
    public class WorldManager : MonoBehaviour
    {
        // シングルトンアクセス用
        private static WorldManager s_instance;
        
        /// <summary>
        /// WorldManagerのインスタンス
        /// </summary>
        public static WorldManager Instance => s_instance;
        
        [Header("ワールド設定")]
        [SerializeField] private Vector3Int m_worldSizeInChunksMin = new Vector3Int(0, 0, 0);
        [SerializeField] private Vector3Int m_worldSizeInChunksMax = new Vector3Int(4, 4, 4);
        [SerializeField] private bool m_isInitializeCreateChunk = false;
        [SerializeField] private Material m_voxelMaterial;

        [Header("境界メッシュ設定")]
        [SerializeField] private BoundaryMeshSettings m_boundaryMeshSettings = new BoundaryMeshSettings();


        [Header("コライダー設定")]
        [SerializeField] private bool m_enableColliders = true;
        [SerializeField] private int m_maxCollidersPerChunk = 256;
        
        [Header("マルチプレイ設定")]
        [SerializeField] private int m_localPlayerId = 0;

        [Header("エフェクト生成")]
        [SerializeField] private VoxelEffectManager m_voxelEffectManager;

        //管理クラス
        private ChunkManager m_chunkManager;
        private VoxelOperationManager m_voxelManager;
        private SeparationManager m_separationManager;
        private SeparatedObjectSpawner m_separatedObjectSpawner;

        // ネットワーク管理
        

        /// <summary>
        ///インスタンス取得
        /// <returns>WorldManagerインスタンス、見つからない場合はnull</returns>
        public static WorldManager GetInstance()
        {
            if (s_instance == null)
            {
                s_instance = FindFirstObjectByType<WorldManager>();
                if (s_instance == null)
                {
                    Debug.LogWarning("[WorldManager] シーン内にWorldManagerが見つかりません。");
                }
            }
            return s_instance;
        }
        
        /// <summary>
        /// WorldManagerが存在するかチェック
        /// </summary>
        /// <returns>インスタンスが存在する場合true</returns>
        public static bool HasInstance()
        {
            return s_instance != null || FindFirstObjectByType<WorldManager>() != null;
        }

        // プロパティ
        ///ワールドサイズ（チャンク単位）
        public Vector3Int WorldSizeInChunks => m_worldSizeInChunksMax - m_worldSizeInChunksMin;
        
        //管理中のチャンク数
        public int ChunkCount => m_chunkManager?.ChunkCount ?? 0;
        
        ///チャンク管理クラス
        public ChunkManager Chunks => m_chunkManager;
        
        //ボクセル操作管理クラス
        public VoxelOperationManager Voxels => m_voxelManager;
        
        
        //コライダー管理クラス
        public VoxelColliderManager Colliders => m_chunkManager?.GetColliderManager();

        //分離オブジェクト管理クラス
        public SeparationManager SeparationManager => m_separationManager;

        /// <summary>
        /// ボクセルマテリアル
        /// </summary>
        public Material VoxelMaterial => m_voxelMaterial;

        /// <summary>
        /// 境界メッシュ設定
        /// </summary>
        public BoundaryMeshSettings BoundarySettings => m_boundaryMeshSettings;

        /// <summary>
        /// ローカルプレイヤーID（Fusion用）
        /// </summary>
        public int LocalPlayerId
        {
            get => m_localPlayerId;
            set => m_localPlayerId = value;
        }

        /// <summary>
        /// 指定されたプレイヤーIDがローカルプレイヤーか判定
        /// </summary>
        /// <param name="playerId">判定対象のプレイヤーID</param>
        /// <returns>ローカルプレイヤーの場合true</returns>
        public bool IsLocalPlayer(int playerId)
        {
            return playerId == m_localPlayerId;
        }
        

        private void Awake()
        {
            // シングルトンインスタンス設定
            if (s_instance != null && s_instance != this)
            {
                Debug.LogWarning($"[WorldManager] 複数のWorldManagerが検出されました: {name}. " +
                               $"既存: {s_instance.name}, 新規: {name}");
            }
            s_instance = this;
            
            InitializeManagers();
            InitializeWorld();
        }

        private void OnDestroy()
        {
            // シングルトンインスタンスクリア
            if (s_instance == this)
            {
                s_instance = null;
            }
            
            CleanupManagers();
        }
        
        /// <summary>
        /// 管理クラスを初期化
        /// </summary>
        private void InitializeManagers()
        {
            if (m_voxelEffectManager == null) Debug.LogWarning("エフェクトマネジャーがありません");

            // ChunkManager初期化（コライダー設定付き）
            m_chunkManager = new ChunkManager();
            m_chunkManager.Initialize(transform, m_voxelMaterial, m_enableColliders, m_maxCollidersPerChunk);

            // SeparatedObjectSpawner初期化（ローカルモード）
            m_separatedObjectSpawner = new SeparatedObjectSpawner();
            m_separatedObjectSpawner.InitializeLocal(m_voxelMaterial);

            // VoxelOperationManager初期化
            m_voxelManager = new VoxelOperationManager();
            m_voxelManager.Initialize(m_chunkManager,m_voxelEffectManager, this, true, true, m_separatedObjectSpawner);

            // SeparationManager初期化
            m_separationManager = GetComponent<SeparationManager>();
            if (m_separationManager == null)
            {
                m_separationManager = gameObject.AddComponent<SeparationManager>();
            }
            m_separationManager.InitializeManager();
        }
        
        /// <summary>
        /// 管理クラスをクリーンアップ
        /// </summary>
        private void CleanupManagers()
        {
            m_chunkManager?.CleanupAllChunks();
            m_separationManager?.ClearAllSeparatedObjects();

            m_chunkManager = null;
            m_voxelManager = null;
            m_separationManager = null;
        }

        /// <summary>
        /// ワールドを初期化
        /// </summary>
        public void InitializeWorld()
        {
            if (m_chunkManager == null)
            {
                Debug.LogError("[WorldManager] ChunkManagerが初期化されていません");
                return;
            }
            
            // 既存チャンクをクリーンアップ
            m_chunkManager.CleanupAllChunks();

            // 新しいチャンクを一括作成
            if(m_isInitializeCreateChunk)CreateChunks(m_worldSizeInChunksMin, m_worldSizeInChunksMax);

        }

        /// <summary>
        /// チャンク生成
        /// </summary>
        /// <param name="min"></param>
        /// <param name="max"></param>
        /// <param name="chunkperfream"></param>
        /// <param name="progressProperty"></param>
        /// <param name="onComplete"></param>
        public void CreateChunks(Vector3Int min,Vector3Int max,int chunkperfream = 100, ReactiveProperty<float> progressProperty = null,
            Action onComplete = null)
        {
            StartCoroutine(m_chunkManager.CreateChunksInRangeCoroutine(min,max,chunkperfream,progressProperty,onComplete));
        }

        /// <summary>
        /// 指定チャンクの境界情報を取得
        /// BoundaryMeshSettingsに設定された座標範囲から判定
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>境界情報</returns>
        public ChunkBoundaryInfo GetChunkBoundaryInfo(Vector3Int chunkPosition)
        {
            // BoundaryMeshSettingsから境界情報を取得
            return m_boundaryMeshSettings.GetChunkBoundaryInfo(chunkPosition);
        }

        /// <summary>
        /// 範囲内の分離オブジェクトを取得
        /// </summary>
        /// <param name="center">検索中心位置</param>
        /// <param name="radius">検索半径</param>
        /// <returns>範囲内の分離オブジェクトリスト</returns>
        public List<SeparatedVoxelObject> GetSeparatedObjectsInRange(Vector3 center, float radius)
        {
            return m_separationManager?.FindObjectsInRange(center, radius) ?? new List<SeparatedVoxelObject>();
        }

        /// <summary>
        /// 分離オブジェクト数を取得
        /// </summary>
        /// <returns>現在の分離オブジェクト数</returns>
        public int GetSeparatedObjectCount()
        {
            return m_separationManager?.GetSeparatedObjectCount() ?? 0;
        }

        /// <summary>
        /// 分離オブジェクトを強制削除
        /// </summary>
        /// <param name="instanceId">削除するオブジェクトのインスタンスID</param>
        public void ForceDestroySeparatedObject(int instanceId)
        {
            m_separationManager?.ForceDestroySeparatedObject(instanceId);
        }

        /// <summary>
        /// 全分離オブジェクトをクリア
        /// </summary>
        public void ClearAllSeparatedObjects()
        {
            m_separationManager?.ClearAllSeparatedObjects();
        }
        

    }
}