using Fusion;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NetWork
{
    public class UserDataManager : NetworkBehaviour
    {
        [Networked, Capacity(4)]
        private NetworkDictionary<PlayerRef, NetworkUserData> n_userDatas => default;
        //データが変わった際に呼ばれるイベント
        private Action<IReadOnlyDictionary<PlayerRef, NetworkUserData>> OnDataChangeAction;

        [Networked]
        int n_id { get; set; } = 1;

        private Action<int> OnIDGet;

        [SerializeField] public bool m_isLog { get; set; }


        public override void Spawned()
        {
            DontDestroyOnLoad(gameObject);
        }

        /// <summary>
        /// 変更時のイベントを設定
        /// </summary>
        /// <param name="action"></param>
        public void SetDataChangeAction(Action<IReadOnlyDictionary<PlayerRef, NetworkUserData>> action)
        {
            OnDataChangeAction = action;
        }

        public void SetIDGet(Action<int> action)
        {
            OnIDGet = action;
        }

        /// <summary>
        /// ユーザーを追加
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_ChangeUserData(PlayerRef user, NetworkUserData data)
        {

            if (n_userDatas.ContainsKey(user))
            {
                n_userDatas.Set(user, data);
                //全ユーザーに変更を送信
                RPC_ReceiptUserData();
            }
            else
            {
                RPC_ReceiptID(user,n_id);
                data.m_id = n_id;
                if (data.m_name == "") data.m_name = user.ToString();
                n_userDatas.Add(user, data);
                if (m_isLog) {
                    Debug.Log($"[UserDataManager]Userを追加しました:{user}");
                    Debug.Log($"現在のデータ件数:{n_userDatas.Count}");
                }
                
                n_id++;
            }
        }

        /// <summary>
        /// ユーザーのデータを削除
        /// </summary>
        /// <param name="user"></param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_PlayerLeft(PlayerRef user)
        {
            if(n_userDatas.ContainsKey(user))
            {
                n_userDatas.Remove(user);
                if (m_isLog)
                {
                    Debug.Log($"{user}のデータを削除しました");
                    Debug.Log($"現在のデータ件数:{n_userDatas.Count}");
                }
            }
        }


        /// <summary>
        /// IDを取得
        /// </summary>
        /// <param name="user"></param>
        /// <param name="data"></param>
        /// <param name="id"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ReceiptID(PlayerRef user,int id)
        {
            if(Runner.LocalPlayer == user)
            {
                OnIDGet?.Invoke(id);
            }
        }


        /// <summary>
        /// ユーザーデータの変更を受け取り
        /// </summary>
        /// <param name="user"></param>
        [Rpc(RpcSources.All, RpcTargets.All)]
        public void RPC_ReceiptUserData()
        {
            //読み取り専用で渡す
            var read = new Dictionary<PlayerRef, NetworkUserData>(n_userDatas);
            OnDataChangeAction?.Invoke(read);
        }

        /// <summary>
        /// 動的にデータ読み込み
        /// </summary>
        /// <returns></returns>
        public IReadOnlyDictionary<PlayerRef, NetworkUserData> GetAllUserData()
        {
            return new Dictionary<PlayerRef, NetworkUserData>(n_userDatas);
        }
    }
}
