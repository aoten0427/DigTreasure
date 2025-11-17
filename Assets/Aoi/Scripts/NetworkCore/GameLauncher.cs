using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Linq;
using static Unity.Collections.Unicode;
using Unity.Collections.LowLevel.Unsafe;
using UniRx;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.Video;
using System.Threading.Tasks;
using VoxelWorld;

namespace NetWork
{
    public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Network Settings")]
        [SerializeField] private NetworkRunner networkRunnerPrefab;
        [SerializeField] private bool m_autoConnect = false;
        [SerializeField] private bool m_isLog = false;

        //
        private NetworkRunner m_networkRunner;
        private string m_roomName;

        SessionList m_sessionList;

        // サービス
        [SerializeField] private NetworkSpawnService m_spawnService;
        [SerializeField] private ReadyService m_readyService;
        [SerializeField] private LoadingService m_loadingService;
        [SerializeField] private UserDataService m_userDataService;

        public static GameLauncher s_instance;

        public static NetWork.GameLauncher Instance { 
            get {
                if (s_instance == null) Debug.LogError("[GameLauncher]インスタンスがありません");
                return s_instance;
            }
            private set { s_instance = value; } }
        public NetworkRunner Runner => m_networkRunner;

        #region 各コールバック
        public event Action<NetworkRunner, NetworkObject, PlayerRef> OnObjectExitAOI;
        public event Action<NetworkRunner, NetworkObject, PlayerRef> OnObjectEnterAOI;
        public event Action<NetworkRunner, PlayerRef> OnPlayerJoined;
        public event Action<NetworkRunner, PlayerRef> OnPlayerLeft;
        public event Action<NetworkRunner, NetworkInput> OnInput;
        public event Action<NetworkRunner, PlayerRef, NetworkInput> OnInputMissing;
        public event Action<NetworkRunner, ShutdownReason> OnShutdown;
        public event Action<NetworkRunner> OnConnectedToServer;
        public event Action<NetworkRunner, NetDisconnectReason> OnDisconnectedFromServer;
        public event Action<NetworkRunner, NetworkRunnerCallbackArgs.ConnectRequest, byte[]> OnConnectRequest;
        public event Action<NetworkRunner, NetAddress, NetConnectFailedReason> OnConnectFailed;
        public event Action<NetworkRunner, SimulationMessagePtr> OnUserSimulationMessage;
        public event Action<NetworkRunner, List<SessionInfo>> OnSessionListUpdated;
        public event Action<NetworkRunner, Dictionary<string, object>> OnCustomAuthenticationResponse;
        public event Action<NetworkRunner, HostMigrationToken> OnHostMigration;
        public event Action<NetworkRunner, PlayerRef, ReliableKey, ArraySegment<byte>> OnReliableDataReceived;
        public event Action<NetworkRunner, PlayerRef, ReliableKey, float> OnReliableDataProgress;
        public event Action<NetworkRunner> OnSceneLoadDone;
        public event Action<NetworkRunner> OnSceneLoadStart;
        #endregion

        //ネットワークオブジェクト生成終了時の関数登録
        public void AddOnNetworkObjectSpawned(Action<NetworkRunner> action) { m_spawnService.OnNetworkObjectsSpawned += action; }
        public void RemoveOnNetworkObjectSpawned(Action<NetworkRunner> action) { m_spawnService.OnNetworkObjectsSpawned -= action; }
        //全ユーザーの準備完了した際の関数登録
        public void AddOnAllUserReady(Action action) { m_readyService.OnAllUserReady += action;}
        public void RemoveOnAllUserReady(Action action) { m_readyService.OnAllUserReady -= action; }
        //ユーザーデータが変わった際の関数登録
        public void AddOnUserDataChange(Action<IReadOnlyDictionary<PlayerRef, NetworkUserData>> action) 
        { m_userDataService.OnDataChangeAction += action; }
        public void RemoveOnDataChangeAction(Action<IReadOnlyDictionary<PlayerRef, NetworkUserData>> action)
        { m_userDataService.OnDataChangeAction -= action; }

        //ゲームランチャーからユーザーデータ取得のためのプロパティ
        public NetworkUserData UserData { get {  return m_userDataService.UserData; }set { m_userDataService.UserData = value; } }
        //全ユーザーデータ読み込み
        public IReadOnlyDictionary<PlayerRef, NetworkUserData> GetAllUserData() { return m_userDataService.GetAllUserData(); }

        /// <summary>
        /// 起動処理
        /// </summary>
        private async void Awake()
        {
            if (!MakeInstance()) return;

            InitializeServices();

            if (m_autoConnect)
            {
                //ConnectFusion();
                await JoinRoom("Room1", 4);
            }
        }

