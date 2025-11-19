using UnityEngine;

public class DamageAnim : MonoBehaviour
{
    public void OnDamageAnimEnd()
    {
        Destroy(gameObject);
    }
}
