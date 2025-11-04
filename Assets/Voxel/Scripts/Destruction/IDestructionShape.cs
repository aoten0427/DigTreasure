using UnityEngine;
using System.Collections.Generic;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊形状を定義するインターフェース
    /// </summary>
    public interface IDestructionShape
    {
        Vector3 GetDestractionPoint();


        /// <summary>
        /// 破壊対象となるワールド座標のリストを取得
        /// </summary>
        /// <returns>破壊対象のワールド座標リスト</returns>
        IEnumerable<Vector3> GetDestructionPositions();
    }

    /// <summary>
    /// 座標リスト指定による破壊形状
    /// </summary>
    public class PointListDestruction : IDestructionShape
    {
        private List<Vector3> m_positions;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="positions">破壊対象の座標リスト</param>
        public PointListDestruction(IEnumerable<Vector3> positions)
        {
            m_positions = new List<Vector3>(positions);
        }

        /// <summary>
        /// 単一座標用コンストラクタ
        /// </summary>
        /// <param name="position">破壊対象の座標</param>
        public PointListDestruction(Vector3 position)
        {
            m_positions = new List<Vector3> { position };
        }

        public Vector3 GetDestractionPoint()
        {
            if(m_positions.Count == 0) return Vector3.zero;
            return m_positions[0];
        }

        public IEnumerable<Vector3> GetDestructionPositions()
        {
            return m_positions;
        }
    }

    /// <summary>
    /// 球体範囲破壊形状
    /// </summary>
    public class SphereDestruction : IDestructionShape
    {
        private Vector3 m_center;
        private float m_radius;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="center">球体の中心座標</param>
        /// <param name="radius">球体の半径</param>
        public SphereDestruction(Vector3 center, float radius)
        {
            m_center = center;
            m_radius = radius;
        }

        public Vector3 GetDestractionPoint()
        {
            return m_center;
        }

        public IEnumerable<Vector3> GetDestructionPositions()
        {
            // キャッシュシステムを使用
            return DestructionShapeCache.GetCachedSpherePositions(m_center, m_radius);
        }
    }

    /// <summary>
    /// 矩形範囲破壊形状
    /// </summary>
    public class BoxDestruction : IDestructionShape
    {
        private Vector3 m_center;
        private Vector3 m_size;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="center">矩形の中心座標</param>
        /// <param name="size">矩形のサイズ (X, Y, Z)</param>
        public BoxDestruction(Vector3 center, Vector3 size)
        {
            m_center = center;
            m_size = size;
        }

        /// <summary>
        /// 最小・最大座標から矩形破壊形状を作成
        /// </summary>
        /// <param name="min">最小座標</param>
        /// <param name="max">最大座標</param>
        /// <returns>BoxDestruction</returns>
        public static BoxDestruction FromMinMax(Vector3 min, Vector3 max)
        {
            Vector3 center = (min + max) * 0.5f;
            Vector3 size = max - min;
            return new BoxDestruction(center, size);
        }

        public Vector3 GetDestractionPoint()
        {
            return m_center;
        }

        public IEnumerable<Vector3> GetDestructionPositions()
        {
            // キャッシュシステムを使用
            return DestructionShapeCache.GetCachedBoxPositions(m_center, m_size);
        }
    }
}