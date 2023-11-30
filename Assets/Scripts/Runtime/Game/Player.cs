using System;
using System.Collections.Generic;
using ImGuiNET;
using nickmaltbie.OpenKCC.Demo;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using Utils;
using Random = UnityEngine.Random;
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

    [SerializeField] private Collider _avatarCollider;
    
    [SerializeField] private InputActionReference resetAction;
    
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
    
    [Header("SFX")]
    [SerializeField] private AudioClip[] _footsteps;
    [SerializeField] private AudioClip _spikeDeath;
    [SerializeField] private AudioClip _squashDeath;
    [SerializeField] private AudioClip _jump;
    [SerializeField] private AudioClip _grapple;
    [SerializeField] private AudioClip _grappleImpact;
    [SerializeField] private AudioClip _land;
    [SerializeField] private AudioClip _monument;

    private AudioSource[] _footstepSrc;
    private AudioSource _spikeDeathSrc;
    private AudioSource _squashDeathSrc;
    private AudioSource _jumpSrc;
    private AudioSource _grappleSrc;
    private AudioSource _grappleImpactSrc;
    private AudioSource _landSrc;
    private AudioSource _monumentSrc;
    
    public bool IsDead => _didDie;
    public PlayerInput PlayerInput => _input;

    private List<GameObject> _queueDestroy = new List<GameObject>();

    public List<Monument> AllMonuments;
    
    public void SFXJump() { _jumpSrc.Play(); }
    public void SFXGrapple() { _grappleSrc.Play(); }
    public void SFXGrappleImpact() { _grappleImpactSrc.Play(); }
    public void SFXLand() { _landSrc.Play(); }
    
    protected override void Awake()
    {
        base.Awake();
        
        _input = GetComponent<PlayerInput>();
        _controller = GetComponent<KinematicCharacterController>();

        _footstepSrc = new AudioSource[8];
        for (int i = 0; i < 8; i++)
        {
            _footstepSrc[i] = gameObject.AddComponent<AudioSource>(); 
            _footstepSrc[i].clip = _footsteps[i];
            _footstepSrc[i].volume = 0.5f;
        }
        
        _spikeDeathSrc = gameObject.AddComponent<AudioSource>();
        _spikeDeathSrc.clip = _spikeDeath;
        
        _squashDeathSrc = gameObject.AddComponent<AudioSource>();
        _squashDeathSrc.clip = _squashDeath;
        
        _jumpSrc = gameObject.AddComponent<AudioSource>();
        _jumpSrc.clip = _jump;
        _jumpSrc.volume = 0.0f;
        
        _grappleSrc = gameObject.AddComponent<AudioSource>();
        _grappleSrc.clip = _grapple;
        
        _grappleImpactSrc = gameObject.AddComponent<AudioSource>();
        _grappleImpactSrc.clip = _grappleImpact;
        
        _landSrc = gameObject.AddComponent<AudioSource>();
        _landSrc.clip = _land;
        _landSrc.volume = 0.0f;

        _monumentSrc = gameObject.AddComponent<AudioSource>();
        _monumentSrc.clip = _monument;
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
        if (GSM.Instance.CurrentState is GameStateAlive || GSM.Instance.CurrentState is GameStateDead || GSM.Instance.CurrentState is GameStateElevator)
        {
            _controller.ManualFixedUpdate();
        }
    }

    private System.Random _rng = new System.Random();
    private static float _runCadence = 0.3f;
    private float _runningTimer = _runCadence * 0.5f;

    private void Update()
    {
        if (resetAction.action.WasPressedThisFrame())
        {
            _controller.ZeroVelocity();
            Respawn();
        }
        
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
            if (KinematicCharacterController.IsRunning)
            {
                _runningTimer += Time.deltaTime;
                if (_runningTimer > _runCadence)
                {
                    _runningTimer = 0.0f;
                    _footstepSrc[_rng.Next() % 8].Play();
                }
            }
            else
            {
                _runningTimer = _runCadence * 0.5f;
            }
        }

        if (_grappleStatePrev != GrappleController.Instance.IsGrappling)
        {
            _avatarRenderer.enabled = !GrappleController.Instance.IsGrappling;
            _grappleRenderer.enabled = GrappleController.Instance.IsGrappling;
            (_avatarCollider as CapsuleCollider).height = GrappleController.Instance.IsGrappling ? 2.0f : 4.0f;
            _grappleHalo.SetActive(GrappleController.Instance.IsGrappling);
            _grappleStatePrev = GrappleController.Instance.IsGrappling;
            _grappleTransitionTimer = 0.0f;

            if (!GrappleController.Instance.IsGrappling)
            {
                bool hit = Physics.SphereCast(transform.position, (_avatarCollider as CapsuleCollider).radius,Vector3.down, out RaycastHit hitInfo, 2.0f, ~0, QueryTriggerInteraction.Ignore);
                if (hit)
                {
                    transform.position += Vector3.up * (2.0f - hitInfo.distance);
                }
                hit = Physics.SphereCast(transform.position, (_avatarCollider as CapsuleCollider).radius,Vector3.up, out hitInfo, 2.0f, ~0, QueryTriggerInteraction.Ignore);
                if (hit)
                {
                    transform.position += Vector3.down * (2.0f - hitInfo.distance);
                }
            }
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
        _spikeDeathSrc.Play();
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
                GrappleController.Instance.IncreaseGrappleLength();
            }
            _monumentSrc.Play();
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
