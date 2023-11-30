using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using Easing = Utils.Easing;

public class GameStateElevator : MonoBehaviour, IGameState
{
    [SerializeField] private RawImage _whiteFade;
    
    private Player _player;
    private float _duration;

    public void OnEnter(IGameState prevState)
    {
    }
    
    public void OnExit(IGameState newState)
    {
    }
    
    public void ManualUpdate()
    {
        _duration += Time.deltaTime;

        float lower = 10.0f;
        float upper = 30.0f;
        float t = Mathf.Clamp01((_duration - lower) / (upper - lower));
        t = Easing.In(t);
        _whiteFade.color = new Color(1.0f, 1.0f, 1.0f, t);

        if (_duration > 35.0f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
    
    public bool ShouldEnter(IGameState currentState)
    {
        return GSM.Instance.CurrentState is GameStateAlive && Game.Instance.PlayerOnTopOfTower;
    }
}
