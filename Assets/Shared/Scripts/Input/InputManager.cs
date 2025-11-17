using UnityEngine;

public class InputManager : MonoBehaviour
{
    private static InputManager instance;
    public static InputManager Instance => instance;

    IInput m_currentInput;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            m_currentInput = InputType.UseInput();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public Vector3 Move() { return m_currentInput.Move(); }
    public bool Jump() { return m_currentInput.Jump(); }
    public bool Dig() { return m_currentInput.Dig(); }
    public bool Attack() { return m_currentInput.Attack(); }
    public bool LookOn() { return m_currentInput.LookOn(); }
    public bool Gard() {  return m_currentInput.Gard(); }   
}
