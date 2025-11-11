using Mukouyama;
using TMPro;
using UnityEngine;
public class PlayerName : MonoBehaviour
{
    public void SetUI_PlayerName(int index)
    {
        this.GetComponent<TextMeshProUGUI>().text =
         PlayersData.instance.m_PlayerInfoArray[index].Player_Name;
    }
}