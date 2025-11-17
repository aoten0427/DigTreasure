using Fusion;
using UnityEngine;

public class TPlayer : NetworkBehaviour
{
    [SerializeField] private GameObject obj;

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (Input.GetKeyUp(KeyCode.Space))
        {
            RPC_ToggleObj();
        }
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ToggleObj()
    {
        obj.SetActive(!obj.activeSelf);
    }
}
