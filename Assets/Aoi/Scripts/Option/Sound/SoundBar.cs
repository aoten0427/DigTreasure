using UnityEngine;
using UnityEngine.Audio;

public class SoundBar : UISelecterBase
{
    [SerializeField] AudioMixer m_audioMixer;
    [SerializeField] string m_audioName;

    float m_value = 0.5f;
    SelectionDirection m_currentDirection = SelectionDirection.None;
    //音量ボリューム
    [SerializeField] float m_changeValue = 0.01f;
    //バー
    [SerializeField] SoundBarView m_view;

    [SerializeField] UISelecterBase m_upSelect;
    [SerializeField] UISelecterBase m_downSelect;

    private void Start()
    {
        SetVolume();
        m_view.Change(m_value);
    }


    private void FixedUpdate()
    {
        ValueChange();
    }

    public override void Select(UISelecterBase back)
    {
        m_view.Select(true);
    }

    public override void Deselect(UISelecterBase next)
    {
       
        m_view.Select(false);
    }


    public override UISelecterBase Selection(SelectionDirection direction)
    {
        switch (direction)
        {
            case SelectionDirection.Up:
                if(m_upSelect)return m_upSelect;
                return this;
            case SelectionDirection.Down:
                if(m_downSelect)return m_downSelect;
                return this;
            default:
                return this;
        }
    }

    public override void Operation(SelectionDirection direction)
    {
        m_currentDirection = direction;
    }

    private void ValueChange()
    {
        if (m_currentDirection == SelectionDirection.None) return;

        //左、右入力なら音量値変更
        if (m_currentDirection == SelectionDirection.Left)
        {
            m_value -= m_changeValue;
        }
        else if (m_currentDirection == SelectionDirection.Right)
        {
            m_value += m_changeValue;
        }
        //0~1にクランプ
        m_value = Mathf.Clamp(m_value, 0, 1);
        //View変更
        m_view.Change(m_value);

        SetVolume();
    }

    private void SetVolume()
    {
        float clampedVolume = Mathf.Clamp(m_value, 0.0001f, 1f);
        float decibels = Mathf.Log10(clampedVolume) * 20f;

        m_audioMixer.SetFloat(m_audioName, decibels);
    }
}
