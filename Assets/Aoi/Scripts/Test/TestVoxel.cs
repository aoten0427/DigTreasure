using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using VoxelWorld;

public class TestVoxel : MonoBehaviour
{
    [SerializeField] VoxelWorld.WorldManager m_manager;
    [SerializeField] PlayerManager m_player;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

        StartCoroutine(Delay());
    }

    IEnumerator Delay()
    {
        yield return new WaitForSeconds(1);

 
        // VoxelOperationManagerを使用してチャンクを埋める
        var chunkPositions = m_manager.Chunks.ChunkPositions;
        var fillVoxel = new Voxel(1);


        m_manager.Voxels.FillChunks(
            chunkPositions,
            fillVoxel,
            onComplete:_ => {
                m_player.gameObject.SetActive(true);
                m_player.StartOffline();
            }
        ) ;
        
    }

    
}
