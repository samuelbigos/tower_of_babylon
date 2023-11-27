using System;
using System.Collections.Generic;
using ImGuiNET;
using nickmaltbie.OpenKCC.Demo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Utils;
using Vector3 = UnityEngine.Vector3;

public class Player : Singleton<Player>
{
    [SerializeField] private ParticleSystem _bloodVfx;
    
    private KinematicCharacterController _controller;
    public KinematicCharacterController Controller => _controller;
    public Camera Camera => _camera;

    [SerializeField] private float _respawnTime = 2.0f;
    [SerializeField] private Camera _camera;
    [SerializeField] private SkinnedMeshRenderer _avatarRenderer;
    [SerializeField] private MeshRenderer _grappleRenderer;
    [SerializeField] private GameObject _grappleHalo;
    
    private PlayerInput _input;

    private bool _prevOverUi;

    public static Action<Player> OnPlayerCreated;
    public static Action<Player> OnPlayerDestroyed;

    private Monument _activeMonument;
    private float _respawnTimer;
    private bool _didDie;
    private List<Monument> _visitedMonuments = new List<Monument>();
    private bool _grappleStatePrev = true;
    private float _grappleTransitionTimer;

    public bool IsDead => _didDie;
    public PlayerInput PlayerInput => _input;

    private List<GameObject> _queueDestroy = new List<GameObject>();
    
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
        if (GSM.Instance.CurrentState is GameStateAlive || GSM.Instance.CurrentState is GameStateDead)
        {
            _controller.ManualFixedUpdate();
        }
    }

    private void Update()
    {
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
        
        if (GSM.Instance.CurrentState is GameStateDead)
        {
            _respawnTime -= Time.deltaTime;
            if (_didDie && _respawnTime < 0.0f)
            {
                Respawn();
            }
        }

        if (GSM.Instance.CurrentState is GameStateAlive)
        {
        }

        if (_grappleStatePrev != GrappleController.Instance.IsGrappling)
        {
            _avatarRenderer.enabled = !GrappleController.Instance.IsGrappling;
            _grappleRenderer.enabled = GrappleController.Instance.IsGrappling;
            _grappleHalo.SetActive(GrappleController.Instance.IsGrappling);
            _grappleStatePrev = GrappleController.Instance.IsGrappling;
            _grappleTransitionTimer = 0.0f;
        }
        if (GrappleController.Instance.IsGrappling)
        {
            _grappleTransitionTimer += Time.deltaTime;
            float t = Easing.Out(Mathf.Clamp01(_grappleTransitionTimer / 0.2f));
            float s = Mathf.Lerp(3.0f, 1.8f, t);
            _grappleRenderer.transform.localScale = new Vector3(s,s,s);
            
            _grappleHalo.transform.localRotation = Quaternion.Euler(0.0f, 360.0f * _grappleTransitionTimer, 0.0f);   
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
        _bloodVfx.Play();
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
        _bloodVfx.Stop();

        foreach (GameObject toDestroy in _queueDestroy)
        {
            Destroy(toDestroy);
        }
        _queueDestroy.Clear();
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;
        
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.KillZone)
        {
            Death();

            if (other.gameObject.GetComponentInParent<Cannonball>())
            {
                _queueDestroy.Add(other.transform.parent.gameObject);
            }
        }

        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument)
        {
            Monument monument = other.gameObject.GetComponent<Monument>();
            monument.OnActivate();
            if (_activeMonument)
                _activeMonument.SetRespawnPoint(false);
            _activeMonument = monument;
            _activeMonument.SetRespawnPoint(true);
            UI.Instance.EnterCinematic();

            if (!_visitedMonuments.Contains(monument))
            {
                _visitedMonuments.Add(monument);
                Scarf.Instance.Upgrade();
            }
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
