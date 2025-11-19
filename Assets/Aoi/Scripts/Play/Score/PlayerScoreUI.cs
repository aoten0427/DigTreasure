using TMPro;
using UnityEngine;
using UnityEngine.UI;


public class PlayerScoreUI:MonoBehaviour
{
    public Image[] m_scoreImages = new Image[4];  // スコア表示（4桁）
    public TextMeshProUGUI m_nameText;            // プレイヤー名
    public RectTransform m_rankUITransform;       // ランク移動用Transform
    public Canvas m_canvas;                        // sortingOrder制御用
}
