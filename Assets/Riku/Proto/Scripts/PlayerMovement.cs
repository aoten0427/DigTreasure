using UnityEngine;

/// <summary>
/// プレイヤーの移動を管理するクラス
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    [Header("基本移動パラメータ")]
    [SerializeField] private float moveSpeed = 5f;           // 移動速度
    [SerializeField] private float maxSpeed = 10f;           // 最大速度
    [SerializeField] private float acceleration = 10f;       // 加速度
    [SerializeField] private float deceleration = 15f;       // 減速度
    [SerializeField] private float drag = 5f;                // 摩擦力
    [SerializeField] private float rotationSpeed = 10f;      // 回転速度

    [Header("空中制御")]
    [SerializeField] private float airControl = 0.5f;        // 空中制御（0-1、地上時の移動力に対する割合）

    [Header("ジャンプパラメータ")]
    [SerializeField] private float jumpForce = 5f;           // ジャンプ力

    [Header("地面判定")]
    [SerializeField] private float groundCheckDistance = 1.5f; // 床の当たり判定距離
    [SerializeField] private LayerMask groundLayer;          // 地面レイヤー

    [Header("段差乗り越え")]
    [SerializeField] private float maxStepHeight = 0.3f;      // 乗り越え可能な最大段差高さ
    [SerializeField] private float stepUpForce = 8f;          // 段差を越える力
    [SerializeField] private float stepCheckDistance = 0.5f;  // 段差検出距離
    [SerializeField] private float stepUpCooldown = 0.2f;     // クールダウン時間

    [Header("重力設定")]
    [SerializeField] private float additionalGravity = 10f;   // 追加の重力
    [SerializeField] private float maxFallSpeed = -20f;       // 最大落下速度

    // 内部状態
    private Rigidbody rb;
    private Collider playerCollider;
    private bool isGrounded;
    private bool canMove = true;

    // ロックオン関連
    private bool needLockOnRot = false;
    private Quaternion lockOnRot;

    // 段差乗り越え関連
    private float lastStepUpTime = 0f;

    // プロパティ
    public bool IsGrounded => isGrounded;
    public bool CanMove { get => canMove; set => canMove = value; }

    /// <summary>
    /// 初期化
    /// </summary>
    /// <param name="rigidbody">リジッドボディ</param>
    /// <param name="collider">コライダー</param>
    public void Initialize(Rigidbody rigidbody, Collider collider)
    {
        rb = rigidbody;
        playerCollider = collider;
    }

    /// <summary>
    /// 地面判定をチェック
    /// </summary>
    public void CheckGrounded()
    {
        if (playerCollider == null)
        {
            isGrounded = false;
            return;
        }

        // Colliderの底の中心位置
        Vector3 boundsBottom = playerCollider.bounds.center - new Vector3(0, playerCollider.bounds.extents.y, 0);
        boundsBottom.y += 0.3f;

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

    /// <summary>
    /// 移動処理
    /// </summary>
    /// <param name="moveInput">移動入力（X, Y）</param>
    /// <param name="cameraRotation">カメラの回転</param>
    /// <param name="deltaTime">デルタタイム</param>
    public void ProcessMovement(Vector2 moveInput, Quaternion cameraRotation, float deltaTime)
    {
        if (rb == null) return;

        if (canMove)
        {
            // 移動入力
            var inputDirection = new Vector3(moveInput.x, 0f, moveInput.y);
            Vector3 moveDirection = cameraRotation * inputDirection;

            // 現在の水平速度を取得
            Vector3 currentVelocity = rb.linearVelocity;
            Vector3 horizontalVelocity = new Vector3(currentVelocity.x, 0, currentVelocity.z);

            // 空中制御の適用
            float controlMultiplier = isGrounded ? 1f : airControl;

            // 水平移動
            if (moveDirection.magnitude > 0.1f)
            {
                // 加速処理（加速度を使用）
                if (horizontalVelocity.magnitude < maxSpeed)
                {
                    Vector3 force = moveDirection.normalized * moveSpeed * acceleration * controlMultiplier;
                    rb.AddForce(force, ForceMode.Force);
                }
            }
            else
            {
                // 減速処理（減速度を使用）
                if (isGrounded)
                {
                    Vector3 dragForce = -horizontalVelocity * deceleration;
                    rb.AddForce(dragForce, ForceMode.Force);
                }
                else
                {
                    // 空中では軽い摩擦のみ
                    Vector3 dragForce = -horizontalVelocity * drag * 0.3f;
                    rb.AddForce(dragForce, ForceMode.Force);
                }
            }

            // 回転処理
            ProcessRotation(moveDirection, deltaTime);

            // 段差乗り越えチェック（移動中で地上にいる場合のみ）
            if (isGrounded && moveDirection.magnitude > 0.1f)
            {
                CheckAndClimbStep();
            }

            // 追加の重力を適用（空中のみ）
            if (!isGrounded)
            {
                rb.AddForce(Vector3.down * additionalGravity, ForceMode.Force);

                // 落下速度を制限
                if (rb.linearVelocity.y < maxFallSpeed)
                {
                    rb.linearVelocity = new Vector3(
                        rb.linearVelocity.x,
                        maxFallSpeed,
                        rb.linearVelocity.z
                    );
                }
            }
        }
        else
        {
            // 移動不可時は速度を減衰
            Vector3 velocity = rb.linearVelocity;
            velocity.x *= 0.9f;
            velocity.z *= 0.9f;
            rb.linearVelocity = velocity;
        }
    }

    /// <summary>
    /// 回転処理
    /// </summary>
    /// <param name="moveDirection">移動方向</param>
    /// <param name="deltaTime">デルタタイム</param>
    private void ProcessRotation(Vector3 moveDirection, float deltaTime)
    {
        if (rb == null) return;

        // ロックオン時の回転
        if (needLockOnRot)
        {
            rb.MoveRotation(lockOnRot);
        }
        else if (moveDirection.magnitude > 0.1f)
        {
            // 移動方向を向く
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            Quaternion newRotation = Quaternion.Slerp(rb.rotation, targetRotation, deltaTime * rotationSpeed);
            rb.MoveRotation(newRotation);
        }
    }

    /// <summary>
    /// ジャンプ処理
    /// </summary>
    /// <param name="shouldJump">ジャンプするか</param>
    public void ProcessJump(bool shouldJump)
    {
        if (rb == null) return;
        if (!canMove) return;

        if (shouldJump && isGrounded)
        {
            rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }

    /// <summary>
    /// 段差乗り越えチェック
    /// </summary>
    private void CheckAndClimbStep()
    {
        if (rb == null) return;

        // クールダウン中はスキップ
        if (Time.time - lastStepUpTime < stepUpCooldown) return;

        // 前方上部のRaycast（段差の上に障害物がないか）
        Vector3 upperRayStart = transform.position + Vector3.up * (maxStepHeight + 0.1f);
        bool hasObstacleAbove = Physics.Raycast(upperRayStart, transform.forward,
            stepCheckDistance, groundLayer);

        // 前方下部のRaycast（段差を検出）
        Vector3 lowerRayStart = transform.position + Vector3.up * 0.1f;
        RaycastHit lowerHit;
        bool hasStep = Physics.Raycast(lowerRayStart, transform.forward,
            out lowerHit, stepCheckDistance, groundLayer);

        // デバッグ表示
        Debug.DrawRay(upperRayStart, transform.forward * stepCheckDistance,
            hasObstacleAbove ? Color.red : Color.green);
        Debug.DrawRay(lowerRayStart, transform.forward * stepCheckDistance,
            hasStep ? Color.yellow : Color.blue);

        // 上部に障害物がなく、下部に段差がある場合
        if (!hasObstacleAbove && hasStep)
        {
            float stepHeight = lowerHit.point.y - transform.position.y;

            // 段差が範囲内なら上向きの力を加える
            if (stepHeight > 0.05f && stepHeight <= maxStepHeight)
            {
                rb.AddForce(Vector3.up * stepUpForce, ForceMode.Impulse);
                lastStepUpTime = Time.time;
            }
        }
    }

    /// <summary>
    /// ロックオン回転を設定
    /// </summary>
    /// <param name="lockOn">ロックオンするか</param>
    /// <param name="targetRot">目標回転</param>
    public void SetLockOnRotation(bool lockOn, Quaternion targetRot)
    {
        needLockOnRot = lockOn;
        lockOnRot = targetRot;
    }

    /// <summary>
    /// 移動を無効化
    /// </summary>
    public void DisableMovement()
    {
        canMove = false;
    }

    /// <summary>
    /// 移動を有効化
    /// </summary>
    public void EnableMovement()
    {
        canMove = true;
    }
}
