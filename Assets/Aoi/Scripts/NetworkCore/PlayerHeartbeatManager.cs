using Fusion;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork
{
    /// <summary>
    /// プレイヤーのハートビートを管理して切断を検出
    /// </summary>
    public class PlayerHeartbeatManager : NetworkBehaviour
    {
        [Networked, Capacity(16)]
        private NetworkDictionary<PlayerRef, int> PlayerHeartbeats => default;

        private float heartbeatInterval = 2f; // 2秒ごとにハートビート送信
        private float heartbeatTimeout = 3f; // 6秒応答なしで切断とみなす
        private TickTimer heartbeatTimer;

        private Dictionary<PlayerRef, float> lastHeartbeatTime = new Dictionary<PlayerRef, float>();

        public System.Action<PlayerRef> OnPlayerTimeout;

        public override void Spawned()
        {
            DontDestroyOnLoad(gameObject);
        }

        public override void FixedUpdateNetwork()
        {
            if (!Object.HasStateAuthority) return;

            // ハートビートタイマー
            if (heartbeatTimer.ExpiredOrNotRunning(Runner))
            {
                heartbeatTimer = TickTimer.CreateFromSeconds(Runner, heartbeatInterval);
                CheckHeartbeats();
            }
        }

        /// <summary>
        /// クライアントからハートビートを受信
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_SendHeartbeat(PlayerRef player)
        {
            if (PlayerHeartbeats.ContainsKey(player))
            {
                PlayerHeartbeats.Set(player, Runner.Tick);
            }
            else
            {
                PlayerHeartbeats.Add(player, Runner.Tick);
            }

            lastHeartbeatTime[player] = Time.time;
        }

        /// <summary>
        /// ハートビートをチェックして、タイムアウトしたプレイヤーを検出
        /// </summary>
        private void CheckHeartbeats()
        {
            List<PlayerRef> timedOutPlayers = new List<PlayerRef>();

            foreach (var kvp in lastHeartbeatTime)
            {
                if (Time.time - kvp.Value > heartbeatTimeout)
                {
                    timedOutPlayers.Add(kvp.Key);
                }
            }

            foreach (var player in timedOutPlayers)
            {
                lastHeartbeatTime.Remove(player);

                if (PlayerHeartbeats.ContainsKey(player))
                {
                    PlayerHeartbeats.Remove(player);
                }

                OnPlayerTimeout?.Invoke(player);
            }
        }

        /// <summary>
        /// プレイヤーを登録
        /// </summary>
        public void RegisterPlayer(PlayerRef player)
        {
            lastHeartbeatTime[player] = Time.time;

            if (Object.HasStateAuthority)
            {
                if (!PlayerHeartbeats.ContainsKey(player))
                {
                    PlayerHeartbeats.Add(player, Runner.Tick);
                }
            }
        }

        /// <summary>
        /// プレイヤーを削除
        /// </summary>
        public void UnregisterPlayer(PlayerRef player)
        {
            lastHeartbeatTime.Remove(player);

            if (Object.HasStateAuthority && PlayerHeartbeats.ContainsKey(player))
            {
                PlayerHeartbeats.Remove(player);
            }
        }
    }
}
