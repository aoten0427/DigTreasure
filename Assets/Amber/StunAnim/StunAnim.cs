using UnityEngine;

public class StunAnim : MonoBehaviour
{
    [SerializeField] float _rotSpd;

    private void Update()
    {
        Vector3 rot = transform.rotation.eulerAngles;
        rot.y += _rotSpd;
        transform.rotation = Quaternion.Euler(rot);
    }
}
