using UnityEngine;

public class Dig : VoxelWorld.BaseAttack
{
    [SerializeField] GameObject m_holder;

    public void DigPoint(Vector3 position,Vector3 direction)
    {
        //Vector3 effectDirection = Vector3.zero;

        //if(m_holder != null)
        //{
        //    effectDirection = m_holder.transform.position - transform.position;
        //    effectDirection.y = 1.0f;
        //}
        
        //effectDirection.Normalize();
        direction.Normalize();

        AttackAtPosition(position,direction);
    }
}
