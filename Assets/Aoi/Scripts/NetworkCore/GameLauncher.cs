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

namespace NetWork
{
    public class GameLauncher : MonoBehaviour, INetworkRunnerCallbacks
    {
        [Header("Network Settings")]
        [SerializeField] private NetworkRunner networkRunnerPrefab;
        [SerializeField] private bool m_autoConnect = false;
        [SerializeField] private bool m_isLog = false;

        private NetworkRunner m_networkRunner;
        private bool m_isConnect = false;


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
        private void Awake()
        {
            if (!MakeInstance()) return;

            InitializeServices();

            if (m_autoConnect)
            {
                ConnectFusion();
            }
        }

        /// <summary>
        /// サービスを初期化
        /// </summary>
        private void InitializeServices()
        {
            if (m_spawnService == null) Debug.LogWarning("[GameLauncher]SpawnServiceがありません");
            if (m_readyService == null) Debug.LogWarning("[GameLauncher]ReadyServiceがありません");
            if (m_loadingService == null) Debug.LogWarning("[GameLauncher]LoadingServiceがありません");
            if (m_userDataService == null) Debug.LogWarning("[GameLauncher]UserDataServiceがありません");

            if (m_isLog) Debug.Log("[GameLauncher] サービスを初期化しました。");
        }

        /// <summary>
        /// ハートビートタイムアウト時の処理
        /// </summary>
        private void HandlePlayerTimeout(PlayerRef player)
        {
            // OnPlayerLeftイベントを発火
            OnPlayerLeft?.Invoke(m_networkRunner, player);
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

        /// <summary>
        /// Fusionへの接続
        /// </summary>
        public async void ConnectFusion()
        {
            //接続されていたラパス
            if (m_isConnect) return;

            //Runner生成
            var networkRunner = Instantiate(networkRunnerPrefab);
            m_networkRunner = networkRunner.GetComponent<NetworkRunner>();
            if (m_networkRunner == null) Debug.LogError("ネットワークランナーがありません", gameObject);



            //永続化
            DontDestroyOnLoad(networkRunner.gameObject);
            DontDestroyOnLoad(gameObject);

            networkRunner.AddCallbacks(this);

            var result = await networkRunner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Shared,
                SessionName = "VoxelWorldTest",
                Scene = SceneRef.FromIndex(SceneManager.GetActiveScene().buildIndex),
                SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()

            });


            m_isConnect = true;


            // サービスの初期化
            m_readyService?.Initialize(m_networkRunner);
            m_userDataService?.Initialize(m_networkRunner);
            m_loadingService?.Initialize(m_networkRunner);

            if (m_isLog) Debug.Log("[GameLauncher] Fusion接続完了", gameObject);
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
            OnPlayerLeft?.Invoke(runner, player);
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
            OnHostMigration?.Invoke(runner, hostMigrationToken);
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

            OnSceneLoadStart?.Invoke(runner);

            // NetworkObjectのSpawn処理を実行
            m_spawnService.SpawnNetworkObjects(runner, () =>
            {
                m_readyService.OnSceneLoadDone(runner);
                m_userDataService.OnSceneLoadDone(runner);
                m_loadingService.CompleteLoading();
            });

            OnSceneLoadDone?.Invoke(runner);


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
