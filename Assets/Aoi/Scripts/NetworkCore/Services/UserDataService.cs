using Fusion;
using NetWork;
using System.Collections.Generic;
using System;
using UnityEngine;

public class UserDataService : MonoBehaviour
{
    [SerializeField] private NetworkPrefabRef m_userdataManagerPrefab;
    [SerializeField] private bool m_isLog = false;

    //自身のユーザーデータ
    private NetworkUserData m_userData = new NetworkUserData();

    // Inspector表示用（デバッグ専用）
    [Header("Debug Info (Read Only)")]
    [SerializeField, Tooltip("ユーザーID")] private int m_debugId;
    [SerializeField, Tooltip("ユーザー名")] private string m_debugName;
    [SerializeField, Tooltip("宝物の数")] private int m_debugTreasureCount;
    [SerializeField, Tooltip("宝物ポイント")] private int m_debugTreasurePoint;
    [SerializeField, Tooltip("掘削ポイント")] private int m_debugDigPoint;
    public NetworkUserData UserData
    {
        get { return m_userData; }
        //データの変更の際は全ユーザーに伝えるようにする
        set
        {
            m_userData = value;
            if (m_userDataManager == null)
            {
                AcquisitionReadyManager();
            }
            if (m_userDataManager != null) m_userDataManager.RPC_ChangeUserData(m_runner.LocalPlayer, UserData);
        }
    }

    private NetworkRunner m_runner;
    [SerializeField]private UserDataManager m_userDataManager;

    //データ変更時のアクション
    public event Action<IReadOnlyDictionary<PlayerRef, NetworkUserData>> OnDataChangeAction;

    /// <summary>
    /// サービスを初期化
    /// </summary>
    public void Initialize(NetworkRunner runner)
    {
        m_runner = runner;
        SpawnUserDataManager();
    }

    /// <summary>
    /// ユーザーデータマネージャー生成
    /// </summary>
    void SpawnUserDataManager()
    {
        if (m_runner == null)
        {
            Debug.LogWarning("[UserDataService] NetworkRunnerが初期化されていません。");
            return;
        }

        if (m_userdataManagerPrefab == null)
        {
            Debug.LogWarning("[UserDataService] Prefabがありません");
            return;
        }

        if (m_userDataManager != null) return;

        // ホスト以外はパス
        if (m_runner.IsSharedModeMasterClient)
        {
            m_userDataManager = m_runner.Spawn(m_userdataManagerPrefab).GetComponent<UserDataManager>();
            m_userDataManager.SetDataChangeAction(data => OnDataChangeAction?.Invoke(data));
            m_userDataManager.SetIDGet(id =>
            {
                m_userData.m_id = id;
            });
            m_userDataManager.m_isLog = m_isLog;
            //自身のデータを追加
            m_userDataManager.RPC_ChangeUserData(m_runner.LocalPlayer, m_userData);

            if (m_isLog) Debug.Log("[UserDataService] UserDataManagerをSpawnしました。");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="runner"></param>
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (m_userDataManager == null) AcquisitionReadyManager();
        if (m_isLog) Debug.Log("[UserDataService] 準備完了を通知しました。");
    }

    /// <summary>
    /// ホスト以外の取得
    /// </summary>
    private void AcquisitionReadyManager()
    {
        if (m_userDataManager != null) return;

        m_userDataManager = FindFirstObjectByType<UserDataManager>();

        if (m_userDataManager == null) return;

        // NetworkBehaviourのObject nullチェック（完全に初期化されているか確認）
        if (m_userDataManager.Object == null)
        {
            m_userDataManager = null;
            return;
        }

        //自身のデータを追加
        m_userDataManager.SetDataChangeAction(data => OnDataChangeAction?.Invoke(data));
        m_userDataManager.SetIDGet(id =>
        {
            m_userData.m_id = id;
        });
        m_userDataManager.RPC_ChangeUserData(m_runner.LocalPlayer, m_userData);
    }

    /// <summary>
    /// 動的にデータ読み込み
    /// </summary>
    /// <returns></returns>
    public IReadOnlyDictionary<PlayerRef, NetworkUserData> GetAllUserData()
    {
        if(m_userDataManager == null) return null;
        return m_userDataManager.GetAllUserData();
    }

    /// <summary>
    /// デバッグ用: Inspectorでデータを表示
    /// </summary>
    private void Update()
    {
        // Managerのnull状態チェック
        if (m_runner != null && m_userDataManager == null)
        {
            AcquisitionReadyManager();
        }

#if UNITY_EDITOR
        // Inspector表示用にデータを同期
        m_debugId = m_userData.m_id;
        m_debugName = m_userData.m_name.ToString();
        m_debugTreasureCount = m_userData.m_treasureCount;
        m_debugTreasurePoint = m_userData.m_treasurePoint;
        m_debugDigPoint = m_userData.m_digPoint;
        #endif
    }
}
