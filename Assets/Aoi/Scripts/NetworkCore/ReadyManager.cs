using Fusion;
using System;
using System.Collections;
using UnityEngine;

namespace NetWork
{
    /// <summary>
    /// 全ユーザーの準備完了を感知
    /// </summary>
    public class ReadyManager : NetworkBehaviour
    {
        //すべてのユーザーがNetWorkObjectを検出したか
        [Networked, Capacity(4)]
        private NetworkLinkedList<PlayerRef> m_readyUser => default;
        [Networked] private int m_statingNumber { get; set; }
        //全てのユーザーの準備が完了したら呼ばれる
        private  Action AllUserReadyAction;
        [SerializeField] bool m_islog = false;


        public override void Spawned()
        {
            DontDestroyOnLoad(gameObject);
        }

        public void SetCompleteAction(Action action)
        {
            AllUserReadyAction = action;
        }

        /// <summary>
        /// 開始人数を設定
        /// </summary>
        /// <param name="number"></param>
        public void SetStandingNumber(int number)
        {
            if (Object.HasStateAuthority)
            {
                m_statingNumber = number;
            }
        }

        /// <summary>
        /// 開始していいかを判断
        /// </summary>
        /// <param name="user"></param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_IsReady(PlayerRef user)
        {
            if (!Runner.IsSharedModeMasterClient) return;

            m_readyUser.Add(user);
            if (m_islog) Debug.Log("人数:" + m_readyUser.Count + "人:" + Time.time);
            if (m_readyUser.Count >= m_statingNumber)
            {
                m_readyUser.Clear();
                RPC_ReadyAction();
            }

        }

        /// <summary>
        /// ホストのみ呼び出し
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_ReadyAction()
        {
            if (!Runner.IsSharedModeMasterClient) return;

            AllUserReadyAction?.Invoke();
            if (m_islog) Debug.Log("全てのユーザーの準備が完了:" + "合計" + m_readyUser.Count + "人" + Time.time);
        }

    }
}
