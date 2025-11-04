using UnityEngine;

namespace VoxelWorld
{
    public class Player : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 5f;   // 前進・後退の速度
        [SerializeField] float rotateSpeed = 180f; // 回転の速度（度/秒）


        private Rigidbody m_rigidbody;


        void Start()
        {
            // Rigidbodyコンポーネントを取得（なければ追加）
            m_rigidbody = GetComponent<Rigidbody>();
            if (m_rigidbody == null)
            {
                m_rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            // CapsuleColliderがなければ追加
            if (GetComponent<CapsuleCollider>() == null)
            {
                var capsuleCollider = gameObject.AddComponent<CapsuleCollider>();
                capsuleCollider.height = 2f;
                capsuleCollider.radius = 0.5f;
                capsuleCollider.center = new Vector3(0, 1f, 0);
            }
        }

        void Update()
        {
            // 前後移動
            float move = Input.GetAxis("Vertical"); // W=1, S=-1
            Vector3 moveDirection = transform.forward * move * moveSpeed;
            m_rigidbody.linearVelocity = new Vector3(moveDirection.x, m_rigidbody.linearVelocity.y, moveDirection.z);

            // 左右回転
            float rotate = Input.GetAxis("Horizontal"); // A=-1, D=1
            transform.Rotate(Vector3.up * rotate * rotateSpeed * Time.deltaTime);


        }
    } 
}
