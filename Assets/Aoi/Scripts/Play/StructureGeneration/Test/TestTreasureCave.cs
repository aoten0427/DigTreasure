using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration.Test
{
    /// <summary>
    /// お宝洞窟のテスト
    /// </summary>
    public class TestTreasureCave : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private TreasureCaveSettings settings;

        [Header("WorldManager")]
        [SerializeField] private WorldManager worldManager;

        [Header("テストパラメータ")]
        [SerializeField] private int testSeed = 12345;
        [SerializeField] private Vector3 fieldCenter = new Vector3(0, -20, 0);
        [SerializeField] private Vector3 fieldSize = new Vector3(100, 50, 100);

        private async void Start()
        {
            if (settings == null)
            {
                Debug.LogError("[TestTreasureCave] TreasureCaveSettingsが設定されていません");
                return;
            }

            if (worldManager == null)
            {
                worldManager = FindObjectOfType<WorldManager>();
                if (worldManager == null)
                {
                    Debug.LogError("[TestTreasureCave] WorldManagerが見つかりません");
                    return;
                }
            }

            await Task.Delay(4000); // ワールド初期化待機

            Debug.Log("[TestTreasureCave] お宝洞窟の生成を開始します...");

            // フィールド範囲を作成
            Bounds fieldBounds = new Bounds(fieldCenter, fieldSize);

            // お宝洞窟を生成
            var structure = settings.CreateStructure("test_treasure", testSeed);
            var result = await structure.GenerateAsync(testSeed, fieldBounds);

            Debug.Log($"[TestTreasureCave] 生成完了: {result.VoxelUpdates.Count}ボクセル");
            Debug.Log($"[TestTreasureCave] バウンディングボックス: {structure.GetBounds()}");
            Debug.Log($"[TestTreasureCave] 接続点数: {result.ConnectionPoints.Count}");

            if (result.SpecialPoints.ContainsKey("treasure"))
            {
                Debug.Log($"[TestTreasureCave] 宝の位置: {result.SpecialPoints["treasure"]}");
            }

            // WorldManagerに適用
            Debug.Log("[TestTreasureCave] WorldManagerに適用中...");
            await ApplyToWorldManager(result.VoxelUpdates);
            Debug.Log("[TestTreasureCave] WorldManagerへの適用完了");

            // 接続点を可視化（デバッグ用）
            VisualizeConnectionPoints(result.ConnectionPoints);
        }

        private async Task ApplyToWorldManager(List<VoxelUpdate> voxelUpdates)
        {
            var operationManager = worldManager.Voxels;
            if (operationManager == null)
            {
                Debug.LogError("[TestTreasureCave] VoxelOperationManagerが見つかりません");
                return;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            operationManager.SetVoxels(
                voxelUpdates,
                false,
                null,
                (successCount) =>
                {
                    Debug.Log($"[TestTreasureCave] メッシュ更新完了: {successCount}ボクセル");
                    tcs.SetResult(true);
                }
            );

            await tcs.Task;
        }

        private void VisualizeConnectionPoints(List<ConnectionPoint> connectionPoints)
        {
            foreach (var cp in connectionPoints)
            {
                // 接続点の位置にギズモ用のオブジェクトを配置（エディタで確認用）
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = $"ConnectionPoint_{cp.Id}";
                debugSphere.transform.position = cp.Position;
                debugSphere.transform.localScale = Vector3.one * cp.Radius * 2f;

                var renderer = debugSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.yellow;
                }

                // コライダーは不要
                var collider = debugSphere.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Debug.Log($"[TestTreasureCave] 接続点可視化: {cp.Id} at {cp.Position}");
            }
        }

        private void OnDrawGizmos()
        {
            // フィールド範囲を描画
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }
    }
}
