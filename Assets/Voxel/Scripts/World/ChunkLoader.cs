using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace VoxelWorld
{
    /// <summary>
    /// 動的チャンクロード・アンロードシステム
    /// </summary>
    public class ChunkLoader : MonoBehaviour
    {
        [Header("ロード設定")]
        [SerializeField] private Transform m_playerTransform;
        [SerializeField] private int m_loadRadius = 2;
        [SerializeField] private int m_unloadRadius = 3;
        [SerializeField] private float m_updateInterval = 0.5f;
        
        [Header("パフォーマンス設定")]
        [SerializeField] private int m_maxChunksPerFrame = 2;
        [SerializeField] private bool m_enableAsyncLoading = true;

        [Header("デバッグ")]
        [SerializeField] private bool m_enableLogging = false;
        [SerializeField] private bool m_showGizmos = false;

        //WorldManager参照
        private WorldManager m_worldManager;
        
        //現在ロード済みのチャンクセット
        private HashSet<Vector3Int> m_loadedChunks = new HashSet<Vector3Int>();
        //ロード予定のチャンクキュー
        private Queue<Vector3Int> m_loadQueue = new Queue<Vector3Int>();
        //アンロード予定のチャンクキュー
        private Queue<Vector3Int> m_unloadQueue = new Queue<Vector3Int>();
        //プレイヤーの現在チャンク座標
        private Vector3Int m_currentPlayerChunk = Vector3Int.zero;
        //最後の更新時刻
        private float m_lastUpdateTime = 0f;
        //処理中フラグ
        private bool m_isProcessing = false;

        // プロパティ
        //ロード済みチャンク数
        public int LoadedChunkCount => m_loadedChunks.Count;
        //ロードキューの長さ
        public int LoadQueueCount => m_loadQueue.Count;
        //アンロードキューの長さ
        public int UnloadQueueCount => m_unloadQueue.Count;
        //現在のプレイヤーチャンク座標
        public Vector3Int CurrentPlayerChunk => m_currentPlayerChunk;

        private void Start()
        {
            Initialize();
        }

        private void Update()
        {
            if (Time.time - m_lastUpdateTime >= m_updateInterval)
            {
                UpdateChunkLoading();
                m_lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// ChunkLoaderを初期化
        /// </summary>
        public void Initialize()
        {
            // WorldManagerを取得
            if (m_worldManager == null)
            {
                m_worldManager = FindFirstObjectByType<WorldManager>();
            }

            if (m_worldManager == null)
            {
                Debug.LogError("[ChunkLoader] WorldManagerが見つかりません。");
                return;
            }

            // プレイヤーTransformが未設定の場合は検索
            if (m_playerTransform == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    m_playerTransform = player.transform;
                }
                else
                {
                    // プレイヤーがない場合はMainCameraを使用
                    Camera mainCamera = Camera.main;
                    if (mainCamera != null)
                    {
                        m_playerTransform = mainCamera.transform;
                    }
                }
            }

            if (m_playerTransform == null)
            {
                Debug.LogWarning("[ChunkLoader] プレイヤーTransformが設定されていません。");
                return;
            }

            // 初期チャンクロード
            UpdateChunkLoading();

            if (m_enableLogging)
            {
                Debug.Log("[ChunkLoader] 初期化完了。");
            }
        }

        /// <summary>
        /// チャンクロードの更新処理
        /// </summary>
        public void UpdateChunkLoading()
        {
            if (m_playerTransform == null || m_worldManager == null)
            {
                return;
            }

            // プレイヤーの現在チャンク座標を取得
            Vector3Int newPlayerChunk = VoxelConstants.WorldToChunkPosition(m_playerTransform.position);

            // プレイヤーが移動した場合のみ更新
            if (newPlayerChunk != m_currentPlayerChunk || m_loadedChunks.Count == 0)
            {
                m_currentPlayerChunk = newPlayerChunk;
                CalculateChunkLoadUnload();
            }

            // キュー処理
            if (!m_isProcessing)
            {
                StartCoroutine(ProcessChunkQueues());
            }
        }

        /// <summary>
        /// ロード・アンロードすべきチャンクを計算
        /// </summary>
        private void CalculateChunkLoadUnload()
        {
            var requiredChunks = new HashSet<Vector3Int>();

            // ロード範囲内のチャンクをすべて列挙（XZ平面中心、Y軸は制限）
            int yRange = Mathf.Min(m_loadRadius, 2); // Y軸は最大2チャンクまで
            
            for (int x = -m_loadRadius; x <= m_loadRadius; x++)
            {
                for (int y = -yRange; y <= yRange; y++)
                {
                    for (int z = -m_loadRadius; z <= m_loadRadius; z++)
                    {
                        Vector3Int chunkPos = m_currentPlayerChunk + new Vector3Int(x, y, z);
                        
                        // ワールド範囲内かチェック
                        if (IsValidChunkPosition(chunkPos))
                        {
                            requiredChunks.Add(chunkPos);
                        }
                    }
                }
            }

            // ロードが必要なチャンクをキューに追加
            foreach (var chunkPos in requiredChunks)
            {
                if (!m_loadedChunks.Contains(chunkPos) && !m_loadQueue.Contains(chunkPos))
                {
                    m_loadQueue.Enqueue(chunkPos);
                }
            }

            // アンロードが必要なチャンクをチェック（XZ平面での距離）
            var chunksToUnload = new List<Vector3Int>();
            foreach (var loadedChunk in m_loadedChunks)
            {
                // XZ平面での距離を計算（Y軸は除外）
                float xDistance = Mathf.Abs(loadedChunk.x - m_currentPlayerChunk.x);
                float zDistance = Mathf.Abs(loadedChunk.z - m_currentPlayerChunk.z);
                float maxDistance = Mathf.Max(xDistance, zDistance);
                
                if (maxDistance > m_unloadRadius)
                {
                    chunksToUnload.Add(loadedChunk);
                }
            }

            // アンロードキューに追加
            foreach (var chunkPos in chunksToUnload)
            {
                if (!m_unloadQueue.Contains(chunkPos))
                {
                    m_unloadQueue.Enqueue(chunkPos);
                }
            }

            if (m_enableLogging && (m_loadQueue.Count > 0 || m_unloadQueue.Count > 0))
            {
                Debug.Log($"[ChunkLoader] プレイヤーチャンク: {m_currentPlayerChunk}, ロード予定: {m_loadQueue.Count}, アンロード予定: {m_unloadQueue.Count}");
            }
        }

        /// <summary>
        /// チャンクキューを処理するコルーチン
        /// </summary>
        private IEnumerator ProcessChunkQueues()
        {
            m_isProcessing = true;
            int processedThisFrame = 0;

            // アンロード処理（優先）
            while (m_unloadQueue.Count > 0 && processedThisFrame < m_maxChunksPerFrame)
            {
                Vector3Int chunkPos = m_unloadQueue.Dequeue();
                UnloadChunk(chunkPos);
                processedThisFrame++;

                if (m_enableAsyncLoading && processedThisFrame >= m_maxChunksPerFrame)
                {
                    yield return null;
                    processedThisFrame = 0;
                }
            }

            // ロード処理
            while (m_loadQueue.Count > 0 && processedThisFrame < m_maxChunksPerFrame)
            {
                Vector3Int chunkPos = m_loadQueue.Dequeue();
                LoadChunk(chunkPos);
                processedThisFrame++;

                if (m_enableAsyncLoading && processedThisFrame >= m_maxChunksPerFrame)
                {
                    yield return null;
                    processedThisFrame = 0;
                }
            }

            m_isProcessing = false;
        }

        /// <summary>
        /// チャンクをロード
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        private void LoadChunk(Vector3Int chunkPosition)
        {
            if (m_loadedChunks.Contains(chunkPosition))
            {
                return;
            }

            // 既存チャンクがあるかチェック
            Chunk existingChunk = m_worldManager.Chunks.GetChunk(chunkPosition);
            if (existingChunk == null)
            {
                // 新規チャンク作成
                m_worldManager.Chunks.CreateChunk(chunkPosition);
            }
            else
            {
                // 既存チャンクのGameObjectを再アクティブ化
                GameObject chunkObject = m_worldManager.Chunks.GetChunkGameObject(chunkPosition);
                if (chunkObject != null && !chunkObject.activeInHierarchy)
                {
                    chunkObject.SetActive(true);
                }
            }

            m_loadedChunks.Add(chunkPosition);

            if (m_enableLogging)
            {
                Debug.Log($"[ChunkLoader] チャンクロード: {chunkPosition}");
            }
        }

        /// <summary>
        /// チャンクをアンロード
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        private void UnloadChunk(Vector3Int chunkPosition)
        {
            if (!m_loadedChunks.Contains(chunkPosition))
            {
                return;
            }

            // チャンクのGameObjectを非アクティブ化
            GameObject chunkObject = m_worldManager.Chunks.GetChunkGameObject(chunkPosition);
            if (chunkObject != null)
            {
                chunkObject.SetActive(false);
            }

            m_loadedChunks.Remove(chunkPosition);

            if (m_enableLogging)
            {
                Debug.Log($"[ChunkLoader] チャンクアンロード: {chunkPosition}");
            }
        }

        /// <summary>
        /// 有効なチャンク座標かチェック
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>有効な場合true</returns>
        private bool IsValidChunkPosition(Vector3Int chunkPosition)
        {
            // 中心原点(0,0,0)を基準とした対称設計
            int halfChunkX = VoxelConstants.MAX_CHUNKS_X / 2;
            int halfChunkY = VoxelConstants.MAX_CHUNKS_Y / 2;
            int halfChunkZ = VoxelConstants.MAX_CHUNKS_Z / 2;
            
            return chunkPosition.x >= -halfChunkX && chunkPosition.x < halfChunkX &&
                   chunkPosition.y >= -halfChunkY && chunkPosition.y < halfChunkY &&
                   chunkPosition.z >= -halfChunkZ && chunkPosition.z < halfChunkZ;
        }

        /// <summary>
        /// 指定範囲内のすべてのチャンクを強制ロード
        /// </summary>
        /// <param name="center">中心チャンク座標</param>
        /// <param name="radius">ロード半径</param>
        public void ForceLoadChunks(Vector3Int center, int radius)
        {
            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    for (int z = -radius; z <= radius; z++)
                    {
                        Vector3Int chunkPos = center + new Vector3Int(x, y, z);
                        if (IsValidChunkPosition(chunkPos))
                        {
                            LoadChunk(chunkPos);
                        }
                    }
                }
            }

            Debug.Log($"[ChunkLoader] 強制ロード完了: 中心{center}, 半径{radius}");
        }

        /// <summary>
        /// すべてのチャンクをアンロード
        /// </summary>
        public void UnloadAllChunks()
        {
            var chunksToUnload = new List<Vector3Int>(m_loadedChunks);
            foreach (var chunkPos in chunksToUnload)
            {
                UnloadChunk(chunkPos);
            }

            m_loadQueue.Clear();
            m_unloadQueue.Clear();

            Debug.Log("[ChunkLoader] 全チャンクアンロード完了。");
        }


#if UNITY_EDITOR
        /// <summary>
        /// ギズモ描画
        /// </summary>
        private void OnDrawGizmos()
        {
            if (!m_showGizmos || m_playerTransform == null)
            {
                return;
            }

            // ロード範囲を描画
            Gizmos.color = Color.green;
            Vector3 loadCenter = VoxelConstants.ChunkToWorldPosition(m_currentPlayerChunk.x, m_currentPlayerChunk.y, m_currentPlayerChunk.z);
            float loadSize = m_loadRadius * VoxelConstants.CHUNK_WIDTH * VoxelConstants.VOXEL_SIZE;
            Gizmos.DrawWireCube(loadCenter, Vector3.one * loadSize * 2);

            // アンロード範囲を描画
            Gizmos.color = Color.red;
            float unloadSize = m_unloadRadius * VoxelConstants.CHUNK_WIDTH * VoxelConstants.VOXEL_SIZE;
            Gizmos.DrawWireCube(loadCenter, Vector3.one * unloadSize * 2);

            // プレイヤー位置を描画
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(m_playerTransform.position, 1.0f);
        }
#endif
    }
}