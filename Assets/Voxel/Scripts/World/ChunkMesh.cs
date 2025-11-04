using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;

namespace VoxelWorld
{
    /// <summary>
    /// Chunk用メッシュ生成システム
    /// 同期版と非同期版の両方を提供
    /// </summary>
    public static class ChunkMesh
    {
        /// <summary>
        /// 並列メッシュ生成用のデータ構造
        /// </summary>
        public class MeshJobData
        {
            public NativeArray<int> voxelArray;
            public NativeArray<float4> colorArray;
            public NativeList<float3> vertices;
            public NativeList<float3> normals;
            public NativeList<float4> colors;
            public NativeList<int> triangles;
            public JobHandle handle;

            // 隣接チャンクの境界データ
            public NativeArray<int> neighborForward;
            public NativeArray<int> neighborBack;
            public NativeArray<int> neighborUp;
            public NativeArray<int> neighborDown;
            public NativeArray<int> neighborRight;
            public NativeArray<int> neighborLeft;

            public void Dispose()
            {
                if (voxelArray.IsCreated) voxelArray.Dispose();
                if (colorArray.IsCreated) colorArray.Dispose();
                if (vertices.IsCreated) vertices.Dispose();
                if (normals.IsCreated) normals.Dispose();
                if (colors.IsCreated) colors.Dispose();
                if (triangles.IsCreated) triangles.Dispose();

                // 隣接データの破棄
                if (neighborForward.IsCreated) neighborForward.Dispose();
                if (neighborBack.IsCreated) neighborBack.Dispose();
                if (neighborUp.IsCreated) neighborUp.Dispose();
                if (neighborDown.IsCreated) neighborDown.Dispose();
                if (neighborRight.IsCreated) neighborRight.Dispose();
                if (neighborLeft.IsCreated) neighborLeft.Dispose();
            }
        }

        /// <summary>
        /// 同期的メッシュ生成（単一チャンクの即座更新用）
        /// 用途: VoxelSeparationManager等、即座にメッシュが必要な場合
        /// 大量のチャンク更新にはVoxelMeshManagerの並列処理を使用してください
        /// </summary>
        /// <param name="chunk">メッシュを生成するチャンク</param>
        /// <returns>生成されたMesh（キャッシュには保存されない）</returns>
        public static Mesh GenerateMesh(Chunk chunk)
        {
            var jobData = ScheduleMeshGeneration(chunk);
            return CompleteMeshGeneration(jobData);
        }

