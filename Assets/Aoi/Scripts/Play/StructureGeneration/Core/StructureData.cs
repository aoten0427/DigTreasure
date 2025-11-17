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
        ///生成されたボクセルデータ
        public List<VoxelUpdate> VoxelUpdates { get; set; }

        ///特殊ポイント（宝の位置など）
        public Dictionary<string, Vector3> SpecialPoints { get; set; }

        //実際に生成された接続点
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
        //接続点のID
        public string Id { get; set; }

        //ワールド座標での位置
        public Vector3 Position { get; set; }

        //接続方向
        public Vector3 Direction { get; set; }

        //接続可能な半径
        public float Radius { get; set; }

        ///既に使用済みか
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
