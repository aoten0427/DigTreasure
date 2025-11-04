using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace MapGeneration
{
    /// <summary>
    /// 地形ボクセルデータの生成と洞窟適用を担当
    /// </summary>
    public class TerrainGenerator
    {
        private CaveGenerationSettings m_settings;
        private VoxelLayerGenerator m_layerGenerator;
        private int m_seed;

        public TerrainGenerator(CaveGenerationSettings settings, VoxelLayerGenerator layerGenerator, int seed)
        {
            m_settings = settings;
            m_layerGenerator = layerGenerator;
            m_seed = seed;
        }

        /// <summary>
        /// チャンクリストを取得
        /// </summary>
        public List<Vector3Int> GetChunkPositions(Vector3Int fieldMin, Vector3Int fieldMax)
        {
            List<Vector3Int> chunkPositions = new List<Vector3Int>();

            for (int x = fieldMin.x; x <= fieldMax.x; x++)
            {
                for (int y = fieldMin.y; y <= fieldMax.y; y++)
                {
                    for (int z = fieldMin.z; z <= fieldMax.z; z++)
                    {
                        chunkPositions.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return chunkPositions;
        }

        /// <summary>
        /// チャンク範囲のボクセルデータを配列で準備
        /// </summary>
        public Dictionary<Vector3Int, List<VoxelUpdate>> PrepareVoxelDataByChunk(List<Vector3Int> chunkPositions)
        {
            var voxelDataByChunk = new Dictionary<Vector3Int, List<VoxelUpdate>>();

            foreach (var chunkPos in chunkPositions)
            {
                var chunkVoxels = new List<VoxelUpdate>(VoxelConstants.CHUNK_WIDTH * VoxelConstants.CHUNK_HEIGHT * VoxelConstants.CHUNK_DEPTH);

                // チャンクのワールド座標を取得
                Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(chunkPos.x, chunkPos.y, chunkPos.z);

                // チャンク内のすべてのボクセルを深さに応じて配置
                for (int x = 0; x < VoxelConstants.CHUNK_WIDTH; x++)
                {
                    for (int y = 0; y < VoxelConstants.CHUNK_HEIGHT; y++)
                    {
                        for (int z = 0; z < VoxelConstants.CHUNK_DEPTH; z++)
                        {
                            // ワールド座標を計算
                            Vector3 worldPos = chunkWorldPos + new Vector3(
                                x * VoxelConstants.VOXEL_SIZE,
                                y * VoxelConstants.VOXEL_SIZE,
                                z * VoxelConstants.VOXEL_SIZE
                            );

                            // 深さに応じたボクセルIDを取得（VoxelLayerGeneratorを使用）
                            int voxelId = m_layerGenerator.GetVoxelIdByHeight(worldPos);

                            // VoxelUpdateを追加
                            chunkVoxels.Add(new VoxelUpdate(worldPos, new Voxel(voxelId)));
                        }
                    }
                }

                voxelDataByChunk[chunkPos] = chunkVoxels;
            }

            return voxelDataByChunk;
        }

        /// <summary>
        /// 洞窟データをボクセルデータに適用
        /// </summary>
        public void ApplyCavesToVoxelData(Dictionary<Vector3Int, List<VoxelUpdate>> voxelDataByChunk, CaveSystem caveSystem)
        {
            foreach (var cave in caveSystem.Caves)
            {
                foreach (var sphere in cave.Spheres)
                {
                    // 楕円体が影響するチャンクを計算
                    var affectedChunks = GetAffectedChunks(sphere);

                    // 影響するチャンクのボクセルだけ処理
                    foreach (var chunkPos in affectedChunks)
                    {
                        if (voxelDataByChunk.TryGetValue(chunkPos, out var chunkVoxels))
                        {
                            ApplyEllipsoidToVoxelData(chunkVoxels, sphere);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 楕円体が影響するチャンクのリストを取得
        /// </summary>
        private List<Vector3Int> GetAffectedChunks(CaveSphere sphere)
        {
            var affectedChunks = new List<Vector3Int>();

            // バウンディングボックスを計算
            float expansionFactor = m_settings.useNoise ? 1.2f : 1.0f;
            float minX = sphere.Center.x - sphere.Scale.x * expansionFactor;
            float maxX = sphere.Center.x + sphere.Scale.x * expansionFactor;
            float minY = sphere.Center.y - sphere.Scale.y * expansionFactor;
            float maxY = sphere.Center.y + sphere.Scale.y * expansionFactor;
            float minZ = sphere.Center.z - sphere.Scale.z * expansionFactor;
            float maxZ = sphere.Center.z + sphere.Scale.z * expansionFactor;

            // ワールド座標をチャンク座標に変換
            Vector3Int minChunk = VoxelConstants.WorldToChunkPosition(new Vector3(minX, minY, minZ));
            Vector3Int maxChunk = VoxelConstants.WorldToChunkPosition(new Vector3(maxX, maxY, maxZ));

            // 影響範囲のチャンクを列挙
            for (int x = minChunk.x; x <= maxChunk.x; x++)
            {
                for (int y = minChunk.y; y <= maxChunk.y; y++)
                {
                    for (int z = minChunk.z; z <= maxChunk.z; z++)
                    {
                        affectedChunks.Add(new Vector3Int(x, y, z));
                    }
                }
            }

            return affectedChunks;
        }

        /// <summary>
        /// 楕円体範囲内のボクセルを空に変更
        /// </summary>
        private void ApplyEllipsoidToVoxelData(List<VoxelUpdate> voxelUpdates, CaveSphere sphere)
        {
            // 計算のキャッシュ
            float scaleXSq = sphere.Scale.x * sphere.Scale.x;
            float scaleYSq = sphere.Scale.y * sphere.Scale.y;
            float scaleZSq = sphere.Scale.z * sphere.Scale.z;

            // チャンク内のボクセルを処理
            for (int i = 0; i < voxelUpdates.Count; i++)
            {
                Vector3 voxelPos = voxelUpdates[i].WorldPosition;

                float dx = voxelPos.x - sphere.Center.x;
                float dy = voxelPos.y - sphere.Center.y;
                float dz = voxelPos.z - sphere.Center.z;

                // 楕円体の正規化距離を計算
                float normalizedDist =
                    (dx * dx) / scaleXSq +
                    (dy * dy) / scaleYSq +
                    (dz * dz) / scaleZSq;

                bool shouldRemove = false;

                if (m_settings.useNoise)
                {
                    // Perlin Noiseを適用
                    if (normalizedDist < 0.7f)
                    {
                        shouldRemove = true;
                    }
                    else if (normalizedDist <= 1.2f)
                    {
                        // 3Dパーリンノイズを取得（0.0~1.0の範囲）
                        float noise = Mathf.PerlinNoise(
                            voxelPos.x * m_settings.noiseScale + m_seed,
                            voxelPos.y * m_settings.noiseScale + m_seed
                        );

                        float threshold = Mathf.Lerp(m_settings.noiseThreshold, 1.0f, (normalizedDist - 0.7f) / 0.5f);
                        shouldRemove = noise > threshold;
                    }
                }
                else
                {
                    shouldRemove = normalizedDist <= 1.0f;
                }

                if (shouldRemove)
                {
                    //削除追加
                    voxelUpdates[i] = new VoxelUpdate(voxelPos, new Voxel(0));
                }
            }
        }
    }
}
