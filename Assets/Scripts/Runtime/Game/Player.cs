using System;
using ImGuiNET;
using nickmaltbie.OpenKCC.Demo;
using UnityEngine;
using UnityEngine.InputSystem;
using Vector3 = UnityEngine.Vector3;

public class Player : Singleton<Player>
{
    private KinematicCharacterController _controller;
    public KinematicCharacterController Controller => _controller;
    public Camera Camera => _camera;
    
    [SerializeField] private Camera _camera;
    private PlayerInput _input;

    private bool _prevOverUi;

    public static Action<Player> OnPlayerCreated;
    public static Action<Player> OnPlayerDestroyed;
    
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
    }

    public void Teleport(Vector3 position)
    {
        transform.position = position;
    }
}
