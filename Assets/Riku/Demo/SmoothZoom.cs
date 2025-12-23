using UnityEngine;

public class SmoothZoom : MonoBehaviour
{
    [Header("ターゲット設定")]
    public Transform targetObject; // 注目するオブジェクト
    public float stopDistance = 3.0f; // どれくらい近づくか

    [Header("動きの設定")]
    public float smoothSpeed = 5.0f; // 移動と回転の速さ

    // 初期の位置と回転
    private Vector3 initialPosition;
    private Quaternion initialRotation;

    // 目標の位置と回転
    private Vector3 targetPosition;
    private Quaternion targetRotation;

    void Start()
    {
        // 開始時の場所と角度を保存
        initialPosition = transform.position;
        initialRotation = transform.rotation;

        // 最初はターゲット＝現在地にして動かないようにする
        targetPosition = initialPosition;
        targetRotation = initialRotation;
    }

    void Update()
    {
        // Spaceキー：ターゲットに近づき、かつターゲットの方を向く
        if (Input.GetKeyUp(KeyCode.A))
        {
            if (targetObject != null)
            {
                // 1. まず目標位置を計算（ターゲットの手前）
                Vector3 directionToCamera = (transform.position - targetObject.position).normalized;
                targetPosition = targetObject.position + (directionToCamera * stopDistance);

                // 2. 「目標位置からターゲットを見る」ための角度を計算
                // (ターゲットの位置 - 目標のカメラ位置 = カメラの向くべきベクトル)
                Vector3 lookDirection = targetObject.position - targetPosition;
                targetRotation = Quaternion.LookRotation(lookDirection);
            }
        }

        // Qキー：元の位置と角度に戻る
        if (Input.GetKeyDown(KeyCode.S))
        {
            targetPosition = initialPosition;
            targetRotation = initialRotation;
        }

        // 毎フレームなめらかに変化させる
        // 位置の移動 (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);

        // 回転の移動 (Slerp) ※回転はSlerpを使うのが定石です
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}