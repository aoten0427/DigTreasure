using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルワールドのコライダー管理専用クラス
    /// </summary>
    public class VoxelColliderManager
    {
        // コライダー管理
        private Dictionary<Vector3Int, List<BoxCollider>> m_chunkColliders = new Dictionary<Vector3Int, List<BoxCollider>>();
        
        // 設定
        private bool m_enableColliders;
        private int m_maxCollidersPerChunk;
        private bool m_enableDebug;
        
        // 参照
        private ChunkManager m_chunkManager;
        
        
        //コライダーが有効かどうか
        public bool EnableColliders
        {
            get => m_enableColliders;
            set => m_enableColliders = value;
        }
        
        
        //チャンク当たりの最大コライダー数
        public int MaxCollidersPerChunk
        {
            get => m_maxCollidersPerChunk;
            set => m_maxCollidersPerChunk = Mathf.Max(1, value);
        }
        
        //デバッグ表示が有効かどうか
        public bool EnableDebug
        {
            get => m_enableDebug;
            set => m_enableDebug = value;
        }
        
        //管理中のチャンク数
        public int ManagedChunkCount => m_chunkColliders.Count;
        
        //総コライダー数
        public int TotalColliderCount => m_chunkColliders.Values.Sum(list => list.Count);
        
        
        /// <summary>
        /// VoxelColliderManagerを初期化
        /// </summary>
        /// <param name="chunkManager">チャンク管理クラス</param>
        /// <param name="enableColliders">コライダー有効フラグ</param>
        /// <param name="quality">コライダー品質</param>
        /// <param name="maxCollidersPerChunk">チャンク当たりの最大コライダー数</param>
        /// <param name="enableDebug">デバッグ有効フラグ</param>
        public void Initialize(ChunkManager chunkManager, bool enableColliders = true,  
                             int maxCollidersPerChunk = 256, bool enableDebug = false)
        {
            m_chunkManager = chunkManager;
            m_enableColliders = enableColliders;
            m_maxCollidersPerChunk = maxCollidersPerChunk;
            m_enableDebug = enableDebug;
            
        }
        
        
        /// <summary>
        /// 指定チャンクのコライダーを生成
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>生成に成功した場合true</returns>
        public bool GenerateChunkColliders(Vector3Int chunkPosition)
        {
            if (!m_enableColliders || m_chunkManager == null)
            {
                return false;
            }
            
            var chunk = m_chunkManager.GetChunk(chunkPosition);
            var chunkObject = m_chunkManager.GetChunkGameObject(chunkPosition);
            
            if (chunk == null || chunkObject == null)
            {
                Debug.LogWarning($"[VoxelColliderManager] チャンクまたはGameObjectが見つかりません: {chunkPosition}");
                return false;
            }
            
            // 既存コライダーを削除
            RemoveChunkColliders(chunkPosition);
            
            // ボクセルデータを取得
            var voxelData = chunk.GetVoxelData();
            if (voxelData == null)
            {
                return false;
            }
            
            // ChunkColliderGeneratorを使用してコライダー生成
            var optimizedColliders = ChunkColliderGenerator.GenerateOptimizedBoxColliders(
                voxelData,  m_maxCollidersPerChunk);
            
            if (optimizedColliders.Count == 0)
            {
                return true; // 空チャンクは正常
            }
            
            // BoxColliderコンポーネントを作成
            var colliders = new List<BoxCollider>();
            foreach (var optimizedCollider in optimizedColliders)
            {
                var boxCollider = chunkObject.AddComponent<BoxCollider>();
                boxCollider.center = optimizedCollider.localCenter;
                boxCollider.size = optimizedCollider.size;
                
                colliders.Add(boxCollider);
            }
            
            // コライダー情報を記録
            m_chunkColliders[chunkPosition] = colliders;
            
            
            return true;
        }
        
        /// <summary>
        /// 指定チャンクのコライダーを更新
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>更新に成功した場合true</returns>
        public bool UpdateChunkColliders(Vector3Int chunkPosition)
        {
            return GenerateChunkColliders(chunkPosition);
        }
        
        /// <summary>
        /// 複数チャンクのコライダーを一括生成
        /// </summary>
        /// <param name="chunkPositions">チャンク座標リスト</param>
        /// <returns>生成成功数</returns>
        public int GenerateMultipleChunkColliders(IEnumerable<Vector3Int> chunkPositions)
        {
            int successCount = 0;
            foreach (var chunkPos in chunkPositions)
            {
                if (GenerateChunkColliders(chunkPos))
                {
                    successCount++;
                }
            }
            
            return successCount;
        }
        
       
        
        /// <summary>
        /// 指定チャンクのコライダーを削除
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>削除に成功した場合true</returns>
        public bool RemoveChunkColliders(Vector3Int chunkPosition)
        {
            if (!m_chunkColliders.TryGetValue(chunkPosition, out var colliders))
            {
                return false;
            }
            
            // BoxColliderコンポーネントを削除
            foreach (var collider in colliders)
            {
                if (collider != null)
                {
                    if (Application.isPlaying)
                    {
                        Object.Destroy(collider);
                    }
                    else
                    {
                        Object.DestroyImmediate(collider);
                    }
                }
            }
            
            // 管理データから削除
            m_chunkColliders.Remove(chunkPosition);
            
            if (m_enableDebug)
            {
                Debug.Log($"[VoxelColliderManager] コライダー削除完了: {chunkPosition}");
            }
            
            return true;
        }
        
        /// <summary>
        /// 全チャンクのコライダーを削除
        /// </summary>
        public void RemoveAllColliders()
        {
            var chunkPositions = new List<Vector3Int>(m_chunkColliders.Keys);
            foreach (var chunkPos in chunkPositions)
            {
                RemoveChunkColliders(chunkPos);
            }
            
            Debug.Log("[VoxelColliderManager] 全コライダー削除完了");
        }
        
       
        
        /// <summary>
        /// 指定チャンクがコライダーを持っているかチェック
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>コライダーを持っている場合true</returns>
        public bool HasColliders(Vector3Int chunkPosition)
        {
            return m_chunkColliders.ContainsKey(chunkPosition) && 
                   m_chunkColliders[chunkPosition].Count > 0;
        }
        
        /// <summary>
        /// 指定チャンクのコライダー数を取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>コライダー数</returns>
        public int GetColliderCount(Vector3Int chunkPosition)
        {
            return m_chunkColliders.TryGetValue(chunkPosition, out var colliders) ? colliders.Count : 0;
        }
        
       
        
    }
    
}