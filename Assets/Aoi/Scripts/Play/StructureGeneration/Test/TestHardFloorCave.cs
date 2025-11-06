using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration.Test
{
    /// <summary>
    /// 硬い床洞窟のテスト
    /// </summary>
    public class TestHardFloorCave : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private HardFloorCaveSettings settings;

        [Header("WorldManager")]
        [SerializeField] private WorldManager worldManager;

        [Header("テストパラメータ")]
        [SerializeField] private int testSeed = 54321;
        [SerializeField] private Vector3 fieldCenter = new Vector3(0, -20, 0);
        [SerializeField] private Vector3 fieldSize = new Vector3(100, 50, 100);

        private async void Start()
        {
            if (settings == null)
            {
                Debug.LogError("[TestHardFloorCave] HardFloorCaveSettingsが設定されていません");
                return;
            }

            if (worldManager == null)
            {
                worldManager = FindObjectOfType<WorldManager>();
                if (worldManager == null)
                {
                    Debug.LogError("[TestHardFloorCave] WorldManagerが見つかりません");
                    return;
                }
            }

            await Task.Delay(4000); // ワールド初期化待機

            Debug.Log("[TestHardFloorCave] 硬い床洞窟の生成を開始します...");

            // フィールド範囲を作成
            Bounds fieldBounds = new Bounds(fieldCenter, fieldSize);

            // 硬い床洞窟を生成
            var structure = settings.CreateStructure("test_hardfloor", testSeed);
            var result = await structure.GenerateAsync(testSeed, fieldBounds);

            Debug.Log($"[TestHardFloorCave] 生成完了: {result.VoxelUpdates.Count}ボクセル");
            Debug.Log($"[TestHardFloorCave] バウンディングボックス: {structure.GetBounds()}");
            Debug.Log($"[TestHardFloorCave] 接続点数: {result.ConnectionPoints.Count}");
            Debug.Log($"[TestHardFloorCave] 中心位置: {structure.CenterPosition}");

            // WorldManagerに適用
            Debug.Log("[TestHardFloorCave] WorldManagerに適用中...");
            await ApplyToWorldManager(result.VoxelUpdates);
            Debug.Log("[TestHardFloorCave] WorldManagerへの適用完了");

            // 接続点を可視化
            VisualizeConnectionPoints(result.ConnectionPoints);
        }

        private async Task ApplyToWorldManager(List<VoxelUpdate> voxelUpdates)
        {
            var operationManager = worldManager.Voxels;
            if (operationManager == null)
            {
                Debug.LogError("[TestHardFloorCave] VoxelOperationManagerが見つかりません");
                return;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            operationManager.SetVoxels(
                voxelUpdates,
                false,
                null,
                (successCount) =>
                {
                    Debug.Log($"[TestHardFloorCave] メッシュ更新完了: {successCount}ボクセル");
                    tcs.SetResult(true);
                }
            );

            await tcs.Task;
        }

        private void VisualizeConnectionPoints(List<ConnectionPoint> connectionPoints)
        {
            foreach (var cp in connectionPoints)
            {
                GameObject debugSphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                debugSphere.name = $"ConnectionPoint_{cp.Id}";
                debugSphere.transform.position = cp.Position;
                debugSphere.transform.localScale = Vector3.one * cp.Radius * 2f;

                var renderer = debugSphere.GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = Color.green;
                }

                var collider = debugSphere.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Debug.Log($"[TestHardFloorCave] 接続点可視化: {cp.Id} at {cp.Position}");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }
    }
}
