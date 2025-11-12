using Fusion;
using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;


namespace NetWork
{
    /// <summary>
    /// ルーム管理
    /// </summary>
    public class RoomManager : NetworkBehaviour
    {
        GameLauncher m_gameLauncher;
        //表示するプレイヤー
        [SerializeField] GameObject[] m_players = new GameObject[4];
        //プレイヤー名
        [SerializeField] TextMeshProUGUI[] m_playerName = new TextMeshProUGUI[4];
        //自身のモデル
        private GameObject m_player;

        //ユーザーとそれに紐づくオブジェクトのインデックス番号
        [Networked, Capacity(4)]
        private NetworkDictionary<PlayerRef, int> n_usertoIndex => default;
        //インデックス番号の使用状況
        [Networked, Capacity(4)]
        private NetworkArray<NetworkBool> n_indexUsage => default;

        //自身が準備完了か
        bool m_isReady = false;
        //全員が準備完了か
        [Networked, Capacity(4)]
        private NetworkDictionary<PlayerRef, bool> n_allisReady => default;

        /// <summary>
        /// 初期化
        /// </summary>
        private void Start()
        {

            m_gameLauncher = GameLauncher.Instance;
            m_gameLauncher?.AddOnNetworkObjectSpawned(Entry);
            m_gameLauncher.OnPlayerLeft += RemovePlayer;

            // 初期状態で全てオフ
            foreach (var p in m_players)
            {
                if (p != null)
                {
                    p.SetActive(false);
                }
            }
            foreach (var p in m_playerName)
            {
                if(p != null)
                {
                    p.gameObject.SetActive(false);
                }
            }

            m_gameLauncher.SetLoadScreen(LoadType.None);
        }

        /// <summary>
        /// 生成
        /// </summary>
        public override void Spawned()
        {
            // 初期化
            if (HasStateAuthority)
            {
                for (int i = 0; i < n_indexUsage.Length; i++)
                {
                    n_indexUsage.Set(i, false);
                }
            }

            // 全ユーザーをオフ
            foreach (var p in m_players)
            {
                if (p != null)
                {
                    p.SetActive(false);
                }
            }

            //プレイシーン用のロードを設定
            
        }

        /// <summary>
        /// 破壊処理　イベントから削除
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="hasState"></param>
        public override void Despawned(NetworkRunner runner, bool hasState)
        {
            if (m_gameLauncher != null)
            {
                m_gameLauncher.RemoveOnNetworkObjectSpawned(Entry);
                m_gameLauncher.OnPlayerLeft -= RemovePlayer;
            }
        }


        /// <summary>
        /// 入室
        /// </summary>
        /// <param name="runner"></param>
        public void Entry(NetworkRunner runner)
        {
            // RoomManager自身がまだSpawnされていない場合は待機
            if (Runner == null || !Object.IsValid)
            {
                Invoke(nameof(DelayedEntry), 1.0f);
                return;
            }
            RPC_Entry(Runner.LocalPlayer);
        }

        private void DelayedEntry()
        {
            StartCoroutine(WaitForSpawnAndEntry());
        }

        /// <summary>
        /// Resultからのよみこみ時に待たないといけないため松
        /// </summary>
        /// <returns></returns>
        private IEnumerator WaitForSpawnAndEntry()
        {
            // Spawnされるまで待つ
            yield return new WaitUntil(() => Runner != null && Object.IsValid);
            RPC_Entry(Runner.LocalPlayer);
        }

        /// <summary>
        /// ホストに入室を通知
        /// </summary>
        /// <param name="user"></param>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        private void RPC_Entry(PlayerRef user)
        {
            var userdatas = m_gameLauncher.GetAllUserData();
            //エントリー通知のユーザーがいるか確認
            if(userdatas.ContainsKey(user))
            {
                var userdata = userdatas[user];
                int index = GetIndexNum();
                //ユーザーIDとインデックス番号を紐づけ
                n_usertoIndex.Add(user, index);
                //準備完了のメンバーに追加
                n_allisReady.Add(user, false);
                //通知を送ったユーザーに使うプレイヤーを伝える
                RPC_ReceiptUsage(user, index);
            }
            else
            {
                Debug.LogWarning($"{user}がUserDataManagerに登録されていません");
            }
            
        }

