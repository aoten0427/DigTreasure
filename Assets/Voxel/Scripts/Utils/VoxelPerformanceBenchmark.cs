using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;

namespace VoxelWorld
{
    /// <summary>
    /// ボクセルシステムのパフォーマンス測定用ベンチマーククラス
    /// 使用例:
    /// voxelOperationManager.EnablePerformanceLogging = true;
    /// VoxelPerformanceBenchmark.TestSetVoxelsPerformance(voxelOperationManager, chunkManager);
    /// </summary>
    public static class VoxelPerformanceBenchmark
    {
        /// <summary>
        /// SetVoxels のパフォーマンステスト
        /// </summary>
        /// <param name="operationManager">VoxelOperationManager</param>
        /// <param name="chunkManager">ChunkManager</param>
        /// <param name="voxelCount">テストするボクセル数</param>
        public static void TestSetVoxelsPerformance(
            VoxelOperationManager operationManager,
            ChunkManager chunkManager,
            int voxelCount = 1000)
        {
            UnityEngine.Debug.Log($"===== SetVoxels Performance Test ({voxelCount} voxels) =====");

            // テスト用のボクセル更新リストを作成
            var voxelUpdates = GenerateRandomVoxelUpdates(chunkManager, voxelCount);

            // パフォーマンス測定を有効化
            bool previousLogging = operationManager.EnablePerformanceLogging;
            operationManager.EnablePerformanceLogging = true;

            // テスト実行
            Stopwatch sw = Stopwatch.StartNew();
            operationManager.SetVoxels(voxelUpdates, false);
            sw.Stop();

            UnityEngine.Debug.Log($"SetVoxels Total Time (External): {sw.ElapsedMilliseconds}ms");
            UnityEngine.Debug.Log($"=================================================");

            // 元の設定に戻す
            operationManager.EnablePerformanceLogging = previousLogging;
        }

        /// <summary>
        /// DestroyVoxelsWithPower のパフォーマンステスト
        /// </summary>
        /// <param name="operationManager">VoxelOperationManager</param>
        /// <param name="chunkManager">ChunkManager</param>
        /// <param name="voxelCount">テストするボクセル数</param>
        public static void TestDestroyVoxelsPerformance(
            VoxelOperationManager operationManager,
            ChunkManager chunkManager,
            int voxelCount = 1000)
        {
            UnityEngine.Debug.Log($"===== DestroyVoxels Performance Test ({voxelCount} voxels) =====");

            // テスト用の座標リストを作成
            var positions = GenerateRandomPositions(chunkManager, voxelCount);

            // パフォーマンス測定を有効化
            bool previousLogging = operationManager.EnablePerformanceLogging;
            operationManager.EnablePerformanceLogging = true;

            // テスト実行
            Stopwatch sw = Stopwatch.StartNew();
            int destroyedCount = operationManager.DestroyVoxelsWithPower(
                positions,
                100f,
                Vector3.zero,
                Vector3.up);
            sw.Stop();

            UnityEngine.Debug.Log($"DestroyVoxels Total Time (External): {sw.ElapsedMilliseconds}ms");
            UnityEngine.Debug.Log($"Destroyed: {destroyedCount} voxels");
            UnityEngine.Debug.Log($"=================================================");

            // 元の設定に戻す
            operationManager.EnablePerformanceLogging = previousLogging;
        }

        /// <summary>
        /// 複数回のテストを実行して平均を取得
        /// </summary>
        public static void RunMultipleTests(
            VoxelOperationManager operationManager,
            ChunkManager chunkManager,
            int iterations = 5,
            int voxelCount = 1000)
        {
            UnityEngine.Debug.Log($"===== Running {iterations} iterations test ({voxelCount} voxels each) =====");

            var times = new List<long>();

            for (int i = 0; i < iterations; i++)
            {
                // テスト用のボクセル更新リストを作成
                var voxelUpdates = GenerateRandomVoxelUpdates(chunkManager, voxelCount);

                // 測定
                Stopwatch sw = Stopwatch.StartNew();
                operationManager.SetVoxels(voxelUpdates, false);
                sw.Stop();

                times.Add(sw.ElapsedMilliseconds);
                UnityEngine.Debug.Log($"Iteration {i + 1}: {sw.ElapsedMilliseconds}ms");
            }

            // 平均計算
            long sum = 0;
            foreach (var time in times)
            {
                sum += time;
            }
            float average = (float)sum / iterations;

            UnityEngine.Debug.Log($"Average Time: {average:F2}ms");
            UnityEngine.Debug.Log($"=================================================");
        }

        /// <summary>
        /// ランダムなボクセル更新リストを生成
        /// </summary>
        private static List<VoxelUpdate> GenerateRandomVoxelUpdates(
            ChunkManager chunkManager,
            int count)
        {
            var updates = new List<VoxelUpdate>(count);
            var positions = GenerateRandomPositions(chunkManager, count);

            foreach (var pos in positions)
            {
                updates.Add(new VoxelUpdate
                {
                    WorldPosition = pos,
                    VoxelID = Random.Range(1, 10)
                });
            }

            return updates;
        }

        /// <summary>
        /// ランダムな座標リストを生成（既存チャンク内）
        /// </summary>
        private static List<Vector3> GenerateRandomPositions(
            ChunkManager chunkManager,
            int count)
        {
            var positions = new List<Vector3>(count);

            // チャンク一覧を取得
            var chunkPositions = new List<Vector3Int>(chunkManager.ChunkPositions);

            if (chunkPositions.Count == 0)
            {
                UnityEngine.Debug.LogWarning("No chunks available for benchmark test!");
                return positions;
            }

            // ランダムな座標を生成
            for (int i = 0; i < count; i++)
            {
                // ランダムなチャンクを選択
                var chunkPos = chunkPositions[Random.Range(0, chunkPositions.Count)];

                // チャンク内のランダムな座標を生成
                Vector3 chunkWorldPos = VoxelConstants.ChunkToWorldPosition(
                    chunkPos.x, chunkPos.y, chunkPos.z);

                Vector3 randomOffset = new Vector3(
                    Random.Range(0, VoxelConstants.CHUNK_WIDTH) * VoxelConstants.VOXEL_SIZE,
                    Random.Range(0, VoxelConstants.CHUNK_HEIGHT) * VoxelConstants.VOXEL_SIZE,
                    Random.Range(0, VoxelConstants.CHUNK_DEPTH) * VoxelConstants.VOXEL_SIZE
                );

                positions.Add(chunkWorldPos + randomOffset);
            }

            return positions;
        }
    }
}
