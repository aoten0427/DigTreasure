using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルデータアクセスインターフェース
    /// </summary>
    public interface IVoxelProvider
    {
        bool IsNonEmptyVoxel(Vector3 position);
        Voxel GetVoxel(Vector3 position);
        bool IsValidPosition(Vector3 position);
        VoxelProviderType GetProviderType();
    }
    
    /// <summary>
    /// ボクセルプロバイダーの種類
    /// </summary>
    public enum VoxelProviderType
    {
        ChunkManager,
        SeparatedObject,
        Custom
    }
    
    /// <summary>
    /// ChunkManagerベースのボクセルプロバイダー
    /// </summary>
    public class ChunkManagerVoxelProvider : IVoxelProvider
    {
        private readonly ChunkManager m_chunkManager;
        
        public ChunkManagerVoxelProvider(ChunkManager chunkManager)
        {
            m_chunkManager = chunkManager ?? throw new System.ArgumentNullException(nameof(chunkManager));
        }
        
        public bool IsNonEmptyVoxel(Vector3 position)
        {
            if (m_chunkManager == null)
            {
                return false;
            }
            
            var chunk = m_chunkManager.GetChunkFromWorldPosition(position);
            if (chunk == null)
            {
                return false;
            }
            
            var voxel = chunk.GetVoxelFromWorldPosition(position);
            return !voxel.IsEmpty;
        }
        
        public Voxel GetVoxel(Vector3 position)
        {
            if (m_chunkManager == null)
            {
                return Voxel.Empty;
            }
            
            var chunk = m_chunkManager.GetChunkFromWorldPosition(position);
            if (chunk == null)
            {
                return Voxel.Empty;
            }
            
            return chunk.GetVoxelFromWorldPosition(position);
        }
        
        public bool IsValidPosition(Vector3 position)
        {
            if (m_chunkManager == null)
            {
                return false;
            }
            
            var chunk = m_chunkManager.GetChunkFromWorldPosition(position);
            return chunk != null;
        }
        
        public VoxelProviderType GetProviderType()
        {
            return VoxelProviderType.ChunkManager;
        }

        /// <summary>
        /// ChunkManagerインスタンスを取得（破壊率最適化用）
        /// </summary>
        /// <returns>ChunkManagerインスタンス</returns>
        public ChunkManager GetChunkManager()
        {
            return m_chunkManager;
        }

        // 非空ボクセル全座標取得（分離検出用）
        public List<Vector3> GetAllNonEmptyVoxels()
        {
            var result = new List<Vector3>();
            
            if (m_chunkManager == null)
            {
                return result;
            }

            // 全チャンクを検索してボクセル座標を収集
            var chunks = m_chunkManager.Chunks.Values;
            foreach (var chunk in chunks)
            {
                if (chunk == null) continue;
                
                for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
                {
                    for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                        {
                            var voxel = chunk.GetVoxel(x, y, z);
                            if (!voxel.IsEmpty)
                            {
                                var worldPos = VoxelConstants.ChunkToWorldPosition(
                                    chunk.ChunkPosition.x, chunk.ChunkPosition.y, chunk.ChunkPosition.z) +
                                    new Vector3(x, y, z) * VoxelConstants.VOXEL_SIZE;
                                result.Add(worldPos);
                            }
                        }
                    }
                }
            }
            
            return result;
        }
    }
    
    /// <summary>
    /// SeparatedVoxelObjectベースのボクセルプロバイダー
    /// </summary>
    public class SeparatedVoxelObjectVoxelProvider : IVoxelProvider
    {
        private readonly SeparatedVoxelObject m_separatedObject;
        
        public SeparatedVoxelObjectVoxelProvider(SeparatedVoxelObject separatedObject)
        {
            m_separatedObject = separatedObject ?? throw new System.ArgumentNullException(nameof(separatedObject));
        }
        
        public bool IsNonEmptyVoxel(Vector3 position)
        {
            if (m_separatedObject == null) return false;
            
            var index = VoxelConstants.SeparatedObjectWorldToIndex(
                position - m_separatedObject.WorldPosition);
            
            if (!m_separatedObject.IsValidIndex(index)) return false;
            
            var voxel = m_separatedObject.GetVoxelData(index);
            return !voxel.IsEmpty;
        }
        
        public Voxel GetVoxel(Vector3 position)
        {
            if (m_separatedObject == null) return Voxel.Empty;
            
            var index = VoxelConstants.SeparatedObjectWorldToIndex(
                position - m_separatedObject.WorldPosition);
            
            if (!m_separatedObject.IsValidIndex(index)) return Voxel.Empty;
            
            return m_separatedObject.GetVoxelData(index);
        }
        
        public bool IsValidPosition(Vector3 position)
        {
            if (m_separatedObject == null) return false;
            
            var index = VoxelConstants.SeparatedObjectWorldToIndex(
                position - m_separatedObject.WorldPosition);
            
            return m_separatedObject.IsValidIndex(index);
        }
        
        public VoxelProviderType GetProviderType()
        {
            return VoxelProviderType.SeparatedObject;
        }
        
        // 非空ボクセル全座標取得
        public List<Vector3> GetAllNonEmptyVoxels()
        {
            var result = new List<Vector3>();
            
            if (m_separatedObject?.GetVoxelData() == null) return result;
            
            var voxelData = m_separatedObject.GetVoxelData();
            var size = m_separatedObject.Size;
            var worldPosition = m_separatedObject.WorldPosition;
            
            for (int x = 0; x < size.x; x++)
            {
                for (int y = 0; y < size.y; y++)
                {
                    for (int z = 0; z < size.z; z++)
                    {
                        var voxel = voxelData[x, y, z];
                        if (!voxel.IsEmpty)
                        {
                            var localPos = VoxelConstants.SeparatedObjectIndexToLocal(new Vector3Int(x, y, z));
                            var worldPos = worldPosition + localPos;
                            result.Add(worldPos);
                        }
                    }
                }
            }
            
            return result;
        }
    }
}