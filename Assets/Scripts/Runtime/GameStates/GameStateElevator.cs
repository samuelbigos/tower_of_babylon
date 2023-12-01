using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using Easing = Utils.Easing;

public class GameStateElevator : MonoBehaviour, IGameState
{
    [SerializeField] private RawImage _whiteFade;
    [SerializeField] private TextMeshProUGUI _credits;
    
    private Player _player;
    private float _duration;

    public void OnEnter(IGameState prevState)
    {
        Game.Instance.TimingRun = false;
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void ManualUpdate()
    {
        _duration += Time.deltaTime;

        float lower = 15.0f;
        float upper = 25.0f;
        float t = Mathf.Clamp01((_duration - lower) / (upper - lower));
        t = Easing.In(t);
        _whiteFade.color = new Color(1.0f, 1.0f, 1.0f, t);

        _credits.text =
            $"You scaled the tower in {Game.Instance.RunTime:F2} seconds.<br><br>Tower of Babylon is a game by Sam Bigos and Floyd Billingy.<br><br>It was inspired by a short story of the same name by Ted Chiang.<br><br>Scale the tower again and beat your time!<br><br><sprite index=0>";
        lower = 20.0f;
        t = Mathf.Clamp01((_duration - lower) / 10.0f);
        t = Easing.In(t);
        _credits.color = new Color(0.0f, 0.0f, 0.0f, t);
        
        if (InputManager.Instance.PlayerShoot.action.WasPressedThisFrame() && _duration > 25.0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        return GSM.Instance.CurrentState is GameStateAlive && Game.Instance.PlayerOnTopOfTower;
    }
}
