using UnityEngine;
using UniRx;
using System.Threading.Tasks;

namespace StructureGeneration
{
    /// <summary>
    /// MapGeneratorの進捗管理機能をテストするスクリプト
    /// </summary>
    public class MapGeneratorProgressTest : MonoBehaviour
    {
        [Header("テスト設定")]
        [SerializeField]
        private MapGeneratorComponent mapGeneratorComponent;

        [SerializeField]
        private bool autoStart = false;

        [Header("進捗表示")]
        [SerializeField]
        [Range(0f, 1f)]
        private float currentProgress = 0f;

        private ReactiveProperty<float> progressProperty;
        private bool isGenerating = false;

        private void Start()
        {
            if (mapGeneratorComponent == null)
            {
                mapGeneratorComponent = FindObjectOfType<MapGeneratorComponent>();
            }

            if (autoStart)
            {
                StartTest();
            }
        }

        private void Update()
        {
            // Spaceキーでテスト開始
            if (Input.GetKeyDown(KeyCode.Space) && !isGenerating)
            {
                StartTest();
            }
        }

        /// <summary>
        /// テストを開始
        /// </summary>
        [ContextMenu("Start Progress Test")]
        public void StartTest()
        {
            if (isGenerating)
            {
                Debug.LogWarning("既にマップ生成中です");
                return;
            }

            if (mapGeneratorComponent == null)
            {
                Debug.LogError("MapGeneratorComponentが見つかりません");
                return;
            }

            _ = RunProgressTest();
        }

        /// <summary>
        /// 進捗テストを実行
        /// </summary>
        private async Task RunProgressTest()
        {
            isGenerating = true;
            currentProgress = 0f;

            Debug.Log("=== MapGenerator 進捗テスト開始 ===");

            // 進捗プロパティを作成
            progressProperty = new ReactiveProperty<float>(0f);

            // 進捗の変化を購読してログ出力とInspector表示
            progressProperty.Subscribe(progress =>
            {
                currentProgress = progress;
                Debug.Log($"[進捗] {(progress * 100f):F1}% 完了");
            }).AddTo(this);

            try
            {
                // MapGeneratorComponentのInitializeAsyncを呼び出し
                await mapGeneratorComponent.InitializeAsync(progressProperty);

                Debug.Log("=== MapGenerator 進捗テスト完了 ===");
                Debug.Log($"最終進捗: {(currentProgress * 100f):F1}%");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"テスト中にエラーが発生しました: {e.Message}\n{e.StackTrace}");
            }
            finally
            {
                isGenerating = false;
            }
        }

        private void OnGUI()
        {
            // 画面左上に進捗バーを表示
            if (isGenerating)
            {
                float barWidth = 400f;
                float barHeight = 30f;
                float margin = 20f;

                // 背景
                GUI.Box(new Rect(margin, margin, barWidth, barHeight), "");

                // 進捗バー
                GUI.color = Color.green;
                GUI.Box(new Rect(margin + 2, margin + 2, (barWidth - 4) * currentProgress, barHeight - 4), "");
                GUI.color = Color.white;

                // テキスト
                GUI.Label(new Rect(margin, margin, barWidth, barHeight),
                    $"マップ生成中: {(currentProgress * 100f):F1}%",
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 16,
                        fontStyle = FontStyle.Bold
                    });

                // 現在のフェーズを表示
                string phase = GetCurrentPhase(currentProgress);
                GUI.Label(new Rect(margin, margin + barHeight + 5, barWidth, 20),
                    $"フェーズ: {phase}",
                    new GUIStyle(GUI.skin.label)
                    {
                        alignment = TextAnchor.MiddleCenter,
                        fontSize = 12
                    });
            }
            else
            {
                // 開始ボタン
                if (GUI.Button(new Rect(20, 20, 200, 50), "Space or Click to Start"))
                {
                    StartTest();
                }
            }
        }

        /// <summary>
        /// 進捗値から現在のフェーズを取得
        /// </summary>
        private string GetCurrentPhase(float progress)
        {
            if (progress < 0.05f) return "初期化中...";
            if (progress < 0.10f) return "境界壁生成中...";
            if (progress < 0.15f) return "地下を埋めるボクセル生成中...";
            if (progress < 0.20f) return "地表ボクセル生成中...";
            if (progress < 0.25f) return "構造物配置位置決定中...";
            if (progress < 0.30f) return "構造物生成中...";
            if (progress < 0.35f) return "構造物ボクセルデータ生成中...";
            if (progress < 0.40f) return "接続生成中...";
            if (progress < 0.50f) return "接続ボクセルデータ生成中...";
            if (progress < 1.0f) return "ボクセル配置中 (SetVoxels)...";
            return "完了！";
        }
    }
}
