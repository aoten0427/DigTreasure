using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ランクUI移動View
/// </summary>
public class RankUIView : MonoBehaviour
{
    // UI要素（ScoreUIViewから渡される）
    private PlayerScoreUI[] m_playerScoreUIArray;
    private ScoreUIData m_scoreUIData;

    // 移動設定
    [SerializeField] private float m_moveAnimationDuration = 1.0f;
    [SerializeField] private float m_checkpointOffsetX = 50f;  // 中継地点のXオフセット

    // 自動計算される座標
    private Vector3 m_firstPlacePosition;
    private float m_rankSpacing;

    // 内部状態
    private Dictionary<int, int> m_userIdToArrayIndex = new Dictionary<int, int>();
    private Dictionary<int, PlayerRankAnimationState> m_rankAnimationStates = new Dictionary<int, PlayerRankAnimationState>();
    private int m_nextAvailableIndex = 0;

    /// <summary>
    /// プレイヤーのランク移動アニメーション状態
    /// </summary>
    private class PlayerRankAnimationState
    {
        public int currentRank = 0;       // 現在の順位
        public bool isMoving = false;     // 移動中フラグ
        public Tween currentTween = null; // 実行中のTween
    }

    /*********************************
     * 初期化処理
     *********************************/

    /// <summary>
    /// PlayerScoreUI配列とScoreUIDataを設定して初期化
    /// </summary>
    public void Initialize(PlayerScoreUI[] playerScoreUIArray, ScoreUIData scoreUIData)
    {
        m_playerScoreUIArray = playerScoreUIArray;
        m_scoreUIData = scoreUIData;
        CalculateRankPositions();
    }

    /// <summary>
    /// 初期配置から順位座標を計算
    /// </summary>
    private void CalculateRankPositions()
    {
        if (m_playerScoreUIArray == null || m_playerScoreUIArray.Length < 2)
        {
            Debug.LogWarning("RankUIView: 最低2つのPlayerScoreUIが必要です");
            return;
        }

        // 1位の座標 = 最初のUI要素の位置
        m_firstPlacePosition = m_playerScoreUIArray[0].m_rankUITransform.localPosition;

        // 順位間隔 = 1位と2位のY座標の差
        Vector3 secondPlacePos = m_playerScoreUIArray[1].m_rankUITransform.localPosition;
        m_rankSpacing = Mathf.Abs(m_firstPlacePosition.y - secondPlacePos.y);

        Debug.Log($"RankUIView: 1位座標={m_firstPlacePosition}, 順位間隔={m_rankSpacing}");
    }

    /*********************************
     * データ更新処理（ScoreUIViewから呼ばれる）
     *********************************/

    /// <summary>
    /// データ更新時の処理（ScoreUIViewから呼ばれる）
    /// </summary>
    public void OnDataUpdated(List<ScoreUIData.UserData> allUsers)
    {
        // 全ての移動中Tweenをキャンセルして再配置準備
        foreach (var state in m_rankAnimationStates.Values)
        {
            if (state.currentTween != null)
            {
                state.currentTween.Kill();  // 現在位置で停止
                state.currentTween = null;
                state.isMoving = false;
            }
        }

        // 最新ランキングで全員を再配置
        foreach (var user in allUsers)
        {
            // 初回ならインデックス割り当て
            if (!m_userIdToArrayIndex.ContainsKey(user.id))
            {
                AssignUserToSlot(user.id, user.rank);
            }

            // ランク位置更新（現在位置から新しい目標へ）
            UpdateRankPosition(user.id, user.rank);

            // Canvas sortingOrder更新
            UpdateCanvasSortingOrder(user.id, user.rank);
        }
    }

    /*********************************
     * ユーザースロット管理
     *********************************/

    /// <summary>
    /// ユーザーを空きスロットに割り当て
    /// </summary>
    private void AssignUserToSlot(int userId, int initialRank)
    {

        if (m_playerScoreUIArray == null) return;

        // 配列サイズを超える場合は警告
        if (m_nextAvailableIndex >= m_playerScoreUIArray.Length)
        {
            Debug.LogWarning($"RankUIView: User {userId} exceeds max display slots");
            return;
        }

        // インデックス割り当て
        int arrayIndex = m_nextAvailableIndex;
        m_userIdToArrayIndex[userId] = arrayIndex;
        m_nextAvailableIndex++;

        // アニメーション状態初期化
        m_rankAnimationStates[userId] = new PlayerRankAnimationState
        {
            currentRank = initialRank,
            isMoving = false
        };

        // 初期位置設定（アニメーションなし）
        Vector3 initialPos = GetRankPosition(initialRank);
        m_playerScoreUIArray[arrayIndex].m_rankUITransform.localPosition = initialPos;

        Debug.Log($"RankUIView: User {userId} assigned to slot {arrayIndex}, rank {initialRank}");
    }