        /// <summary>
        /// ユーザが抜けた際
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="user"></param>
        private void RemovePlayer(NetworkRunner runner, PlayerRef user)
        {
            //ホスト以外は実行しない
            if (!Object.HasStateAuthority) return;

            if (n_usertoIndex.ContainsKey(user))
            {
                //対応したインデックス番号を取得
                int index = n_usertoIndex[user];
                //ユーザーを削除
                n_usertoIndex.Remove(user);
                //インデックス使用状況を変更
                n_indexUsage.Set(index, false);
                //準備完了メンバーから削除
                n_allisReady.Remove(user);

                //全ユーザーに変更を通知
                RPC_ReceiptUsage(user, index);

                Debug.Log("ユーザーが抜けました");
            }
        }

        /// <summary>
        /// 空いているインデックス番号を返す
        /// </summary>
        /// <returns></returns>
        private int GetIndexNum()
        {
            for(int i = 0;i < n_indexUsage.Length;i++)
            {
                if (n_indexUsage[i]) continue;
                n_indexUsage.Set(i, true);

                return i;
            }
            //全て使っている場合は-1
            return -1;
        }

        /// <summary>
        /// インデックス番号を取得
        /// </summary>
        /// <param name="user"></param>
        /// <param name="index"></param>
        [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
        private void RPC_ReceiptUsage(PlayerRef user, int index)
        {
            if (user == Runner.LocalPlayer)
            {
                if (index >= 0 && index < m_players.Length)
                {
                    m_player = m_players[index];
                }
            }

            //全ユーザーでプレイヤーをオンに
            for (int i = 0; i < m_players.Length && i < n_indexUsage.Length; i++)
            {
                if (m_players[i] != null)
                {
                    m_players[i].SetActive(n_indexUsage[i]);
                }
            }


            //名前設定
            for(int i = 0;i < m_playerName.Length;i++)
            {
                if (m_playerName[i] != null)
                {
                    //オブジェクトをオンに
                    m_playerName[i].gameObject.SetActive(n_indexUsage[i]);
                }
            }

            //名前を取得して設定
            var userdatas = m_gameLauncher.GetAllUserData();
            var result = n_usertoIndex.Where(kvp => kvp.Key != null && userdatas.ContainsKey(kvp.Key));
            foreach (var data in result)
            {
                var userdata = userdatas[data.Key];
                m_playerName[data.Value].text = userdata.m_name.ToString();
            }


        }

        public void Ready()
        {
            m_gameLauncher.SetLoadScreen(LoadType.Load1);
            //完了フラグを反転
            m_isReady = !m_isReady;
            //ホストへの送信
            RPC_Ready(Runner.LocalPlayer, m_isReady);
        }

        /// <summary>
        /// 準備完了通知を受け取り
        /// </summary>
        [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
        public void RPC_Ready(PlayerRef user,bool ready)
        {
            Debug.Log("準備完了受付");
            if(n_allisReady.ContainsKey(user))
            {
                n_allisReady.Set(user, ready);
                Debug.Log($"{user}の準備完了状態:{ready}");
            }
            
            //全員が準備完了ならゲーム開始
            if(n_allisReady.All(kvp => kvp.Value))
            {
                GameStart();
            }
        }

        public void GameStart()
        {
            if(Object.HasStateAuthority)
            {
                //遊ぶ人数を設定
                m_gameLauncher.SetStartingNumber(Runner.ActivePlayers.Count());
                //シーンを変更
                Runner.LoadScene(SceneRef.FromIndex(2), LoadSceneMode.Single);
            }
        }

        public async void Exit()
        {
            await m_gameLauncher.LeaveRoom();

            SceneManager.LoadScene(Config.ENTRANCE_SCENE_NUMBER, LoadSceneMode.Single);
        }
    }
}