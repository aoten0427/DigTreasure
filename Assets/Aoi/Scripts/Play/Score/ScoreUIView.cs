using DG.Tweening;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;




/// <summary>
/// スコア表示View
/// </summary>
public class ScoreUIView : MonoBehaviour
{
    [System.Serializable]
    struct UISet
    {
        public GameObject rank;
        public GameObject data;
    }


    // データ参照
    [SerializeField] private ScoreUIData m_scoreUIData;

    // UI要素
    [SerializeField] private Sprite[] m_scoreSprits = new Sprite[11];  // 0-9 + 空白(index 10)
    [SerializeField] private PlayerScoreUI[] m_playerScoreArray;       

    // ランクUI参照
    [SerializeField] private RankUIView m_rankUIView;

    // アニメーション設定
    [SerializeField] private float m_animationDuration = 1.0f;

    // 内部状態
    private Dictionary<int, int> m_userIdToArrayIndex = new Dictionary<int, int>();
    private Dictionary<int, PlayerScoreAnimationState> m_animationStates = new Dictionary<int, PlayerScoreAnimationState>();
    private int m_nextAvailableIndex = 0;

    [SerializeField]
    UISet[] m_uiSets = new UISet[4];

    /// <summary>
    /// プレイヤーのスコアアニメーション状態
    /// </summary>
    private class PlayerScoreAnimationState
    {
        public int displayScore = 0;
        public Tween currentTween = null;
    }


    private void Start()
    {
        InitializeScore();

        // RankUIViewを初期化
        if (m_rankUIView != null)
        {
            m_rankUIView.Initialize(m_playerScoreArray, m_scoreUIData);
        }

        for(int i = 0; i < m_uiSets.Length; i++)
        {
            m_uiSets[i].rank.SetActive(false);
            m_uiSets[i].data.SetActive(false);
        }
    }

    /// <summary>
    /// 全スロットを0で初期化
    /// </summary>
    private void InitializeScore()
    {
        for (int i = 0; i < m_playerScoreArray.Length; i++)
        {
            UpdatePlayerScoreImage(i, 0);
        }
    }


    private void OnEnable()
    {
        if (m_scoreUIData != null)
            m_scoreUIData.OnDataChanged += OnDataUpdated;
    }

    private void OnDisable()
    {
        if (m_scoreUIData != null)
            m_scoreUIData.OnDataChanged -= OnDataUpdated;
    }

    /// <summary>
    /// データ更新時のコールバック
    /// </summary>
    private void OnDataUpdated(List<ScoreUIData.UserData> allUsers)
    {
        foreach (var user in allUsers)
        {
            // 初回ならインデックス割り当て
            if (!m_userIdToArrayIndex.ContainsKey(user.id))
            {
                AssignUserToSlot(user.id, user.name);
            }

            // スコアアニメーション更新
            UpdateScoreAnimation(user.id, user.point);
        }

        // ランクUI更新（RankUIViewに処理を委譲）
        if (m_rankUIView != null)
        {
            m_rankUIView.OnDataUpdated(allUsers);
        }
    }


    /// <summary>
    /// ユーザーを空きスロットに割り当て
    /// </summary>
    private void AssignUserToSlot(int userId, string userName)
    {
        // 配列サイズを超える場合は警告
        if (m_nextAvailableIndex >= m_playerScoreArray.Length|| m_nextAvailableIndex >= m_uiSets.Length)
        {
            
            return;
        }

        m_uiSets[m_nextAvailableIndex].rank.SetActive(true);
        m_uiSets[m_nextAvailableIndex].data.SetActive(true);

        // インデックス割り当て
        int arrayIndex = m_nextAvailableIndex;
        m_userIdToArrayIndex[userId] = arrayIndex;
        m_nextAvailableIndex++;

        

        // アニメーション状態初期化
        m_animationStates[userId] = new PlayerScoreAnimationState();

        // 名前設定
        if (m_playerScoreArray[arrayIndex].m_nameText != null)
        {
            m_playerScoreArray[arrayIndex].m_nameText.text = userName;
        }

        Debug.Log($"User {userId} ({userName}) assigned to slot {arrayIndex}");
    }


    /// <summary>
    /// スコアアニメーションを開始/上書き（案B実装）
    /// </summary>
    private void UpdateScoreAnimation(int userId, int newScore)
    {
        // ユーザーが割り当てられていない場合は無視
        if (!m_userIdToArrayIndex.ContainsKey(userId))
            return;

        if (!m_animationStates.ContainsKey(userId))
            return;

        var animState = m_animationStates[userId];
        int arrayIndex = m_userIdToArrayIndex[userId];

        // 既存のアニメーションをキャンセル
        if (animState.currentTween != null)
        {
            animState.currentTween.Kill();
            animState.currentTween = null;
        }

        // 現在の表示値から新しい目標値へアニメーション
        animState.currentTween = DOVirtual.Int(
            from: animState.displayScore,
            to: newScore,
            duration: m_animationDuration,
            onVirtualUpdate: (int value) =>
            {
                animState.displayScore = value;
                UpdatePlayerScoreImage(arrayIndex, value);
            }
        )
        .OnComplete(() =>
        {
            animState.currentTween = null;
        });
    }


    /// <summary>
    /// スコアを4桁の画像で表示
    /// </summary>
    private void UpdatePlayerScoreImage(int arrayIndex, int score)
    {
        // スコア範囲制限
        if (score > 9999)
            score = 9999;
        if (score < 0)
            score = 0;

        // スコアを文字列化（例: "1234"）
        string scoreStr = score.ToString();

        // 対象プレイヤーのスコア画像配列を取得
        var playerImages = m_playerScoreArray[arrayIndex].m_scoreImages;

        // スコア文字列の開始位置を計算（右詰め表示）
        // 例: 3桁なら右端3つのインデックスに詰める
        int startIndex = 4 - scoreStr.Length;

        // 右詰め配置処理
        for (int i = 0; i < 4; i++)
        {
            int imageIndex = 3 - i; // 配列[0]=1桁目 ～ [3]=4桁目 に合わせる

            if (i >= startIndex)
            {
                // 右詰め後の位置に数値を表示
                int strIndex = i - startIndex; // scoreStr の対応位置
                int num = scoreStr[strIndex] - '0';
                playerImages[imageIndex].sprite = GetSpriteNumber(num);
            }
            else
            {
                // 右詰め前の余白部分は非表示スプライト
                playerImages[imageIndex].sprite = m_scoreSprits[10];
            }
        }
    }

    /// <summary>
    /// 受け取った数値に対応した画像を返す
    /// </summary>
    private Sprite GetSpriteNumber(int num)
    {
        return (num >= 0 && num <= 9) ? m_scoreSprits[num] : m_scoreSprits[10];
    }
}
