using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルシステムの定数定義クラス
    /// チャンクサイズ、ボクセルタイプ、パフォーマンス設定などの定数を管理
    /// </summary>
    public static class VoxelConstants
    {
        // チャンク関連定数
        //チャンクの幅（X軸方向のボクセル数)
        public const int CHUNK_WIDTH = 16;
        //チャンクの高さ（Y軸方向のボクセル数）
        public const int CHUNK_HEIGHT = 16;
        //チャンクの奥行き（Z軸方向のボクセル数)
        public const int CHUNK_DEPTH = 16;
        //チャンク内の総ボクセル数
        public const int VOXELS_PER_CHUNK = CHUNK_WIDTH * CHUNK_HEIGHT * CHUNK_DEPTH;

        // ボクセル関連定数
        //個別ボクセルのサイズ
        public const float VOXEL_SIZE = 0.5f;

        // パフォーマンス関連定数
        //同時破壊可能な最大ボクセル数
        public const int MAX_DESTRUCTION_COUNT = 2000;
        //破壊処理の分散フレーム数
        public const int MIN_DESTRUCTION_FRAMES = 5;
        //破壊処理の分散フレーム数
        public const int MAX_DESTRUCTION_FRAMES = 10;
        //1フレームあたりの最大破壊処理数
        public const int MAX_DESTRUCTION_PER_FRAME = MAX_DESTRUCTION_COUNT / MIN_DESTRUCTION_FRAMES;

        // ワールド関連定数
        //想定ワールドサイズ
        public const int MAX_WORLD_SIZE_X = 1000;
        public const int MAX_WORLD_SIZE_Y = 1000;
        public const int MAX_WORLD_SIZE_Z = 1000;
        
        //ワールドの最大チャンク数
        public const int MAX_CHUNKS_X = MAX_WORLD_SIZE_X / CHUNK_WIDTH;
        public const int MAX_CHUNKS_Y = MAX_WORLD_SIZE_Y / CHUNK_HEIGHT;
        public const int MAX_CHUNKS_Z = MAX_WORLD_SIZE_Z / CHUNK_DEPTH;

        // ボクセルタイプ定数
        //空のボクセルを表すID
        public const int EMPTY_VOXEL_ID = 0;
        //基本ボクセルタイプの開始ID
        public const int BASE_VOXEL_ID_START = 1;
        //最大ボクセルタイプ数
        public const int MAX_VOXEL_TYPES = 256;

        // メッシュ生成関連定数
        //面の方向を表す列挙型
        public enum FaceDirection
        {
            //前面（+Z方向
            Forward = 0,
            //背面（-Z方向
            Back = 1,
            //上面（+Y方向
            Up = 2,
            //下面（-Y方向）
            Down = 3,
            //右面（+X方向）
            Right = 4,
            //左面（-X方向
            Left = 5
        }
        
        //面の総数
        public const int FACE_COUNT = 6;

        //デフォルトのテスト用硬度
        public const float DEFAULT_TEST_HARDNESS = 1.0f;

        //デフォルトの最大耐久度
        public const float DEFAULT_MAX_DURABILITY = 1f;


        // 座標変換用ヘルパーメソッド
        /// <summary>
        /// チャンク座標からワールド座標を取得
        /// </summary>
        /// <param name="chunkX">チャンクX座標</param>
        /// <param name="chunkY">チャンクY座標</param>
        /// <param name="chunkZ">チャンクZ座標</param>
        /// <returns>ワールド座標</returns>
        public static Vector3 ChunkToWorldPosition(int chunkX, int chunkY, int chunkZ)
        {
            return new Vector3(
                chunkX * CHUNK_WIDTH * VOXEL_SIZE,
                chunkY * CHUNK_HEIGHT * VOXEL_SIZE,
                chunkZ * CHUNK_DEPTH * VOXEL_SIZE
            );
        }
        
        /// <summary>
        /// ワールド座標からチャンク座標を取得
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <returns>チャンク座標</returns>
        public static Vector3Int WorldToChunkPosition(Vector3 worldPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(worldPosition.x / (CHUNK_WIDTH * VOXEL_SIZE)),
                Mathf.FloorToInt(worldPosition.y / (CHUNK_HEIGHT * VOXEL_SIZE)),
                Mathf.FloorToInt(worldPosition.z / (CHUNK_DEPTH * VOXEL_SIZE))
            );
        }
        
        /// <summary>
        /// チャンク内のローカル座標からワールド座標を取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <param name="localX">チャンク内X座標</param>
        /// <param name="localY">チャンク内Y座標</param>
        /// <param name="localZ">チャンク内Z座標</param>
        /// <returns>ワールド座標</returns>
        public static Vector3 LocalToWorldPosition(Vector3Int chunkPosition, int localX, int localY, int localZ)
        {
            Vector3 chunkWorldPos = ChunkToWorldPosition(chunkPosition.x, chunkPosition.y, chunkPosition.z);
            return chunkWorldPos + new Vector3(localX * VOXEL_SIZE, localY * VOXEL_SIZE, localZ * VOXEL_SIZE);
        }
        
        #region SeparatedVoxelObject Coordinate Conversion
        
        /// <summary>
        /// ワールド座標をSeparatedVoxelObjectのローカル座標に変換
        /// </summary>
        /// <param name="worldPosition">ワールド座標</param>
        /// <param name="objectWorldPosition">SeparatedVoxelObjectのワールド座標</param>
        /// <returns>ローカル座標</returns>
        public static Vector3 SeparatedObjectWorldToLocal(Vector3 worldPosition, Vector3 objectWorldPosition)
        {
            return worldPosition - objectWorldPosition;
        }
        
        /// <summary>
        /// ワールド座標をSeparatedVoxelObjectのボクセルインデックスに変換
        /// </summary>
        /// <param name="localPosition">ローカル座標</param>
        /// <returns>ボクセルインデックス</returns>
        public static Vector3Int SeparatedObjectWorldToIndex(Vector3 localPosition)
        {
            return new Vector3Int(
                Mathf.FloorToInt(localPosition.x / VOXEL_SIZE),
                Mathf.FloorToInt(localPosition.y / VOXEL_SIZE),
                Mathf.FloorToInt(localPosition.z / VOXEL_SIZE)
            );
        }
        
        /// <summary>
        /// SeparatedVoxelObjectのボクセルインデックスをローカル座標に変換
        /// </summary>
        /// <param name="index">ボクセルインデックス</param>
        /// <returns>ローカル座標</returns>
        public static Vector3 SeparatedObjectIndexToLocal(Vector3Int index)
        {
            return new Vector3(
                index.x * VOXEL_SIZE,
                index.y * VOXEL_SIZE,
                index.z * VOXEL_SIZE
            );
        }
        
        /// <summary>
        /// SeparatedVoxelObjectのローカル座標をワールド座標に変換
        /// </summary>
        /// <param name="localPosition">ローカル座標</param>
        /// <param name="objectWorldPosition">SeparatedVoxelObjectのワールド座標</param>
        /// <returns>ワールド座標</returns>
        public static Vector3 SeparatedObjectLocalToWorld(Vector3 localPosition, Vector3 objectWorldPosition)
        {
            return localPosition + objectWorldPosition;
        }
        
        /// <summary>
        /// SeparatedVoxelObjectのワールド境界ボックスを取得
        /// </summary>
        /// <param name="size">オブジェクトのサイズ（ボクセル単位）</param>
        /// <param name="worldPosition">オブジェクトのワールド座標</param>
        /// <returns>ワールド境界ボックス</returns>
        public static Bounds GetSeparatedObjectWorldBounds(Vector3Int size, Vector3 worldPosition)
        {
            Vector3 worldSize = new Vector3(
                size.x * VOXEL_SIZE,
                size.y * VOXEL_SIZE,
                size.z * VOXEL_SIZE
            );
            
            Vector3 center = worldPosition + worldSize * 0.5f;
            return new Bounds(center, worldSize);
        }
        
        #endregion
    }
}