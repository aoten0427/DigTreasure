using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Jobs;
using UniRx;
using System;

namespace VoxelWorld
{
    /// <summary>
    /// メッシュ更新の遅延実行システムを管理
    /// </summary>
    public class VoxelMeshManager
    {
        private ChunkManager m_chunkManager;
        private MonoBehaviour m_coroutineRunner;

        // メッシュ更新キュー（遅延実行用）
        private Queue<Vector3Int> m_meshUpdateQueue = new Queue<Vector3Int>();
        private HashSet<Vector3Int> m_meshUpdateQueueSet = new HashSet<Vector3Int>(); // 重複チェック用
        private bool m_isMeshUpdateRunning = false;
        private int m_meshUpdatesPerFrame = 50; // 1フレームあたりのメッシュ更新数

        // 自動メッシュ更新フラグ
        private bool m_enableAutoMeshUpdate;

        // メッシュ更新追跡用
        private class MeshUpdateTracker
        {
            public HashSet<Vector3Int> TargetChunks;
            public ReactiveProperty<float> ProgressProperty;
            public Action OnComplete;
            public int ProcessedCount;

            public MeshUpdateTracker(HashSet<Vector3Int> chunks, ReactiveProperty<float> progress, Action onComplete)
            {
                TargetChunks = chunks;
                ProgressProperty = progress;
                OnComplete = onComplete;
                ProcessedCount = 0;
            }
        }

        private List<MeshUpdateTracker> m_activeTrackers = new List<MeshUpdateTracker>();

        /// <summary>
        /// VoxelMeshManagerを初期化
        /// </summary>
        public void Initialize(ChunkManager chunkManager, MonoBehaviour coroutineRunner, bool enableAutoMeshUpdate)
        {
            m_chunkManager = chunkManager;
            m_coroutineRunner = coroutineRunner;
            m_enableAutoMeshUpdate = enableAutoMeshUpdate;
        }

        /// <summary>
        /// 自動メッシュ更新フラグを設定
        /// </summary>
        public void SetAutoMeshUpdate(bool enabled)
        {
            m_enableAutoMeshUpdate = enabled;
        }

        /// <summary>
        /// ボクセル変更をチャンクに通知
        /// </summary>
        public void NotifyVoxelChanged(Vector3Int chunkPosition)
        {
            if (m_chunkManager == null)
            {
                return;
            }

            // チャンクを変更済みとしてマーク
            m_chunkManager.MarkChunkDirty(chunkPosition);

            // メッシュ更新をキューに追加（遅延実行）
            if (m_enableAutoMeshUpdate)
            {
                // coroutineRunnerチェック
                if (m_coroutineRunner == null)
                {
                    Debug.LogError("[VoxelMeshManager] CoroutineRunnerが設定されていないためメッシュ更新をスキップします");
                    return;
                }

                // 重複チェック
                if (!m_meshUpdateQueueSet.Contains(chunkPosition))
                {
                    m_meshUpdateQueue.Enqueue(chunkPosition);
                    m_meshUpdateQueueSet.Add(chunkPosition);
                }

                // キュー処理が動いていなければ開始
                if (!m_isMeshUpdateRunning)
                {
                    m_coroutineRunner.StartCoroutine(ProcessMeshUpdateQueue());
                }
            }
        }

        /// <summary>
        /// メッシュ更新を登録
        /// </summary>
        public void MeshUpdate(
            HashSet<Vector3Int> chunkPositions,
            ReactiveProperty<float> progressProperty,
            Action onComplete)
        {
            if (chunkPositions == null || chunkPositions.Count == 0)
            {
                progressProperty?.SetValueAndForceNotify(1.0f);
                onComplete?.Invoke();
                return;
            }

            var tracker = new MeshUpdateTracker(chunkPositions, progressProperty, onComplete);
            m_activeTrackers.Add(tracker);

            // チャンクをキューに追加
            foreach (var chunkPos in chunkPositions)
            {
                NotifyVoxelChanged(chunkPos);
            }
        }

        /// <summary>
        /// メッシュ更新キューを段階的に処理
        /// </summary>
        private IEnumerator ProcessMeshUpdateQueue()
        {
            m_isMeshUpdateRunning = true;

            while (m_meshUpdateQueue.Count > 0)
            {
                var jobDataList = new List<(Vector3Int chunkPos, ChunkMesh.MeshJobData jobData)>();

                for (int i = 0; i < m_meshUpdatesPerFrame && m_meshUpdateQueue.Count > 0; i++)
                {
                    var chunkPos = m_meshUpdateQueue.Dequeue();
                    m_meshUpdateQueueSet.Remove(chunkPos);

                    // Chunkを取得してJobをスケジュール
                    var chunk = m_chunkManager.GetChunk(chunkPos);
                    if (chunk != null)
                    {
                        // WorldManagerから境界情報と設定を取得
                        var worldManager = WorldManager.GetInstance();
                        ChunkBoundaryInfo? boundaryInfo = null;
                        BoundaryMeshSettings boundarySettings = null;

                        if (worldManager != null)
                        {
                            boundaryInfo = worldManager.GetChunkBoundaryInfo(chunkPos);
                            boundarySettings = worldManager.BoundarySettings;
                        }

                        var jobData = chunk.ScheduleMeshGeneration(m_chunkManager, boundaryInfo, boundarySettings);
                        jobDataList.Add((chunkPos, jobData));
                    }
                    else
                    {
                        // NULLチャンクでもトラッカーを更新（このチャンクは「処理済み（スキップ）」としてカウント）
                        UpdateTrackers(chunkPos);
                    }
                }

                var handles = new Unity.Collections.NativeArray<JobHandle>(jobDataList.Count, Unity.Collections.Allocator.Temp);
                for (int i = 0; i < jobDataList.Count; i++)
                {
                    handles[i] = jobDataList[i].jobData.handle;
                }

                // 全Jobの完了を一括で待機
                JobHandle.CompleteAll(handles);
                handles.Dispose();

                foreach (var (chunkPos, jobData) in jobDataList)
                {
                    var chunk = m_chunkManager.GetChunk(chunkPos);
                    if (chunk != null)
                    {
                        var mesh = chunk.CompleteMeshGeneration(jobData);

                        // ChunkRendererにメッシュを適用
                        m_chunkManager.ApplyMeshToRenderer(chunkPos, mesh);

                        // トラッカーを更新
                        UpdateTrackers(chunkPos);
                    }
                    else
                    {
                        // チャンクが削除されていた場合、リソースを解放
                        jobData.Dispose();
                    }
                }

                yield return null; // 次フレームへ
            }

            m_isMeshUpdateRunning = false;
        }

        /// <summary>
        /// トラッカーを更新
        /// </summary>
        private void UpdateTrackers(Vector3Int processedChunk)
        {
            // 逆順ループで削除（インデックスエラー防止）
            for (int i = m_activeTrackers.Count - 1; i >= 0; i--)
            {
                var tracker = m_activeTrackers[i];

                // このトラッカーが対象とするチャンクか確認
                if (tracker.TargetChunks.Contains(processedChunk))
                {
                    tracker.ProcessedCount++;

                    // 進捗を更新
                    float progress = (float)tracker.ProcessedCount / tracker.TargetChunks.Count;
                    if (tracker.ProgressProperty != null)
                    {
                        tracker.ProgressProperty.Value = progress;
                    }

                    // 完了判定
                    if (tracker.ProcessedCount >= tracker.TargetChunks.Count)
                    {
                        tracker.OnComplete?.Invoke();
                        m_activeTrackers.RemoveAt(i);
                    }
                }
            }
        }
    }
}
