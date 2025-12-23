using System;
using UnityEngine;
using UnityEngine.UI;

public class CircleButton : MonoBehaviour
{
    //サークル画像
    [SerializeField] Image circle;
    //値
    private float m_value = 0;
    //完了時アクション
    public Action OnFillAction;
    //更新フラグ
    bool m_isUpdate = false;
    //更新速度
    [SerializeField] float m_updateSpeed = 1.0f;
    //実行フラグ
    bool m_isCompleted = false;

    public float UpdateSpeed { get { return m_updateSpeed; } set { m_updateSpeed = value; } }
    public bool IsCompleted { get { return m_isCompleted; } set {  if (m_isCompleted != value) { } } }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        circle.fillAmount = 0;
    }


    private void FixedUpdate()
    {
        if (m_isCompleted) return;
        if(m_isUpdate)
        {
            AddValue(UpdateSpeed *  Time.deltaTime);
        }
        else
        {
            AddValue(-UpdateSpeed * Time.deltaTime);
        }
    }

    public void OnUpdate(bool isUpdate)
    {
        m_isUpdate=isUpdate;
    }

    public void SetValue(float value)
    {
        m_value = value;
        m_value = Mathf.Clamp(m_value, 0, 1);
        circle.fillAmount = m_value;
        CheckFill();
    }

    public void AddValue(float add)
    {
        m_value += add;
        m_value = Mathf.Clamp(m_value, 0, 1);
        circle.fillAmount = m_value;
        CheckFill() ;
    }

    private void CheckFill()
    {
        if (m_isCompleted) return;
        if(m_value >= 1)
        {
            OnFillAction?.Invoke();
        }
    }
}
