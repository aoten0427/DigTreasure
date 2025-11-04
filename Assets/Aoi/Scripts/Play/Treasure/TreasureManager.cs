using Fusion;
using NetWork;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class TreasureManager : NetworkBehaviour,IPlayInitialize
{
    [SerializeField] int m_treasuerNum = 40;
    [SerializeField] private NetworkTreasureSpawner m_treasureSpawner;

    public InitializationPriority Priority => InitializationPriority.TreasureCreate;

    public string Name => "TreasureManager";



    /// <summary>
    /// ‚¨•ó‚ğ¶¬
    /// </summary>
    void CreateTresure()
    {
        if (!Object.HasStateAuthority) return;
        if (m_treasureSpawner == null) return;
        for (int i = 0; i < m_treasuerNum; i++)
        {
            m_treasureSpawner.SpawnRandomTreasure();
        }
    }

    public Task InitializeAsync(ReactiveProperty<float> progressProperty = null)
    {
        Debug.Log("•ó¶¬");

        CreateTresure();
        if (progressProperty != null) progressProperty.Value = 1.0f;

        return Task.CompletedTask;
    }

    public void SetManager(PlayManager manager)
    {
        
    }
}
