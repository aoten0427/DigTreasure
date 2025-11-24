using Fusion;
using NetWork;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 王冠の生成、移動管理
/// </summary>
public class Crown : NetworkBehaviour
{
    [SerializeField] private GameObject m_crown;
    GameLauncher m_gameLauncher;

    private void Start()
    {
        m_gameLauncher = GameLauncher.Instance;
        m_gameLauncher.AddOnUserDataChange(CrownUpdate);
        m_crown.SetActive(false);
    }

    private void OnDestroy()
    {
        m_gameLauncher.RemoveOnDataChangeAction(CrownUpdate);
    }

    private void CrownUpdate(IReadOnlyDictionary<PlayerRef, NetworkUserData> updateData)
    {
        if (!Object.HasStateAuthority) return;

        //一番ポイントを持っているプレイヤーを取得
         var max = updateData.OrderByDescending(kvp => kvp.Value.m_treasurePoint).FirstOrDefault();
        var userdata = updateData[Runner.LocalPlayer];

        bool isCrown = (max.Key == Runner.LocalPlayer) || (max.Value.m_treasurePoint == userdata.m_treasurePoint);

        

        RPC_CrownChange(isCrown);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_CrownChange(bool crownActive)
    {
        m_crown.SetActive(crownActive);
    }
}
