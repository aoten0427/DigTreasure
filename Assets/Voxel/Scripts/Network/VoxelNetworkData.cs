using UnityEngine;
using Fusion;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// ネットワーク送信用のボクセル変更データ（8バイト/個）
    /// </summary>
    public struct NetworkVoxelChange : INetworkStruct
    {
        public short x;          // 2バイト (ボクセル座標: -32768～32767)
        public short y;          // 2バイト
        public short z;          // 2バイト
        public short voxelID;    // 2バイト

        /// <summary>
        /// VoxelUpdateからNetworkVoxelChangeに変換
        /// </summary>
        public static NetworkVoxelChange FromVoxelUpdate(VoxelUpdate update)
        {
            return new NetworkVoxelChange
            {
                x = (short)Mathf.RoundToInt(update.WorldPosition.x / VoxelConstants.VOXEL_SIZE),
                y = (short)Mathf.RoundToInt(update.WorldPosition.y / VoxelConstants.VOXEL_SIZE),
                z = (short)Mathf.RoundToInt(update.WorldPosition.z / VoxelConstants.VOXEL_SIZE),
                voxelID = (short)update.VoxelID
            };
        }

        /// <summary>
        /// NetworkVoxelChangeからVoxelUpdateに変換
        /// </summary>
        public VoxelUpdate ToVoxelUpdate()
        {
            return new VoxelUpdate
            {
                WorldPosition = new Vector3(
                    x * VoxelConstants.VOXEL_SIZE,
                    y * VoxelConstants.VOXEL_SIZE,
                    z * VoxelConstants.VOXEL_SIZE
                ),
                VoxelID = voxelID
            };
        }
    }
}
