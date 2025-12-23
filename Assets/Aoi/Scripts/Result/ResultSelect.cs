using UnityEngine;

public class ResultSelect : MonoBehaviour
{
    [SerializeField] ResultManager m_resultManager;
    [SerializeField]ResultInput m_input;

    [SerializeField] CircleButton m_againButton;
    [SerializeField] CircleButton m_exitButton;

    private void Awake()
    {
        //入力受付
        m_input.OnSelect += PushAgain;
        m_input.OnCancel += PushExit;

        //イベント設定
        m_againButton.OnFillAction += Again;
        m_exitButton.OnFillAction += Exit;
    }



    void PushAgain(bool ispush)
    {
        m_againButton.OnUpdate(ispush);
    }

    void PushExit(bool ispush)
    {
        m_exitButton.OnUpdate(ispush);
    }

    private void OnDestroy()
    {
        Clear();
    }

    private void Clear()
    {
        if(m_input)
        {
            m_input.OnSelect -= PushAgain;
            m_input.OnCancel -= PushExit;
        }
        if(m_againButton)m_againButton.OnFillAction -= Again;
        if(m_exitButton)m_exitButton.OnFillAction -= Exit;
        
    }


    private void Again()
    {
        m_againButton.IsCompleted = true;
        m_input.IsActive = false;
        m_resultManager.PlayAgain();
        Clear();
    }

    private void Exit()
    {
        m_exitButton.IsCompleted= true;
        m_input.IsActive = false;
        m_resultManager.Exit();
        Clear();
    }
}
