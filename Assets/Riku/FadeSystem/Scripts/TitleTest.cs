using UnityEngine;
using UnityEngine.UI;

public class TitleTest : MonoBehaviour
{
    private Material material;
    private bool isStart;
    private float matValue;
    [SerializeField] private float defaultValue = .3f;
    [SerializeField] private float speed = .1f;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        material = this.GetComponent<Image>().material;
        matValue = defaultValue;
        material.SetFloat("_time", matValue);
        isStart = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (isStart && matValue != 0f)
        {
            matValue -= speed * Time.deltaTime;
            if (matValue < 0) matValue = 0f;
            material.SetFloat("_time", matValue);
            return;
        }
        else if (isStart && Input.GetKeyDown(KeyCode.R)) this.Start();

        if (Input.GetKeyDown(KeyCode.Space)) isStart = true;
    }
}
