using UnityEngine;
using Fusion;
using System.Collections;

public class LockOnIcon : NetworkBehaviour
{
    [SerializeField] private float _immuneDuration;
    private IEnumerator _immuneCoroutine = null;
    private SpriteRenderer _renderer;

    public override void Spawned()
    {
        base.Spawned();
        if (HasStateAuthority)
        {
            _renderer = GetComponent<SpriteRenderer>();
            Debug.Log("------------RENDERER: " + (_renderer != null));
        }
    }
    private void StartImmunity()
    {
        //remove icon etc
        if (_immuneCoroutine != null)
            return;
        _immuneCoroutine = MakeImmune();
        StartCoroutine(_immuneCoroutine);
    }
    private void Update()
    {
        if(HasStateAuthority)
            RpcToggleIcon(false, this, true);
    }
    private IEnumerator MakeImmune()
    {
        yield return new WaitForSeconds(_immuneDuration);
        _immuneCoroutine = null;
    }
    public void ToggleIcon(bool visible, bool init = false)
    {
        RpcToggleIcon(visible, this, init);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RpcToggleIcon(bool visible, LockOnIcon playerWithLock, bool init = false)
    {
        if (playerWithLock._renderer.enabled && !init)
        {
            //untarget combat
            Debug.Log("Should untarget");
        }
        _renderer.enabled = visible;
    }
}
