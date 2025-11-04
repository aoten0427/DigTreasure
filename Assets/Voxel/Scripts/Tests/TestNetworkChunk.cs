using UnityEngine;
using Fusion;
using VoxelWorld;
using System.Collections.Generic;

public class TestNetworkChunk : NetworkBehaviour
{
    [SerializeField] WorldManager m_worldManager;
    [SerializeField] private int m_fillVoxelID = 1;
    [SerializeField] VoxelWorld.VoxelNetWorkManager m_workManager;

    public override void Spawned()
    {
        m_worldManager = WorldManager.Instance;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetVoxel();
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            FillChunk();
        }
    }

    private void SetVoxel()
    {
        m_worldManager.Voxels.SetVoxel(new Vector3(0, 0, 0), m_fillVoxelID);
        //List<VoxelUpdate> voxelUpdates = new List<VoxelUpdate>();

        //voxelUpdates.Add(new VoxelUpdate(new Vector3(0,0,0),m_fillVoxelID));

        //m_workManager.SyncVoxelUpdates(voxelUpdates);
    }

    private void FillChunk()
    {
        // VoxelOperationManagerを使用してチャンクを埋める
        var chunkPositions = m_worldManager.Chunks.ChunkPositions;
        var fillVoxel = new Voxel(m_fillVoxelID);

        List<VoxelUpdate> voxelUpdates = new List<VoxelUpdate>();

        foreach (var chunk in chunkPositions)
        {

            var chunkoffset = (Vector3)chunk * (1.0f / VoxelConstants.VOXEL_SIZE);
            for (int i = 0; i < 16; i++)
            {
                for (int j = 0; j < 16; j++)
                {
                    for (int k = 0; k < 16; k++)
                    {
                        Vector3 pos = new Vector3(i * VoxelConstants.VOXEL_SIZE,
                            j * VoxelConstants.VOXEL_SIZE,
                            k * VoxelConstants.VOXEL_SIZE);
                        pos += chunkoffset;
                        voxelUpdates.Add(new VoxelUpdate(pos, fillVoxel));
                    }
                }
            }
        }


        m_workManager.SyncVoxelUpdates(voxelUpdates);
        //m_worldManager.Voxels.FillChunks(chunkPositions, fillVoxel);
    }
}
