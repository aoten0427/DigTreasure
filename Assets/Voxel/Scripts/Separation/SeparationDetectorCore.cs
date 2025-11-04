using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// SeparationDetector コアアルゴリズム
    /// </summary>
    public partial class SeparationDetector
    {
        /// <summary>
        /// Flood Fillアルゴリズムを使用して接続されたボクセルグループを検出
        /// </summary>
        /// <param name="startPos">開始座標</param>
        /// <param name="globalVisited">グローバル訪問済み座標セット</param>
        /// <param name="voxelProvider">ボクセルプロバイダー</param>
        /// <returns>接続されたボクセル座標リスト</returns>
        public List<Vector3> FloodFill(Vector3 startPos, HashSet<Vector3> globalVisited, IVoxelProvider voxelProvider)
        {
            return FloodFill(startPos, globalVisited, voxelProvider, m_settings.EnableDiagonalConnection);
        }

        /// <summary>
        /// Flood Fillアルゴリズムを使用して接続されたボクセルグループを検出
        /// </summary>
        /// <param name="startPos">開始座標</param>
        /// <param name="globalVisited">グローバル訪問済み座標セット</param>
        /// <param name="voxelProvider">ボクセルプロバイダー</param>
        /// <param name="useDiagonalConnection">斜め接続を使用するか</param>
        /// <returns>接続されたボクセル座標リスト</returns>
        public List<Vector3> FloodFill(Vector3 startPos, HashSet<Vector3> globalVisited, IVoxelProvider voxelProvider, bool useDiagonalConnection)
        {
            EnsureInitialized();

            var result = new List<Vector3>();
            var localVisited = new HashSet<Vector3>();
            var queue = new Queue<Vector3>();

            // 開始位置の妥当性チェック
            if (!voxelProvider.IsNonEmptyVoxel(startPos))
            {
                return result;
            }

            if (globalVisited.Contains(startPos))
            {
                return result;
            }

            queue.Enqueue(startPos);
            localVisited.Add(startPos);
            globalVisited.Add(startPos);
            result.Add(startPos);


            // 制限チェック用
            bool exceededMaxSize = false;
            bool exceededChunkRange = false;
            HashSet<Vector3Int> visitedChunks = new HashSet<Vector3Int>();

            // 開始チャンクを記録
            if (m_settings.EnableChunkRangeLimit)
            {
                Vector3Int startChunk = GetChunkPosition(startPos);
                visitedChunks.Add(startChunk);
            }

            Vector3[] neighborOffsets = VoxelNeighborUtility.GetNeighborOffsets(useDiagonalConnection);
            int directionCount = neighborOffsets.Length;

            while (queue.Count > 0)
            {
                // 制限超過時は即座にループを抜ける
                if (exceededMaxSize || exceededChunkRange)
                {
                    break;
                }

                Vector3 current = queue.Dequeue();

                // 近傍をチェック
                for (int i = 0; i < directionCount; i++)
                {
                    Vector3 neighbor = current + neighborOffsets[i];

                    if (!globalVisited.Contains(neighbor) &&
                        voxelProvider.IsNonEmptyVoxel(neighbor))
                    {
                        // チャンク範囲制限チェック
                        if (m_settings.EnableChunkRangeLimit)
                        {
                            Vector3Int neighborChunk = GetChunkPosition(neighbor);
                            if (!visitedChunks.Contains(neighborChunk))
                            {
                                if (visitedChunks.Count >= m_settings.MaxChunkRange)
                                {
                                    exceededChunkRange = true;
                                    if(m_settings.m_isLog)Debug.Log($"チャンク範囲制限到達: {m_settings.MaxChunkRange}チャンク、検出を中断");
                                    break; // 即座にループを抜ける
                                }
                                visitedChunks.Add(neighborChunk);
                            }
                        }

                        localVisited.Add(neighbor);
                        globalVisited.Add(neighbor);
                        result.Add(neighbor);
                        queue.Enqueue(neighbor);

                        // サイズ制限チェック
                        if (result.Count >= m_settings.MaxSeparationSize)
                        {
                            exceededMaxSize = true;
                            if (m_settings.m_isLog) Debug.Log($"最大サイズに到達: {m_settings.MaxSeparationSize}、検出を中断");
                            break; // 即座にループを抜ける
                        }

                        if (m_settings.m_isLog) Debug.Log($"FloodFill: {neighbor} を追加 (総数: {localVisited.Count})");
                    }
                }
            }

            // 制限超過の場合は空のリストを返す
            if (exceededMaxSize || exceededChunkRange)
            {
                result.Clear();
            }

            return result;
        }

        /// <summary>
        /// Flood Fillアルゴリズムを使用して接続されたボクセルグループを検出
        /// </summary>
        /// <param name="startPos">開始座標</param>
        /// <param name="globalVisited">グローバル訪問済み座標セット</param>
        /// <param name="chunkManager">チャンク管理クラス</param>
        /// <returns>接続されたボクセル座標リスト</returns>
        public List<Vector3> FloodFill(Vector3 startPos, HashSet<Vector3> globalVisited, ChunkManager chunkManager)
        {
            var provider = new ChunkManagerVoxelProvider(chunkManager);
            return FloodFill(startPos, globalVisited, provider);
        }

        /// <summary>
        /// 簡易的な分離検出
        /// </summary>
        /// <param name="destroyedPositions">破壊された座標リスト</param>
        /// <param name="voxelProvider">ボクセルプロバイダー</param>
        /// <returns>分離検出結果</returns>
        public SeparationResult DetectSeparations(List<Vector3> destroyedPositions, IVoxelProvider voxelProvider)
        {
            EnsureInitialized();

            float startTime = Time.realtimeSinceStartup;


            var result = new SeparationResult();
            var globalVisited = new HashSet<Vector3>(destroyedPositions); // 破壊された座標は訪問済みとして扱う
            var separatedGroups = new List<List<Vector3>>();
            var oversizedGroups = new List<List<Vector3>>();

            // 影響範囲のボクセルを取得
            var affectedVoxels = GetAffectedVoxels(destroyedPositions, voxelProvider);

            // 破壊率による早期スキップ
            if (m_settings.EnableDestructionRateOptimization)
            {
                int beforeCount = affectedVoxels.Count;
                affectedVoxels = FilterByDestructionRate(destroyedPositions, affectedVoxels, voxelProvider);
            }

            int totalGroupsFound = 0;

            // 2段階判定
            PerformTwoStageDetection(affectedVoxels, globalVisited, voxelProvider, separatedGroups, oversizedGroups, ref totalGroupsFound);

            float endTime = Time.realtimeSinceStartup;

            result.SeparatedGroups = separatedGroups;
            result.OversizedGroups = oversizedGroups;
            result.ProcessingTime = endTime - startTime;
            result.TotalProcessedVoxels = affectedVoxels.Count;
            result.TotalGroupsFound = totalGroupsFound;
            result.ValidGroupsCreated = separatedGroups.Count;
            result.OversizedGroupsCount = oversizedGroups.Count;

            return result;
        }

        /// <summary>
        /// 簡易的な分離検出
        /// </summary>
        /// <param name="destroyedPositions">破壊された座標リスト</param>
        /// <param name="chunkManager">チャンク管理クラス</param>
        /// <returns>分離検出結果</returns>
        public SeparationResult DetectSeparations(List<Vector3> destroyedPositions, ChunkManager chunkManager)
        {
            var provider = new ChunkManagerVoxelProvider(chunkManager);
            return DetectSeparations(destroyedPositions, provider);
        }

        /// <summary>
        /// 影響ボクセルをサイズ推定でソート（小さい順）
        /// </summary>
        private List<Vector3> EstimateAndSortVoxelsBySize(
            List<Vector3> affectedVoxels,
            HashSet<Vector3> globalVisited,
            IVoxelProvider voxelProvider)
        {
            // 各ボクセルの推定サイズを計算
            var voxelSizeEstimates = new List<(Vector3 position, int estimatedSize)>();

            foreach (var voxel in affectedVoxels)
            {
                if (!globalVisited.Contains(voxel))
                {
                    // 軽量なFloodFillでサイズ推定（最大10ステップ）
                    int estimatedSize = EstimateGroupSize(voxel, globalVisited, voxelProvider, maxSteps: 10);
                    voxelSizeEstimates.Add((voxel, estimatedSize));
                }
            }

            // 推定サイズでソート（小さい順）
            var sorted = voxelSizeEstimates.OrderBy(x => x.estimatedSize).Select(x => x.position).ToList();

            return sorted;
        }

        /// <summary>
        /// グループサイズを軽量に推定
        /// </summary>
        private int EstimateGroupSize(
            Vector3 startPos,
            HashSet<Vector3> globalVisited,
            IVoxelProvider voxelProvider,
            int maxSteps)
        {
            if (!voxelProvider.IsNonEmptyVoxel(startPos) || globalVisited.Contains(startPos))
            {
                return 0;
            }

            var localVisited = new HashSet<Vector3>();
            var queue = new Queue<Vector3>();

            queue.Enqueue(startPos);
            localVisited.Add(startPos);

            Vector3[] neighborOffsets = VoxelNeighborUtility.GetNeighborOffsets(m_settings.EnableDiagonalConnection);
            int directionCount = neighborOffsets.Length;
            int steps = 0;

            while (queue.Count > 0 && steps < maxSteps)
            {
                Vector3 current = queue.Dequeue();
                steps++;

                for (int i = 0; i < directionCount; i++)
                {
                    Vector3 neighbor = current + neighborOffsets[i];

                    if (!globalVisited.Contains(neighbor) &&
                        !localVisited.Contains(neighbor) &&
                        voxelProvider.IsNonEmptyVoxel(neighbor))
                    {
                        localVisited.Add(neighbor);
                        queue.Enqueue(neighbor);
                    }
                }
            }

            // キューに残っているボクセル数も含めて推定
            int estimatedSize = localVisited.Count + queue.Count * 2;

            return estimatedSize;
        }

        /// <summary>
        /// 2段階判定
        /// </summary>
        private void PerformTwoStageDetection(
            List<Vector3> affectedVoxels,
            HashSet<Vector3> globalVisited,
            IVoxelProvider voxelProvider,
            List<List<Vector3>> separatedGroups,
            List<List<Vector3>> oversizedGroups,
            ref int totalGroupsFound)
        {
            var coarseVisited = new HashSet<Vector3>(globalVisited);
            var suspiciousVoxels = new List<Vector3>(); // 詳細判定が必要なボクセル

            // 粗判定の前にサイズ推定でソート（小さい順）
            var sortedAffectedVoxels = EstimateAndSortVoxelsBySize(affectedVoxels, coarseVisited, voxelProvider);

            // 粗判定（6方向のみ、斜め接続なし）- サイズ順に処理
            foreach (var voxel in sortedAffectedVoxels)
            {
                if (!coarseVisited.Contains(voxel))
                {
                    var coarseGroup = FloodFill(voxel, coarseVisited, voxelProvider, useDiagonalConnection: false);

                    // チャンク範囲制限到達（Count=0）→ 詳細判定で確認が必要
                    if (coarseGroup.Count == 0)
                    {
                        // voxelが起点のボクセルを詳細判定候補に追加
                        suspiciousVoxels.Add(voxel);
                        
                    }
                    // 小規模グループ → 詳細判定が必要
                    else if (coarseGroup.Count > 0 && coarseGroup.Count <= m_settings.SuspiciousSizeThreshold)
                    {
                        suspiciousVoxels.AddRange(coarseGroup);
                        
                    }
                    else if (coarseGroup.Count > m_settings.SuspiciousSizeThreshold)
                    {
                        // globalVisitedにも追加して、詳細判定でスキップ
                        foreach (var v in coarseGroup)globalVisited.Add(v);
                    }
                }
            }

            // 詳細判定の前にもサイズ推定でソート（小さい順）
            var sortedSuspiciousVoxels = EstimateAndSortVoxelsBySize(suspiciousVoxels, globalVisited, voxelProvider);

            // 詳細判定（26方向、斜め含む）- サイズ順に処理
            foreach (var voxel in sortedSuspiciousVoxels)
            {
                if (!globalVisited.Contains(voxel))
                {
                    var detailedGroup = FloodFill(voxel, globalVisited, voxelProvider, useDiagonalConnection: true);

                    // group.Countが0の場合はサイズ超過で座標リストがクリアされている
                    if (detailedGroup.Count == 0)
                    {
                        totalGroupsFound++;
                        oversizedGroups.Add(detailedGroup);
                    }
                    else if (detailedGroup.Count >= m_settings.MinSeparationSize)
                    {
                        totalGroupsFound++;

                        if (detailedGroup.Count <= m_settings.MaxSeparationSize)
                        {
                            separatedGroups.Add(detailedGroup);
                        }
                        else
                        {
                            oversizedGroups.Add(detailedGroup);
                        }
                    }
                }
            }
        }
    }
}
