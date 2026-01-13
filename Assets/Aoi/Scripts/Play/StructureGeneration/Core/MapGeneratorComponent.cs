using UnityEngine;
using VoxelWorld;
using UniRx;
using System.Threading.Tasks;
using Fusion;

namespace StructureGeneration
{
    /// <summary>
    /// MapGeneratorのUnity MonoBehaviourラッパー
    /// </summary>
    public class MapGeneratorComponent : NetworkBehaviour, IPlayInitialize
    {
        // IPlayInitializeの実装
        public InitializationPriority Priority => InitializationPriority.Map;
        public int LoadWeight => 99;
        public string Name => "MapGenerator";

        

        [Header("設定")]
        [SerializeField]
        private MapGeneratorSettings settings;


        private MapGenerator mapGenerator;
        private bool isGenerating = false;

        public MapGenerator Generator => mapGenerator;
        public bool IsGenerating => isGenerating;

        [Networked]
        int n_Seed {  get; set; }

        public void SetManager(PlayManager manager)
        {

        }

        public override void Spawned()
        {
            if(Object.HasStateAuthority)
            {
                n_Seed = System.Environment.TickCount;
                n_Seed = n_Seed % 20;
            }
        }

        /// <summary>
        /// IPlayInitializeの実装：マップ生成を進捗付きで実行
        /// </summary>
        public async Task InitializeAsync(ReactiveProperty<float> progressProperty = null)
        {
            // WorldManagerを取得
            var worldManager = FindFirstObjectByType<WorldManager>();
            if (worldManager == null)
            {
                Debug.LogError("WorldManagerが見つかりません");
                return;
            }

            if (settings == null)
            {
                Debug.LogError("MapGeneratorSettingsが設定されていません");
                return;
            }

            // MapGeneratorインスタンスを作成
            settings.masterSeed = n_Seed;
            mapGenerator = new MapGenerator(settings, worldManager.Voxels);

            // マップ生成を実行
            isGenerating = true;
           
            await mapGenerator.GenerateMapAsync(progressProperty);
           
            
            isGenerating = false;
            
        }
    }
}