    /*********************************
     * ランク位置更新処理
     *********************************/

    /// <summary>
    /// ランク位置を更新（現在位置から目標ランクへ移動）
    /// </summary>
    private void UpdateRankPosition(int userId, int newRank)
    {
        if (!m_userIdToArrayIndex.ContainsKey(userId)) return;
        if (!m_rankAnimationStates.ContainsKey(userId)) return;

        int arrayIndex = m_userIdToArrayIndex[userId];
        RectTransform targetTransform = m_playerScoreUIArray[arrayIndex].m_rankUITransform;
        Vector3 targetPos = GetRankPosition(newRank);

        // 現在位置と目標位置が同じなら移動不要
        if (Vector3.Distance(targetTransform.localPosition, targetPos) < 0.1f)
        {
            return;
        }

        // 移動開始
        StartMoveToRank(userId, newRank);
    }

    /// <summary>
    /// 指定順位への移動を開始（現在の実際の位置から）
    /// </summary>
    private void StartMoveToRank(int userId, int targetRank)
    {
        int arrayIndex = m_userIdToArrayIndex[userId];
        var animState = m_rankAnimationStates[userId];
        RectTransform targetTransform = m_playerScoreUIArray[arrayIndex].m_rankUITransform;

        // 実際の現在位置を取得
        Vector3 currentPos = targetTransform.localPosition;
        Vector3 targetPos = GetRankPosition(targetRank);

        // 移動中フラグを立てる
        animState.isMoving = true;

        // 既存のTweenをキャンセル（OnDataUpdated()で既にキャンセル済みだが念のため）
        if (animState.currentTween != null)
        {
            animState.currentTween.Kill();
            animState.currentTween = null;
        }

        // 現在位置から目標位置への方向で上昇/下降を判定（Y軸は上がプラス）
        bool isMovingUp = currentPos.y < targetPos.y;

        if (isMovingUp)
        {
            // 順位上昇の場合は中継地点を経由（現在位置ベース）
            float yDistance = Mathf.Abs(targetPos.y - currentPos.y);
            float offsetX = m_checkpointOffsetX * (yDistance / m_rankSpacing);  // 距離に応じて調整

            Vector3 checkpointPos = new Vector3(
                currentPos.x + offsetX,
                (currentPos.y + targetPos.y) / 2f,
                0
            );

            Vector3[] path = new Vector3[] { currentPos, checkpointPos, targetPos };
            animState.currentTween = targetTransform.DOLocalPath(path, m_moveAnimationDuration, PathType.CatmullRom)
                .OnComplete(() => OnMoveComplete(userId, targetRank));
        }
        else
        {
            // 順位下降の場合は直線移動
            animState.currentTween = targetTransform.DOLocalMove(targetPos, m_moveAnimationDuration)
                .OnComplete(() => OnMoveComplete(userId, targetRank));
        }

        Debug.Log($"RankUIView: User {userId} moving from Y={currentPos.y:F0} to rank {targetRank} (Y={targetPos.y:F0})");
    }

    /// <summary>
    /// 移動完了時の処理
    /// </summary>
    private void OnMoveComplete(int userId, int arrivedRank)
    {
        if (!m_rankAnimationStates.ContainsKey(userId)) return;

        var animState = m_rankAnimationStates[userId];
        animState.currentRank = arrivedRank;
        animState.isMoving = false;
        animState.currentTween = null;

        // 注: OnDataUpdated()が来たら全員が再配置されるため、ここでの再チェックは不要
    }

    /*********************************
     * Canvas sortingOrder更新
     *********************************/

    /// <summary>
    /// Canvas sortingOrderを更新（順位が高いほど手前に表示）
    /// </summary>
    private void UpdateCanvasSortingOrder(int userId, int rank)
    {
        if (!m_userIdToArrayIndex.ContainsKey(userId)) return;

        int arrayIndex = m_userIdToArrayIndex[userId];
        var canvas = m_playerScoreUIArray[arrayIndex].m_canvas;

        if (canvas != null)
        {
            canvas.sortingOrder = 5 - rank;  // 1位=4, 2位=3, 3位=2, 4位=1
        }
    }

    /*********************************
     * 座標計算処理
     *********************************/

    /// <summary>
    /// 指定順位の座標を取得
    /// </summary>
    private Vector3 GetRankPosition(int rank)
    {
        return m_firstPlacePosition + new Vector3(0, -(rank - 1) * m_rankSpacing, 0);
    }
}
