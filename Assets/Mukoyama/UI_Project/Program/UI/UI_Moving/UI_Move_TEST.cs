using DG.Tweening;
using UnityEngine;

namespace Mukouyama
{
    public class UI_Move_TEST : MonoBehaviour
    {
        /**/// 各プレイヤーのUI
        [SerializeField] private Canvas[] m_Player_Canvas = new Canvas[4];
        /**/// 順位表示
        [SerializeField] private GameObject[] m_Player_Places = new GameObject[4];

        // プレイヤーの人数
        private int m_PlayerNum = 4;

        /**/// 各プレイヤーのUIの挙動ステート
        public enum UI_MOVE_TYPE
        {
            MOVING,     // 移動中
            STANDBY,    // 待機中
        }

        /**/// プレイヤーの情報クラス
        public class UI_Info
        {
            public int UI_ID;                   // UIのID
            public int UI_VariablePosition;     // UIの現在地
            public RectTransform UI_Position;   // UIの座標
            public UI_MOVE_TYPE UI_MoveType;    // UIの挙動ステート

            // プレイヤーの情報設定
            public UI_Info(
                int _UI_ID,
                int _UI_VariablePosition,
                RectTransform _UI_Position,
                UI_MOVE_TYPE _UI_MoveType)
            {
                UI_ID = _UI_ID;
                UI_VariablePosition = _UI_VariablePosition;
                UI_Position = _UI_Position;
                UI_MoveType = _UI_MoveType;
            }
        }
        /**/// UI情報配列の雛形
        public UI_Info[] m_UI_InfoArray = new UI_Info[4];

        /**/// プレイヤーUIが順位によってソートされているか確認するフラグ
        private bool m_RankSortedFlag = true;

        /**/// 各順位の座標
        static Vector3 m_Pos_1stPlace = new(820, -140, 0);
        static Vector3 m_Pos_2ndPlace = new(820, -250, 0);
        static Vector3 m_Pos_3rdPlace = new(820, -360, 0);
        static Vector3 m_Pos_4thPlace = new(820, -470, 0);

        /**/// 各順位に行く際の中間地点
        static Vector3 m_Pos_CheckPoint_2to1 = new(870, -195, 0);
        static Vector3 m_Pos_CheckPoint_3to1 = new(890, -250, 0);
        static Vector3 m_Pos_CheckPoint_3to2 = new(870, -305, 0);
        static Vector3 m_Pos_CheckPoint_4to1 = new(910, -305, 0);
        static Vector3 m_Pos_CheckPoint_4to2 = new(890, -360, 0);
        static Vector3 m_Pos_CheckPoint_4to3 = new(870, -415, 0);

        /**/// 順位が上がる場合の中継地点をまとめた配列
            // 2 > 1
        Vector3[] m_Array_Route_2to1 = new[] {
            m_Pos_2ndPlace,
            m_Pos_CheckPoint_2to1,
            m_Pos_1stPlace
        };
        // 3 > 1
        Vector3[] m_Array_Route_3to1 = new[] {
            m_Pos_3rdPlace,
            m_Pos_CheckPoint_3to1,
            m_Pos_1stPlace
        };
        // 3 > 2
        Vector3[] m_Array_Route_3to2 = new[] {
            m_Pos_3rdPlace,
            m_Pos_CheckPoint_3to2,
            m_Pos_2ndPlace
        };
        // 4 > 3
        Vector3[] m_Array_Route_4to3 = new[] {
            m_Pos_4thPlace,
            m_Pos_CheckPoint_4to3,
            m_Pos_3rdPlace
        };
        // 4 > 2
        Vector3[] m_Array_Route_4to2 = new[] {
            m_Pos_4thPlace,
            m_Pos_CheckPoint_4to2,
            m_Pos_2ndPlace
        };
        // 4 > 1
        Vector3[] m_Array_Route_4to1 = new[] {
            m_Pos_4thPlace,
            m_Pos_CheckPoint_4to1,
            m_Pos_1stPlace
        };

        /**/// UIが目的地に到着するまでにかかる時間
        [SerializeField] float m_arrivalTime;

        /*********************************
        * 
        * プログラム開始時処理
        *
        **********************************/
        private void Start()
        {
            // プレイヤー人数を取得(PlayersDataからプレイヤーの人数を取得できるように)
            SetPlayerNum(PlayersData.instance.m_PlayerInfoArray.Length);

            // UI情報の初期化
            Initialize_UI_InfoArray(m_PlayerNum);
        }

        /*********************************
        * 
        * UI情報設定、変更処理
        *
        **********************************/

