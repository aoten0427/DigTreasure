using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace StructureGeneration
{
    /// <summary>
    /// 設置オブジェクト（爆弾、宝箱など）の配置位置を管理
    /// </summary>
    public class PlacementObjectManager
    {
        private const float BOMB_DISTANCE_FROM_CONNECTION = 5f;

        private readonly List<PlacementObjectData> placementObjects = new List<PlacementObjectData>();

        public IReadOnlyList<PlacementObjectData> PlacementObjects => placementObjects;

        /// <summary>
        /// すべての設置オブジェクトを生成
        /// </summary>
        /// <param name="structures">全構造物リスト</param>
        /// <param name="connections">全接続リスト</param>
        public void GenerateAllPlacements(
            IReadOnlyList<IStructure> structures,
            IReadOnlyList<ConnectionData> connections)
        {
            Clear();
            GenerateBombPlacements(structures, connections);
            GenerateTreasurePlacements(structures);
        }

        /// <summary>
        /// HardFloorCaveのFilledTunnel接続入り口に爆弾を配置
        /// </summary>
        /// <param name="structures">全構造物リスト</param>
        /// <param name="connections">全接続リスト</param>
        private void GenerateBombPlacements(
            IReadOnlyList<IStructure> structures,
            IReadOnlyList<ConnectionData> connections)
        {
            // HardFloorCaveを取得
            var hardFloorCave = structures.FirstOrDefault(s => s.Type == StructureType.HardFloorCave);
            if (hardFloorCave == null)
            {
                Debug.LogWarning("[PlacementObjectManager] HardFloorCaveが見つかりません");
                return;
            }

            // HardFloorCaveから出るFilledTunnel接続を取得
            var filledTunnelConnections = connections
                .Where(c => c.Type == ConnectionType.FilledTunnel)
                .ToList();

            foreach (var connection in filledTunnelConnections)
            {
                //大洞窟側の場所を取得
                Vector3 position = connection.SourceStructureId == hardFloorCave.Id ? connection.SourcePoint.Position : connection.TargetPoint.Position;
                Vector3 direction = connection.SourceStructureId == hardFloorCave.Id ? -connection.SourcePoint.Direction : -connection.TargetPoint.Direction;
                Vector3 offset = new Vector3(0, 5, 0);

                // 接続点から通路方向に少し離れた位置に爆弾を配置
                Vector3 bombPosition = position + direction * BOMB_DISTANCE_FROM_CONNECTION + offset;

                var bombData = new BombPlacementData(
                    bombPosition,
                    connection.SourcePoint.Direction,
                    hardFloorCave.Id,
                    connection.Id
                );

                placementObjects.Add(bombData);

                Debug.Log($"[PlacementObjectManager] 爆弾配置: {bombPosition}, 接続ID: {connection.Id}");
            }
        }

        /// <summary>
        /// SecretRoomの中心に宝箱を配置
        /// </summary>
        /// <param name="structures">全構造物リスト</param>
        private void GenerateTreasurePlacements(IReadOnlyList<IStructure> structures)
        {
            // SecretRoomを取得
            var secretRooms = structures.Where(s => s.Type == StructureType.SecretRoom).ToList();

            foreach (var secretRoom in secretRooms)
            {
                // 部屋の中心に宝箱を配置
                Vector3 treasurePosition = secretRoom.CenterPosition;

                var treasureData = new TreasurePlacementData(
                    treasurePosition,
                    Vector3.forward,
                    secretRoom.Id
                );

                placementObjects.Add(treasureData);

                Debug.Log($"[PlacementObjectManager] 宝箱配置: {treasurePosition}, 構造物ID: {secretRoom.Id}");
            }
        }

        /// <summary>
        /// すべての配置オブジェクトをクリア
        /// </summary>
        public void Clear()
        {
            placementObjects.Clear();
        }

        /// <summary>
        /// 配置オブジェクトの統計情報を取得
        /// </summary>
        public string GetStats()
        {
            var stats = new System.Text.StringBuilder();
            stats.AppendLine("=== 設置オブジェクト統計 ===");

            var groups = placementObjects.GroupBy(p => p.ObjectType);
            foreach (var group in groups)
            {
                stats.AppendLine($"{group.Key}: {group.Count()}個");
            }

            return stats.ToString();
        }
    }
}
