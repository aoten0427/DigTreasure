using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration
{
    /// <summary>
    /// 接続管理クラス（構造物間の接続を生成・管理）
    /// </summary>
    public class ConnectionManager
    {
        private readonly Dictionary<ConnectionType, IConnectionGenerator> generators;
        private readonly List<ConnectionData> connections;
        private readonly ConnectionSettings settings;

        public IReadOnlyList<ConnectionData> Connections => connections;

        public ConnectionManager(ConnectionSettings settings = null)
        {
            generators = new Dictionary<ConnectionType, IConnectionGenerator>();
            connections = new List<ConnectionData>();
            this.settings = settings;

            // 設定を使ってジェネレーターを登録
            if (settings != null)
            {
                RegisterGenerator(new OpenTunnelGenerator(
                    tunnelRadius: settings.openTunnelRadius,
                    noiseScale: 0.2f,
                    airVoxelId: settings.openTunnelVoxelId,
                    pathMode: settings.pathMode,
                    curveHeight: 10f
                ));
                RegisterGenerator(new FilledTunnelGenerator());
            }
            else
            {
                // デフォルトのジェネレーターを登録
                RegisterGenerator(new OpenTunnelGenerator());
                RegisterGenerator(new FilledTunnelGenerator());
            }
        }

        /// <summary>
        /// カスタムジェネレーターを登録
        /// </summary>
        public void RegisterGenerator(IConnectionGenerator generator)
        {
            generators[generator.SupportedType] = generator;
        }

        /// <summary>
        /// 2つの構造物を接続
        /// </summary>
        public ConnectionData CreateConnection(
            string connectionId,
            IStructure source,
            IStructure target,
            ConnectionType type)
        {
            // 最も近い接続点のペアを見つける
            var (sourcePoint, targetPoint) = FindClosestConnectionPoints(source, target);

            if (sourcePoint == null || targetPoint == null)
            {
                Debug.LogWarning($"接続点が見つかりません: {source.Id} -> {target.Id}");
                return null;
            }

            // 接続点を使用済みにマーク
            sourcePoint.IsUsed = true;
            targetPoint.IsUsed = true;

            var connection = new ConnectionData(
                connectionId,
                source.Id,
                target.Id,
                sourcePoint,
                targetPoint,
                type
            );

            connections.Add(connection);
            return connection;
        }

        /// <summary>
        /// 最も近い接続点のペアを検索
        /// </summary>
        private (ConnectionPoint source, ConnectionPoint target) FindClosestConnectionPoints(
            IStructure source,
            IStructure target)
        {
            // 使用されていない接続点のみを対象
            var sourcePoints = source.GetConnectionPoints().Where(p => !p.IsUsed).ToList();
            var targetPoints = target.GetConnectionPoints().Where(p => !p.IsUsed).ToList();

            if (sourcePoints.Count == 0 || targetPoints.Count == 0)
            {
                return (null, null);
            }

            // 最短距離のペアを探索
            float minDistance = float.MaxValue;
            ConnectionPoint bestSource = null;
            ConnectionPoint bestTarget = null;

            foreach (var sp in sourcePoints)
            {
                foreach (var tp in targetPoints)
                {
                    float distance = Vector3.Distance(sp.Position, tp.Position);
                    if (distance < minDistance)
                    {
                        minDistance = distance;
                        bestSource = sp;
                        bestTarget = tp;
                    }
                }
            }

            return (bestSource, bestTarget);
        }

        /// <summary>
        /// すべての接続のボクセルデータを生成
        /// </summary>
        public async Task<List<VoxelUpdate>> GenerateAllConnectionsAsync(int baseSeed)
        {
            var allUpdates = new List<VoxelUpdate>();

            for (int i = 0; i < connections.Count; i++)
            {
                var connection = connections[i];

                if (!generators.TryGetValue(connection.Type, out var generator))
                {
                    Debug.LogError($"接続タイプ {connection.Type} のジェネレーターが見つかりません");
                    continue;
                }

                // 接続ごとに異なるシードを使用
                int connectionSeed = baseSeed ^ connection.Id.GetHashCode();
                var updates = await generator.GenerateAsync(connection, connectionSeed);
                allUpdates.AddRange(updates);

                // フレーム分散
                await Task.Yield();
            }

            return allUpdates;
        }

        /// <summary>
        /// 特定の接続のボクセルデータを生成
        /// </summary>
        public async Task<List<VoxelUpdate>> GenerateConnectionAsync(ConnectionData connection, int seed)
        {
            if (!generators.TryGetValue(connection.Type, out var generator))
            {
                Debug.LogError($"接続タイプ {connection.Type} のジェネレーターが見つかりません");
                return new List<VoxelUpdate>();
            }

            return await generator.GenerateAsync(connection, seed);
        }

        /// <summary>
        /// すべての接続をクリア
        /// </summary>
        public void ClearConnections()
        {
            connections.Clear();
        }

        /// <summary>
        /// 接続の統計情報を取得
        /// </summary>
        public string GetConnectionStats()
        {
            var typeGroups = connections.GroupBy(c => c.Type);
            var stats = new System.Text.StringBuilder();
            stats.AppendLine($"総接続数: {connections.Count}");

            foreach (var group in typeGroups)
            {
                stats.AppendLine($"  {group.Key}: {group.Count()}個");
            }

            return stats.ToString();
        }
    }
}