        /**/// 各UIの情報を初期化する     
        public void Initialize_UI_InfoArray(int PlayerNum)
        {
            // 配列を初期化する
            m_UI_InfoArray = new UI_Info[PlayerNum];

            // プレイヤーの人数に応じて配列を作成
            for (int i = 0; i < 4; i++)
            {
                // 配列を生成
                if (i < PlayerNum) m_UI_InfoArray[i] = new UI_Info(i + 1, i + 1, m_Player_Canvas[i].GetComponent<RectTransform>(), UI_MOVE_TYPE.STANDBY);
                // 配列を生成させず、UIの表示を消す
                else
                {
                    m_Player_Canvas[i].enabled = false;
                    m_Player_Places[i].SetActive(false);
                }
            }
        }

        // UIの配列を取得
        public UI_Info[] GetUI_InfoArray() { return m_UI_InfoArray; }

        // プレイヤーの人数を設定する
        public void SetPlayerNum(int PlayerNum) { m_PlayerNum = PlayerNum; }

        /*********************************
        * 
        * 全体更新処理
        *
        **********************************/
        private void Update()
        {
            // 順位変更処理(プレイヤーの順位によってUIを移動させる処理)
            MoveRank(m_UI_InfoArray, PlayersData.instance.m_PlayerInfoArray);
        }

        /**********************************
        * 
        * 順位によってUIを移動させる処理
        *
        **********************************/

        /**/// 各プレイヤーの順位の移動
        private void MoveRank(UI_Info[] UI_Data_Array, PlayersData.PlayerInfo[] PlayersDataArray)
        {
            // UIのUIの現在地が順位通りに整列しているかをチェック
            if (CheckRankSorted(UI_Data_Array, PlayersDataArray) == true) return;

            // ヒエラルキーの順序を変更
            UpdateUI_LayerPlace(PlayersDataArray);

            // UIを動かす
            MovePlayerRankUI(UI_Data_Array, PlayersDataArray);
        }

        /*********************************
        * 
        * 順位変更処理(数値情報のみ)
        *
        **********************************/

        /**/// UIの現在地が順位通りに整列しているかをチェック
        private bool CheckRankSorted(UI_Info[] UI_Data_Array, PlayersData.PlayerInfo[] PlayersDataArray)
        {
            m_RankSortedFlag = true;
            for (int i = 0; i < PlayersDataArray.Length; i++)
            {
                for (int j = 0; j < PlayersDataArray.Length; j++)
                {
                    if (UI_Data_Array[i].UI_ID != PlayersDataArray[j].Player_ID) continue;
                    if (UI_Data_Array[i].UI_VariablePosition != PlayersDataArray[j].Player_CurrentPlace)
                    {
                        m_RankSortedFlag = false;
                        return m_RankSortedFlag;
                    }
                }
            }
            return m_RankSortedFlag;
        }

        /*********************************
        * 
        * UI移動関数
        *
        **********************************/

        /**/// 各UIのヒエラルキーを更新
        private void UpdateUI_LayerPlace(PlayersData.PlayerInfo[] PlayersDataArray)
        {
            for (int i = 0; i < PlayersDataArray.Length; i++)
            {
                // プレイヤーIDをもとに、プレイヤーのUIの表示順を変更
                switch (PlayersDataArray[i].Player_ID)
                {
                    case 1:
                        m_Player_Canvas[0].sortingOrder = 5 - PlayersDataArray[i].Player_CurrentPlace;
                        break;
                    case 2:
                        m_Player_Canvas[1].sortingOrder = 5 - PlayersDataArray[i].Player_CurrentPlace;
                        break;
                    case 3:
                        m_Player_Canvas[2].sortingOrder = 5 - PlayersDataArray[i].Player_CurrentPlace;
                        break;
                    case 4:
                        m_Player_Canvas[3].sortingOrder = 5 - PlayersDataArray[i].Player_CurrentPlace;
                        break;
                    default:
                        Debug.Log("error!");
                        break;
                }
            }
        }

        /**/// 各プレイヤーUIを動かす
        private void MovePlayerRankUI(UI_Info[] UI_DataArray, PlayersData.PlayerInfo[] PlayersDataArray)
        {
            // 各プレイヤーUIの状態をチェックし、動かせる条件に合っていれば動かす
            for (int i = 0; i < PlayersDataArray.Length; i++) { CheckPlayerRankAndMove(UI_DataArray[i], PlayersDataArray); }
        }

        /**/// UIの状態をチェックし、動かせる条件に合っていれば動かす
        private void CheckPlayerRankAndMove(UI_Info UI, PlayersData.PlayerInfo[] PlayersDataArray)
        {
            // UIが移動中なら関数処理終了
            if (UI.UI_MoveType == UI_MOVE_TYPE.MOVING) return;

            // UIを動かす
            MoveUI(UI, PlayersDataArray);
        }

