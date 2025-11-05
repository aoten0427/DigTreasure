using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Mukouyama
{
    public class ScoreManager : MonoBehaviour
    {
        // 数字の画像
        [SerializeField] private Sprite[] scoreSprits = new Sprite[11];

        // プレイヤーの人数
        private int m_PlayerNum = 4;

        // 配列４個生成されているけどプレイヤーの人数に合わせてキャンバスを消すのでこのままにしておく
        // 各プレイヤーのスコア表示
        [SerializeField]
        public ChildArray[] m_Player_Score_Array = new ChildArray[4];
        // シリアライズされた子要素クラス
        // プレイヤー各桁の位置の画像
        [System.Serializable]
        public class ChildArray { public Image[] m_Player_Score_Image = new Image[4]; }
        /*********************************
        * 
        * プログラム開始時処理
        *
        **********************************/
        private void Start()
        {
            // プレイヤー人数を取得(PlayersDataからプレイヤーの人数を取得できるように)
            SetPlayerNum(PlayersData.instance.m_PlayerInfoArray.Length);

            // スコア表示を初期化
            InitializeScore(m_PlayerNum);
        }

        /**/// スコア表示をリセット
        private void InitializeScore(int PlayerNum)
        {
            for (int i = 0; i < PlayerNum; i++) { UpdatePlayerScoreImage(i + 1, 0); }
        }

        /*********************************
        * 
        * スコア変更処理(数値情報)
        *
        **********************************/

        /**/// スコアを変更する(デバッグ用)
        public void AddPlayerScore(int PlayerID, int AddScore)
        {
            for (int i = 0; i < m_PlayerNum; i++)
            {
                if (PlayerID == PlayersData.instance.m_PlayerInfoArray[i].Player_ID)
                {
                    PlayersData.instance.m_PlayerInfoArray[i].Player_CurrentScore += AddScore;
                    UpdateVariableScore(PlayersData.instance.m_PlayerInfoArray[i]);
                }
            }
        }

        /**/// スコアを徐々に変動させる
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
                    // tween開始時の値
                    from: Player.Player_BeforeScore,
                    // 終了時の値
                    to: Player.Player_AfterScore,
                    // tween時間
                    duration: 1.0f,
                    // 変動値、変動中の処理
                    tweenValue =>
                    {
                        Player.Player_VariableScore = tweenValue;
                        UpdatePlayerScoreImage(Player.Player_ID, tweenValue);
                    }
                )
                // tween完了時
                .OnComplete(() =>
                {
                    Player.Player_IsTweeningScore = false;
                    Player.Player_BeforeScore = Player.Player_AfterScore;
                });
            }
        }

        /*********************************
        * 
        * 情報設定処理
        *
        **********************************/
        /**/// プレイヤーの人数を設定する
        public void SetPlayerNum(int PlayerNum) { m_PlayerNum = PlayerNum; }

        /*********************************
        * 
        * スコア表示処理
        *
        **********************************/

        /**/// スコアをもとにプレイヤーの各桁の位置の画像を変更する
        private void UpdatePlayerScoreImage(int PlayerID, int PlayerVariableScore)
        {
            // スコア範囲を制限
            if (PlayerVariableScore > 9999) PlayerVariableScore = 9999;
            if (PlayerVariableScore < 0) PlayerVariableScore = 0;

            // スコアを文字列化
            string scoreStr = PlayerVariableScore.ToString();

            // 対象プレイヤーのスコア画像配列を取得
            var playerImages = m_Player_Score_Array[PlayerID - 1].m_Player_Score_Image;

            // スコア文字列の開始位置を計算
            int startIndex = 4 - scoreStr.Length;

            // 右詰め配置処理
            for (int i = 0; i < 4; i++)
            {
                int arrayIndex = 3 - i;

                if (i >= startIndex)
                {
                    // 右詰めした位置に数字を入れる
                    int strIndex = i - startIndex;
                    int num = scoreStr[strIndex] - '0';
                    playerImages[arrayIndex].sprite = GetSpriteNumber(num);
                }
                // 右詰め前の余白部分は透明スプライト
                else playerImages[arrayIndex].sprite = scoreSprits[10];
            }
        }

        /**/// 受け取った数字に対応する画像を渡す
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