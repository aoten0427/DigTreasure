using Fusion;
using System;
using UnityEngine;

namespace NetWork
{
    /// <summary>
    /// ReadyManagerの生成・管理を担当するサービス
    /// </summary>
    public class ReadyService : MonoBehaviour
    {
        [Header("Ready Settings")]
        [SerializeField] private NetworkPrefabRef m_readyPrefab;
        [SerializeField] private int m_startingNumber = 2;
        [SerializeField] private bool m_isLog = false;

        private NetworkRunner m_runner;
        private ReadyManager m_readyManager;

        public int StartingNumber { get { return m_startingNumber; }set { m_startingNumber = value; } }

        /// <summary>
        /// 全てのユーザーの準備が完了した際のイベント
        /// </summary>
        public event Action OnAllUserReady;

        /// <summary>
        /// サービスを初期化
        /// </summary>
        public void Initialize(NetworkRunner runner)
        {
            m_runner = runner;
            SpawnReadyManager();
        }

        public void DataReset()
        {
            m_runner = null;
            m_readyManager = null;
        }

        /// <summary>
        /// シーンロード完了時の処理
        /// </summary>
        public void OnSceneLoadDone(NetworkRunner runner)
        {
            if (m_readyManager == null) AcquisitionReadyManager();
            m_readyManager.SetStandingNumber(m_startingNumber);
            m_readyManager.RPC_IsReady(runner.LocalPlayer);
            if (m_isLog) Debug.Log("[ReadyService] 準備完了を通知しました。");
        }

        /// <summary>
        /// 開始人数を設定
        /// </summary>
        public void SetStartingNumber(int number)
        {
            m_startingNumber = number;
        }

        /// <summary>
        /// ReadyManagerを取得
        /// </summary>
        public ReadyManager GetReadyManager()
        {
            return m_readyManager;
        }

        /// <summary>
        /// ReadyManagerを生成
        /// </summary>
        private void SpawnReadyManager()
        {
            if (m_runner == null)
            {
                Debug.LogWarning("[ReadyService] NetworkRunnerが初期化されていません。");
                return;
            }

            if (m_readyManager != null) return;

            // ホスト以外はパス
            if (m_runner.IsSharedModeMasterClient)
            {
                NetworkObject spawnedObject = m_runner.Spawn(m_readyPrefab, onBeforeSpawned: (runner, obj) =>
                {
                    // Flags を設定：AllowStateAuthorityOverride = ON, DestroyWhenStateAuthorityLeaves = OFF
                    obj.Flags = NetworkObjectFlags.AllowStateAuthorityOverride;
                    Debug.Log($"[ReadyService] ReadyManager Flags設定: {obj.Flags}");
                });

                m_readyManager = spawnedObject.GetComponent<ReadyManager>();
                m_readyManager.SetCompleteAction(() => OnAllUserReady?.Invoke());
                if (m_isLog) Debug.Log("[ReadyService] ReadyManagerをSpawnしました。");
            }
        }

        private void AcquisitionReadyManager()
        {
            if (m_readyManager != null) return;

            m_readyManager = FindFirstObjectByType<ReadyManager>();

            if (m_readyManager == null)
            {
                if (m_isLog) Debug.LogWarning("[ReadyService] ReadyManagerが見つかりませんでした。");
                return;
            }
            m_readyManager.SetCompleteAction(() => OnAllUserReady?.Invoke());
        }

    }
}