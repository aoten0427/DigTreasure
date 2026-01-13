using System.Collections;
using UnityEngine;

/// <summary>
/// プレイヤー移動ロジッククラス
/// </summary>
public class PlayerMovementLogic : MonoBehaviour
{
    [Header("基本移動パラメータ")]
    [SerializeField] private float m_moveSpeed = 5f;
    [SerializeField] private float m_maxSpeed = 10f;
    [SerializeField] private float m_acceleration = 10f;
    [SerializeField] private float m_deceleration = 15f;
    [SerializeField] private float m_drag = 5f;
    [SerializeField] private float m_rotationSpeed = 10f;

    [Header("空中制御")]
    [SerializeField] private float m_airControl = 0.5f;

    [Header("ジャンプパラメータ")]
    [SerializeField] private float m_jumpForce = 5f;

    [Header("地面判定")]
    [SerializeField] private float m_groundCheckDistance = 1.5f;
    [SerializeField] private LayerMask m_groundLayer;

    [Header("段差乗り越え")]
    [SerializeField] private float m_maxStepHeight = 0.3f;
    [SerializeField] private float m_stepUpForce = 8f;
    [SerializeField] private float m_stepCheckDistance = 0.5f;
    [SerializeField] private float m_stepUpCooldown = 0.2f;

    [Header("重力設定")]
    [SerializeField] private float m_additionalGravity = 10f;
    [SerializeField] private float m_maxFallSpeed = -20f;

    //PlayerManager参照
    private PlayerManager m_manager;

    //物理コンポーネント
    private Rigidbody m_rigidbody;
    private Collider m_collider;

    //状態
    private bool m_isGrounded;
    private bool m_canMove = true;
    private bool m_wasGrounded;
    private Vector2 m_moveInput;
    private float m_lastStepUpTime;

    //ロックオン
    private bool m_needLockOnRot;
    private Quaternion m_lockOnRot;

    //プロパティ
    public bool IsGrounded => m_isGrounded;
    public bool CanMove
    {
        get => m_canMove;
        set => m_canMove = value;
    }

