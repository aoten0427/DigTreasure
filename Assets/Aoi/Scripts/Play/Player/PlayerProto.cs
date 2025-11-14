using Fusion;
using System.Globalization;
using UnityEngine;


namespace Aoi
{

    public class PlayerProto : NetworkBehaviour
    {
        //名前
        [Networked] public NetworkString<_16> NickName { get; set; }
        //掘る場所
        [SerializeField] private GameObject digPoint;

        //動作可能か
        bool m_isAction = true;

        //リジッドボディ
        private Rigidbody rb;
        //コライダー
        private Collider playerCollider;
        //移動可能か
        private bool canMove = true;


        [SerializeField] private Dig m_dig;
        [SerializeField] private GameObject m_digpoint2;
        //掘り可能か
        private bool canDig = true;

        [Header("リジッドボディセッティング")]
        [SerializeField] private float moveSpeed = 5f;//移動速度
        [SerializeField] private float maxSpeed = 10f; //最大速度
        [SerializeField] private float jumpForce = 5f;//ジャンプ力
        [SerializeField] private float groundCheckDistance = 1.5f;//床の当たり判定
        [SerializeField] private LayerMask groundLayer;//地面レイヤー
        [SerializeField] private float drag = 5f;

        private bool isJump = false;//ジャンプフラグ

        private bool isGrounded;//地面についているか

        //ロックオン
        private bool needLockOnRot = false;
        private Quaternion lockOnRot;

        [SerializeField] PlayerCombat m_combat;
        [SerializeField] SurroundingsDig m_surroundingsDig;

        // Input System
        private GameController m_gameController;
        private Vector2 m_moveInput;

        private void OnEnable()
        {
            PlayerCombat.OnPlayerStunStart += DisableMove;
            PlayerCombat.OnPlayerStunEnd += EnableMove;
            PlayerCombat.OnPlayerBarrierStart += DisableMove;
            PlayerCombat.OnPlayerBarrierStart += DisableDig;
            PlayerCombat.OnPlayerBarrierEnd += EnableMove;
            PlayerCombat.OnPlayerBarrierEnd += EnableDig;
        }
        private void OnDisable()
        {
            PlayerCombat.OnPlayerStunStart -= DisableMove;
            PlayerCombat.OnPlayerStunEnd -= EnableMove;
            PlayerCombat.OnPlayerBarrierStart -= DisableMove;
            PlayerCombat.OnPlayerBarrierStart -= DisableDig;
            PlayerCombat.OnPlayerBarrierEnd -= EnableMove;
            PlayerCombat.OnPlayerBarrierEnd -= EnableDig;

            if (m_combat != null)
            {
                m_combat.OnPlayerAttack -= AttackDig;
                m_combat.OnPlayerDamage -= Blownaway;
            }

            // Input Systemのクリーンアップ
            if (m_gameController != null)
            {
                m_gameController.Play.Disable();
                m_gameController.Dispose();
            }
        }
        public override void Spawned()
        {
            rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.freezeRotation = true; // プレイヤーが倒れないように
            }

            playerCollider = GetComponent<Collider>();

            // Input System初期化
            if (HasStateAuthority)
            {
                m_gameController = new GameController();

                // 移動入力
                m_gameController.Play.Move.performed += ctx => m_moveInput = ctx.ReadValue<Vector2>();
                m_gameController.Play.Move.canceled += ctx => m_moveInput = Vector2.zero;

                // ジャンプ入力
                m_gameController.Play.Jump.performed += ctx =>
                {
                    if (isGrounded && !isJump && m_isAction)
                    {
                        isJump = true;
                    }
                };

                // 掘る入力
                m_gameController.Play.Dig.performed += ctx =>
                {
                    if (canDig && m_isAction)
                    {
                        Dig(digPoint.transform.position);
                    }
                };

                m_gameController.Play.Enable();
            }

            var view = GetComponent<PlayerViewProto>();
            view.SetNickName(NickName.Value);
            if (HasStateAuthority)
            {
                view.MakeCameraTarget();
            }

            if (m_combat != null)
            {
                m_combat.OnPlayerAttack += AttackDig;
                m_combat.OnPlayerDamage += Blownaway;
            }
        }

        public void SetPlayManager(PlayManager playManager)
        {
            if (playManager == null) return;
            m_isAction = false;
            playManager.OnGameStartAction += () => m_isAction = true;
            playManager.OnGameEndAction += () => m_isAction = false;
        }

        private void Update()
        {
            if (!Object.HasStateAuthority) return;
            if (!m_isAction) return;

            // 地面判定
            CheckGrounded();
        }

