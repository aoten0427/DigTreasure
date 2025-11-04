using UnityEngine;

namespace VoxelWorld
{
    /// <summary>
    /// 境界メッシュの両面描画設定
    /// ワールド境界のボクセルで裏面も描画するかを方向別に制御
    /// </summary>
    [System.Serializable]
    public class BoundaryMeshSettings
    {
        [Header("境界範囲設定（チャンク座標）")]
        [Tooltip("前方(+Z)境界となるチャンクZ座標")]
        public int forwardBoundaryZ = int.MaxValue;

        [Tooltip("後方(-Z)境界となるチャンクZ座標")]
        public int backBoundaryZ = int.MinValue;

        [Tooltip("上方(+Y)境界となるチャンクY座標")]
        public int upBoundaryY = int.MaxValue;

        [Tooltip("下方(-Y)境界となるチャンクY座標")]
        public int downBoundaryY = int.MinValue;

        [Tooltip("右方(+X)境界となるチャンクX座標")]
        public int rightBoundaryX = int.MaxValue;

        [Tooltip("左方(-X)境界となるチャンクX座標")]
        public int leftBoundaryX = int.MinValue;

        [Header("両面描画を有効にする方向")]
        [Tooltip("前方(+Z)境界で両面描画")]
        public bool enableForward = true;

        [Tooltip("後方(-Z)境界で両面描画")]
        public bool enableBack = true;

        [Tooltip("上方(+Y)境界で両面描画")]
        public bool enableUp = true;

        [Tooltip("下方(-Y)境界で両面描画")]
        public bool enableDown = true;

        [Tooltip("右方(+X)境界で両面描画")]
        public bool enableRight = true;

        [Tooltip("左方(-X)境界で両面描画")]
        public bool enableLeft = true;

        /// <summary>
        /// 指定方向で両面描画が有効か判定
        /// </summary>
        public bool IsEnabledForDirection(Direction direction)
        {
            return direction switch
            {
                Direction.Forward => enableForward,
                Direction.Back => enableBack,
                Direction.Up => enableUp,
                Direction.Down => enableDown,
                Direction.Right => enableRight,
                Direction.Left => enableLeft,
                _ => false
            };
        }

        /// <summary>
        /// チャンク座標から境界情報を取得
        /// </summary>
        /// <param name="chunkPosition">チャンク座標</param>
        /// <returns>境界情報</returns>
        public ChunkBoundaryInfo GetChunkBoundaryInfo(Vector3Int chunkPosition)
        {
            ChunkBoundaryInfo info = new ChunkBoundaryInfo();
            info.isAtForwardBoundary = chunkPosition.z >= forwardBoundaryZ;
            info.isAtBackBoundary = chunkPosition.z <= backBoundaryZ;
            info.isAtUpBoundary = chunkPosition.y >= upBoundaryY;
            info.isAtDownBoundary = chunkPosition.y <= downBoundaryY;
            info.isAtRightBoundary = chunkPosition.x >= rightBoundaryX;
            info.isAtLeftBoundary = chunkPosition.x <= leftBoundaryX;
            return info;
        }
    }

    /// <summary>
    /// チャンクの境界情報
    /// どの方向がワールド境界かを示す
    /// </summary>
    public struct ChunkBoundaryInfo
    {
        public bool isAtForwardBoundary;  // +Z
        public bool isAtBackBoundary;     // -Z
        public bool isAtUpBoundary;       // +Y
        public bool isAtDownBoundary;     // -Y
        public bool isAtRightBoundary;    // +X
        public bool isAtLeftBoundary;     // -X

        /// <summary>
        /// いずれかの方向で境界にあるか判定
        /// </summary>
        public bool IsAtAnyBoundary()
        {
            return isAtForwardBoundary || isAtBackBoundary ||
                   isAtUpBoundary || isAtDownBoundary ||
                   isAtRightBoundary || isAtLeftBoundary;
        }

        /// <summary>
        /// 指定方向で境界にあるか判定
        /// </summary>
        public bool IsAtBoundary(Direction direction)
        {
            return direction switch
            {
                Direction.Forward => isAtForwardBoundary,
                Direction.Back => isAtBackBoundary,
                Direction.Up => isAtUpBoundary,
                Direction.Down => isAtDownBoundary,
                Direction.Right => isAtRightBoundary,
                Direction.Left => isAtLeftBoundary,
                _ => false
            };
        }
    }
}
