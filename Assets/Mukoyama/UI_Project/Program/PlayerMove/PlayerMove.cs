namespace Mukouyama
{
    using UnityEngine;
    public class PlayerMove : MonoBehaviour
    {
        [SerializeField] GameObject UI_Manager;
        [SerializeField] GameObject ScoreManager;
        /*********************************
        * 
        * 全体更新処理
        *
        **********************************/
        void Update()
        {
            // 終了処理
            //EndGame();

            // 得点加算
            AddScores();

            // プレイヤー移動
            PlayerControll();
        }

        // ゲーム終了
        private void EndGame()
        {
            //Escが押された時
            if (Input.GetKey(KeyCode.Escape))
            {

#if UNITY_EDITOR
                UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
#else
    Application.Quit();//ゲームプレイ終了
#endif
            }
        }

        // (デバッグ)プレイヤーのスコアを増加させる
        private void AddScores()
        {
            // 数字キーで各プレイヤーの得点を増やす。
            // １～４キー：100点増やす
            if (Input.GetKeyDown(KeyCode.Alpha1)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(1, 100);
            else if (Input.GetKeyDown(KeyCode.Alpha2)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(2, 100);
            else if (Input.GetKeyDown(KeyCode.Alpha3)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(3, 100);
            else if (Input.GetKeyDown(KeyCode.Alpha4)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(4, 100);
            // ５～８キー：300点増やす
            else if (Input.GetKeyDown(KeyCode.Alpha5)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(1, 300);
            else if (Input.GetKeyDown(KeyCode.Alpha6)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(2, 300);
            else if (Input.GetKeyDown(KeyCode.Alpha7)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(3, 300);
            else if (Input.GetKeyDown(KeyCode.Alpha8)) ScoreManager.GetComponent<ScoreManager>().AddPlayerScore(4, 300);
        }

        // プレイヤーが前後左右に移動
        private void PlayerControll()
        {
            // デバッグ
            //if (Input.anyKey) Debug.Log("Move!");

            // 前後移動
            if (Input.GetKey(KeyCode.UpArrow)) transform.Translate(0, 0, 0.1f);
            else if (Input.GetKey(KeyCode.DownArrow)) transform.Translate(0, 0, -0.1f);

            // 左右移動
            if (Input.GetKey(KeyCode.LeftArrow)) transform.Translate(-0.1f, 0, 0);
            else if (Input.GetKey(KeyCode.RightArrow)) transform.Translate(0.1f, 0, 0);
        }
    }
}