    /// <summary>
    /// 初期化
    /// </summary>
    public void Initialize(PlayerManager manager)
    {
        m_manager = manager;
        m_rigidbody = manager.Rigidbody;
        m_collider = manager.Collider;

        if (m_rigidbody != null)
        {
            m_rigidbody.freezeRotation = true;
        }

        //イベント購読
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart += DisableMovement;
            m_manager.Events.OnStunEnd += EnableMovementDelay;
            m_manager.Events.OnBarrierStart += DisableMovement;
            m_manager.Events.OnBarrierEnd += EnableMovement;
        }
    }

    private void OnDestroy()
    {
        //イベント解除
        if (m_manager?.Events != null)
        {
            m_manager.Events.OnStunStart -= DisableMovement;
            m_manager.Events.OnStunEnd -= EnableMovementDelay;
            m_manager.Events.OnBarrierStart -= DisableMovement;
            m_manager.Events.OnBarrierEnd -= EnableMovement;
        }
    }

    private void Update()
    {
        CheckGrounded();

        //地面状態変更イベント
        if (m_isGrounded != m_wasGrounded)
        {
            m_manager?.Events?.InvokeGroundedChanged(m_isGrounded);
            m_wasGrounded = m_isGrounded;
        }
    }

    private void FixedUpdate()
    {
        if (m_rigidbody == null) return;

        var cameraRotation = Quaternion.Euler(0f, Camera.main.transform.rotation.eulerAngles.y, 0f);
        ProcessMovement(m_moveInput, cameraRotation, Time.fixedDeltaTime);
    }

    /// <summary>
    /// 移動入力を設定
    /// </summary>
    public void SetMoveInput(Vector2 moveInput)
    {
        m_moveInput = moveInput;
    }

    /// <summary>
    /// ジャンプ試行
    /// </summary>
    public void TryJump()
    {
        if (m_canMove && m_isGrounded)
        {
            ProcessJump(true);
            m_manager?.Events?.InvokeJump();
        }
    }

    /// <summary>
    /// 地面判定
    /// </summary>
    public void CheckGrounded()
    {
        if (m_collider == null)
        {
            m_isGrounded = false;
            return;
        }

        Vector3 boundsBottom = m_collider.bounds.center - new Vector3(0, m_collider.bounds.extents.y, 0);
        boundsBottom.y += 0.3f;

        float horizontalOffset = m_collider.bounds.extents.x * 0.8f;
        float forwardOffset = m_collider.bounds.extents.z * 0.8f;

        Vector3[] rayPositions = new Vector3[5]
        {
            boundsBottom,
            boundsBottom + transform.forward * forwardOffset,
            boundsBottom - transform.forward * forwardOffset,
            boundsBottom + transform.right * horizontalOffset,
            boundsBottom - transform.right * horizontalOffset
        };

        m_isGrounded = false;
        RaycastHit hit;

        for (int i = 0; i < rayPositions.Length; i++)
        {
            bool hitGround = Physics.Raycast(rayPositions[i], Vector3.down, out hit, m_groundCheckDistance, m_groundLayer);
            Debug.DrawRay(rayPositions[i], Vector3.down * m_groundCheckDistance, hitGround ? Color.green : Color.red);

            if (hitGround)
            {
                m_isGrounded = true;
                break;
            }
        }
    }

    /// <summary>
    /// 移動処理
    /// </summary>
    /// <summary>
    /// 移動処理
    /// </summary>
    public void ProcessMovement(Vector2 moveInput, Quaternion cameraRotation, float deltaTime)
    {
        if (m_rigidbody == null) return;

        if (m_canMove)
        {
            Vector3 moveDirection;

            //if (m_needLockOnRot)
            //{
            //    // ロックオン中：ロックオンの向き（m_lockOnRot）を基準に移動ベクトルを作る

            //    Vector3 forward = m_lockOnRot * Vector3.forward;
            //    forward.y = 0f;
            //    forward.Normalize();

            //    Vector3 right = m_lockOnRot * Vector3.right;
            //    right.y = 0f;
            //    right.Normalize();


            //    moveDirection = (forward * moveInput.y + right * moveInput.x);
            //}
            //else
            //{
                
            //}
            var inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            moveDirection = cameraRotation * inputDirection;

            Vector3 currentVelocity = m_rigidbody.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            //水平方向の速度と入力方向
            Vector3 horizontalMoveDirection = moveDirection.normalized; // 入力方向の水平成分
            Vector3 horizontalVelocityNormalized = horizontalVelocity.normalized; // 現在速度の水平成分

            float controlMultiplier = m_isGrounded ? 1f : m_airControl;

            if (moveDirection.magnitude > 0.1f)
            {
                
                // 速度と入力方向の内積をチェック
                float dotProduct = Vector3.Dot(horizontalVelocityNormalized, horizontalMoveDirection);

                //逆方向への入力
                if (m_isGrounded && horizontalVelocity.magnitude > 0.1f && dotProduct < 0.5f)
                {
                    // 入力方向に沿った速度成分を取得
                    float forwardSpeed = Vector3.Dot(horizontalVelocity, horizontalMoveDirection);
                    Vector3 forwardVelocity = horizontalMoveDirection * Mathf.Max(0, forwardSpeed);

                    // 横滑り成分（入力方向と垂直な速度）を減衰
                    Vector3 lateralVelocity = horizontalVelocity - horizontalMoveDirection * forwardSpeed;
                    lateralVelocity *= 0.5f;

                    // 新しい速度を設定
                    Vector3 newHorizontalVelocity = forwardVelocity + lateralVelocity;
                    m_rigidbody.linearVelocity = new Vector3(newHorizontalVelocity.x, currentVelocity.y, newHorizontalVelocity.z);
                }

                if (horizontalVelocity.magnitude < m_maxSpeed)
                {
                    // 通常の加速
                    Vector3 force = horizontalMoveDirection * m_moveSpeed * m_acceleration * controlMultiplier;
                    m_rigidbody.AddForce(force, ForceMode.Force);
                }
            }
            else
            {
                // 入力がない場合の減速・ドラッグ処理 
                if (m_isGrounded)
                {
                    Vector3 dragForce = -horizontalVelocity * m_deceleration;
                    m_rigidbody.AddForce(dragForce, ForceMode.Force);
                }
                else
                {
                    Vector3 dragForce = -horizontalVelocity * m_drag * 0.3f;
                    m_rigidbody.AddForce(dragForce, ForceMode.Force);
                }
            }

            ProcessRotation(moveDirection, deltaTime);

            if (m_isGrounded && moveDirection.magnitude > 0.1f)
            {
                CheckAndClimbStep();
            }

            if (!m_isGrounded)
            {
                //重力処理
                m_rigidbody.AddForce(Vector3.down * m_additionalGravity, ForceMode.Force);

                if (m_rigidbody.linearVelocity.y < m_maxFallSpeed)
                {
                    m_rigidbody.linearVelocity = new Vector3(
                        m_rigidbody.linearVelocity.x,
                        m_maxFallSpeed,
                        m_rigidbody.linearVelocity.z
                    );
                }
            }
        }
        else
        {
            //移動無効時の処理 
            Vector3 velocity = m_rigidbody.linearVelocity;
            velocity.x *= 0.9f;
            velocity.z *= 0.9f;
            m_rigidbody.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// 回転処理
    /// </summary>
    private void ProcessRotation(Vector3 moveDirection, float deltaTime)
    {
        if (m_rigidbody == null) return;

        if (m_needLockOnRot)
        {
            m_rigidbody.MoveRotation(m_lockOnRot);
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(m_rigidbody.rotation, targetRotation, deltaTime * m_rotationSpeed);
            m_rigidbody.MoveRotation(newRotation);
        }
    }

    /// <summary>
    /// ジャンプ処理
    /// </summary>
    public void ProcessJump(bool shouldJump)
    {
        if (m_rigidbody == null || !m_canMove) return;

        if (shouldJump && m_isGrounded)
        {
            m_rigidbody.AddForce(Vector3.up * m_jumpForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 段差乗り越えチェック
    /// </summary>
    private void CheckAndClimbStep()
    {
        if (m_rigidbody == null) return;
        if (Time.time - m_lastStepUpTime < m_stepUpCooldown) return;

        Vector3 upperRayStart = transform.position + Vector3.up * (m_maxStepHeight + 0.1f);
        bool hasObstacleAbove = Physics.Raycast(upperRayStart, transform.forward, m_stepCheckDistance, m_groundLayer);

        Vector3 lowerRayStart = transform.position + Vector3.up * 0.1f;
        RaycastHit lowerHit;
        bool hasStep = Physics.Raycast(lowerRayStart, transform.forward, out lowerHit, m_stepCheckDistance, m_groundLayer);

        

        if (!hasObstacleAbove && hasStep)
        {
            float stepHeight = lowerHit.point.y - transform.position.y;
            if (stepHeight > 0.05f && stepHeight <= m_maxStepHeight)
            {
                m_rigidbody.AddForce(Vector3.up * m_stepUpForce, ForceMode.Impulse);
                m_lastStepUpTime = Time.time;
            }
        }
    }

    /// <summary>
    /// ロックオン回転を設定
    /// </summary>
    public void SetLockOnRotation(bool lockOn, Quaternion targetRot)
    {
        m_needLockOnRot = lockOn;
        m_lockOnRot = targetRot;
    }

    /// <summary>
    /// 移動を無効化
    /// </summary>
    public void DisableMovement()
    {
        m_canMove = false;
    }

    /// <summary>
    /// 移動を有効化
    /// </summary>
    public void EnableMovement()
    {
        m_canMove = true;
    }

    public void EnableMovementDelay()
    {
        StartCoroutine(Delya());
    }

    IEnumerator Delya()
    {
        yield return new WaitForSeconds(1.5f);
        m_canMove = true;
    }
}
