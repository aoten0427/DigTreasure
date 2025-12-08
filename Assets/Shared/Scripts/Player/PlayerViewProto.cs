using Fusion;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;

public class PlayerViewProto : NetworkBehaviour
{
    [SerializeField] private CinemachineCamera cinemachineCamera;
    [SerializeField] private TextMeshPro nameLabel;

    public void MakeCameraTarget()
    {
        // ChinemachineCameraの優先度を上げて、カメラの追従対象にする
        cinemachineCamera.Priority.Value = 100;
    }

    public void SetNickName(string nickName)
    {
        nameLabel.text = nickName;
    }

    public void LateUpdate()
    {
        // プレイヤー名のテキストを、ビルボード（常にカメラ正面向き）にする
        nameLabel.transform.rotation = Camera.main.transform.rotation;
    }
}
