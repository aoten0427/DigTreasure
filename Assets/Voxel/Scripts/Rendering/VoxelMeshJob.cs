using Unity.Jobs;
using Unity.Collections;
using Unity.Burst;
using Unity.Mathematics;
using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルメッシュ生成Job
    /// </summary>
    [BurstCompile]
    public struct VoxelMeshJob : IJob
    {
        // 入力: ボクセルデータ（16×16×16 = 4096要素）
        [ReadOnly] public NativeArray<int> voxels;
        [ReadOnly] public NativeArray<float4> voxelColors; // RGBA

        // 隣接チャンクのボクセルデータ（境界の1層のみ、-1は存在しない）
        [ReadOnly] public NativeArray<int> neighborForward;  // +Z
        [ReadOnly] public NativeArray<int> neighborBack;     // -Z
        [ReadOnly] public NativeArray<int> neighborUp;       // +Y
        [ReadOnly] public NativeArray<int> neighborDown;     // -Y
        [ReadOnly] public NativeArray<int> neighborRight;    // +X
        [ReadOnly] public NativeArray<int> neighborLeft;     // -X

        // 境界情報（ワールド境界で両面メッシュ生成用）
        public bool isAtForwardBoundary;
        public bool isAtBackBoundary;
        public bool isAtUpBoundary;
        public bool isAtDownBoundary;
        public bool isAtRightBoundary;
        public bool isAtLeftBoundary;

        // 境界設定（各方向で両面描画を有効にするか）
        public bool enableForwardDoubleSided;
        public bool enableBackDoubleSided;
        public bool enableUpDoubleSided;
        public bool enableDownDoubleSided;
        public bool enableRightDoubleSided;
        public bool enableLeftDoubleSided;

        // 出力: メッシュデータ
        public NativeList<float3> vertices;
        public NativeList<float3> normals;
        public NativeList<float4> colors;
        public NativeList<int> triangles;

        // チャンク基準座標
        public float3 basePosition;

        // 定数
        public float voxelSize;
        public int sizeX;
        public int sizeY;
        public int sizeZ;

        public void Execute()
        {
            int vertexIndex = 0;

            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y < sizeY; y++)
                {
                    for (int z = 0; z < sizeZ; z++)
                    {
                        int index = GetIndex(x, y, z);
                        int voxelID = voxels[index];

                        if (voxelID == 0) continue; // 空ボクセル

                        float3 voxelPosition = basePosition + new float3(x, y, z) * voxelSize;
                        float4 voxelColor = voxelColors[index];

                        // 各面について隣接チェック
                        // Forward (+Z)
                        if (ShouldRenderFace(x, y, z + 1))
                        {
                            AddForwardFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (z == sizeZ - 1 && isAtForwardBoundary && enableForwardDoubleSided)
                                AddBackFace(voxelPosition + new float3(0, 0, voxelSize), voxelColor, ref vertexIndex);
                        }

                        // Back (-Z)
                        if (ShouldRenderFace(x, y, z - 1))
                        {
                            AddBackFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (z == 0 && isAtBackBoundary && enableBackDoubleSided)
                                AddForwardFace(voxelPosition - new float3(0, 0, voxelSize), voxelColor, ref vertexIndex);
                        }

                        // Up (+Y)
                        if (ShouldRenderFace(x, y + 1, z))
                        {
                            AddUpFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (y == sizeY - 1 && isAtUpBoundary && enableUpDoubleSided)
                                AddDownFace(voxelPosition + new float3(0, voxelSize, 0), voxelColor, ref vertexIndex);
                        }

                        // Down (-Y)
                        if (ShouldRenderFace(x, y - 1, z))
                        {
                            AddDownFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (y == 0 && isAtDownBoundary && enableDownDoubleSided)
                                AddUpFace(voxelPosition - new float3(0, voxelSize, 0), voxelColor, ref vertexIndex);
                        }

                        // Right (+X)
                        if (ShouldRenderFace(x + 1, y, z))
                        {
                            AddRightFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (x == sizeX - 1 && isAtRightBoundary && enableRightDoubleSided)
                                AddLeftFace(voxelPosition + new float3(voxelSize, 0, 0), voxelColor, ref vertexIndex);
                        }

                        // Left (-X)
                        if (ShouldRenderFace(x - 1, y, z))
                        {
                            AddLeftFace(voxelPosition, voxelColor, ref vertexIndex);
                            // 境界で両面描画が有効な場合、裏面も追加
                            if (x == 0 && isAtLeftBoundary && enableLeftDoubleSided)
                                AddRightFace(voxelPosition - new float3(voxelSize, 0, 0), voxelColor, ref vertexIndex);
                        }
                    }
                }
            }
        }

        private int GetIndex(int x, int y, int z)
        {
            return x + y * sizeX + z * sizeX * sizeY;
        }

        private bool ShouldRenderFace(int x, int y, int z)
        {
            // チャンク内の場合
            if (x >= 0 && x < sizeX && y >= 0 && y < sizeY && z >= 0 && z < sizeZ)
            {
                return voxels[GetIndex(x, y, z)] == 0;
            }

            //// チャンク境界の場合、隣接チャンクをチェック
            // Forward (+Z)
            if (z >= sizeZ)
            {
                if (neighborForward.IsCreated && neighborForward.Length > 0)
                {
                    int index = x + y * sizeX;
                    if (index >= 0 && index < neighborForward.Length)
                        return neighborForward[index] == 0;
                }
                return true; // 隣接チャンクデータがない場合は描画
            }

            // Back (-Z)
            if (z < 0)
            {
                if (neighborBack.IsCreated && neighborBack.Length > 0)
                {
                    int index = x + y * sizeX;
                    if (index >= 0 && index < neighborBack.Length)
                        return neighborBack[index] == 0;
                }
                return true;
            }

            // Up (+Y)
            if (y >= sizeY)
            {
                if (neighborUp.IsCreated && neighborUp.Length > 0)
                {
                    int index = x + z * sizeX;
                    if (index >= 0 && index < neighborUp.Length)
                        return neighborUp[index] == 0;
                }
                return true;
            }

            // Down (-Y)
            if (y < 0)
            {
                if (neighborDown.IsCreated && neighborDown.Length > 0)
                {
                    int index = x + z * sizeX;
                    if (index >= 0 && index < neighborDown.Length)
                        return neighborDown[index] == 0;
                }
                return true;
            }

            // Right (+X)
            if (x >= sizeX)
            {
                if (neighborRight.IsCreated && neighborRight.Length > 0)
                {
                    int index = y + z * sizeY;
                    if (index >= 0 && index < neighborRight.Length)
                        return neighborRight[index] == 0;
                }
                return true;
            }

            // Left (-X)
            if (x < 0)
            {
                if (neighborLeft.IsCreated && neighborLeft.Length > 0)
                {
                    int index = y + z * sizeY;
                    if (index >= 0 && index < neighborLeft.Length)
                        return neighborLeft[index] == 0;
                }
                return true;
            }

            // どの境界にも該当しない場合は描画
            return true;

        }

        // Forward面 (+Z)
        private void AddForwardFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(0, 0, voxelSize));
            vertices.Add(pos + new float3(voxelSize, 0, voxelSize));
            vertices.Add(pos + new float3(voxelSize, voxelSize, voxelSize));
            vertices.Add(pos + new float3(0, voxelSize, voxelSize));

            float3 normal = new float3(0, 0, 1);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // Back面 (-Z)
        private void AddBackFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(voxelSize, 0, 0));
            vertices.Add(pos + new float3(0, 0, 0));
            vertices.Add(pos + new float3(0, voxelSize, 0));
            vertices.Add(pos + new float3(voxelSize, voxelSize, 0));

            float3 normal = new float3(0, 0, -1);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // Up面 (+Y)
        private void AddUpFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(0, voxelSize, voxelSize));
            vertices.Add(pos + new float3(voxelSize, voxelSize, voxelSize));
            vertices.Add(pos + new float3(voxelSize, voxelSize, 0));
            vertices.Add(pos + new float3(0, voxelSize, 0));

            float3 normal = new float3(0, 1, 0);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // Down面 (-Y)
        private void AddDownFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(0, 0, 0));
            vertices.Add(pos + new float3(voxelSize, 0, 0));
            vertices.Add(pos + new float3(voxelSize, 0, voxelSize));
            vertices.Add(pos + new float3(0, 0, voxelSize));

            float3 normal = new float3(0, -1, 0);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // Right面 (+X)
        private void AddRightFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(voxelSize, 0, voxelSize));
            vertices.Add(pos + new float3(voxelSize, 0, 0));
            vertices.Add(pos + new float3(voxelSize, voxelSize, 0));
            vertices.Add(pos + new float3(voxelSize, voxelSize, voxelSize));

            float3 normal = new float3(1, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        // Left面 (-X)
        private void AddLeftFace(float3 pos, float4 color, ref int vertexIndex)
        {
            vertices.Add(pos + new float3(0, 0, 0));
            vertices.Add(pos + new float3(0, 0, voxelSize));
            vertices.Add(pos + new float3(0, voxelSize, voxelSize));
            vertices.Add(pos + new float3(0, voxelSize, 0));

            float3 normal = new float3(-1, 0, 0);
            for (int i = 0; i < 4; i++)
            {
                normals.Add(normal);
                colors.Add(color);
            }

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }
    }
}
