using Fusion;
using UnityEngine;

public class NetworkItem : NetworkBehaviour
{
    //‚±‚ÌƒAƒCƒeƒ€‚Ì•ÛÒ
    [Networked] PlayerRef m_holder { get; set; }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void RPC_HolderChange(PlayerRef user)
    {
        if(Object.HasStateAuthority)
        {
            m_holder = user;
        }
        HolderChangeAction(user == Runner.LocalPlayer);
    }

    public virtual void HolderChangeAction(bool isholder)
    {

    }


}
