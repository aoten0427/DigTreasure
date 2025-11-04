using UnityEngine;
using UnityEngine.UI;

public class AutoFont : MonoBehaviour
{
    [SerializeField] private Sprite[] _sprites;
    [SerializeField][TextArea] private string _text;
    [SerializeField] private Image _imagePrefab;
    [SerializeField][Tooltip("0 ˆÈ‰º = ‚Å‚«‚é‚¾‚¯‘å‚«‚¢")] private int _fontSize;
    private HorizontalLayoutGroup _layoutGroup;

    private void Awake()
    {
        _layoutGroup = GetComponent<HorizontalLayoutGroup>();
        SetText(_text);
    }

    public void SetText(string text)
    {
        foreach (Transform t in transform)
            Destroy(t.gameObject);
        _text = text;
        if (_fontSize > 0)
            _layoutGroup.childForceExpandWidth = false;

        for (int i = 0; i < _text.Length; i++)
        {
            //0 = 48, 9 = 57, p = 112, t = 116, ' ' = 32
            int n = _text.ToLower()[i];
            Sprite sprite;

            //0-9
            if (n >= 48 && n <= 57)
                sprite = _sprites[n - 48];
            //' '
            else if (n == 32)
                sprite = _sprites[0];
            //pt
            else if (n == 112 && i < _text.Length - 1 && _text.ToLower()[i + 1] == 116)
                sprite = _sprites[10];
            else
                continue;

            Image newChar = Instantiate(_imagePrefab, transform);
            if(_fontSize > 0)
                newChar.GetComponent<LayoutElement>().preferredWidth = _fontSize;
            newChar.sprite = sprite;
        }
    }
}
