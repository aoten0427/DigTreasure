using System;
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
        /**/// プレイヤー情報配列の雛形
        public PlayerInfo[] m_PlayerInfoArray = new PlayerInfo[]{
        new(1,"アアア", 0, 0, 0, 0, 1),
        new(2,"イイイ", 0, 0, 0, 0, 2),
        new(3,"ウウウ", 0, 0, 0, 0, 3),
        new(4,"エエエ", 0, 0, 0, 0, 4)
    };

        [SerializeField]
        private PlayerName[] m_PlayerNames = new PlayerName[4];

        /*********************************
        * 
        * プログラム開始時処理
        *
        **********************************/

        // シングルトン化する
        private void Awake()
        {
            if (instance == null)
            {
                // 自身をインスタンスとする
                instance = this;
                // シーンをまたいでも消去されないようにする
                //DontDestroyOnLoad(gameObject);
            }
            else
            {
                // インスタンスが複数存在しないように、既に存在していたら自身を消去する
                Destroy(gameObject);
            }
        }
        private void Start()
        {
            // プレイヤー情報の初期化
            InitializePleyerInfo();
        }

        /*********************************
        * 
        * 全体更新処理
        *
        **********************************/
        private void Update()
        {
            // 順位変更処理(数値情報のみ)
            UpdatePlace(m_PlayerInfoArray);

            // 数値確認(デバッグ)
            CheckPlayerData();
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
            for (int i = 0; i < 4; i++) { PlayerArray[i].Player_CurrentPlace = i + 1; }
        }

        /*********************************
        * 
        * プレイヤー情報設定、変更処理
        *
        **********************************/

        /**/// 各プレイヤーの情報を初期化する
        public void InitializePleyerInfo()
        {
            m_PlayerInfoArray = new PlayerInfo[]{
                new(1,"アアア",  0, 0, 0, 0, 1),
                new(2,"イイイ",  0, 0, 0, 0, 2),
                new(3,"ウウウ",  0, 0, 0, 0, 3),
                new(4,"エエエ",  0, 0, 0, 0, 4)
        };
            for (int i = 0; i < 4; i++)
            {
                m_PlayerNames[i].SetPlayerName(i);
            }
        }


        public void ChangePlayerName(int id,string name)
        {
            m_PlayerInfoArray[id].Player_Name = name;
            m_PlayerNames[id].name = name;
            m_PlayerNames[id].SetPlayerName(id);
        }

        public PlayerInfo[] GetPlayerArray()
        {
            return m_PlayerInfoArray;
        }

        public PlayerInfo GetPlayerInfo(int PlayerID)
        {
            for (int i = 0; i < 4; i++)
            {
                if (PlayerID == m_PlayerInfoArray[i].Player_ID)
                {
                    return m_PlayerInfoArray[i];
                }
            }
            return null;
        }

        /*********************************
        * 
        * デバッグ用関数
        *
        **********************************/
        /**/// 各プレイヤーの現在のパラメータをチェック(デバッグ表示)
        private void CheckPlayerData()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 0; i < 4; i++)
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
        }
    } 
}
