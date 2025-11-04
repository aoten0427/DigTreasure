using UnityEngine;
using VoxelWorld;
using UniRx;
using System;

public class TestChunk : MonoBehaviour
{
    [SerializeField] WorldManager m_worldManager;
    [SerializeField] private int m_fillVoxelID = 1;

    private ReactiveProperty<float> m_progress;
    private float m_startTime;

    private void Start()
    {
        // WorldManagerを取得
        if (m_worldManager == null)
        {
            m_worldManager = WorldManager.Instance;
        }

    }

    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.M))
        {
            FillChunk();

        }
    }

    private void FillChunk()
    {
        // VoxelOperationManagerを使用してチャンクを埋める
        var chunkPositions = m_worldManager.Chunks.ChunkPositions;
        var fillVoxel = new Voxel(m_fillVoxelID);

       

        // チャンクを埋める（進捗とコールバック付き）
        m_worldManager.Voxels.FillChunks(
            chunkPositions,
            fillVoxel
        );

    }
}
