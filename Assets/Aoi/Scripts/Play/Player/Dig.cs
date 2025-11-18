using System.Collections;
using UnityEngine;

public class Dig : VoxelWorld.BaseAttack
{
    [SerializeField] GameObject m_holder;
    [SerializeField] float m_colliderAppearanceTime = 0.5f;

    public void DigPoint(Vector3 position,Vector3 direction)
    {
        direction.Normalize();

        ColliderAppearance();

        AttackAtPosition(position,direction,false);
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
