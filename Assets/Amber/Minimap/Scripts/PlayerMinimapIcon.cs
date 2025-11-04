using Fusion;
using UnityEngine;

public class PlayerMinimapIcon : NetworkBehaviour
{
    private SpriteRenderer _spriteRenderer;
    [SerializeField] private Color _playerColour;
    [SerializeField] private Camera _minimapCamera;

    public override void Spawned()
    {
        if (!HasStateAuthority)
            return;
        base.Spawned();
        _minimapCamera.enabled = true;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _spriteRenderer.color = _playerColour;
    }
}