        /// <summary>
        /// 非同期メッシュ生成（Jobスケジュールのみ、完了待機なし）
        /// 並列処理用 - 複数チャンクを一度にスケジュールして並列実行可能
        /// </summary>
        /// <param name="chunk">メッシュを生成するチャンク</param>
        /// <param name="chunkManager">隣接チャンク取得用のChunkManager（nullの場合は隣接データなし）</param>
        /// <param name="boundaryInfo">境界情報（nullの場合は境界なし）</param>
        /// <param name="boundarySettings">境界設定（nullの場合はデフォルト設定）</param>
        /// <returns>JobHandleとNativeContainerを含むデータ</returns>
        public static MeshJobData ScheduleMeshGeneration(Chunk chunk, ChunkManager chunkManager = null, ChunkBoundaryInfo? boundaryInfo = null, BoundaryMeshSettings boundarySettings = null)
        {
            // チャンクサイズ定数
            int width = VoxelConstants.CHUNK_WIDTH;
            int height = VoxelConstants.CHUNK_HEIGHT;
            int depth = VoxelConstants.CHUNK_DEPTH;
            int totalVoxels = width * height * depth;

            // ボクセルデータを取得
            Voxel[,,] voxels = chunk.GetVoxelData();
            Vector3Int chunkPosition = chunk.ChunkPosition;

            // NativeArrayの作成（TempJob: 複数フレームまたぐ可能性があるため）
            var voxelArray = new NativeArray<int>(totalVoxels, Allocator.TempJob);
            var colorArray = new NativeArray<float4>(totalVoxels, Allocator.TempJob);

            // Voxel[,,]からNativeArrayに変換
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int z = 0; z < depth; z++)
                    {
                        int index = x + y * width + z * width * height;
                        voxelArray[index] = voxels[x, y, z].VoxelId;

                        // 色情報を取得（VoxelDataBase経由、Dictionaryキャッシュで高速）
                        Color voxelColor = VoxelDataBase.GetVoxelColorStatic(voxelArray[index]);
                        colorArray[index] = new float4(voxelColor.r, voxelColor.g, voxelColor.b, voxelColor.a);
                    }
                }
            }

            // 出力用NativeListの作成（初期容量は推定値）
            var vertices = new NativeList<float3>(totalVoxels * 4, Allocator.TempJob);
            var normals = new NativeList<float3>(totalVoxels * 4, Allocator.TempJob);
            var colors = new NativeList<float4>(totalVoxels * 4, Allocator.TempJob);
            var triangles = new NativeList<int>(totalVoxels * 6, Allocator.TempJob);

            // 隣接チャンクの境界データを抽出
            var neighborForward = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Forward);
            var neighborBack = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Back);
            var neighborUp = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Up);
            var neighborDown = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Down);
            var neighborRight = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Right);
            var neighborLeft = ExtractNeighborBoundary(chunkManager, chunkPosition, Direction.Left);

            // 境界情報とSettings設定
            ChunkBoundaryInfo boundary = boundaryInfo ?? new ChunkBoundaryInfo();
            BoundaryMeshSettings settings = boundarySettings ?? new BoundaryMeshSettings();

            // VoxelMeshJobを作成
            var meshJob = new VoxelMeshJob
            {
                voxels = voxelArray,
                voxelColors = colorArray,
                vertices = vertices,
                normals = normals,
                colors = colors,
                triangles = triangles,
                basePosition = float3.zero,
                voxelSize = VoxelConstants.VOXEL_SIZE,
                sizeX = width,
                sizeY = height,
                sizeZ = depth,
                neighborForward = neighborForward,
                neighborBack = neighborBack,
                neighborUp = neighborUp,
                neighborDown = neighborDown,
                neighborRight = neighborRight,
                neighborLeft = neighborLeft,
                // 境界情報
                isAtForwardBoundary = boundary.isAtForwardBoundary,
                isAtBackBoundary = boundary.isAtBackBoundary,
                isAtUpBoundary = boundary.isAtUpBoundary,
                isAtDownBoundary = boundary.isAtDownBoundary,
                isAtRightBoundary = boundary.isAtRightBoundary,
                isAtLeftBoundary = boundary.isAtLeftBoundary,
                // 境界設定
                enableForwardDoubleSided = settings.enableForward,
                enableBackDoubleSided = settings.enableBack,
                enableUpDoubleSided = settings.enableUp,
                enableDownDoubleSided = settings.enableDown,
                enableRightDoubleSided = settings.enableRight,
                enableLeftDoubleSided = settings.enableLeft
            };

            // Jobをスケジュール（完了待機なし）
            JobHandle handle = meshJob.Schedule();

            return new MeshJobData
            {
                voxelArray = voxelArray,
                colorArray = colorArray,
                vertices = vertices,
                normals = normals,
                colors = colors,
                triangles = triangles,
                handle = handle,
                neighborForward = neighborForward,
                neighborBack = neighborBack,
                neighborUp = neighborUp,
                neighborDown = neighborDown,
                neighborRight = neighborRight,
                neighborLeft = neighborLeft
            };
        }

        /// <summary>
        /// MeshJobDataからMeshを構築
        /// Job完了待ちとNativeArray→Unity配列変換を実行
        /// </summary>
        /// <param name="jobData">完了待ち対象のMeshJobData</param>
        /// <returns>生成されたMesh（キャッシュには保存されない）</returns>
        public static Mesh CompleteMeshGeneration(MeshJobData jobData)
        {
            // Jobが完了していることを確認
            jobData.handle.Complete();

            // Meshを生成
            Mesh newMesh = new Mesh();
            newMesh.name = "VoxelChunk";

            // Meshにデータを直接設定
            newMesh.SetVertices(jobData.vertices.AsArray());
            newMesh.SetNormals(jobData.normals.AsArray());
            newMesh.SetColors(jobData.colors.AsArray());

            // Trianglesのみ配列変換が必要（API制限）
            int[] triangleArray = new int[jobData.triangles.Length];
            for (int i = 0; i < jobData.triangles.Length; i++)
            {
                triangleArray[i] = jobData.triangles[i];
            }
            newMesh.triangles = triangleArray;

            // 法線は事前計算済みのため、RecalculateNormals()不要（約15ms削減）
            newMesh.RecalculateBounds();

            // NativeContainerをDispose
            jobData.Dispose();

            return newMesh;
        }

        /// <summary>
        /// 隣接チャンクの境界データを抽出
        /// </summary>
        /// <param name="chunkManager">ChunkManager</param>
        /// <param name="chunkPosition">現在のチャンク座標</param>
        /// <param name="direction">隣接方向</param>
        /// <returns>境界データのNativeArray（隣接チャンクが存在しない場合は空配列）</returns>
        private static NativeArray<int> ExtractNeighborBoundary(ChunkManager chunkManager, Vector3Int chunkPosition, Direction direction)
        {
            // ChunkManagerがnullの場合は空配列を返す
            if (chunkManager == null)
            {
                return new NativeArray<int>(0, Allocator.TempJob);
            }

            // 隣接チャンクを取得
            Chunk neighborChunk = chunkManager.GetNeighborChunk(chunkPosition, direction);
            if (neighborChunk == null)
            {
                return new NativeArray<int>(0, Allocator.TempJob);
            }

            // 境界データのサイズを計算
            int width = VoxelConstants.CHUNK_WIDTH;
            int height = VoxelConstants.CHUNK_HEIGHT;
            int depth = VoxelConstants.CHUNK_DEPTH;
            int boundarySize = 0;

            // 方向に応じた境界サイズを決定
            switch (direction)
            {
                case Direction.Forward:
                case Direction.Back:
                    boundarySize = width * height; // X×Y面
                    break;
                case Direction.Up:
                case Direction.Down:
                    boundarySize = width * depth; // X×Z面
                    break;
                case Direction.Right:
                case Direction.Left:
                    boundarySize = height * depth; // Y×Z面
                    break;
            }

            var boundaryData = new NativeArray<int>(boundarySize, Allocator.TempJob);
            Voxel[,,] neighborVoxels = neighborChunk.GetVoxelData();

            // 方向に応じて境界データを抽出
            switch (direction)
            {
                case Direction.Forward: // +Z方向の隣 → その最初のZ層（z=0）を取得
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int index = x + y * width;
                            boundaryData[index] = neighborVoxels[x, y, 0].VoxelId;
                        }
                    }
                    break;

                case Direction.Back: // -Z方向の隣 → その最後のZ層（z=depth-1）を取得
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            int index = x + y * width;
                            boundaryData[index] = neighborVoxels[x, y, depth - 1].VoxelId;
                        }
                    }
                    break;

                case Direction.Up: // +Y方向の隣 → その最初のY層（y=0）を取得
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int index = x + z * width;
                            boundaryData[index] = neighborVoxels[x, 0, z].VoxelId;
                        }
                    }
                    break;

                case Direction.Down: // -Y方向の隣 → その最後のY層（y=height-1）を取得
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int index = x + z * width;
                            boundaryData[index] = neighborVoxels[x, height - 1, z].VoxelId;
                        }
                    }
                    break;

                case Direction.Right: // +X方向の隣 → その最初のX層（x=0）を取得
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int index = y + z * height;
                            boundaryData[index] = neighborVoxels[0, y, z].VoxelId;
                        }
                    }
                    break;

                case Direction.Left: // -X方向の隣 → その最後のX層（x=width-1）を取得
                    for (int y = 0; y < height; y++)
                    {
                        for (int z = 0; z < depth; z++)
                        {
                            int index = y + z * height;
                            boundaryData[index] = neighborVoxels[width - 1, y, z].VoxelId;
                        }
                    }
                    break;
            }

            return boundaryData;
        }
    }
}
