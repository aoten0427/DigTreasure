using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration
{
    /// <summary>
    /// 洞窟を構成する楕円体
    /// </summary>
    public struct CaveSphere
    {
        public Vector3 Center;
        public Vector3 Scale;  // X, Y, Z方向の半径

        public CaveSphere(Vector3 center, Vector3 scale)
        {
            Center = center;
            Scale = scale;
        }
    }

    /// <summary>
    /// 個別の洞窟データ
    /// </summary>
    public class CaveData
    {
        public Vector3 StartPosition { get; set; }
        public List<CaveSphere> Spheres { get; set; }
        public List<Vector3> FloorPositions { get; set; }

        public CaveData(Vector3 startPosition)
        {
            StartPosition = startPosition;
            Spheres = new List<CaveSphere>();
            FloorPositions = new List<Vector3>();
        }

        /// <summary>
        /// 深度ファクターを取得
        /// </summary>
        public float GetDepthFactor(float maxDepth)
        {
            return Mathf.Clamp01(Mathf.Abs(StartPosition.y) / Mathf.Abs(maxDepth));
        }
    }

    /// <summary>
    /// 洞窟システム全体のデータ
    /// </summary>
    public class CaveSystem
    {
        public int Seed { get; set; }
        public List<CaveData> Caves { get; set; }

        public CaveSystem(int seed)
        {
            Seed = seed;
            Caves = new List<CaveData>();
        }
    }
}
