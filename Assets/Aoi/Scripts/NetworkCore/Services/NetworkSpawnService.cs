using Fusion;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NetWork
{
    /// <summary>
    /// NetworkObjectのSpawn管理を担当するサービス
    /// </summary>
    public class NetworkSpawnService : MonoBehaviour
    {
        [Header("Spawn Settings")]
        [SerializeField] private bool m_isLog = false;


        /// <summary>
        /// 全てのNetworkObjectがSpawnされた際のイベント
        /// </summary>
        public event Action<NetworkRunner> OnNetworkObjectsSpawned;

        /// <summary>
        /// シーン内のNetworkObjectをSpawn
        /// </summary>
        public void SpawnNetworkObjects(NetworkRunner runner, Action onComplete = null)
        {

            StartCoroutine(SpawnNetworkObjectsCoroutine(runner, onComplete));
        }

        /// <summary>
        /// NetworkObject Spawnのコルーチン
        /// </summary>
        private IEnumerator SpawnNetworkObjectsCoroutine(NetworkRunner runner, Action onComplete)
        {

            // 初期化待ち
            yield return null;

            // シーン内の未Spawn NetworkObject を検索
            var allNetworkObjects = FindObjectsByType<NetworkObject>(FindObjectsSortMode.None);

            foreach (NetworkObject netObj in allNetworkObjects)
            {
                // まだ Spawn されていない、かつ Runner が割り当てられていない場合
                if (!netObj.IsValid && netObj.Runner == null)
                {
                    // State Authority を持つクライアントが Spawn
                    if (runner.IsServer || runner.IsSharedModeMasterClient)
                    {
                        runner.Spawn(netObj);
                        if (m_isLog) Debug.Log($"{netObj.name}を見つけました");
                    }
                }
            }

            if (m_isLog) Debug.Log("[NetworkSpawnService] 全てのNetworkObjectをSpawn: " + Time.time);




            // イベント発火
            OnNetworkObjectsSpawned?.Invoke(runner);

            // 完了コールバック実行
            onComplete?.Invoke();

            OnNetworkObjectsSpawned = null;
        }

    }
        
}
