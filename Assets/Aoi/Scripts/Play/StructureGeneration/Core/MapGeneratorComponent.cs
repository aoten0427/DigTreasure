using UnityEngine;
using VoxelWorld;
using UniRx;
using System.Threading.Tasks;

namespace StructureGeneration
{
    /// <summary>
    /// MapGeneratorのUnity MonoBehaviourラッパー
    /// </summary>
    public class MapGeneratorComponent : MonoBehaviour, IPlayInitialize
    {
        // IPlayInitializeの実装
        public InitializationPriority Priority => InitializationPriority.Map;
        public int LoadWeight => 99;
        public string Name => "MapGenerator";

        public void SetManager(PlayManager manager)
        {
            // 必要に応じて実装
        }

        /// <summary>
        /// IPlayInitializeの実装：マップ生成を進捗付きで実行
        /// </summary>
        public async Task InitializeAsync(ReactiveProperty<float> progressProperty = null)
        {
            // WorldManagerを取得
            var worldManager = FindObjectOfType<WorldManager>();
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
            mapGenerator = new MapGenerator(settings, worldManager.Voxels);

            // マップ生成を実行
            isGenerating = true;
            try
            {
                var result = await mapGenerator.GenerateMapAsync(progressProperty);
                Debug.Log(mapGenerator.GetGenerationStats());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"マップ生成中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isGenerating = false;
            }
        }

        [Header("設定")]
        [SerializeField]
        private MapGeneratorSettings settings;

        [Header("自動生成")]
        [SerializeField]
        [Tooltip("開始時に自動的にマップを生成")]
        private bool generateOnStart = false;

        [SerializeField]
        [Tooltip("生成前の遅延時間（秒）")]
        private float startDelay = 3f;

        private MapGenerator mapGenerator;
        private bool isGenerating = false;

        public MapGenerator Generator => mapGenerator;
        public bool IsGenerating => isGenerating;

        private async void Start()
        {
            // WorldManagerを取得
            var worldManager = FindObjectOfType<WorldManager>();
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
            mapGenerator = new MapGenerator(settings, worldManager.Voxels);

            if (generateOnStart)
            {
                Debug.Log($"マップ生成を{startDelay}秒後に開始します...");
                await System.Threading.Tasks.Task.Delay((int)(startDelay * 1000));
                await GenerateMapAsync();
            }
        }

        /// <summary>
        /// マップを生成（外部から呼び出し可能）
        /// </summary>
        public async System.Threading.Tasks.Task<MapGenerationResult> GenerateMapAsync()
        {
            if (isGenerating)
            {
                Debug.LogWarning("既にマップ生成中です");
                return null;
            }

            if (mapGenerator == null)
            {
                Debug.LogError("MapGeneratorが初期化されていません");
                return null;
            }

            isGenerating = true;

            try
            {
                var result = await mapGenerator.GenerateMapAsync();
                Debug.Log(mapGenerator.GetGenerationStats());
                return result;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"マップ生成中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
                return null;
            }
            finally
            {
                isGenerating = false;
            }
        }

        /// <summary>
        /// インスペクターからのボタン呼び出し用
        /// </summary>
        [ContextMenu("Generate Map Now")]
        private async void GenerateMapNow()
        {
            await GenerateMapAsync();
        }

        /// <summary>
        /// デバッグ用統計情報を表示
        /// </summary>
        [ContextMenu("Show Generation Stats")]
        private void ShowGenerationStats()
        {
            if (mapGenerator != null)
            {
                Debug.Log(mapGenerator.GetGenerationStats());
            }
            else
            {
                Debug.LogWarning("MapGeneratorがまだ初期化されていません");
            }
        }
    }
}