        public override void FixedUpdateNetwork()
        {
            if (rb == null) return;
            if (!HasStateAuthority) return;
            if (!m_isAction) return;

            //Debug.Log("入力受付");



            var cameraRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);

            if (canMove)
            {
                // 移動入力（GameControllerのVector2を使用）
                var inputDirection = new Vector3(m_moveInput.x, 0f, m_moveInput.y);
                Vector3 moveDirection = cameraRotation * inputDirection;

                // 現在の水平速度を取得
                Vector3 currentVelocity = rb.linearVelocity;
                Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

                // 水平移動
                if (moveDirection.magnitude > 0.1f)
                {
                    // 最大速度化
                    if (horizontalVelocity.magnitude < maxSpeed)
                    {
                        Vector3 force = moveDirection.normalized * moveSpeed;
                        rb.AddForce(force, ForceMode.Force);
                    }
                }
                else
                {
                    // 摩擦
                    Vector3 dragForce = -horizontalVelocity * drag;
                    rb.AddForce(dragForce, ForceMode.Force);
                }

                // 回転 - Rigidbodyで制御
                if (needLockOnRot)
                {
                    rb.MoveRotation(lockOnRot);
                }
                else if (moveDirection.magnitude > 0.1f)
                {
                    // 移動方向を向く
                    Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                    Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, Runner.DeltaTime * 10f);
                    rb.MoveRotation(newRotation);
                }

                // ジャンプ
                if (isJump)
                {
                    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    isJump = false;
                }
            }
            else
            {
                // スタン中は速度を減衰
                Vector3 velocity = rb.linearVelocity;
                velocity.x *= 0.9f;
                velocity.z *= 0.9f;
                rb.linearVelocity = velocity;
                Debug.Log("Stunned");
            }
        }

        private void CheckGrounded()
        {
            if (playerCollider == null)
            {
                isGrounded = false;
                return;
            }

            // Colliderの底の中心位置
            Vector3 boundsBottom = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);

            // Colliderのサイズから適切なオフセット距離を計算（端より少し内側）
            float horizontalOffset = playerCollider.bounds.extents.x * 0.8f;
            float forwardOffset = playerCollider.bounds.extents.z * 0.8f;

            // 5箇所のRaycast開始位置
            Vector3[] rayPositions = new Vector3[5]
            {
                boundsBottom,                                              // 中央
                boundsBottom + transform.forward * forwardOffset,          // 前
                boundsBottom - transform.forward * forwardOffset,          // 後
                boundsBottom + transform.right * horizontalOffset,         // 右
                boundsBottom - transform.right * horizontalOffset          // 左
            };

            // いずれか1つでも地面に接していればtrue
            isGrounded = false;
            RaycastHit hit;

            for (int i = 0; i < rayPositions.Length; i++)
            {
                bool hitGround = Physics.Raycast(rayPositions[i], Vector3.down, out hit, groundCheckDistance, groundLayer);

                // デバッグ用の視覚化
                Debug.DrawRay(rayPositions[i], Vector3.down * groundCheckDistance, hitGround ? Color.green : Color.red);

                if (hitGround)
                {
                    isGrounded = true;
                    break;
                }
            }
        }

        private void OnDrawGizmos()
        {
            // 地面判定のRayを可視化
            Gizmos.color = isGrounded ? Color.green : Color.red;

            Vector3 startPos = transform.position;
            if (playerCollider != null)
            {
                // Colliderの底から開始
                startPos = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
            }

            Vector3 endPos = startPos + Vector3.down * groundCheckDistance;
            Gizmos.DrawLine(startPos, endPos);
            Gizmos.DrawWireSphere(endPos, 0.1f);
            Gizmos.DrawWireSphere(startPos, 0.05f); // 開始位置も表示
        }

        private void Dig(Vector3 point)
        {
            if (m_dig == null) return;
            m_dig.DigPoint(point, transform.position - point + new Vector3(0, 1, 0));

            // pointが掘る位置(プレイヤーの前方、高さは足元)になってる
        }

        //機能オン・オッフ
        private void DisableMove()
        {
            canMove = false;
        }
        private void EnableMove()
        {
            canMove = true;
        }
        private void DisableDig()
        {
            canDig = false;
        }
        private void EnableDig()
        {
            canDig = true;
        }

        //ロックオン
        public void SetRotateTarget(bool lockOn, Quaternion targetRot)
        {
            needLockOnRot = lockOn;
            lockOnRot = targetRot;
        }

        public void AttackDig()
        {
            Dig(m_digpoint2.transform.position);
        }

        //吹き飛ばされ
        public void Blownaway()
        {
            m_surroundingsDig.Blownaway();
        }
    }

}