using System;
using ImGuiNET;
using nickmaltbie.OpenKCC.Demo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Vector3 = UnityEngine.Vector3;

public class Player : Singleton<Player>
{
    private KinematicCharacterController _controller;
    public KinematicCharacterController Controller => _controller;
    public Camera Camera => _camera;

    [SerializeField] private float _respawnTime = 2.0f;
    
    [SerializeField] private Camera _camera;
    private PlayerInput _input;

    private bool _prevOverUi;

    public static Action<Player> OnPlayerCreated;
    public static Action<Player> OnPlayerDestroyed;

    private GameObject _activeMonument;
    private float _respawnTimer;
    private bool _didDie;

    public bool IsDead => _didDie;
    
    protected override void Awake()
    {
        base.Awake();
        
        _input = GetComponent<PlayerInput>();
        _controller = GetComponent<KinematicCharacterController>();
    }

    private void Start()
    {
        OnPlayerCreated?.Invoke(this);
    }

    protected override void OnDestroy()
    {
        OnPlayerDestroyed?.Invoke(this);
        
        base.OnDestroy();
    }

    private void FixedUpdate()
    {
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;
        
        _controller.ManualFixedUpdate();
    }

    private void Update()
    {
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;
        
        // Disable player input if mouse over ImGui elements.
        ImGuiIOPtr io = ImGui.GetIO();
        if (io.WantCaptureMouse && !_prevOverUi)
        {
            _input.DeactivateInput();
            _prevOverUi = true;
        }
        else if (!io.WantCaptureMouse && _prevOverUi)
        {
            _input.ActivateInput();
            _prevOverUi = false;
        }

        _respawnTime -= Time.deltaTime;
        if (_didDie && _respawnTime < 0.0f)
        {
            Respawn();
        }
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
    }

    private void Death()
    {
        _respawnTime = 2.0f;
        _didDie = true;
    }

    private void Respawn()
    {
        if (_activeMonument == null)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        _didDie = false;
        transform.position = _activeMonument.transform.position + Vector3.up * 5.0f;
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;
        
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.KillZone)
        {
            Death();
        }

        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument)
        {
            Monument monument = other.gameObject.GetComponent<Monument>();
            monument.OnActivate();
            _activeMonument = monument.gameObject;
            UI.Instance.EnterCinematic();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;
        
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument)
        {
            Monument monument = other.gameObject.GetComponent<Monument>();
            monument.OnDeactivate();
            UI.Instance.ExitCinematic();
        }
    }
}
