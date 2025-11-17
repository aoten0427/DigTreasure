using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UniRx;
using System;

namespace VoxelWorld
{
    /// <summary>
    /// チャンク管理専用クラス
    /// チャンクの生成、削除、取得、メッシュ更新を一元管理
    /// WorldManagerから分離された独立したチャンク管理システム
    /// </summary>
    public class ChunkManager
    {
        
        
        // チャンクデータの管理
        private Dictionary<Vector3Int, Chunk> m_chunks = new Dictionary<Vector3Int, Chunk>();
        // チャンクの描画オブジェクト管理
        private Dictionary<Vector3Int, GameObject> m_chunkGameObjects = new Dictionary<Vector3Int, GameObject>();

        // メッシュ遅延更新管理
        private Dictionary<Vector3Int, float> m_pendingMeshUpdates = new Dictionary<Vector3Int, float>();

        // 設定
        private Transform m_parentTransform;
        private Material m_voxelMaterial;
        private bool m_enableColliders;

        // コライダー管理
        private VoxelColliderManager m_colliderManager;
        
        //管理中のチャンク数
        public int ChunkCount => m_chunks.Count;
        
        //管理中のチャンク座標リスト
        public IEnumerable<Vector3Int> ChunkPositions => m_chunks.Keys;
        
        //全チャンクのデータ
        public IReadOnlyDictionary<Vector3Int, Chunk> Chunks => m_chunks;
        
       
        /// <summary>
        /// ChunkManagerを初期化
        /// </summary>
        /// <param name="parentTransform">チャンクGameObjectの親Transform</param>
        /// <param name="voxelMaterial">ボクセル用マテリアル</param>
        /// <param name="enableColliders">コライダー有効フラグ</param>
        /// <param name="colliderQuality">コライダー品質</param>
        /// <param name="maxCollidersPerChunk">チャンク当たり最大コライダー数</param>
        public void Initialize(Transform parentTransform, Material voxelMaterial, bool enableColliders,
                             int maxCollidersPerChunk = 256)
        {
            m_parentTransform = parentTransform;
            m_voxelMaterial = voxelMaterial;
            m_enableColliders = enableColliders;
            
            // コライダーマネージャー初期化
            m_colliderManager = new VoxelColliderManager();
            m_colliderManager.Initialize(this, enableColliders,  maxCollidersPerChunk, false);
            
        }
        
        
        /// <summary>
        /// 指定された座標にチャンクを作成
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>作成されたChunk（既存の場合は既存を返す）</returns>
        public Chunk CreateChunk(Vector3Int chunkPosition)
        {
            if (m_chunks.ContainsKey(chunkPosition))
            {
                Debug.LogWarning($"[ChunkManager] チャンク {chunkPosition} は既に存在します。");
                return m_chunks[chunkPosition];
            }

            // チャンク作成
            Chunk newChunk = new Chunk(chunkPosition);
            m_chunks[chunkPosition] = newChunk;

            // チャンク用のGameObjectを作成
            GameObject chunkObject = CreateChunkGameObject(chunkPosition);
            m_chunkGameObjects[chunkPosition] = chunkObject;
            
            // コライダー生成
            if (m_enableColliders && m_colliderManager != null)
            {
                m_colliderManager.GenerateChunkColliders(chunkPosition);
            }

            return newChunk;
        }
        
        /// <summary>
        /// チャンク用GameObjectを作成
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>作成されたGameObject</returns>
        private GameObject CreateChunkGameObject(Vector3Int chunkPosition)
        {
            GameObject chunkObject = new GameObject($"Chunk_{chunkPosition.x}_{chunkPosition.y}_{chunkPosition.z}");
            
            if (m_parentTransform != null)
            {
                chunkObject.transform.parent = m_parentTransform;
            }
            
            chunkObject.transform.position = VoxelConstants.ChunkToWorldPosition(chunkPosition.x, chunkPosition.y, chunkPosition.z);

            // MeshFilterとMeshRendererを設定
            MeshFilter meshFilter = chunkObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = chunkObject.AddComponent<MeshRenderer>();
            meshRenderer.material = m_voxelMaterial;

            return chunkObject;
        }
        
        /// <summary>
        /// 指定されたチャンクを削除
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>削除に成功した場合true</returns>
        public bool RemoveChunk(Vector3Int chunkPosition)
        {
            if (!m_chunks.ContainsKey(chunkPosition))
            {
                return false;
            }
            
            // コライダーを削除
            if (m_colliderManager != null)
            {
                m_colliderManager.RemoveChunkColliders(chunkPosition);
            }
            
            // チャンクデータを削除
            Chunk chunk = m_chunks[chunkPosition];
            chunk.Cleanup();
            m_chunks.Remove(chunkPosition);
            
            // GameObjectを削除
            if (m_chunkGameObjects.TryGetValue(chunkPosition, out GameObject chunkObject))
            {
                if (chunkObject != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(chunkObject);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(chunkObject);
                    }
                }
                m_chunkGameObjects.Remove(chunkPosition);
            }
            
            return true;
        }
        