        /**/// UIを動かす
        private void MoveUI(UI_Info UI, PlayersData.PlayerInfo[] PlayersDataArray)
        {
            for (int i = 0; i < PlayersDataArray.Length; i++)
            {
                // IDが一致しているかをチェック
                if (UI.UI_ID != PlayersDataArray[i].Player_ID) continue;

                // 現在順位とUIの位置の差をチェック
                int rankUPorDOWN = UI.UI_VariablePosition - PlayersDataArray[i].Player_CurrentPlace;

                // UIが現在順位より下の位置にある時
                if (rankUPorDOWN > 0)
                {
                    // 移動前の順位に合わせて移動の仕方を変更
                    switch (UI.UI_VariablePosition)
                    {
                        case 2:
                            // UIの位置が２位の座標の場合
                            // ２位から１位へ移動
                            MoveUpward(UI, m_Array_Route_2to1, 1);
                            break;
                        case 3:
                            // UIの位置が３位の座標の場合
                            switch (PlayersDataArray[i].Player_CurrentPlace)
                            {
                                case 1:
                                    // ３位から１位へ移動
                                    MoveUpward(UI, m_Array_Route_3to1, 1);
                                    break;
                                case 2:
                                    // ３位から２位へ移動
                                    MoveUpward(UI, m_Array_Route_3to2, 2);
                                    break;
                                default:
                                    Debug.Log("error!");
                                    break;
                            }
                            break;
                        case 4:
                            // UIの位置が４位の座標の場合
                            // プレイヤーの順位を確認して
                            switch (PlayersDataArray[i].Player_CurrentPlace)
                            {
                                case 1:
                                    // ４位から１位へ移動
                                    MoveUpward(UI, m_Array_Route_4to1, 1);
                                    break;
                                case 2:
                                    // ４位から２位へ移動
                                    MoveUpward(UI, m_Array_Route_4to2, 2);
                                    break;
                                case 3:
                                    // ４位から３位へ移動
                                    MoveUpward(UI, m_Array_Route_4to3, 3);
                                    break;
                                default:
                                    Debug.Log("error!");
                                    break;
                            }
                            break;
                        default:
                            Debug.Log("error!");
                            break;
                    }
                }
                // UIが現在順位より上の位置にある時
                else if (rankUPorDOWN < 0)
                {
                    // 移動前の順位に合わせて移動の仕方を変更
                    switch (UI.UI_VariablePosition)
                    {
                        case 1:
                            // １位から２位へ移動
                            MoveDownward(UI, m_Pos_2ndPlace, 2);
                            break;
                        case 2:
                            // ２位から３位へ移動
                            MoveDownward(UI, m_Pos_3rdPlace, 3);
                            break;
                        case 3:
                            // ３位から４位へ移動
                            MoveDownward(UI, m_Pos_4thPlace, 4);
                            break;
                        default:
                            Debug.Log("error!");
                            break;
                    }
                }
                // 順位変動していないので、処理終了
                else return;
                break;
            }
        }

        /**/// 順位が上がる場合
        private void MoveUpward(UI_Info UI, Vector3[] ArrivalPath, int ArrivalPos)
        {
            // 移動地点を曲がりながら移動
            UI.UI_Position.transform.DOLocalPath(ArrivalPath, m_arrivalTime, PathType.CatmullRom)
            // 完了時に呼ばれる        
            .OnComplete(() => { UpdateArrivalUIParam(UI, ArrivalPos); });
            // UIのパラメータを更新
            UI.UI_MoveType = UI_MOVE_TYPE.MOVING;
        }

        // DoTweenで各順位の座標へ動かす
        /**/// 順位が下がる場合
        private void MoveDownward(UI_Info UI, Vector3 ArrivalPath, int ArrivalPos)
        {
            // 下に移動
            UI.UI_Position.transform.DOLocalMove(ArrivalPath, m_arrivalTime)
            // 完了時に呼ばれる
            .OnComplete(() => { UpdateArrivalUIParam(UI, ArrivalPos); });
            // UIのパラメータを更新
            UI.UI_MoveType = UI_MOVE_TYPE.MOVING;
        }

        /**/// UIが到着した場合にパラメータを変更させる
        private void UpdateArrivalUIParam(UI_Info UI, int ArrivalPos)
        {
            // 現在位置を目的地の順位に変更
            UI.UI_VariablePosition = ArrivalPos;

            // 待機中ステートに変更
            UI.UI_MoveType = UI_MOVE_TYPE.STANDBY;
        }

        /*********************************
        * 
        * デバッグ用関数
        *
        **********************************/

        /*// 各UIの現在のパラメータをチェック(デバッグ表示)
        private void CheckUI_Data()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 0; i < m_PlayerNum; i++)
                {
                    Debug.Log("\n" +
                        "UIの現在地 :" + m_UI_InfoArray[i].UI_VariablePosition + "\n" +
                        "UIの座標 :" + m_UI_InfoArray[i].UI_Position.localPosition + "\n" +
                        "UIの挙動ステート :" + m_UI_InfoArray[i].UI_MoveType
                        );
                }
            }
        }*/

        /*// ゲーム強制終了
        private void EndGame()
        {
    #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;//ゲームプレイ終了
    #else
        Application.Quit();//ゲームプレイ終了
    #endif
        }*/
    }
}