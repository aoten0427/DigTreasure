using UnityEngine;

public class RoomPlayer : MonoBehaviour
{
    [SerializeField]
    Animator m_animator;


    public void Entry()
    {
        m_animator.SetBool("isParticipation", true);
    }
}
