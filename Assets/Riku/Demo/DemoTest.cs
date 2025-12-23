using UnityEngine;

public class DemoTest : MonoBehaviour
{
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip clip;
    [SerializeField] AudioClip clip2;

    [SerializeField] private GameObject player1;
    [SerializeField] private GameObject player2;
    [SerializeField] private GameObject player3;
    [SerializeField] private GameObject player4;

    private Animator animator1;
    private Animator animator2;
    private Animator animator3;
    private Animator animator4;

    [SerializeField] private GameObject lightObj;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        LookCamera(player1);
        LookCamera(player2);
        LookCamera(player3);
        LookCamera(player4);
        RenderSettings.ambientIntensity = 0.75f;
        //lightObj.SetActive(false);

        animator1 = player1.GetComponent<Animator>();
        animator2 = player2.GetComponent<Animator>();
        animator3 = player3.GetComponent<Animator>();
        animator4 = player4.GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            audioSource.PlayOneShot(clip);
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            lightObj.SetActive(true);
            audioSource.PlayOneShot(clip2);
        }
        if (Input.GetKeyDown(KeyCode.S))
        {
            RenderSettings.ambientIntensity = 1f;

            animator1.SetBool("isApplause", true);
            animator2.SetBool("isApplause", true);
            animator3.SetBool("isRejoice", true);
            animator4.SetBool("isApplause", true);
        }
    }

    void LookCamera(GameObject go)
    {
        Vector3 targetPos = Camera.main.transform.position;
        targetPos.y = go.transform.position.y;
        go.transform.LookAt(targetPos);
    }
}
