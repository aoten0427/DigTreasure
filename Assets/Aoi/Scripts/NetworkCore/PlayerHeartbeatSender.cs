using Fusion;
using UnityEngine;

namespace NetWork
{
    /// <summary>
    /// 各プレイヤーがハートビートを送信
    /// </summary>
    public class PlayerHeartbeatSender : NetworkBehaviour
    {
        private PlayerHeartbeatManager heartbeatManager;
        private float heartbeatInterval = 2f; // 2秒ごとに送信
        private float lastHeartbeatTime = 0f;

        public void Initialize(PlayerHeartbeatManager manager)
        {
            heartbeatManager = manager;
        }

        public override void FixedUpdateNetwork()
        {
            // 自分のプレイヤーのみハートビートを送信
            if (!Object.HasInputAuthority) return;
            if (heartbeatManager == null) return;

            if (Time.time - lastHeartbeatTime >= heartbeatInterval)
            {
                lastHeartbeatTime = Time.time;
                heartbeatManager.RPC_SendHeartbeat(Object.InputAuthority);
            }
        }
    }
}
