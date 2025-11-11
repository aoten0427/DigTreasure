using System;
using TMPro;
using UnityEngine;

namespace Mukouyama
{
    /*********************************
    * 
    * プレイヤー情報クラス(仮)
    *
    **********************************/
    public class PlayersData : MonoBehaviour
    {
        // インスタンスの定義
        public static PlayersData instance;

        // 外部から取得するプレイヤーの人数
        [SerializeField] public int m_PlayerNum = 4;

        /**/// プレイヤーの情報クラス
        public class PlayerInfo : IComparable
        {
            public int Player_ID;               // プレイヤーID
            public string Player_Name;          // プレイヤー名
            public int Player_BeforeScore;      // プレイヤーの変動前スコア
            public int Player_VariableScore;    // プレイヤーの変動中スコア
            public int Player_AfterScore;       // プレイヤーの変動後スコア
            public int Player_CurrentScore;     // プレイヤーの現在獲得スコア
            public bool Player_IsTweeningScore; // プレイヤーのスコアが変動中か
            public int Player_CurrentPlace;     // プレイヤーの現在順位

            // プレイヤーの情報設定
            public PlayerInfo(
                int _Player_ID,
                string _Player_Name,
                int _Player_BeforeScore,
                int _Player_VariableScore,
                int _Player_AfterScore,
                int _Player_CurrentScore,
                int _Player_CurrentPlace)
            {
                this.Player_ID = _Player_ID;
                this.Player_Name = _Player_Name;
                this.Player_BeforeScore = _Player_BeforeScore;
                this.Player_VariableScore = _Player_VariableScore;
                this.Player_AfterScore = _Player_AfterScore;
                this.Player_CurrentScore = _Player_CurrentScore;
                this.Player_IsTweeningScore = false;
                this.Player_CurrentPlace = _Player_CurrentPlace;
            }
            // プレイヤーのスコアを比較する：順位のソート用
            public int CompareTo(object obj)
            {
                PlayerInfo i = obj as PlayerInfo;
                return i.Player_CurrentScore.CompareTo(this.Player_CurrentScore);
            }
        }
        /**/// プレイヤー情報配列
        public PlayerInfo[] m_PlayerInfoArray;

        // プレイヤー名クラス配列
        [SerializeField]
        private GameObject[] m_PlayerNames = new GameObject[4];

        /*********************************
        * 
        * プログラム開始時処理
        *
        **********************************/

        /**/// プログラム開始時処理(ゲームオブジェクト生成前)
            // シングルトン化する
        private void Awake()
        {
            if (instance == null)
            {
                // 自身をインスタンスとする
                instance = this;
                // シーンをまたいでも消去されないようにする
                DontDestroyOnLoad(gameObject);
            }
            // インスタンスが複数存在しないよう、既に存在していたら自身を消去する
            else Destroy(gameObject);
        }

        /**/// プログラム開始時処理(ゲームオブジェクト生成後)
        private void Start()
        {
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // ↓外部からプレイヤー数を取得

            // ↑外部からプレイヤー数を取得
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // プレイヤー情報の初期化(↑から取得した人数を使う)
            InitializePleyerInfo(m_PlayerNum);
        }

        /*********************************
        * 
        * プレイヤー情報設定、変更処理
        *
        **********************************/

        /**/// 各プレイヤーの情報を初期化する
        public void InitializePleyerInfo(int PlayerNum)
        {
            // 配列を初期化
            m_PlayerInfoArray = new PlayerInfo[PlayerNum];

            // 生成した配列にデータを挿入する
            for (int i = 0; i < PlayerNum; i++)
            {
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // ↓外部からプレイヤー名を取得

                // ↑外部からプレイヤー名を取得
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                m_PlayerInfoArray[i] = new PlayerInfo(i + 1, "プレイヤー" + (i + 1)/*外部から取得したプレイヤー名に変更*/, 0, 0, 0, 0, i + 1);
                m_PlayerNames[i].GetComponent<TextMeshProUGUI>().text = m_PlayerInfoArray[i].Player_Name;
            }
        }

        /*// プレイヤーの配列を取得
        public PlayerInfo[] GetPlayerArray()
        {
            return m_PlayerInfoArray;
        }

        public PlayerInfo GetPlayerInfo(int PlayerID)
        {
            for (int i = 0; i < m_PlayerInfoArray.Length; i++)
            {
                if (PlayerID == m_PlayerInfoArray[i].Player_ID)
                {
                    return m_PlayerInfoArray[i];
                }
            }
            return null;
        }*/

        /*********************************
        * 
        * 全体更新処理
        *
        **********************************/
        private void Update()
        {
            // 順位変更処理(数値情報のみ)
            UpdatePlace(m_PlayerInfoArray);

            /*// 数値確認(デバッグ)
            CheckPlayerData();*/
        }

        /**********************************
        * 
        * 順位変更処理
        *
        **********************************/

        /**/// 各プレイヤーの順位の更新
        private void UpdatePlace(PlayerInfo[] PlayerArray)
        {
            // listをソート
            Array.Sort(PlayerArray);
            // ソート後の配列をもとにプレイヤーの順位(データ上)を変更
            for (int i = 0; i < m_PlayerInfoArray.Length; i++) { PlayerArray[i].Player_CurrentPlace = i + 1; }
        }

        /*********************************
        * 
        * デバッグ用関数
        *
        **********************************/
        /*// 各プレイヤーの現在のパラメータをチェック(デバッグ表示)
        private void CheckPlayerData()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 0; i < m_PlayerInfoArray.Length; i++)
                {
                    Debug.Log("\n" +
                        "プレイヤーID :" + m_PlayerInfoArray[i].Player_ID + "\n" +
                        "プレイヤーの変動前スコア" + m_PlayerInfoArray[i].Player_BeforeScore + "\n" +
                        "プレイヤーの変動中スコア" + m_PlayerInfoArray[i].Player_VariableScore + "\n" +
                        "プレイヤーの変動後スコア" + m_PlayerInfoArray[i].Player_AfterScore + "\n" +
                        "プレイヤーの獲得スコア :" + m_PlayerInfoArray[i].Player_CurrentScore + "\n" +
                        "プレイヤーのスコアが変動中か" + m_PlayerInfoArray[i].Player_IsTweeningScore + "\n" +
                        "プレイヤーの現在順位 :" + m_PlayerInfoArray[i].Player_CurrentPlace + "位："
                        );
                }
            }
        }*/
    }
}
