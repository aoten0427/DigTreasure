using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

namespace Option
{
    public class OptionManager : MonoBehaviour
    {
        InputGame m_input;
        [SerializeField] OptionBaseView m_optionBaseView;

        [SerializeField] KeyConfig m_keyConfig;

        private void Start()
        {
            m_input = new InputGame();
            if (m_optionBaseView == null) m_optionBaseView = GetComponent<OptionBaseView>();

            m_keyConfig.Initailize(m_input);
        }

        private void Update()
        {
            if (Input.GetKeyUp(KeyCode.Alpha1))
            {
                m_optionBaseView.Open();
            }
            if (Input.GetKeyUp(KeyCode.Alpha2))
            {
                m_optionBaseView.Close();
            }
        }
    } 
}