        /// <summary>
        /// サービスを初期化
        /// </summary>
        private void InitializeServices()
        {
            m_sessionList = new SessionList(this);

            if (m_spawnService == null) Debug.LogWarning("[GameLauncher]SpawnServiceがありません");
            if (m_readyService == null) Debug.LogWarning("[GameLauncher]ReadyServiceがありません");
            if (m_loadingService == null) Debug.LogWarning("[GameLauncher]LoadingServiceがありません");
            if (m_userDataService == null) Debug.LogWarning("[GameLauncher]UserDataServiceがありません");

            //ネットワークに関係ないのでここで初期化
            m_loadingService?.Initialize(m_networkRunner);

            if (m_isLog) Debug.Log("[GameLauncher] サービスを初期化しました。");
        }


        /// <summary>
        /// インスタンス作成
        /// </summary>
        /// <returns></returns>
        private bool MakeInstance()
        {
            if (s_instance != null && s_instance != this)
            {
                Destroy(gameObject);
                return false;
            }
            s_instance = this;
            return true;
        }

        public bool IsConect()
        {
            return (m_networkRunner != null)&&m_networkRunner.IsRunning;
        }

        private async void OnApplicationQuit()
        {
            // アプリケーション終了時に自動的にルームから退出
            if (m_networkRunner != null && m_networkRunner.IsRunning)
            {
                Debug.Log("[GameLauncher] アプリケーション終了により、ルームから退出します");
                await m_networkRunner.Shutdown();
            }

            await m_sessionList.Disconnect();
        }

        /// <summary>
        /// 特定のルームへ参加
        /// </summary>
        /// <param name="roomName"></param>
        /// <param name="maxPlayers"></param>
        public async Task<bool> JoinRoom(string roomName, int maxPlayers = 4)
        {
            if (IsConect())
            {
                if(m_isLog)Debug.Log($"[GameLauncher] 既に接続中です。現在のルーム: {m_roomName}");
                return false;
            }

            m_roomName = roomName;

            // NetworkRunnerの初期化
            if (m_networkRunner == null)
            {
                GameObject runnerObj = new GameObject("NetworkRunner");
                m_networkRunner = runnerObj.AddComponent<NetworkRunner>();
                m_networkRunner.AddCallbacks(this);
            }

            //永続化
            DontDestroyOnLoad(m_networkRunner.gameObject);
            DontDestroyOnLoad(gameObject);

            //起動
            var startGameArgs = new StartGameArgs()
            {
                GameMode = GameMode.Shared,
                SessionName = roomName,
                PlayerCount = maxPlayers,
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
                SessionProperties = new Dictionary<string, SessionProperty>()
                {
                    { "IsVisible", true }
                }
            };

            //結果を確認
            var result = await m_networkRunner.StartGame(startGameArgs);
            if (result.Ok)
            {
                if(m_isLog)Debug.Log($"[GameLauncher]ルーム '{roomName}' への接続に成功しました");
            }
            else
            {
                Debug.LogWarning($"[GameLauncher]ルーム '{roomName}' への接続に失敗: {result.ShutdownReason}");
                return false;
            }


            // サービスの初期化
            m_readyService?.Initialize(m_networkRunner);
            m_userDataService?.Initialize(m_networkRunner);
            

            return true;
        }

        /// <summary>
        /// 現在のルームから退出
        /// </summary>
        public async Task LeaveRoom()
        {
            if(m_networkRunner == null&&m_networkRunner.IsRunning)
            {
                Debug.Log("[GameLauncher]接続されていません");
                return;
            }

            await m_networkRunner.Shutdown();
            if (m_isLog) Debug.Log($"[GameLauncher]ルーム{m_roomName}から退出");
            m_loadingService.DataReset();
            m_readyService.DataReset();
            m_userDataService?.DataReset();

            m_roomName = null;
        }


        /// <summary>
        /// 存在するセッションんの更新
        /// </summary>
        /// <returns></returns>
        public async Task UpdateSessions()
        {
            //現在どこかに接続している場合は行わない
            if (m_networkRunner != null && m_networkRunner.IsRunning) return;
            //更新
            await m_sessionList.UpdateSessions();
            if (m_isLog) Debug.Log("[GameLauncher]セッションデータを更新");
        }

        /// <summary>
        /// セッションデータを全て渡す
        /// </summary>
        /// <returns></returns>
        public Dictionary<string, SessionInfo> GetSessionInfo()
        {
            return m_sessionList.AvailableSessions;
        }

        /// <summary>
        /// セッション情報を返す
        /// </summary>
        /// <param name="roomName"></param>
        /// <returns></returns>
        public SessionInfo GetSessionInfo(string roomName)
        {
            return m_sessionList.GetSessionInfo(roomName);
        }

