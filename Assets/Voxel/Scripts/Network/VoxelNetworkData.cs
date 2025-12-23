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

    /// <summary>
    /// ネットワーク送信用の破壊専用データ（6バイト/個）
    /// </summary>
    public struct NetworkVoxelDestruction : INetworkStruct
    {
        public short x;  // 2バイト (ボクセル座標: -32768～32767)
        public short y;  // 2バイト
        public short z;  // 2バイト
        // voxelID なし（常に0のため）

        /// <summary>
        /// VoxelUpdateからNetworkVoxelDestructionに変換
        /// </summary>
        public static NetworkVoxelDestruction FromVoxelUpdate(VoxelUpdate update)
        {
            return new NetworkVoxelDestruction
            {
                x = (short)Mathf.RoundToInt(update.WorldPosition.x / VoxelConstants.VOXEL_SIZE),
                y = (short)Mathf.RoundToInt(update.WorldPosition.y / VoxelConstants.VOXEL_SIZE),
                z = (short)Mathf.RoundToInt(update.WorldPosition.z / VoxelConstants.VOXEL_SIZE)
            };
        }

        /// <summary>
        /// WorldPositionから直接変換
        /// </summary>
        public static NetworkVoxelDestruction FromWorldPosition(Vector3 worldPosition)
        {
            return new NetworkVoxelDestruction
            {
                x = (short)Mathf.RoundToInt(worldPosition.x / VoxelConstants.VOXEL_SIZE),
                y = (short)Mathf.RoundToInt(worldPosition.y / VoxelConstants.VOXEL_SIZE),
                z = (short)Mathf.RoundToInt(worldPosition.z / VoxelConstants.VOXEL_SIZE)
            };
        }

        /// <summary>
        /// VoxelUpdateに変換（VoxelID=0固定）
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
                VoxelID = 0  // 常に Empty
            };
        }
    }

    /// <summary>
    /// ネットワークパケットヘッダー
    /// </summary>
    public struct NetworkPacketHeader
    {
        public int SenderId;
        public int BatchId;
        public int BatchIndex;
        public int TotalBatches;
        public int Count;

        public const int SIZE = 20;  // 4 × 5 バイト
    }

    /// <summary>
    /// ネットワークデータシリアライザ
    /// </summary>
    public static class VoxelNetworkSerializer
    {
        /// <summary>
        /// ヘッダーをバイト配列に書き込み
        /// </summary>
        public static int WriteHeader(byte[] data, int offset, NetworkPacketHeader header)
        {
            System.BitConverter.GetBytes(header.SenderId).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(header.BatchId).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(header.BatchIndex).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(header.TotalBatches).CopyTo(data, offset);
            offset += 4;
            System.BitConverter.GetBytes(header.Count).CopyTo(data, offset);
            offset += 4;
            return offset;
        }

        /// <summary>
        /// ヘッダーをバイト配列から読み込み
        /// </summary>
        public static NetworkPacketHeader ReadHeader(System.ArraySegment<byte> data, ref int offset)
        {
            var header = new NetworkPacketHeader
            {
                SenderId = System.BitConverter.ToInt32(data.Array, data.Offset + offset),
                BatchId = System.BitConverter.ToInt32(data.Array, data.Offset + offset + 4),
                BatchIndex = System.BitConverter.ToInt32(data.Array, data.Offset + offset + 8),
                TotalBatches = System.BitConverter.ToInt32(data.Array, data.Offset + offset + 12),
                Count = System.BitConverter.ToInt32(data.Array, data.Offset + offset + 16)
            };
            offset += NetworkPacketHeader.SIZE;
            return header;
        }
    }
}
