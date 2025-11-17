using Fusion;
using NetWork;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class TreasureManager : NetworkBehaviour,IPlayInitialize
{
    [SerializeField] int m_treasuerNum = 40;
    [SerializeField] private NetworkTreasureSpawner m_treasureSpawner;
    [SerializeField] private StructureGeneration.MapGeneratorComponent m_mapGeneration;

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


        CreateStructrueTreasure();
    }

    public Task InitializeAsync(ReactiveProperty<float> progressProperty = null)
    {
        Debug.Log("•ó¶¬");

        CreateTresure();
        if (progressProperty != null) progressProperty.Value = 1.0f;

        return Task.CompletedTask;
    }

    private void CreateStructrueTreasure()
    {
        if(m_mapGeneration == null) return;
        var structrues = m_mapGeneration.Generator.Structures;

        foreach (var structure in structrues)
        {
            if(structure.Type != StructureGeneration.StructureType.TreasureCave) continue;
            Vector3 center = structure.CenterPosition;
            int num = Random.Range(10, 15);
            for(int i = 0;i < num;i++)
            {
                Vector3 rpos = new Vector3(Random.Range(-7, 7), Random.Range(1, 5), Random.Range(-7, 7));
                m_treasureSpawner.SpawnTreasure(rpos + center);
            }
        }

    }

    public void SetManager(PlayManager manager)
    {
        
    }
}
