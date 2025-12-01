using System.Collections;
using UnityEngine;

public class Dig : VoxelWorld.BaseAttack,IPlayerAttack
{
    [SerializeField] GameObject m_holder;
    [SerializeField] float m_colliderAppearanceTime = 0.5f;
    [SerializeField] PlayerProto m_player;
    [SerializeField] PlayerCombat m_attacker;

    public PlayerProto Player => m_player;
    public PlayerCombat Attacker => m_attacker;

    public void DigPoint(Vector3 position,Vector3 direction)
    {
        direction.Normalize();

        StartCoroutine(ColliderAppearance());

        AttackAtPosition(position,direction);
    }

    IEnumerator ColliderAppearance()
    {
        Collider collider = GetComponent<Collider>();
        if(collider == null)yield break;

        collider.enabled = true;

        yield return new WaitForSeconds(m_colliderAppearanceTime);

        collider.enabled = false;
    }
}
