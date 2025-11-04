using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Mukouyama
{
    public class PlayerName : MonoBehaviour
    {
        [SerializeField] GameObject m_PlayerName;
        // Start is called before the first frame update
        public void SetPlayerName(int index)
        {
            m_PlayerName.GetComponent<TextMeshProUGUI>().text =
            PlayersData.instance.m_PlayerInfoArray[index].Player_Name;
        }
    } 
}
