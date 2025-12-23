using NetWork;
using TMPro;
using UnityEngine;

public class ReadyButton : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] string m_readyText;
    [SerializeField] string m_cancelText;
    [SerializeField]CircleButton m_circleButton;

    [SerializeField] RoomInput m_roomInput;
    [SerializeField] RoomManager m_roomManager;

    bool m_isPush = false;
    bool m_ready = false;
    bool m_isFilled = false;


    private void Awake()
    {
        m_roomInput.OnSelect += Push;
        m_circleButton.OnFillAction = FillAction;
    }

    private void OnDestroy()
    {
        if(m_roomInput)
        {
            m_roomInput.OnSelect -= Push;
        }
        if(m_circleButton)
        {
            m_circleButton.OnFillAction -= FillAction;
        }
    }

    private void Push(bool ispush)
    {
        m_isPush = ispush;
        if (!ispush) m_isFilled = false;
        //一度完了した後は一度離されるまでキャンセル
        if (!m_isFilled)
        {
            m_circleButton.OnUpdate(m_isPush);
        }
    }

    private void FillAction()
    {
        m_isFilled = true;
        m_circleButton.SetValue(0);
        m_circleButton.IsCompleted = false;
        m_circleButton.OnUpdate(false);
        Change();
        if(m_ready)//キャンセルイベント
        {
            
        }
        else//完了イベント
        {

        }
    }
    

    public void Change()
    {
        //内部システム変更
        m_roomManager.Ready();
        //テキスト変更
        m_ready = !m_ready;
        if(m_ready)
        {
            
            m_text.text = m_cancelText;
        }
        else
        {
            m_text.text = m_readyText;
        }
    }
}
