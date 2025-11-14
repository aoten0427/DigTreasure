using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;
using UniRx;

namespace StructureGeneration
{
    /// <summary>
    /// マップ全体の生成を管理するメインクラス
    /// </summary>
    public class MapGenerator
    {
        private readonly MapGeneratorSettings m_settings;
        private readonly VoxelOperationManager m_operationManager;
        private readonly ConnectionManager m_connectionManager;

        // 各種ジェネレーター
        private readonly BoundaryWallGenerator m_boundaryWallGenerator;
        private readonly SurfaceTerrainGenerator m_surfaceTerrainGenerator;
        private readonly VoxelFillGenerator m_voxelFillGenerator;

        private readonly List<IStructure> m_structures;
        private System.Random random;
        private int m_currentSeed = 12345;

        public IReadOnlyList<IStructure> Structures => m_structures;
        public IReadOnlyList<ConnectionData> Connections => m_connectionManager.Connections;

        private bool m_isLog;

        public MapGenerator(MapGeneratorSettings settings, VoxelOperationManager operationManager, bool isLog = false)
        {
            m_settings = settings;
            m_operationManager = operationManager;
            m_connectionManager = new ConnectionManager(settings.connectionSettings);
            m_structures = new List<IStructure>();

            // ジェネレーターの初期化
            m_boundaryWallGenerator = new BoundaryWallGenerator();
            m_surfaceTerrainGenerator = new SurfaceTerrainGenerator();
            m_voxelFillGenerator = new VoxelFillGenerator();
            m_isLog = isLog; 

        }

        /// <summary>
        /// マップ全体を生成
        /// </summary>
        /// <param name="progressProperty">進捗を報告するReactiveProperty（0.0～1.0）</param>
        public async Task<MapGenerationResult> GenerateMapAsync(ReactiveProperty<float> progressProperty = null)
        {

            // シード初期化
            //m_currentSeed = m_settings.masterSeed;
            //if (m_currentSeed == 0)
            //{
            //    m_currentSeed = System.Environment.TickCount;
            //}
            //random = new System.Random(m_currentSeed);
            //Debug.Log($"マップシード: {m_currentSeed}");

            // 既存のデータをクリア
            m_structures.Clear();
            m_connectionManager.ClearConnections();

            // 境界壁のボクセルリストを生成
            List<VoxelUpdate> boundaryVoxels = null;
            if (m_settings.generateBoundaryWalls)
            {
                boundaryVoxels = await m_boundaryWallGenerator.GenerateWallsAsync(
                    m_settings.minChunkCoord,
                    m_settings.maxChunkCoord,
                    m_settings.boundaryWallVoxelId
                );
            }
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_BOUNDARY_WALLS);

            // 地下を埋める
            List<VoxelUpdate> fillVoxels = null;
            if (m_settings.fillPlacementArea && m_settings.fillVoxelId > 0)
            {
                fillVoxels = await m_voxelFillGenerator.GenerateAsync(
                    m_settings.minChunkCoord,
                    m_settings.maxChunkCoord,
                    m_settings.fillVoxelId,
                    m_settings.surfaceBaseHeight
                );
            }
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_FILL_VOXELS);

