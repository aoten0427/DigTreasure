using Fusion;
using NetWork;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class ScorePresenter : MonoBehaviour,IPlayInitialize
{
    NetWork.GameLauncher m_gameLauncher;
    [SerializeField]Mukouyama.ScoreManager m_scoreManager;

    public InitializationPriority Priority => InitializationPriority.UI;

    public string Name => "ScorePresenter";

    //スコア変更記録のためのデータ
    Dictionary<PlayerRef, NetWork.NetworkUserData> m_oldDatas = new Dictionary<PlayerRef, NetworkUserData> ();

    /// <summary>
    /// ユーザーネーム変更
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    public void ChangeUserName(int id, string name)
    {
        Mukouyama.PlayersData.instance.ChangePlayerName(id - 1, name);
    }

    /// <summary>
    /// スコア加算
    /// </summary>
    /// <param name="id"></param>
    /// <param name="socre"></param>
    public void AddSocre(int id,int socre)
    {
        m_scoreManager.AddPlayerScore(id, socre);
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public Task InitializeAsync(ReactiveProperty<float> task = null)
    {
       
        m_gameLauncher = GameLauncher.Instance;
        

        //ユーザーデータを取得して名前を反映
        var userdatas = m_gameLauncher.GetAllUserData();
        foreach(var userdata in userdatas)
        {
            ChangeUserName(userdata.Value.m_id, userdata.Value.m_name.ToString());
            m_oldDatas.Add(userdata.Key, userdata.Value);
        }

        m_gameLauncher.AddOnUserDataChange(ChangeData);

        return Task.CompletedTask;
    }

    /// <summary>
    /// データ変更受付
    /// </summary>
    /// <param name="userdatas"></param>
    private void ChangeData(IReadOnlyDictionary<PlayerRef, NetworkUserData> userdatas)
    {
        foreach(var userdata in userdatas)
        {
            if(m_oldDatas.ContainsKey(userdata.Key))
            {
                if (m_oldDatas[userdata.Key].m_treasurePoint == userdata.Value.m_treasurePoint) continue;
                int addscore = userdata.Value.m_treasurePoint - m_oldDatas[userdata.Key].m_treasurePoint;
                AddSocre(m_oldDatas[userdata.Key].m_id, addscore);

                //データ更新
                m_oldDatas[(userdata.Key)] = userdata.Value;
            }
        }
    }

    public void SetManager(PlayManager manager)
    {
        
    }

    private void OnDestroy()
    {
        if (m_gameLauncher != null) m_gameLauncher.RemoveOnDataChangeAction(ChangeData);
    }
}
