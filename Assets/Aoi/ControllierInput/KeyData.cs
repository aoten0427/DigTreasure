using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KeyData : UISelecterBase<KeyData>
{
    public enum Type
    {
        Character,
        Delete,
        Space,
        Change,
        Decision,
        Cycle
    }

    [SerializeField] Type m_type = Type.Character;

    [SerializeField] KeyData m_upSelect;
    [SerializeField] KeyData m_downSelect;
    [SerializeField] KeyData m_leftSelect;
    [SerializeField] KeyData m_rightSelect;

    [SerializeField] Image m_bacgroundImage;
    [SerializeField] TextMeshProUGUI m_text;
    [SerializeField] Image m_icon;

    [SerializeField] CharacterVariants m_characterVariants;

    public char GetCase1() => m_characterVariants.Case1;
    public char GetCase2() => m_characterVariants.Case2;

    public Type KeyType {  get { return m_type; } }

    private void Start()
    {
        SetCase1();
    }

    public void SetCase1()
    {
        if(m_text&&m_characterVariants)m_text.text = m_characterVariants.Case1.ToString();
    }

    public void SetCase2()
    {
        if (m_text && m_characterVariants) m_text.text = m_characterVariants.Case2.ToString();
    }

    public void SetString(string text)
    {
        if(m_text)m_text.text = text;
    }

    public override void Select(UISelecterBase back)
    {
        m_bacgroundImage.color = new Color32(0x6D, 0x6D, 0x6D, 0xFF);
        if (m_text) m_text.color = Color.white;
        if(m_icon)m_icon.color = Color.white;
    }

    public override void Deselect(UISelecterBase next)
    {
        m_bacgroundImage.color = Color.white;
        if (m_text) m_text.color = new Color32(0x3b, 0x1c, 0x00, 0xff);
        if(m_icon)m_icon.color = new Color32(0x3b, 0x1c, 0x00, 0xff);
    }

    public override KeyData SelectionGeneric(SelectionDirection direction)
    {
        switch (direction)
        {
            case SelectionDirection.Up:
                if (m_upSelect != null) return m_upSelect;
                return this;
            case SelectionDirection.Down:
                if (m_downSelect != null) return m_downSelect;
                return this;
            case SelectionDirection.Left:
                if (m_leftSelect != null) return m_leftSelect;
                return this;
            case SelectionDirection.Right:
                if (m_rightSelect != null) return m_rightSelect;
                return this;
            default:
                return this;
        }
    }

}
