using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊ターゲット検索専用クラス
    /// </summary>
    public static class DestructionTargetFinder
    {
        /// <summary>
        /// 破壊形状に基づいて検索半径を計算
        /// </summary>
        /// <param name="shape">破壊形状</param>
        /// <param name="center">破壊中心位置</param>
        /// <returns>適切な検索半径</returns>
        public static float CalculateSearchRadius(IDestructionShape shape, Vector3 center)
        {
            if (shape == null)
            {
                Debug.LogWarning("[DestructionTargetFinder] 破壊形状がnullです");
                return 10.0f; // デフォルト値
            }

            // 破壊形状から実際の座標を取得
            var positions = shape.GetDestructionPositions()?.ToList();
            if (positions == null || positions.Count == 0)
            {
                Debug.LogWarning("[DestructionTargetFinder] 破壊座標が取得できません");
                return 10.0f; // デフォルト値
            }

            // 中心点から最も遠い座標までの距離を算出
            float maxDistance = 0f;
            foreach (var pos in positions)
            {
                float distance = Vector3.Distance(center, pos);
                if (distance > maxDistance)
                {
                    maxDistance = distance;
                }
            }

            // 少し余裕を持たせる（分離オブジェクトの境界を考慮）
            return maxDistance + 2.0f;
        }


        /// <summary>
        /// 範囲内の分離オブジェクトを検索
        /// </summary>
        /// <param name="center">検索中心位置</param>
        /// <param name="radius">検索半径</param>
        /// <param name="separationManager">分離オブジェクト管理クラス</param>
        /// <returns>範囲内の分離オブジェクトリスト</returns>
        public static List<SeparatedVoxelObject> FindSeparatedObjects(
            Vector3 center,
            float radius,
            SeparationManager separationManager)
        {
            if (separationManager == null)
            {
                Debug.LogWarning("[DestructionTargetFinder] SeparationManagerがnullです");
                return new List<SeparatedVoxelObject>();
            }

            // SeparationManager経由で検索
            return separationManager.FindObjectsInRange(center, radius);
        }

        /// <summary>
        /// WorldManager経由で範囲内の分離オブジェクトを検索（便利メソッド）
        /// </summary>
        /// <param name="center">検索中心位置</param>
        /// <param name="radius">検索半径</param>
        /// <param name="worldManager">ワールド管理クラス</param>
        /// <returns>範囲内の分離オブジェクトリスト</returns>
        public static List<SeparatedVoxelObject> FindSeparatedObjects(
            Vector3 center,
            float radius,
            WorldManager worldManager)
        {
            if (worldManager == null)
            {
                Debug.LogWarning("[DestructionTargetFinder] WorldManagerがnullです");
                return new List<SeparatedVoxelObject>();
            }

            return worldManager.GetSeparatedObjectsInRange(center, radius);
        }

        /// <summary>
        /// 破壊形状から自動的に検索半径を計算して分離オブジェクトを検索（統合メソッド）
        /// </summary>
        /// <param name="shape">破壊形状</param>
        /// <param name="center">検索・破壊中心位置</param>
        /// <param name="separationManager">分離オブジェクト管理クラス</param>
        /// <returns>範囲内の分離オブジェクトリスト</returns>
        public static List<SeparatedVoxelObject> FindSeparatedObjectsForShape(
            IDestructionShape shape,
            Vector3 center,
            SeparationManager separationManager)
        {
            // 検索半径を自動計算
            float radius = CalculateSearchRadius(shape, center);

            // 分離オブジェクトを検索
            return FindSeparatedObjects(center, radius, separationManager);
        }


        /// <summary>
        /// チャンクターゲットが有効かどうかをチェック
        /// </summary>
        /// <param name="worldManager">ワールド管理クラス</param>
        /// <returns>チャンクターゲットが有効な場合true</returns>
        public static bool HasValidChunkTarget(WorldManager worldManager)
        {
            return worldManager != null && worldManager.Chunks != null;
        }

        /// <summary>
        /// 分離オブジェクトターゲットが有効かどうかをチェック
        /// </summary>
        /// <param name="worldManager">ワールド管理クラス</param>
        /// <returns>分離オブジェクトターゲットが有効な場合true</returns>
        public static bool HasValidSeparatedObjectTarget(WorldManager worldManager)
        {
            return worldManager != null && worldManager.SeparationManager != null;
        }


    }
}