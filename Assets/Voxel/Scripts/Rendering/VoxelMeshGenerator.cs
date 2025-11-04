using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// メッシュ生成
    /// </summary>
    public static class VoxelMeshGenerator
    {
        
        /// <summary>
        /// ボクセル配列からMeshを生成
        /// </summary>
        /// <param name="voxels">ボクセル3次元配列</param>
        /// <param name="sizeX">X方向サイズ</param>
        /// <param name="sizeY">Y方向サイズ</param>
        /// <param name="sizeZ">Z方向サイズ</param>
        /// <param name="basePosition">基準位置</param>
        /// <returns>生成されたMesh</returns>
        public static Mesh GenerateVoxelMesh(Voxel[,,] voxels, int sizeX, int sizeY, int sizeZ, Vector3 basePosition = default)
        {
            // 範囲指定なしの場合は全体をスキャン
            return GenerateVoxelMeshPartial(
                voxels, sizeX, sizeY, sizeZ, basePosition,
                Vector3Int.zero,
                new Vector3Int(sizeX - 1, sizeY - 1, sizeZ - 1)
            );
        }

        /// <summary>
        /// ボクセル配列から部分的にMeshを生成
        /// </summary>
        /// <param name="voxels">ボクセル3次元配列</param>
        /// <param name="sizeX">X方向サイズ</param>
        /// <param name="sizeY">Y方向サイズ</param>
        /// <param name="sizeZ">Z方向サイズ</param>
        /// <param name="basePosition">基準位置</param>
        /// <param name="scanMin">スキャン開始位置（ローカル座標）</param>
        /// <param name="scanMax">スキャン終了位置（ローカル座標、含む）</param>
        /// <returns>生成されたMesh</returns>
        public static Mesh GenerateVoxelMeshPartial(
            Voxel[,,] voxels,
            int sizeX, int sizeY, int sizeZ,
            Vector3 basePosition,
            Vector3Int scanMin,
            Vector3Int scanMax)
        {
            if (voxels == null)
            {
                Debug.LogWarning("[VoxelMeshGenerator] ボクセル配列がnullです。");
                return null;
            }

            var mesh = new Mesh();
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var colors = new List<Color>();
            int vertexIndex = 0;

            // 範囲制限付きループ
            for (int x = scanMin.x; x <= scanMax.x; x++)
            {
                for (int y = scanMin.y; y <= scanMax.y; y++)
                {
                    for (int z = scanMin.z; z <= scanMax.z; z++)
                    {
                        Voxel currentVoxel = voxels[x, y, z];
                        if (currentVoxel.IsEmpty)
                        {
                            continue;
                        }

                        Vector3 voxelPosition = basePosition + new Vector3(x, y, z) * VoxelConstants.VOXEL_SIZE;
                        Color voxelColor = currentVoxel.GetColor();

                        // 各面について隣接チェック
                        for (int faceIndex = 0; faceIndex < VoxelConstants.FACE_COUNT; faceIndex++)
                        {
                            var faceDirection = (VoxelConstants.FaceDirection)faceIndex;

                            // 隣接ボクセルをチェック
                            Vector3Int neighborPos = GetNeighborPosition(x, y, z, faceDirection);
                            bool shouldRenderFace = ShouldRenderFace(voxels, sizeX, sizeY, sizeZ, neighborPos);

                            if (shouldRenderFace)
                            {
                                AddFaceToMesh(vertices, triangles, colors, voxelPosition, faceDirection, voxelColor, ref vertexIndex);
                            }
                        }
                    }
                }
            }

            if (vertices.Count == 0)
            {
                return null;
            }

            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.colors = colors.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            mesh.name = $"VoxelMesh_{sizeX}x{sizeY}x{sizeZ}_Partial";
            return mesh;
        }

        /// <summary>
        /// 指定された方向の隣接ボクセル位置を取得
        /// </summary>
        /// <param name="x">X座標</param>
        /// <param name="y">Y座標</param>
        /// <param name="z">Z座標</param>
        /// <param name="direction">面の方向</param>
        /// <returns>隣接位置</returns>
        private static Vector3Int GetNeighborPosition(int x, int y, int z, VoxelConstants.FaceDirection direction)
        {
            return direction switch
            {
                VoxelConstants.FaceDirection.Forward => new Vector3Int(x, y, z + 1),
                VoxelConstants.FaceDirection.Back => new Vector3Int(x, y, z - 1),
                VoxelConstants.FaceDirection.Up => new Vector3Int(x, y + 1, z),
                VoxelConstants.FaceDirection.Down => new Vector3Int(x, y - 1, z),
                VoxelConstants.FaceDirection.Right => new Vector3Int(x + 1, y, z),
                VoxelConstants.FaceDirection.Left => new Vector3Int(x - 1, y, z),
                _ => new Vector3Int(x, y, z)
            };
        }

        /// <summary>
        /// 面を描画すべきかチェック
        /// </summary>
        /// <param name="voxels">ボクセル配列</param>
        /// <param name="sizeX">X方向サイズ</param>
        /// <param name="sizeY">Y方向サイズ</param>
        /// <param name="sizeZ">Z方向サイズ</param>
        /// <param name="neighborPos">隣接位置</param>
        /// <returns>描画すべき場合true</returns>
        private static bool ShouldRenderFace(Voxel[,,] voxels, int sizeX, int sizeY, int sizeZ, Vector3Int neighborPos)
        {
            // 範囲外の場合は面を描画する（チャンク境界）
            if (neighborPos.x < 0 || neighborPos.x >= sizeX ||
                neighborPos.y < 0 || neighborPos.y >= sizeY ||
                neighborPos.z < 0 || neighborPos.z >= sizeZ)
            {
                return true;
            }

            // 隣接ボクセルが空の場合は面を描画する
            return voxels[neighborPos.x, neighborPos.y, neighborPos.z].IsEmpty;
        }

        /// <summary>
        /// 面をメッシュに追加
        /// </summary>
        /// <param name="vertices">頂点リスト</param>
        /// <param name="triangles">三角形リスト</param>
        /// <param name="colors">色リスト</param>
        /// <param name="position">ボクセル位置</param>
        /// <param name="direction">面の方向</param>
        /// <param name="color">色</param>
        /// <param name="vertexIndex">頂点インデックス</param>
        private static void AddFaceToMesh(List<Vector3> vertices, List<int> triangles, List<Color> colors, 
            Vector3 position, VoxelConstants.FaceDirection direction, Color color, ref int vertexIndex)
        {
            Vector3[] faceVertices = GetFaceVertices(position, direction);
            
            // 頂点を追加
            foreach (Vector3 vertex in faceVertices)
            {
                vertices.Add(vertex);
                colors.Add(color);
            }

            // 三角形を追加（時計回り）
            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 1);
            triangles.Add(vertexIndex + 2);

            triangles.Add(vertexIndex);
            triangles.Add(vertexIndex + 2);
            triangles.Add(vertexIndex + 3);

            vertexIndex += 4;
        }

        /// <summary>
        /// 指定方向の面の頂点を取得
        /// </summary>
        /// <param name="position">ボクセル位置</param>
        /// <param name="direction">面の方向</param>
        /// <returns>面の頂点配列（4個）</returns>
        private static Vector3[] GetFaceVertices(Vector3 position, VoxelConstants.FaceDirection direction)
        {
            float size = VoxelConstants.VOXEL_SIZE;
            Vector3[] vertices = new Vector3[4];

            switch (direction)
            {
                case VoxelConstants.FaceDirection.Forward: // +Z
                    vertices[0] = position + new Vector3(0, 0, size);
                    vertices[1] = position + new Vector3(size, 0, size);
                    vertices[2] = position + new Vector3(size, size, size);
                    vertices[3] = position + new Vector3(0, size, size);
                    break;

                case VoxelConstants.FaceDirection.Back: // -Z
                    vertices[0] = position + new Vector3(size, 0, 0);
                    vertices[1] = position + new Vector3(0, 0, 0);
                    vertices[2] = position + new Vector3(0, size, 0);
                    vertices[3] = position + new Vector3(size, size, 0);
                    break;

                case VoxelConstants.FaceDirection.Up: // +Y
                    vertices[0] = position + new Vector3(0, size, size);
                    vertices[1] = position + new Vector3(size, size, size);
                    vertices[2] = position + new Vector3(size, size, 0);
                    vertices[3] = position + new Vector3(0, size, 0);
                    break;

                case VoxelConstants.FaceDirection.Down: // -Y
                    vertices[0] = position + new Vector3(0, 0, 0);
                    vertices[1] = position + new Vector3(size, 0, 0);
                    vertices[2] = position + new Vector3(size, 0, size);
                    vertices[3] = position + new Vector3(0, 0, size);
                    break;

                case VoxelConstants.FaceDirection.Right: // +X
                    vertices[0] = position + new Vector3(size, 0, size);
                    vertices[1] = position + new Vector3(size, 0, 0);
                    vertices[2] = position + new Vector3(size, size, 0);
                    vertices[3] = position + new Vector3(size, size, size);
                    break;

                case VoxelConstants.FaceDirection.Left: // -X
                    vertices[0] = position + new Vector3(0, 0, 0);
                    vertices[1] = position + new Vector3(0, 0, size);
                    vertices[2] = position + new Vector3(0, size, size);
                    vertices[3] = position + new Vector3(0, size, 0);
                    break;
            }

            return vertices;
        }
    }
}