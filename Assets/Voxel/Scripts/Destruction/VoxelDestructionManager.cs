using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace VoxelWorld
{
    /// <summary>
    /// 破壊処理専用管理クラス
    /// </summary>
    public class VoxelDestructionManager:MonoBehaviour
    {
        
        [Header("破壊設定")]
        [SerializeField] private float m_defaultAttackPower = 1.0f;
        [SerializeField] private bool m_enableDestructionLogging = true;

        [Header("パフォーマンス設定")]
        [SerializeField] private int m_maxChunksPerFrame = 2;
        [SerializeField] private float m_processingTimePerBatch = 0.016f;
        

        // 破壊要求管理
        private Queue<DestructionRequest> m_destructionQueue = new Queue<DestructionRequest>();
        
        //破壊処理実行中フラグ
        private bool m_isProcessingDestruction = false;
        
        // システム参照
        private WorldManager m_worldManager;
        private VoxelOperationManager m_voxelManager;
        
        // チャンク別処理用
        private Dictionary<Vector3Int, List<Vector3>> m_chunkGroupedPositions = new Dictionary<Vector3Int, List<Vector3>>();

        // プロパティ
        //破壊処理中かどうか
        public bool IsProcessingDestruction => m_isProcessingDestruction;
        
        //待機中の要求数
        public int PendingRequestCount => m_destructionQueue.Count;
        

        private void Start()
        {
            Initialize();
        }

        /// <summary>
        /// DestructionManagerを初期化
        /// </summary>
        public void Initialize()
        {
            // WorldManagerを取得
            if (m_worldManager == null)
            {
                m_worldManager = WorldManager.GetInstance();
            }

            if (m_worldManager == null)
            {
                Debug.LogError("[VoxelDestructionManager] WorldManagerが見つかりません。");
                return;
            }
            
            
            // VoxelOperationManagerを取得
            m_voxelManager = m_worldManager.Voxels;
            if (m_voxelManager == null)
            {
                Debug.LogError("[VoxelDestructionManager] VoxelOperationManagerが見つかりません。");
                return;
            }
        }


        /// <summary>
        /// 指定された形状のボクセルを破壊（破壊数付きコールバック版）
        /// </summary>
        /// <param name="destructionShape">破壊形状</param>
        /// <param name="attackPower">攻撃力</param>
        /// <param name="onCompleteWithCount">完了時コールバック（破壊数を受け取る）</param>
        public void DestroyVoxels(IDestructionShape destructionShape, float attackPower,Vector3 direction,
            System.Action<int> onCompleteWithCount)
        {
            if (destructionShape == null)
            {
                Debug.LogWarning("[VoxelDestructionManager] 破壊形状がnullです。");
                onCompleteWithCount?.Invoke(0);
                return;
            }

            if (attackPower < 0)
            {
                attackPower = m_defaultAttackPower;
            }

            var request = new DestructionRequest(destructionShape, attackPower,direction, onCompleteWithCount);

            m_destructionQueue.Enqueue(request);
            // 破壊処理が実行中でない場合は開始
            if (!m_isProcessingDestruction)
            {
                StartCoroutine(ProcessDestructionQueue());
            }
        }
        

        /// <summary>
        /// 破壊処理キューを処理するコルーチン
        /// 優先度順で処理を実行
        /// </summary>
        /// <returns>コルーチン</returns>
        private IEnumerator ProcessDestructionQueue()
        {
            m_isProcessingDestruction = true;

            while (HasPendingRequests())
            {
                // 優先度順で要求を取得
                var request = GetNextRequest();
                if (request != null)
                {
                    yield return StartCoroutine(ProcessSingleDestructionRequest(request));
                }
            }

            m_isProcessingDestruction = false;
        }

        /// <summary>
        /// 待機中の要求があるかチェック
        /// </summary>
        /// <returns>要求がある場合true</returns>
        private bool HasPendingRequests()
        {
            return m_destructionQueue.Count > 0;
        }

        /// <summary>
        /// 次の処理要求を取得
        /// </summary>
        /// <returns>次の処理要求</returns>
        private DestructionRequest GetNextRequest()
        {
            return m_destructionQueue.Count > 0 ? m_destructionQueue.Dequeue() : null;
        }

        /// <summary>
        /// 単一の破壊要求を処理するコルーチン
        /// </summary>
        /// <param name="request">破壊要求</param>
        /// <returns>コルーチン</returns>
        private IEnumerator ProcessSingleDestructionRequest(DestructionRequest request)
        {
            var targetPositions = request.Shape.GetDestructionPositions().ToList();

            if (targetPositions.Count == 0)
            {
                request.OnCompleteWithCount?.Invoke(0);
                yield break;
            }

            var chunkGroups = GroupPositionsByChunk(targetPositions);

            int processedChunks = 0;
            int totalDestroyedCount = 0; // 実際に破壊されたボクセル数

            foreach (var (chunkPos, positions) in chunkGroups)
            {
                // 破壊実行
                int destroyedCount = m_voxelManager.DestroyVoxelsWithPower(positions, request.AttackPower,
                    request.Shape.GetDestractionPoint(),request.EffectDirection);
                totalDestroyedCount += destroyedCount;
                processedChunks++;

                if (processedChunks >= m_maxChunksPerFrame)
                {
                    processedChunks = 0;
                    // 固定時間ベース待機
                    yield return new WaitForSeconds(m_processingTimePerBatch);
                }
            }

            // 完了コールバックを実行
            request.OnCompleteWithCount?.Invoke(totalDestroyedCount);
        }

        /// <summary>
        /// 座標リストをチャンク別にグループ化
        /// </summary>
        /// <param name="positions">破壊対象座標リスト</param>
        /// <returns>チャンク別にグループ化された座標</returns>
        private Dictionary<Vector3Int, List<Vector3>> GroupPositionsByChunk(List<Vector3> positions)
        {
            m_chunkGroupedPositions.Clear();

            foreach (var pos in positions)
            {
                var chunkPos = VoxelConstants.WorldToChunkPosition(pos);
                
                if (!m_chunkGroupedPositions.ContainsKey(chunkPos))
                {
                    m_chunkGroupedPositions[chunkPos] = new List<Vector3>();
                }
                
                m_chunkGroupedPositions[chunkPos].Add(pos);
            }


            return m_chunkGroupedPositions;
        }
    }
}