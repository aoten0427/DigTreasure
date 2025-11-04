using Fusion;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using VoxelWorld;
using MapGeneration;

public class FieldManager : NetworkBehaviour,IPlayInitialize
{
    [SerializeField] Vector3Int m_fieldRangeMin;
    [SerializeField] Vector3Int m_fieldRangeMax;
    [SerializeField] WorldManager m_worldManager;

    [Header("Cave Generation")]
    [SerializeField] private CaveGenerationSettings m_caveSettings;

    public InitializationPriority Priority => InitializationPriority.Map;
    public int LoadWeight => 99;

    //マップ生成シード
    int m_seed { get; set; } = 12345;

    public string Name => "FieldManager";

    // 洞窟システムデータ
    private CaveSystem m_caveSystem;

    // レイヤー生成システム
    private VoxelLayerGenerator m_layerGenerator;

    // 範囲外エリア生成システム
    private BoundaryGenerator m_boundaryGenerator;

    // 地形生成システム
    private TerrainGenerator m_terrainGenerator;

    public override void Spawned()
    {
        //if(Object.HasStateAuthority)
        //{
        //    m_seed = UnityEngine.Random.Range(0, int.MaxValue);
        //}
        
    }

    public async Task InitializeAsync(ReactiveProperty<float> progressProperty = null)
    {
        // 各生成システムを初期化
        m_layerGenerator = new VoxelLayerGenerator(m_caveSettings, m_seed);
        m_boundaryGenerator = new BoundaryGenerator(m_caveSettings, m_seed, m_layerGenerator);
        m_terrainGenerator = new TerrainGenerator(m_caveSettings, m_layerGenerator, m_seed);

        // 洞窟データを生成
        GenerateCaveData();

        // 洞窟を適用した地形を生成
        await CreateFieldWithCaves(progressProperty);
    }


    /// <summary>
    /// 洞窟データを生成
    /// </summary>
    private void GenerateCaveData()
    {
        if (m_caveSettings == null)
        {
            Debug.LogWarning("[FieldManager] CaveGenerationSettingsが設定されていません");
            return;
        }

        var caveGenerator = new CaveGenerator(m_caveSettings);
        m_caveSystem = caveGenerator.Generate(m_seed, m_fieldRangeMin, m_fieldRangeMax);
    }



    /// <summary>
    /// ボクセルデータを準備して洞窟を適用
    /// </summary>
    private async Task CreateFieldWithCaves(ReactiveProperty<float> progressProperty)
    {
        // チャンクリストを取得
        var chunkPositions = m_terrainGenerator.GetChunkPositions(m_fieldRangeMin, m_fieldRangeMax);

        // 基本地形を生成
        var voxelDataByChunk = m_terrainGenerator.PrepareVoxelDataByChunk(chunkPositions);

        // 洞窟を適用
        if (m_caveSystem != null)
        {
            m_terrainGenerator.ApplyCavesToVoxelData(voxelDataByChunk, m_caveSystem);
        }

        // 範囲外エリアを生成してマージ
        var boundaryVoxelData = m_boundaryGenerator.GenerateBoundary(m_fieldRangeMin, m_fieldRangeMax);
        foreach (var kvp in boundaryVoxelData)
        {
            voxelDataByChunk[kvp.Key] = kvp.Value;
        }

        // WorldManagerのBoundaryMeshSettingsを設定
        ConfigureBoundaryMeshSettings();

        // チャンク単位のデータを1つのリストに統合
        int totalVoxels = voxelDataByChunk.Sum(kvp => kvp.Value.Count);
        var voxelUpdates = new List<VoxelUpdate>(totalVoxels);
        foreach (var chunkVoxels in voxelDataByChunk.Values)
        {
            voxelUpdates.AddRange(chunkVoxels);
        }

        // SetVoxelsで一括適用
        bool isComplete = false;
        m_worldManager.Voxels.SetVoxels(voxelUpdates, false, progressProperty, _ => isComplete = true);

        while (!isComplete)
        {
            await Task.Yield();
        }

        Debug.Log("生成完了");
    }


    /// <summary>
    /// WorldManagerのBoundaryMeshSettingsを設定
    /// </summary>
    private void ConfigureBoundaryMeshSettings()
    {
        if (m_worldManager == null) return;

        var boundarySettings = m_worldManager.BoundarySettings;
        if (boundarySettings == null) return;

        // フィールド範囲に基づいて境界座標を設定
        boundarySettings.forwardBoundaryZ = m_fieldRangeMax.z;
        boundarySettings.backBoundaryZ = m_fieldRangeMin.z;
        boundarySettings.upBoundaryY = m_fieldRangeMax.y;
        boundarySettings.downBoundaryY = m_fieldRangeMin.y;
        boundarySettings.rightBoundaryX = m_fieldRangeMax.x;
        boundarySettings.leftBoundaryX = m_fieldRangeMin.x;

        Debug.Log($"[FieldManager] Boundary設定完了: X({m_fieldRangeMin.x}~{m_fieldRangeMax.x}), Y({m_fieldRangeMin.y}~{m_fieldRangeMax.y}), Z({m_fieldRangeMin.z}~{m_fieldRangeMax.z})");
    }

    public void SetManager(PlayManager manager)
    {
        
    }
}
