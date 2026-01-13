using Fusion;
using NetWork;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;



/// <summary>
/// プレイ時のスコア表示
/// </summary>
public class ScoreUIData : MonoBehaviour,IPlayInitialize
{
    public class UserData
    {
        public int id;
        public string name;
        public int point;
        public int rank;

        public UserData(int id, string name, int point, int rank = 0)
        {
            this.id = id;
            this.name = name;
            this.point = point;
            this.rank = rank;
        }
    }

    //ユーザーIDとユーザーデータの組み合わせ
    Dictionary<int, UserData> m_datas = new Dictionary<int, UserData>();
    //最大人数
    [SerializeField] int m_maxNumber = 4;

    public InitializationPriority Priority => InitializationPriority.UI;

    public string Name => "Score";

    /// <summary>
    /// データ更新時に発火するイベント（全ユーザーデータを渡す）
    /// </summary>
    public event System.Action<List<UserData>> OnDataChanged;

    public void AddUser(int id,string name = "",int point = 0)
    {
        if(m_datas.Count > m_maxNumber)
        {
            Debug.LogWarning("最大ユーザー人数です");
            return;
        }
        if (m_datas.ContainsKey(id)) return;
        //ユーザーを追加
        m_datas[id] = new UserData(id, name, point);

        // 順位を更新してイベント発火（Viewに名前を通知）
        UpdateRankings();
    }

    public void UpdatePoint(int id, int point)
    {
        if (m_datas.TryGetValue(id, out UserData data))
        {
            data.point = point;
            UpdateRankings();
        }
        else
        {
            Debug.LogWarning($"ID '{id}' のユーザーが見つかりませんでした");
        }
    }

    public void UpdatePoint(string name, int point)
    {
        foreach (var pair in m_datas)
        {
            if (pair.Value.name == name)
            {
                pair.Value.point = point;
                UpdateRankings();
                return;
            }
        }
        Debug.LogWarning($"ユーザー '{name}' が見つかりませんでした");
    }

    /// <summary>
    /// 全ユーザーのスコアをソートして順位を更新
    /// </summary>
    private void UpdateRankings()
    {
        // スコアでソート（降順）
        var sortedUsers = new List<KeyValuePair<int, UserData>>(m_datas);
        sortedUsers.Sort((a, b) => b.Value.point.CompareTo(a.Value.point));

        // 順位を割り当て
        for (int i = 0; i < sortedUsers.Count; i++)
        {
            sortedUsers[i].Value.rank = i + 1;
        }

        // イベント発火（全ユーザーデータを通知）
        OnDataChanged?.Invoke(GetAllUsersSortedByRank());
    }

    /// <summary>
    /// ユーザーデータを取得
    /// </summary>
    public UserData GetUserData(int id)
    {
        if (m_datas.TryGetValue(id, out UserData data))
        {
            return data;
        }
        return null;
    }

    /// <summary>
    /// 全ユーザーデータを順位順で取得
    /// </summary>
    public List<UserData> GetAllUsersSortedByRank()
    {
        var sortedUsers = new List<UserData>(m_datas.Values);
        sortedUsers.Sort((a, b) => a.rank.CompareTo(b.rank));
        return sortedUsers;
    }

    /// <summary>
    /// ユーザー数を取得
    /// </summary>
    public int GetUserCount()
    {
        return m_datas.Count;
    }

    public void SetManager(PlayManager manager)
    {
        manager.OnGameEndAction += () => gameObject.SetActive(false);
    }

    public Task InitializeAsync(ReactiveProperty<float> task = null)
    {
        return Task.CompletedTask;
    }
}


