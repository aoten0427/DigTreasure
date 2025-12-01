using Fusion;
using NetWork;
using System.Collections.Generic;
using System.Threading.Tasks;
using UniRx;
using UnityEngine;

public class ScorePresenter : MonoBehaviour,IPlayInitialize
{
    NetWork.GameLauncher m_gameLauncher;
    [SerializeField]ScoreUIData m_scoreUIData;

    public InitializationPriority Priority => InitializationPriority.UI;

    public string Name => "ScorePresenter";

    
    /// <summary>
    /// ユーザーネーム変更
    /// </summary>
    /// <param name="id"></param>
    /// <param name="name"></param>
    public void ChangeUserName(int id, string name)
    {
        
    }

    /// <summary>
    /// スコア加算
    /// </summary>
    /// <param name="id"></param>
    /// <param name="socre"></param>
    public void AddSocre(int id,int socre)
    {
        
    }

    /// <summary>
    /// 初期化処理
    /// </summary>
    /// <param name="task"></param>
    /// <returns></returns>
    public Task InitializeAsync(ReactiveProperty<float> task = null)
    {
       
        m_gameLauncher = GameLauncher.Instance;
        

        ////ユーザーデータを取得して名前を反映
        var userdatas = m_gameLauncher.GetAllUserData();
        foreach (var userdata in userdatas.Values)
        {
            m_scoreUIData.AddUser(userdata.m_id,userdata.m_name.ToString());
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
         foreach(var userdata in userdatas.Values)
        {
            m_scoreUIData.UpdatePoint(userdata.m_id, userdata.m_treasurePoint);
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
