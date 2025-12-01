using UnityEngine;

public class ScoreTest : MonoBehaviour
{
    [SerializeField] ScoreUIData m_scoredata;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       
    }

    // Update is called once per frame
    void Update()
    {
        if(Input.GetKeyUp(KeyCode.Space))
        {
            m_scoredata.AddUser(0, "user1");
            m_scoredata.AddUser(1, "user2");
            //m_scoredata.AddUser(2, "user3");
            m_scoredata.AddUser(3, "user4");
        }

        if(Input.GetKeyDown(KeyCode.Alpha1))
        {
            m_scoredata.UpdatePoint(0, 100);
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            m_scoredata.UpdatePoint(1, 200);
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            m_scoredata.UpdatePoint(2, 300);
        }
        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            m_scoredata.UpdatePoint(3, 400);
        }
    }
}
