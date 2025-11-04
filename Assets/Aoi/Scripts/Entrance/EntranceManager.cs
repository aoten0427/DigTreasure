using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 入室シーンの管理
/// </summary>
public class EntranceManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI tmproUGUI;//ユーザー名
    private NetWork.GameLauncher gameLauncher;

    public void GameStart()
    {
        //ゲームランチャーを探す
        gameLauncher = NetWork.GameLauncher.Instance;
        gameLauncher.OnPlayerJoined += MoveToRoom;

        NetWork.NetworkUserData userdata = gameLauncher.UserData;

        //名前が設定されているか確認
        if (!(tmproUGUI.text.Length > 1)) return;

        //名前変更
        userdata.m_name = tmproUGUI.text;
        gameLauncher.UserData = userdata;


        //photon接続
        gameLauncher.ConnectFusion();
    }

    private void MoveToRoom(NetworkRunner runner, PlayerRef player)
    {
        if (runner.LocalPlayer != player) return;
        //待機ルームへ移動
        if (runner.IsSceneAuthority)
        {
            runner.LoadScene(SceneRef.FromIndex(1), LoadSceneMode.Single);
        }
    }
}