        /// <summary>
        /// 指定範囲のチャンクを一括作成
        /// </summary>
        /// <param name="minChunk">範囲の最小チャンク座標</param>
        /// <param name="maxChunk">範囲の最大チャンク座標</param>
        public void CreateChunksInRange(Vector3Int minChunk, Vector3Int maxChunk)
        {
            int createdCount = 0;
            
            // 最小・最大を正規化（minが必ずmaxより小さくなるように）
            Vector3Int actualMin = new Vector3Int(
                Mathf.Min(minChunk.x, maxChunk.x),
                Mathf.Min(minChunk.y, maxChunk.y),
                Mathf.Min(minChunk.z, maxChunk.z)
            );
            
            Vector3Int actualMax = new Vector3Int(
                Mathf.Max(minChunk.x, maxChunk.x),
                Mathf.Max(minChunk.y, maxChunk.y),
                Mathf.Max(minChunk.z, maxChunk.z)
            );
            
            for (int x = actualMin.x; x <= actualMax.x; x++)
            {
                for (int y = actualMin.y; y <= actualMax.y; y++)
                {
                    for (int z = actualMin.z; z <= actualMax.z; z++)
                    {
                        Vector3Int chunkPos = new Vector3Int(x, y, z);
                        if (!m_chunks.ContainsKey(chunkPos))
                        {
                            CreateChunk(chunkPos);
                            createdCount++;
                        }
                    }
                }
            }
            
            Debug.Log($"[ChunkManager] 範囲 ({actualMin}) - ({actualMax}) に {createdCount} 個のチャンクを作成しました。");
        }
        

        /// <summary>
        /// 指定範囲のチャンクをフレーム分散して作成（コルーチン版）
        /// </summary>
        /// <param name="minChunk">範囲の最小チャンク座標</param>
        /// <param name="maxChunk">範囲の最大チャンク座標</param>
        /// <param name="chunksPerFrame">1フレームあたりに作成するチャンク数</param>
        /// <returns>IEnumerator</returns>
        public IEnumerator CreateChunksInRangeCoroutine(Vector3Int minChunk, Vector3Int maxChunk, int chunksPerFrame = 1,
            ReactiveProperty<float> progressProperty = null,
            Action onComplete = null)
        {
            int createdCount = 0;
            int frameChunkCount = 0;

            // 最小・最大を正規化（minが必ずmaxより小さくなるように）
            Vector3Int actualMin = new Vector3Int(
                Mathf.Min(minChunk.x, maxChunk.x),
                Mathf.Min(minChunk.y, maxChunk.y),
                Mathf.Min(minChunk.z, maxChunk.z)
            );

            Vector3Int actualMax = new Vector3Int(
                Mathf.Max(minChunk.x, maxChunk.x),
                Mathf.Max(minChunk.y, maxChunk.y),
                Mathf.Max(minChunk.z, maxChunk.z)
            );

            Vector3Int size = new Vector3Int(
                actualMax.x - actualMin.x + 1,
                actualMax.y - actualMin.y + 1,
                actualMax.z - actualMin.z + 1
            );

            int totalChunks = size.x * size.y * size.z;

            Debug.Log($"[ChunkManager] フレーム分散チャンク作成開始: 範囲 ({actualMin}) - ({actualMax}), {chunksPerFrame}チャンク/フレーム");

            for (int x = actualMin.x; x <= actualMax.x; x++)
            {
                for (int y = actualMin.y; y <= actualMax.y; y++)
                {
                    for (int z = actualMin.z; z <= actualMax.z; z++)
                    {
                        Vector3Int chunkPos = new Vector3Int(x, y, z);
                        if (!m_chunks.ContainsKey(chunkPos))
                        {
                            CreateChunk(chunkPos);
                            createdCount++;
                            frameChunkCount++;

                            // 指定数のチャンクを作成したら次のフレームへ
                            if (frameChunkCount >= chunksPerFrame)
                            {
                                frameChunkCount = 0;
                                if (progressProperty != null) progressProperty.Value = (float)createdCount / totalChunks;

                                yield return null;
                            }
                        }
                    }
                }
            }

            onComplete?.Invoke();
            Debug.Log($"[ChunkManager] フレーム分散チャンク作成完了: {createdCount} 個のチャンクを作成しました。");
        }
        
        
        /// <summary>
        /// 指定されたチャンクを取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>Chunk、存在しない場合はnull</returns>
        public Chunk GetChunk(Vector3Int chunkPosition)
        {
            m_chunks.TryGetValue(chunkPosition, out Chunk chunk);
            return chunk;
        }
        
        /// <summary>
        /// ワールド座標からチャンクを取得
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <returns>Chunk、存在しない場合はnull</returns>
        public Chunk GetChunkFromWorldPosition(Vector3 worldPosition)
        {
            Vector3Int chunkPos = VoxelConstants.WorldToChunkPosition(worldPosition);
            return GetChunk(chunkPos);
        }

