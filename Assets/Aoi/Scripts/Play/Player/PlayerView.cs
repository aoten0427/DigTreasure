using UnityEngine;

public class PlayerView : MonoBehaviour
{
    [SerializeField]
    MeshFilter m_meshFilter;

    public MeshFilter MeshFilter { get { return m_meshFilter; } set { MeshFilter = value; } }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
