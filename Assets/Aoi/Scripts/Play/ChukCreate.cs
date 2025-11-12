using System.Threading.Tasks;
using UniRx;
using UnityEngine;
using VoxelWorld;

public class ChukCreate : MonoBehaviour, IPlayInitialize
{
    //
    [SerializeField] WorldManager m_worldManager;
    //ƒ`ƒƒƒ“ƒN¶¬”ÍˆÍ
    [SerializeField] private Vector3Int m_worldSizeInChunksMin = new Vector3Int(0, 0, 0);
    [SerializeField] private Vector3Int m_worldSizeInChunksMax = new Vector3Int(4, 4, 4);

    public InitializationPriority Priority => InitializationPriority.Map;

    public int LoadWeight => 30;

    public int PriorityOffset => -10;

    public string Name => "ChankCreate";

    public async Task InitializeAsync(ReactiveProperty<float> task = null)
    {
        if(m_worldManager == null)m_worldManager = WorldManager.Instance;

        bool isComplete = false;

        m_worldManager.CreateChunks(m_worldSizeInChunksMin, m_worldSizeInChunksMax,100,task,() => isComplete = true);

        while (!isComplete)
        {
            await Task.Yield();
        }
    }

    public void SetManager(PlayManager manager)
    {
        
    }
}
