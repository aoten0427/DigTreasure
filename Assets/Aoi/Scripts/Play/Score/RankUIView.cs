using DG.Tweening;
using System.Collections.Generic;
using UnityEngine;

public class RankUIView : MonoBehaviour
{
    private PlayerScoreUI[] m_playerScoreUIArray;
    private ScoreUIData m_scoreUIData;

    [SerializeField] private float m_moveAnimationDuration = 1.0f;
    [SerializeField] private float m_checkpointOffsetX = 75f;
    [SerializeField] private Ease m_moveEase = Ease.InOutSine;

    private Vector3 m_firstPlacePosition;
    private float m_rankSpacing;

    private Dictionary<int, int> m_userIdToArrayIndex = new Dictionary<int, int>();
    private Dictionary<int, PlayerRankAnimationState> m_rankAnimationStates = new Dictionary<int, PlayerRankAnimationState>();
    private int m_nextAvailableIndex = 0;

    private class PlayerRankAnimationState
    {
        public int targetRank = 0;        // 目標順位
        public Vector3 targetPosition;    // 目標座標
        public Tween currentTween = null;

        public void KillTween()
        {
            if (currentTween != null && currentTween.IsActive())
            {
                currentTween.Kill();
                currentTween = null;
            }
        }
    }

    public void Initialize(PlayerScoreUI[] playerScoreUIArray, ScoreUIData scoreUIData)
    {
        m_playerScoreUIArray = playerScoreUIArray;
        m_scoreUIData = scoreUIData;
        CalculateRankPositions();
    }

    private void CalculateRankPositions()
    {
        if (m_playerScoreUIArray == null || m_playerScoreUIArray.Length < 2)
        {
            Debug.LogWarning("[RankUIView] 最低2つのPlayerScoreUIが必要です");
            return;
        }

        m_firstPlacePosition = m_playerScoreUIArray[0].m_rankUITransform.localPosition;
        Vector3 secondPlacePos = m_playerScoreUIArray[1].m_rankUITransform.localPosition;
        m_rankSpacing = Mathf.Abs(m_firstPlacePosition.y - secondPlacePos.y);
    }

    public void OnDataUpdated(List<ScoreUIData.UserData> allUsers)
    {
        foreach (var user in allUsers)
        {
            if (!m_userIdToArrayIndex.ContainsKey(user.id))
            {
                AssignUserToSlot(user.id, user.rank);
                continue;  // 初回配置はアニメーションなし
            }

            // 目標ランクが変わった場合のみ更新
            var state = m_rankAnimationStates[user.id];
            if (state.targetRank != user.rank)
            {
                MoveToRank(user.id, user.rank);
            }

            UpdateCanvasSortingOrder(user.id, user.rank);
        }
    }

    private void AssignUserToSlot(int userId, int initialRank)
    {
        if (m_playerScoreUIArray == null) return;
        if (m_nextAvailableIndex >= m_playerScoreUIArray.Length) return;

        int arrayIndex = m_nextAvailableIndex;
        m_userIdToArrayIndex[userId] = arrayIndex;
        m_nextAvailableIndex++;

        Vector3 initialPos = GetRankPosition(initialRank);
        m_playerScoreUIArray[arrayIndex].m_rankUITransform.localPosition = initialPos;

        m_rankAnimationStates[userId] = new PlayerRankAnimationState
        {
            targetRank = initialRank,
            targetPosition = initialPos
        };
    }

    /// <summary>
    /// 指定ランクへ移動（核心部分）
    /// </summary>
    private void MoveToRank(int userId, int newRank)
    {
        int arrayIndex = m_userIdToArrayIndex[userId];
        var state = m_rankAnimationStates[userId];
        RectTransform transform = m_playerScoreUIArray[arrayIndex].m_rankUITransform;

        Vector3 currentPos = transform.localPosition;
        Vector3 newTargetPos = GetRankPosition(newRank);

        // 目標更新
        state.targetRank = newRank;
        state.targetPosition = newTargetPos;

        // 既存Tweenを停止（現在位置は維持される）
        state.KillTween();

        // 移動距離が小さければスキップ
        if (Vector3.Distance(currentPos, newTargetPos) < 0.5f)
        {
            transform.localPosition = newTargetPos;
            return;
        }

        // 残り時間を距離に応じて計算（自然な速度感を維持）
        float fullDistance = m_rankSpacing * 4f;  // 基準距離（4ランク分）
        float actualDistance = Vector3.Distance(currentPos, newTargetPos);
        float duration = m_moveAnimationDuration * (actualDistance / fullDistance);
        duration = Mathf.Clamp(duration, 0.2f, m_moveAnimationDuration);

        // 上昇か下降かを判定
        bool isMovingUp = currentPos.y < newTargetPos.y;

        if (isMovingUp)
        {
            // 曲線移動（中継地点経由）
            Vector3 checkpoint = CalculateCheckpoint(currentPos, newTargetPos);
            Vector3[] path = { currentPos, checkpoint, newTargetPos };

            state.currentTween = transform
                .DOLocalPath(path, duration, PathType.CatmullRom)
                .SetEase(m_moveEase);
        }
        else
        {
            // 直線移動
            state.currentTween = transform
                .DOLocalMove(newTargetPos, duration)
                .SetEase(m_moveEase);
        }
    }

    /// <summary>
    /// 中継地点を計算
    /// </summary>
    private Vector3 CalculateCheckpoint(Vector3 from, Vector3 to)
    {
        float yDistance = Mathf.Abs(to.y - from.y);
        float normalizedDistance = yDistance / (m_rankSpacing * 4f);  // 正規化
        float offsetX = m_checkpointOffsetX * Mathf.Clamp01(normalizedDistance);

        return new Vector3(
            from.x + offsetX,
            (from.y + to.y) * 0.5f,
            0
        );
    }

    private void UpdateCanvasSortingOrder(int userId, int rank)
    {
        if (!m_userIdToArrayIndex.ContainsKey(userId)) return;

        int arrayIndex = m_userIdToArrayIndex[userId];
        var canvas = m_playerScoreUIArray[arrayIndex].m_canvas;

        if (canvas != null)
        {
            canvas.sortingOrder = 100 - rank;  // 余裕を持った値に
        }
    }

    private Vector3 GetRankPosition(int rank)
    {
        return m_firstPlacePosition + new Vector3(0, -(rank - 1) * m_rankSpacing, 0);
    }

    private void OnDestroy()
    {
        // クリーンアップ
        foreach (var state in m_rankAnimationStates.Values)
        {
            state.KillTween();
        }
    }
}