        /// <summary>
        /// 指定方向の隣接チャンクを取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <param name="direction">隣接方向</param>
        /// <returns>隣接チャンク、存在しない場合はnull</returns>
        public Chunk GetNeighborChunk(Vector3Int chunkPosition, Direction direction)
        {
            Vector3Int offset = GetDirectionOffset(direction);
            return GetChunk(chunkPosition + offset);
        }

        /// <summary>
        /// 方向を座標オフセットに変換
        /// </summary>
        /// <param name="direction">方向</param>
        /// <returns>Vector3Int オフセット</returns>
        private Vector3Int GetDirectionOffset(Direction direction)
        {
            return direction switch
            {
                Direction.Forward => new Vector3Int(0, 0, 1),
                Direction.Back => new Vector3Int(0, 0, -1),
                Direction.Up => new Vector3Int(0, 1, 0),
                Direction.Down => new Vector3Int(0, -1, 0),
                Direction.Right => new Vector3Int(1, 0, 0),
                Direction.Left => new Vector3Int(-1, 0, 0),
                _ => Vector3Int.zero
            };
        }

        /// <summary>
        /// チャンクが存在するかチェック
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>存在する場合true</returns>
        public bool HasChunk(Vector3Int chunkPosition)
        {
            return m_chunks.ContainsKey(chunkPosition);
        }
        
        /// <summary>
        /// チャンクのGameObjectを取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>GameObject、存在しない場合はnull</returns>
        public GameObject GetChunkGameObject(Vector3Int chunkPosition)
        {
            m_chunkGameObjects.TryGetValue(chunkPosition, out GameObject gameObject);
            return gameObject;
        }
        
        
        /// <summary>
        /// 指定されたチャンクのメッシュを更新
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>更新に成功した場合true</returns>
        public bool UpdateChunkMesh(Vector3Int chunkPosition)
        {
            Chunk chunk = GetChunk(chunkPosition);
            GameObject chunkObject = GetChunkGameObject(chunkPosition);

            if (chunk == null || chunkObject == null)
            {
                return false;
            }

            MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning($"[ChunkManager] MeshFilterが見つかりません: {chunkPosition}");
                return false;
            }

            // 新しいメッシュを生成
            Mesh newMesh = chunk.GenerateMesh();
            meshFilter.mesh = newMesh;

            // 空のチャンクは非表示に
            chunkObject.SetActive(newMesh != null && !chunk.IsEmpty());

            // コライダー更新
            if (m_enableColliders && m_colliderManager != null)
            {
                m_colliderManager.UpdateChunkColliders(chunkPosition);
            }

            return true;
        }

        /// <summary>
        /// 既に生成済みのメッシュをRendererに適用（並列処理用）
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <param name="mesh">適用するメッシュ</param>
        /// <returns>適用に成功した場合true</returns>
        public bool ApplyMeshToRenderer(Vector3Int chunkPosition, Mesh mesh)
        {
            Chunk chunk = GetChunk(chunkPosition);
            GameObject chunkObject = GetChunkGameObject(chunkPosition);

            if (chunk == null || chunkObject == null)
            {
                return false;
            }

            MeshFilter meshFilter = chunkObject.GetComponent<MeshFilter>();
            if (meshFilter == null)
            {
                Debug.LogWarning($"[ChunkManager] MeshFilterが見つかりません: {chunkPosition}");
                return false;
            }

            // メッシュを適用
            meshFilter.mesh = mesh;

            // 空のチャンクは非表示に
            chunkObject.SetActive(mesh != null && !chunk.IsEmpty());

            // コライダー更新
            if (m_enableColliders && m_colliderManager != null)
            {
                m_colliderManager.UpdateChunkColliders(chunkPosition);
            }

            return true;
        }
        
        
        /// <summary>
        /// 指定チャンクを変更済みとしてマーク
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        public void MarkChunkDirty(Vector3Int chunkPosition)
        {
            Chunk chunk = GetChunk(chunkPosition);
            chunk?.SetDirty();
        }
        
        
        /// <summary>
        /// 全チャンクを削除
        /// </summary>
        public void CleanupAllChunks()
        {
            // GameObjectsを削除
            foreach (var kvp in m_chunkGameObjects)
            {
                if (kvp.Value != null)
                {
                    if (Application.isPlaying)
                    {
                        UnityEngine.Object.Destroy(kvp.Value);
                    }
                    else
                    {
                        UnityEngine.Object.DestroyImmediate(kvp.Value);
                    }
                }
            }
            
            // チャンクデータをクリーンアップ
            foreach (var kvp in m_chunks)
            {
                kvp.Value.Cleanup();
            }
            
            m_chunkGameObjects.Clear();
            m_chunks.Clear();
            
            // コライダーマネージャークリーンアップ
            if (m_colliderManager != null)
            {
                m_colliderManager.RemoveAllColliders();
            }
            
            Debug.Log("[ChunkManager] 全チャンククリーンアップ完了");
        }
        
        
        /// <summary>
        /// コライダー管理クラスを取得
        /// </summary>
        /// <returns>VoxelColliderManager</returns>
        public VoxelColliderManager GetColliderManager()
        {
            return m_colliderManager;
        }

    }
   
}