            // 地表のボクセルリストを生成
            List<VoxelUpdate> surfaceVoxels = null;
            if (m_settings.generateSurfaceTerrain)
            {
                var surfaceSettings = new SurfaceSettings(
                    m_settings.surfaceBaseHeight,
                    m_settings.surfaceCenterHeight,
                    m_settings.surfaceEdgeHeight,
                    m_settings.surfaceFlatCenterRatio,
                    m_settings.surfaceBoundaryInset,
                    m_settings.surfaceVoxelId,
                    m_settings.surfaceNoiseAmplitude,
                    m_settings.surfaceNoiseFrequency
                );
                surfaceVoxels = await m_surfaceTerrainGenerator.GenerateAsync(
                    m_settings.minChunkCoord,
                    m_settings.maxChunkCoord,
                    surfaceSettings,
                    m_currentSeed
                );
            }
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_SURFACE_VOXELS);

            //構造物の配置位置を決定
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_STRUCTURE_POSITIONS);

            //構造物を生成
            await GenerateStructuresAsync();
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_STRUCTURES);

            // すべての構造物のボクセルデータを生成
            var structureVoxels = await GenerateAllStructureVoxelsAsync();
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_STRUCTURE_VOXELS);

            // 接続を生成
            GenerateConnections();
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_CONNECTIONS);

            // すべての接続のボクセルデータを生成
            var connectionVoxels = await m_connectionManager.GenerateAllConnectionsAsync(m_currentSeed);
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_CONNECTION_VOXELS);

            //ワールドに一度に配置
            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_VOXEL_PLACEMENT_START);
            await PlaceVoxelsInWorldAsync(fillVoxels, surfaceVoxels, structureVoxels, connectionVoxels, boundaryVoxels, progressProperty);

            //透明な境界壁（Collider）を生成
            if (m_settings.generateInvisibleBoundaryColliders)
            {
                m_boundaryWallGenerator.GenerateColliders(
                    m_settings.minChunkCoord,
                    m_settings.maxChunkCoord,
                    m_settings.ceilingHeight
                );
            }

            progressProperty?.SetValueAndForceNotify(MapGeneratorConstants.PROGRESS_COMPLETE);

            return new MapGenerationResult(
                m_structures.ToList(),
                m_connectionManager.Connections.ToList(),
                m_currentSeed
            );
        }


        /// <summary>
        /// 構造物を生成
        /// </summary>
        private async Task GenerateStructuresAsync()
        {
            // TreasureCave（お宝部屋）を放射状配置
            await GenerateRadialTreasureCavesAsync();

            // HardFloorCave（中央洞窟）を配置
            GenerateCentralCaveAsync();
        }

        /// <summary>
        /// TreasureCaveを中央洞窟の周りに放射状配置
        /// </summary>
        private async Task GenerateRadialTreasureCavesAsync()
        {
            if (m_settings.treasureCaveSettings == null || m_settings.hardFloorCaveSettings == null)
                return;

            // seedを使って生成数を決定
            Random.InitState(m_currentSeed ^ "TreasureCaveCount".GetHashCode());
            int actualRoomCount = Random.Range(m_settings.treasureCaveSettings.minCount, m_settings.treasureCaveSettings.maxCount + 1);

            if (actualRoomCount <= 0)
                return;

            Vector3 centerCavePos = m_settings.hardFloorCaveSettings.centerPosition;
            float angleStep = 360f / actualRoomCount;

            for (int i = 0; i < actualRoomCount; i++)
            {
                // シードを使った乱数初期化
                Random.InitState(m_currentSeed ^ i.GetHashCode());

                // 角度計算
                float angle = (angleStep * i + Random.Range(-m_settings.treasureCaveSettings.angleVariation,
                                                             m_settings.treasureCaveSettings.angleVariation)) * Mathf.Deg2Rad;

                // 距離計算
                float distance = m_settings.treasureCaveSettings.baseDistance +
                                Random.Range(-m_settings.treasureCaveSettings.distanceVariation,
                                            m_settings.treasureCaveSettings.distanceVariation);

                // Y座標のオフセット
                float yOffset = Random.Range(-m_settings.treasureCaveSettings.yVariation,
                                             m_settings.treasureCaveSettings.yVariation);

                // 放射状の位置を計算
                Vector3 position = new Vector3(
                    centerCavePos.x + distance * Mathf.Cos(angle),
                    centerCavePos.y + yOffset,
                    centerCavePos.z + distance * Mathf.Sin(angle)
                );

                // 構造物シード
                int structureSeed = m_currentSeed ^ i.GetHashCode();
                string id = $"TreasureCave_{i}";

                // 中央洞窟の位置をtargetPositionとして渡す（接続点の向き先）
                var treasureCave = new TreasureCave(id, structureSeed, m_settings.treasureCaveSettings, position, centerCavePos);
                m_structures.Add(treasureCave);

                if (i % MapGeneratorConstants.STRUCTURE_YIELD_INTERVAL == 0)
                {
                    await Task.Yield();
                }
            }

        }

        /// <summary>
        /// HardFloorCave（中央洞窟）を配置
        /// </summary>
        private void GenerateCentralCaveAsync()
        {
            m_structures.Add(new HardFloorCave("HardFloor",m_currentSeed, m_settings.hardFloorCaveSettings));
        }


        /// <summary>
        /// 接続を生成
        /// </summary>
        private void GenerateConnections()
        {
            // 各構造物から指定数の接続を生成
            for (int i = 0; i < m_structures.Count; i++)
            {
                var source = m_structures[i];

                // 接続点がない構造物はスキップ
                var sourcePoints = source.GetConnectionPoints();
                if (sourcePoints == null || sourcePoints.Count == 0)
                {
                    Debug.LogWarning($"構造物 {source.Id} には接続点がありません");
                    continue;
                }

                // 接続先の候補を取得（近い順にソート）
                var candidates = m_structures
                    .Where(s => s != source && s.GetConnectionPoints() != null && s.GetConnectionPoints().Count > 0)
                    .OrderBy(s => Vector3.Distance(
                        sourcePoints[0].Position,
                        s.GetConnectionPoints()[0].Position))
                    .ToList();

                int connectionsCreated = 0;
                foreach (var target in candidates)
                {
                    if (connectionsCreated >= m_settings.connectionsPerStructure)
                        break;

                    // CanConnectToチェック（お宝部屋同士は接続しないなど）
                    if (!source.CanConnectTo(target))
                        continue;

                    // 高度差チェック
                    float heightDiff = Mathf.Abs(
                        sourcePoints[0].Position.y -
                        target.GetConnectionPoints()[0].Position.y);

                    if (heightDiff > m_settings.maxConnectionHeightDiff)
                        continue;

                    // 既存の接続をチェック（双方向）
                    bool alreadyConnected = m_connectionManager.Connections.Any(c =>
                        (c.SourceStructureId == source.Id && c.TargetStructureId == target.Id) ||
                        (c.SourceStructureId == target.Id && c.TargetStructureId == source.Id));

                    if (alreadyConnected)
                        continue;

                    // 接続タイプを決定
                    ConnectionType type = DetermineConnectionType();

                    // 接続を作成
                    var connection = m_connectionManager.CreateConnection(
                        $"connection_{i}_{connectionsCreated}",
                        source,
                        target,
                        type
                    );

                    if (connection != null)
                    {
                        connectionsCreated++;
                        Debug.Log($"接続作成: {source.Id} -> {target.Id} ({type})");
                    }
                }
            }
        }

        /// <summary>
        /// 接続タイプを決定
        /// </summary>
        private ConnectionType DetermineConnectionType()
        {
            // 現在は OpenTunnel（空洞トンネル）のみ使用
            return ConnectionType.FilledTunnel;
        }

        /// <summary>
        /// すべての構造物のボクセルデータを生成
        /// </summary>
        private async Task<List<VoxelUpdate>> GenerateAllStructureVoxelsAsync()
        {
            var allVoxels = new List<VoxelUpdate>();


            for (int i = 0; i < m_structures.Count; i++)
            {
                var structure = m_structures[i];
                int structureSeed = m_currentSeed ^ structure.Id.GetHashCode();

                var result = await structure.GenerateAsync(structureSeed);
                allVoxels.AddRange(result.VoxelUpdates);


                // フレーム分散
                await Task.Yield();
            }

            return allVoxels;
        }

        /// <summary>
        /// すべてのボクセルをワールドに一度に配置
        /// </summary>
        /// <param name="progressProperty">親の進捗プロパティ（0.5～1.0にマッピングされる）</param>
        private async Task PlaceVoxelsInWorldAsync(List<VoxelUpdate> fillVoxels, List<VoxelUpdate> surfaceVoxels, List<VoxelUpdate> structureVoxels, List<VoxelUpdate> connectionVoxels, List<VoxelUpdate> boundaryVoxels, ReactiveProperty<float> progressProperty)
        {
            int fillCount = fillVoxels?.Count ?? 0;
            int surfaceCount = surfaceVoxels?.Count ?? 0;
            int structureCount = structureVoxels?.Count ?? 0;
            int connectionCount = connectionVoxels?.Count ?? 0;
            int boundaryCount = boundaryVoxels?.Count ?? 0;
            int totalCount = fillCount + surfaceCount + structureCount + connectionCount + boundaryCount;

            var allVoxels = new List<VoxelUpdate>(totalCount);

            if (fillVoxels != null)
                allVoxels.AddRange(fillVoxels); 
            if (surfaceVoxels != null)
                allVoxels.AddRange(surfaceVoxels);
            if (structureVoxels != null)
                allVoxels.AddRange(structureVoxels);
            if (connectionVoxels != null)
                allVoxels.AddRange(connectionVoxels);
            if (boundaryVoxels != null)
                allVoxels.AddRange(boundaryVoxels);


            // SetVoxels用の内部進捗プロパティを作成（0～1）
            var voxelProgress = new ReactiveProperty<float>(0f);

            //進捗更新
            voxelProgress.Subscribe(value =>
            {
                if (progressProperty != null)
                {
                    float range = MapGeneratorConstants.PROGRESS_VOXEL_PLACEMENT_END - MapGeneratorConstants.PROGRESS_VOXEL_PLACEMENT_START;
                    float mappedProgress = MapGeneratorConstants.PROGRESS_VOXEL_PLACEMENT_START + value * range;
                    progressProperty.SetValueAndForceNotify(mappedProgress);
                }
            });

            // SetVoxelsを実行し、完了を待機
            bool isComplete = false;
            m_operationManager.SetVoxels(allVoxels, false, voxelProgress, _ => isComplete = true);

            while (!isComplete)
            {
                await Task.Yield();
            }
        }

       
    }

    /// <summary>
    /// マップ生成結果
    /// </summary>
    public class MapGenerationResult
    {
        public List<IStructure> Structures { get; }
        public List<ConnectionData> Connections { get; }
        public int Seed { get; }

        public MapGenerationResult(List<IStructure> structures, List<ConnectionData> connections, int seed)
        {
            Structures = structures;
            Connections = connections;
            Seed = seed;
        }
    }
}
