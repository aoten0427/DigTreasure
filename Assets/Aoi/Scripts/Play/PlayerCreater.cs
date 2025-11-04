using UnityEngine;
using Fusion;
using System.Threading.Tasks;
using UniRx;
using VoxelWorld;
using Unity.Cinemachine;


[System.Serializable]
public struct PlayerSpownData
{ 
    [SerializeField]public Vector3 Position;
    [SerializeField]public float RotatinY;
}


public class PlayerCreater : NetworkBehaviour,IPlayInitialize
{
    NetWork.GameLauncher m_gameLauncher;
    //ユーザーの生成場所データ
    [SerializeField]
    PlayerSpownData[] m_spownDatas = new PlayerSpownData[4];
    //生成場所の管理
    [Networked, Capacity(4)]
    private NetworkArray<bool> m_spownDataUsage => default;
    //生成ID
    int m_id = 0;

    //プレイヤープレハブ
    [SerializeField] private NetworkPrefabRef playerPrefab;
    [SerializeField] private GameObject m_camera;
    //生成優先度
    public InitializationPriority Priority => InitializationPriority.PlayerCreate;

    public string Name => "PlayerCreate";

    //生成処理完了フラグ
    bool m_isCreate = false;

    PlayManager m_playManager;

    /// <summary>
    /// 初期化呼び出し
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public async Task InitializeAsync(ReactiveProperty<float> task = null)
    {
        
        m_gameLauncher = NetWork.GameLauncher.Instance;
        

        RPC_RequestDataID(Runner.LocalPlayer);
        
        while(!m_isCreate)
        {
            await Task.Yield();
        }

        if (task != null) task.Value = 1.0f;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_RequestDataID(PlayerRef user)
    {
        for (int i = 0; i < m_spownDataUsage.Length; i++)
        {
            if (m_spownDataUsage[i]) continue;

            m_spownDataUsage.Set(i, true);
            RPC_RespondDataID(user, i);
            return;
        }

        Debug.LogWarning($"{user}ユーザー生成場所がありません");
        m_isCreate = true;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_RespondDataID(PlayerRef target, int id)
    {
        if (Runner.LocalPlayer == target)
        {
            m_id = id;
            Debug.Log($"自分へのレスポンスを受信: {id}");
            CreatePlayer();
            m_isCreate = true;
        }
    }

    private void CreatePlayer()
    {
        PlayerSpownData data = m_spownDatas[m_id];
        var userData = m_gameLauncher.UserData;

        var rand = UnityEngine.Random.insideUnitCircle * 5f;
        var spawnPosition = data.Position;
        var rotation = Quaternion.Euler(0f, data.RotatinY, 0f);

        NetworkObject spawnedPlayer = Runner.Spawn(playerPrefab, spawnPosition, rotation, onBeforeSpawned: (_, networkObject) =>
        {
            var player = networkObject.GetComponent<PlayerProto>();
            player.NickName = userData.m_name;
            player.SetPlayManager(m_playManager);
            networkObject.gameObject.name = Runner.LocalPlayer.ToString();
        });

        Debug.Log("プレイヤー生成");



        var camera = FindFirstObjectByType<CinemachineOrbitalFollow>();
        if(camera != null)
        {
            Debug.Log("カメラを見つけました");
            m_camera.transform.position = Vector3.zero;
            camera.HorizontalAxis.Value = data.RotatinY;
        }
        
            
    }

    public void SetManager(PlayManager manager)
    {
        m_playManager = manager;
    }
}