        void INetworkRunnerCallbacks.OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            OnObjectExitAOI?.Invoke(runner, obj, player);
        }
        void INetworkRunnerCallbacks.OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
        {
            OnObjectEnterAOI?.Invoke(runner, obj, player);
        }
        void INetworkRunnerCallbacks.OnPlayerJoined(NetworkRunner runner, PlayerRef player)
        {

            OnPlayerJoined?.Invoke(runner, player);
        }
        void INetworkRunnerCallbacks.OnPlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            

            StartCoroutine(PlayerLeft(runner, player));
        }

        /// <summary>
        /// ホストか確認を行いユーザーが抜けた際の処理を行う
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        /// <returns></returns>
        IEnumerator PlayerLeft(NetworkRunner runner, PlayerRef player)
        {
            //自身が現時点でホストの場合はすぐさま処理を実行
            if(runner.IsSharedModeMasterClient)
            {
                PlayerLeftAction(runner, player);
                yield break;
            }
            else//ホスト出ない場合ラグがある可能性があるので待つ
            {
                float statitime = 0;
                float waittime = 3.0f;
                while(statitime > waittime)
                {
                    statitime += Time.deltaTime;
                    yield return new WaitForSeconds(0.1f);
                    if(runner.IsSceneManagerBusy)
                    {
                        yield return new WaitForSeconds(1.0f);
                        PlayerLeftAction(runner, player);
                    }
                }
            }
        }

        /// <summary>
        /// ユーザーが抜けた際に呼ばれるイベント
        /// </summary>
        /// <param name="runner"></param>
        /// <param name="player"></param>
        private void PlayerLeftAction(NetworkRunner runner, PlayerRef player)
        {
            m_spawnService.SpawnNetworkObjects(runner, () =>
            {
                m_userDataService.PlayerLeft(player);
                OnPlayerLeft?.Invoke(runner, player);
            });
            
        }


        void INetworkRunnerCallbacks.OnInput(NetworkRunner runner, NetworkInput input)
        {
            OnInput?.Invoke(runner, input);
        }
        void INetworkRunnerCallbacks.OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
        {
            OnInputMissing?.Invoke(runner, player, input);
        }
        void INetworkRunnerCallbacks.OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
        {
            Debug.Log($"[GameLauncher.OnShutdown] GameLauncher Instance: {GetInstanceID()}, Reason: {shutdownReason}");
            OnShutdown?.Invoke(runner, shutdownReason);
        }
        void INetworkRunnerCallbacks.OnConnectedToServer(NetworkRunner runner)
        {
            OnConnectedToServer?.Invoke(runner);
        }
        void INetworkRunnerCallbacks.OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
        {
            OnDisconnectedFromServer?.Invoke(runner, reason);
        }
        void INetworkRunnerCallbacks.OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
        {
            OnConnectRequest?.Invoke(runner, request, token);
        }
        void INetworkRunnerCallbacks.OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
        {
            OnConnectFailed?.Invoke(runner, remoteAddress, reason);
        }
        void INetworkRunnerCallbacks.OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
        {
            OnUserSimulationMessage?.Invoke(runner, message);
        }
        void INetworkRunnerCallbacks.OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
        {
            OnSessionListUpdated?.Invoke(runner, sessionList);
        }
        void INetworkRunnerCallbacks.OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
        {
            OnCustomAuthenticationResponse?.Invoke(runner, data);
        }
        void INetworkRunnerCallbacks.OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
        {

        }

        void INetworkRunnerCallbacks.OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
        {
            OnReliableDataReceived?.Invoke(runner, player, key, data);
        }
        void INetworkRunnerCallbacks.OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
        {
            OnReliableDataProgress?.Invoke(runner, player, key, progress);
        }
        void INetworkRunnerCallbacks.OnSceneLoadDone(NetworkRunner runner)
        {


            // NetworkObjectのSpawn処理を実行
            m_spawnService.SpawnNetworkObjects(runner, () =>
            {
                m_readyService.OnSceneLoadDone(runner);
                m_userDataService.OnSceneLoadDone(runner);
            });

            OnSceneLoadDone?.Invoke(runner);

            m_loadingService.CompleteLoading();
        }

        void INetworkRunnerCallbacks.OnSceneLoadStart(NetworkRunner runner)
        {
            m_loadingService.OnSceneLoadStart(runner);
            OnSceneLoadStart?.Invoke(runner);
        }


        /// <summary>
        /// ロード中に行うイベントを登録
        /// </summary>
        public ReactiveProperty<float> AddLoadingEvent(float weight = 1.0f,string name = null)
        {
            return m_loadingService?.AddLoadingEvent(weight,name);
        }

        /// <summary>
        /// ロード画面のタイプを設定
        /// </summary>
        public void SetLoadScreen(LoadType loadType)
        {
            m_loadingService?.SetLoadScreen(loadType);
        }

        /// <summary>
        /// 開始人数を設定
        /// </summary>
        public void SetStartingNumber(int number)
        {
            m_readyService?.SetStartingNumber(number);
        }
    }
}
