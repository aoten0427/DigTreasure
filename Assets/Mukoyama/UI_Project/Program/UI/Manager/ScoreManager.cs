using DG.Tweening;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;


namespace Mukouyama
{
    public class ScoreManager : MonoBehaviour
    {
        // 数字の画像
        [SerializeField] private Sprite[] scoreSprits = new Sprite[11];

        //// プレイヤー各桁の位置の画像
        //[SerializeField] private Image[,] m_Player_Score_Image = new Image[4, 4];

        [SerializeField]
        public ChildArray[] m_Player_Score_Array = new ChildArray[4];
        //シリアライズされた子要素クラス
        [System.Serializable]
        public class ChildArray
        {
            public Image[] m_Player_Score_Image = new Image[4];
        }
        /*********************************
        * 
        * プログラム開始時処理
        *
        **********************************/
        private void Start()
        {
            InitializeScore();
        }

        // 表示される数値をリセット
        private void InitializeScore()
        {
            for (int i = 0; i < 4; i++)
            {
                UpdatePlayerScoreImage(i + 1, 0);
            }
        }


        /*********************************
        * 
        * スコア変更処理(数値情報)
        *
        **********************************/

        /**/
        // スコアを変更する(デバッグ用)
        public void AddPlayerScore(int PlayerID, int AddScore)
        {
            for (int i = 0; i < 4; i++)
            {
                if (PlayerID == PlayersData.instance.m_PlayerInfoArray[i].Player_ID)
                {
                    PlayersData.instance.m_PlayerInfoArray[i].Player_CurrentScore += AddScore;
                    UpdateVariableScore(PlayersData.instance.m_PlayerInfoArray[i]);
                }
            }
        }

        // スコアを徐々に変動させる
        private void UpdateVariableScore(PlayersData.PlayerInfo Player)
        {
            if (!Player.Player_IsTweeningScore)
            {
                // tween中に変更
                Player.Player_IsTweeningScore = true;

                // 終了値を設定
                Player.Player_AfterScore = Player.Player_CurrentScore;

                // 値が徐々に上昇するように
                DOVirtual.Int(
                    // Tween開始時の値
                    from: Player.Player_BeforeScore,
                    // 終了時の値
                    to: Player.Player_AfterScore,
                    // Tween時間
                    duration: 1.0f,
                    // 変動値
                    tweenValue =>
                    {
                        Player.Player_VariableScore = tweenValue;
                        //UpdatePlayerScore(Player.Player_ID, tweenValue);
                        UpdatePlayerScoreImage(Player.Player_ID, tweenValue);
                    }
                )
                // 完了時
                .OnComplete(() =>
                {
                    Player.Player_IsTweeningScore = false;
                    Player.Player_BeforeScore = Player.Player_AfterScore;
                });
            }
        }

        /*********************************
        * 
        * スコア表示処理
        *
        **********************************/

        // スコアをもとにプレイヤーの各桁の位置の画像を変更する
        private void UpdatePlayerScoreImage(int PlayerID, int PlayerVariableScore)
        {
            // スコア範囲を制限
            if (PlayerVariableScore > 9999)
                PlayerVariableScore = 9999;
            if (PlayerVariableScore < 0)
                PlayerVariableScore = 0;

            // スコアを文字列化（例："1234"）
            string scoreStr = PlayerVariableScore.ToString();

            // 対象プレイヤーのスコア画像配列を取得
            var playerImages = m_Player_Score_Array[PlayerID - 1].m_Player_Score_Image;

            // スコア文字列の開始位置を計算（右詰め表示）
            // 例：3桁なら右側3つのインデックスに詰める
            int startIndex = 4 - scoreStr.Length;

            // 右詰め配置処理
            for (int i = 0; i < 4; i++)
            {
                int arrayIndex = 3 - i; // 配列[0]=1桁目 → [3]=4桁目 に合わせる

                if (i >= startIndex)
                {
                    // 右詰めした位置に数字を入れる
                    int strIndex = i - startIndex; // scoreStr の対応位置
                    int num = scoreStr[strIndex] - '0';
                    playerImages[arrayIndex].sprite = GetSpriteNumber(num);
                }
                else
                {
                    // 右詰め前の余白部分は非表示スプライト
                    playerImages[arrayIndex].sprite = scoreSprits[10];
                }
            }
        }

        // 受け取った数字に対応する画像を渡す
        private Sprite GetSpriteNumber(int num)
        {
            switch (num)
            {
                case 0:
                    return scoreSprits[0];
                case 1:
                    return scoreSprits[1];
                case 2:
                    return scoreSprits[2];
                case 3:
                    return scoreSprits[3];
                case 4:
                    return scoreSprits[4];
                case 5:
                    return scoreSprits[5];
                case 6:
                    return scoreSprits[6];
                case 7:
                    return scoreSprits[7];
                case 8:
                    return scoreSprits[8];
                case 9:
                    return scoreSprits[9];
                default:
                    return scoreSprits[10];
            }
        }
    } 
}