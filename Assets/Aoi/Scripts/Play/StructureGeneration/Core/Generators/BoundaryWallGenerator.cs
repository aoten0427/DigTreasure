using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 境界壁（ボクセル壁と透明コライダー）の生成を担当
    /// </summary>
    public class BoundaryWallGenerator
    {
        // 定数
        private const float BOUNDARY_WALL_THICKNESS = 0.5f;

        /// <summary>
        /// 境界壁のボクセルリストを生成（6つの面を並列生成）
        /// </summary>
        /// <param name="minChunk">最小チャンク座標</param>
        /// <param name="maxChunk">最大チャンク座標</param>
        /// <param name="wallVoxelId">境界壁のボクセルID</param>
        /// <returns>境界壁のボクセルリスト</returns>
        public async Task<List<VoxelUpdate>> GenerateWallsAsync(
            Vector3Int minChunk,
            Vector3Int maxChunk,
            byte wallVoxelId)
        {
            float voxelSize = VoxelConstants.VOXEL_SIZE;

            // チャンク座標からワールド座標範囲を計算
            Vector3 minWorld = new Vector3(
                minChunk.x * VoxelConstants.CHUNK_WIDTH,
                minChunk.y * VoxelConstants.CHUNK_HEIGHT,
                minChunk.z * VoxelConstants.CHUNK_DEPTH
            );
            Vector3 maxWorld = new Vector3(
                (maxChunk.x + 1) * VoxelConstants.CHUNK_WIDTH,
                (maxChunk.y + 1) * VoxelConstants.CHUNK_HEIGHT,
                (maxChunk.z + 1) * VoxelConstants.CHUNK_DEPTH
            );

            // ボクセル範囲を計算
            Vector3Int minVoxel = new Vector3Int(
                Mathf.FloorToInt(minWorld.x / voxelSize),
                Mathf.FloorToInt(minWorld.y / voxelSize),
                Mathf.FloorToInt(minWorld.z / voxelSize)
            );
            Vector3Int maxVoxel = new Vector3Int(
                Mathf.FloorToInt(maxWorld.x / voxelSize),
                Mathf.FloorToInt(maxWorld.y / voxelSize),
                Mathf.FloorToInt(maxWorld.z / voxelSize)
            );

            Debug.Log($"境界壁生成: {minVoxel} - {maxVoxel}");

            var boundaryVoxels = new System.Collections.Concurrent.ConcurrentBag<VoxelUpdate>();

            // 6つの面を並列処理
            await Task.Run(() =>
            {
                System.Threading.Tasks.Parallel.Invoke(
                    // x- 面（minX）
                    () => {
                        int xMin = minVoxel.x;
                        for (int y = minVoxel.y; y < maxVoxel.y; y++)
                        {
                            for (int z = minVoxel.z; z < maxVoxel.z; z++)
                            {
                                Vector3 worldPos = new Vector3(xMin * voxelSize, y * voxelSize, z * voxelSize);
                                boundaryVoxels.Add(new VoxelUpdate(worldPos, wallVoxelId));
                            }
                        }
                    },
                    // x+ 面（maxX - 1）
                    () => {
                        int xMax = maxVoxel.x - 1;
                        for (int y = minVoxel.y; y < maxVoxel.y; y++)
                        {
                            for (int z = minVoxel.z; z < maxVoxel.z; z++)
                            {
                                Vector3 worldPos = new Vector3(xMax * voxelSize, y * voxelSize, z * voxelSize);
                                boundaryVoxels.Add(new VoxelUpdate(worldPos, wallVoxelId));
                            }
                        }
                    },
                    // y- 面（minY）
                    () => {
                        int yMin = minVoxel.y;
                        for (int x = minVoxel.x; x < maxVoxel.x; x++)
                        {
                            for (int z = minVoxel.z; z < maxVoxel.z; z++)
                            {
                                Vector3 worldPos = new Vector3(x * voxelSize, yMin * voxelSize, z * voxelSize);
                                boundaryVoxels.Add(new VoxelUpdate(worldPos, wallVoxelId));
                            }
                        }
                    },
                    // z- 面（minZ）
                    () => {
                        int zMin = minVoxel.z;
                        for (int x = minVoxel.x; x < maxVoxel.x; x++)
                        {
                            for (int y = minVoxel.y; y < maxVoxel.y; y++)
                            {
                                Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, zMin * voxelSize);
                                boundaryVoxels.Add(new VoxelUpdate(worldPos, wallVoxelId));
                            }
                        }
                    },
                    // z+ 面（maxZ - 1）
                    () => {
                        int zMax = maxVoxel.z - 1;
                        for (int x = minVoxel.x; x < maxVoxel.x; x++)
                        {
                            for (int y = minVoxel.y; y < maxVoxel.y; y++)
                            {
                                Vector3 worldPos = new Vector3(x * voxelSize, y * voxelSize, zMax * voxelSize);
                                boundaryVoxels.Add(new VoxelUpdate(worldPos, wallVoxelId));
                            }
                        }
                    }
                );
            });

            await Task.Yield(); 

            return boundaryVoxels.ToList();
        }

        /// <summary>
        /// 透明な境界壁（Collider）を生成
        /// </summary>
        /// <param name="minChunk">最小チャンク座標</param>
        /// <param name="maxChunk">最大チャンク座標</param>
        /// <param name="ceilingHeight">天井の高さ（Y座標）</param>
        public void GenerateColliders(
            Vector3Int minChunk,
            Vector3Int maxChunk,
            float ceilingHeight)
        {
            // チャンク座標からワールド座標範囲を計算
            Vector3 minWorld = new Vector3(
                minChunk.x * VoxelConstants.CHUNK_WIDTH,
                minChunk.y * VoxelConstants.CHUNK_HEIGHT,
                minChunk.z * VoxelConstants.CHUNK_DEPTH
            );
            Vector3 maxWorld = new Vector3(
                (maxChunk.x + 1) * VoxelConstants.CHUNK_WIDTH,
                (maxChunk.y + 1) * VoxelConstants.CHUNK_HEIGHT,
                (maxChunk.z + 1) * VoxelConstants.CHUNK_DEPTH
            );

            // マップのサイズを計算
            Vector3 mapSize = maxWorld - minWorld;
            Vector3 mapCenter = (minWorld + maxWorld) * 0.5f;

            // x, z 方向の壁の高さ（地下から天井まで）
            float verticalWallHeight = ceilingHeight - minWorld.y;
            float verticalWallCenterY = (minWorld.y + ceilingHeight) * 0.5f;

            // 境界壁用の親オブジェクトを作成
            GameObject boundaryParent = new GameObject("InvisibleBoundaryWalls");
            boundaryParent.transform.position = Vector3.zero;

            // 6つの面にコライダーを配置
            // x- 面（minX、地下から天井まで）
            CreateColliderWall(boundaryParent.transform, "Wall_X_Minus",
                new Vector3(minWorld.x - BOUNDARY_WALL_THICKNESS * 0.5f, verticalWallCenterY, mapCenter.z),
                new Vector3(BOUNDARY_WALL_THICKNESS, verticalWallHeight, mapSize.z));

            // x+ 面（maxX、地下から天井まで）
            CreateColliderWall(boundaryParent.transform, "Wall_X_Plus",
                new Vector3(maxWorld.x + BOUNDARY_WALL_THICKNESS * 0.5f, verticalWallCenterY, mapCenter.z),
                new Vector3(BOUNDARY_WALL_THICKNESS, verticalWallHeight, mapSize.z));

            // y- 面（minY、底面）
            CreateColliderWall(boundaryParent.transform, "Wall_Y_Minus",
                new Vector3(mapCenter.x, minWorld.y - BOUNDARY_WALL_THICKNESS * 0.5f, mapCenter.z),
                new Vector3(mapSize.x, BOUNDARY_WALL_THICKNESS, mapSize.z));

            // y+ 面（maxY、天井）
            CreateColliderWall(boundaryParent.transform, "Wall_Y_Plus_Ceiling",
                new Vector3(mapCenter.x, ceilingHeight + BOUNDARY_WALL_THICKNESS * 0.5f, mapCenter.z),
                new Vector3(mapSize.x, BOUNDARY_WALL_THICKNESS, mapSize.z));

            // z- 面（minZ、地下から天井まで）
            CreateColliderWall(boundaryParent.transform, "Wall_Z_Minus",
                new Vector3(mapCenter.x, verticalWallCenterY, minWorld.z - BOUNDARY_WALL_THICKNESS * 0.5f),
                new Vector3(mapSize.x, verticalWallHeight, BOUNDARY_WALL_THICKNESS));

            // z+ 面（maxZ、地下から天井まで）
            CreateColliderWall(boundaryParent.transform, "Wall_Z_Plus",
                new Vector3(mapCenter.x, verticalWallCenterY, maxWorld.z + BOUNDARY_WALL_THICKNESS * 0.5f),
                new Vector3(mapSize.x, verticalWallHeight, BOUNDARY_WALL_THICKNESS));

            Debug.Log($"透明境界壁生成: 6面（x±, y±, z±）、天井高さ={ceilingHeight}m");
        }

        /// <summary>
        /// Collider壁を作成
        /// </summary>
        private void CreateColliderWall(Transform parent, string name, Vector3 position, Vector3 size)
        {
            GameObject wall = new GameObject(name);
            wall.transform.SetParent(parent);
            wall.transform.position = position;

            BoxCollider collider = wall.AddComponent<BoxCollider>();
            collider.size = size;

        }
    }
}
