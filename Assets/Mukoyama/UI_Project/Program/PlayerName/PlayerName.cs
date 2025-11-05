using Mukouyama;
using TMPro;
using UnityEngine;
public class PlayerName : MonoBehaviour
{
    [SerializeField] GameObject m_UI_PlayerName;
    public void SetUI_PlayerName(int index)
    {
        m_UI_PlayerName.GetComponent<TextMeshProUGUI>().text =
            PlayersData.instance.m_PlayerInfoArray[index].Player_Name;
    }
}
