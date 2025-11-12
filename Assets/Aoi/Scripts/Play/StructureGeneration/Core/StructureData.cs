using System.Collections.Generic;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 構造物の生成結果
    /// </summary>
    public class StructureResult
    {
        /// <summary>生成されたボクセルデータ</summary>
        public List<VoxelUpdate> VoxelUpdates { get; set; }

        /// <summary>特殊ポイント（宝の位置など）</summary>
        public Dictionary<string, Vector3> SpecialPoints { get; set; }

        /// <summary>実際に生成された接続点</summary>
        public List<ConnectionPoint> ConnectionPoints { get; set; }

        public StructureResult()
        {
            VoxelUpdates = new List<VoxelUpdate>();
            SpecialPoints = new Dictionary<string, Vector3>();
            ConnectionPoints = new List<ConnectionPoint>();
        }
    }

    /// <summary>
    /// 接続点
    /// </summary>
    public class ConnectionPoint
    {
        /// <summary>接続点のID</summary>
        public string Id { get; set; }

        /// <summary>ワールド座標での位置</summary>
        public Vector3 Position { get; set; }

        /// <summary>接続方向（正規化されたベクトル）</summary>
        public Vector3 Direction { get; set; }

        /// <summary>接続可能な半径</summary>
        public float Radius { get; set; }

        /// <summary>既に使用済みか</summary>
        public bool IsUsed { get; set; }

        public ConnectionPoint(string id, Vector3 position, Vector3 direction, float radius)
        {
            Id = id;
            Position = position;
            Direction = direction.normalized;
            Radius = radius;
            IsUsed = false;
        }
    }
}
