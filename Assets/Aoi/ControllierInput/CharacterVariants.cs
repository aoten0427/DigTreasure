using UnityEngine;


/// <summary>
/// ƒL[‚Ìí—Ş‚ğ’è‹`
/// </summary>


[CreateAssetMenu(fileName = "CharacterVariants", menuName = "CharacterVariants")]
public class CharacterVariants : ScriptableObject
{


    [SerializeField] private char m_case1;
    [SerializeField] private char m_case2;

    public char Case1 { get { return m_case1; } }
    public char Case2 { get { return m_case2;} }
}
