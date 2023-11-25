using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;

public class UI : Singleton<UI>
{
    [SerializeField] private float _cinematicTransitionTime = 0.5f;
    [SerializeField] private RectTransform _barTop;
    [SerializeField] private RectTransform _barBot;
    [SerializeField] private RawImage _fade;
    [SerializeField] private TextMeshProUGUI _monumentText;
    
    private bool _cinematicEnabled;
    private float _cinematicTransitionTimer;

    public void SetText(string text)
    {
        _monumentText.text = text;
    }

    public void EnterCinematic()
    {
        _cinematicEnabled = true;

    }

    public void ExitCinematic()
    {
        _cinematicEnabled = false;
    }

    private void Update()
    {
        if (_cinematicEnabled)
        {
            _cinematicTransitionTimer = Mathf.Min(_cinematicTransitionTimer + Time.deltaTime, _cinematicTransitionTime);
        }
        else
        {
            _cinematicTransitionTimer = Mathf.Max(_cinematicTransitionTimer - Time.deltaTime, 0.0f);
        }
        
        float t = Easing.InOut(Mathf.Clamp01(_cinematicTransitionTimer / _cinematicTransitionTime));
        _barTop.sizeDelta = new Vector2(Screen.width, Screen.height * 0.2f * t);
        _barBot.sizeDelta = new Vector2(Screen.width, Screen.height * 0.2f * t);
        _fade.color = new Color(1.0f, 1.0f, 1.0f, t * 0.66f);
        _monumentText.color = new Color(1.0f, 1.0f, 1.0f, t);
    }
}
