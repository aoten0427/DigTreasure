using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using VoxelWorld;

namespace StructureGeneration.Test
{
    /// <summary>
    /// ランダムウォーク洞窟のテスト
    /// </summary>
    public class TestRandomWalkCave : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private RandomWalkCaveSettings settings;

        [Header("WorldManager")]
        [SerializeField] private WorldManager worldManager;

        [Header("テストパラメータ")]
        [SerializeField] private int testSeed = 99999;
        [SerializeField] private Vector3 fieldCenter = new Vector3(0, -20, 0);
        [SerializeField] private Vector3 fieldSize = new Vector3(100, 50, 100);
        [SerializeField] private bool useVisibleBlocks = true; // テスト用：石を配置して可視化
        [SerializeField] private byte visibleBlockId = 2; // 表示用ボクセルID

        private async void Start()
        {
            if (settings == null)
            {
                Debug.LogError("[TestRandomWalkCave] RandomWalkCaveSettingsが設定されていません");
                return;
            }

            if (worldManager == null)
            {
                worldManager = FindObjectOfType<WorldManager>();
                if (worldManager == null)
                {
                    Debug.LogError("[TestRandomWalkCave] WorldManagerが見つかりません");
                    return;
                }
            }

            await Task.Delay(10000); // ワールド初期化待機

            Debug.Log("[TestRandomWalkCave] ランダムウォーク洞窟の生成を開始します...");

            // フィールド範囲を作成
            Bounds fieldBounds = new Bounds(fieldCenter, fieldSize);

            // ランダムウォーク洞窟を生成
            var structure = settings.CreateStructure("test_randomwalk", testSeed);
            var result = await structure.GenerateAsync(testSeed, fieldBounds);

            // テスト用：空気を石に置き換えて可視化
            if (useVisibleBlocks)
            {
                for (int i = 0; i < result.VoxelUpdates.Count; i++)
                {
                    var update = result.VoxelUpdates[i];
                    if (update.VoxelID == 0) // 空気の場合
                    {
                        result.VoxelUpdates[i] = new VoxelUpdate(update.WorldPosition, new Voxel(visibleBlockId));
                    }
                }
                Debug.Log($"[TestRandomWalkCave] テストモード: 空気を石(ID={visibleBlockId})に置き換えました");
            }

            Debug.Log($"[TestRandomWalkCave] 生成完了: {result.VoxelUpdates.Count}ボクセル");
            Debug.Log($"[TestRandomWalkCave] バウンディングボックス: {structure.GetBounds()}");
            Debug.Log($"[TestRandomWalkCave] 接続点数: {result.ConnectionPoints.Count}");
            Debug.Log($"[TestRandomWalkCave] 中心位置: {structure.CenterPosition}");

            if (result.SpecialPoints.ContainsKey("start"))
            {
                Debug.Log($"[TestRandomWalkCave] 開始位置: {result.SpecialPoints["start"]}");
            }
            if (result.SpecialPoints.ContainsKey("end"))
            {
                Debug.Log($"[TestRandomWalkCave] 終了位置: {result.SpecialPoints["end"]}");
            }

            // WorldManagerに適用
            Debug.Log("[TestRandomWalkCave] WorldManagerに適用中...");
            await ApplyToWorldManager(result.VoxelUpdates);
            Debug.Log("[TestRandomWalkCave] WorldManagerへの適用完了");

            // 接続点を可視化
            VisualizeConnectionPoints(result.ConnectionPoints);
        }

        private async Task ApplyToWorldManager(List<VoxelUpdate> voxelUpdates)
        {
            var operationManager = worldManager.Voxels;
            if (operationManager == null)
            {
                Debug.LogError("[TestRandomWalkCave] VoxelOperationManagerが見つかりません");
                return;
            }

            TaskCompletionSource<bool> tcs = new TaskCompletionSource<bool>();

            operationManager.SetVoxels(
                voxelUpdates,
                false,
                null,
                (successCount) =>
                {
                    Debug.Log($"[TestRandomWalkCave] メッシュ更新完了: {successCount}ボクセル");
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
                    // 始点は赤、終点は青
                    renderer.material.color = cp.Id.Contains("start") ? Color.red : Color.blue;
                }

                var collider = debugSphere.GetComponent<Collider>();
                if (collider != null)
                {
                    Destroy(collider);
                }

                Debug.Log($"[TestRandomWalkCave] 接続点可視化: {cp.Id} at {cp.Position}");
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(fieldCenter, fieldSize);
        }
    }
}
