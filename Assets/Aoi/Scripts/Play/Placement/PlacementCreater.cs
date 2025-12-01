using Fusion;
using StructureGeneration;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class PlacementCreater : NetworkBehaviour,IPlayInitialize
{
    //IPlayInitialize
    public InitializationPriority Priority => InitializationPriority.PlacementCreate;
    public string Name => "PlacementCreater";

    //マップ
    [SerializeField]private MapGeneratorComponent m_mapGenerator;

    //爆弾
    [SerializeField] private NetworkPrefabRef m_bomb;


    //
    public Task InitializeAsync(ReactiveProperty<float> task = null)
    {
        if (!Object.HasStateAuthority) return Task.CompletedTask;
        if (m_mapGenerator == null)
        {
            Debug.LogWarning("[PlacementCreater]MapGeneratorがありません");
            return Task.CompletedTask;
        }

        //オブジェクトの設置場所を取得
        var placementPoint = m_mapGenerator.Generator.PlacementObjects;
        
         foreach ( var placement in placementPoint )
        {
            if (placement.ObjectType == PlacementObjectType.Bomb)
            {
                Runner.Spawn(m_bomb, placement.Position);
            }
        }

        //Runner.Spawn(m_bomb,new Vector3(0,10,0));


        Debug.Log("爆弾オブジェクトを生成");

        return Task.CompletedTask;
    }

    public void SetManager(PlayManager manager)
    {
        
    }
}
