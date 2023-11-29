using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using UnityEngine.InputSystem;
using Plane = UnityEngine.Plane;
using Quaternion = UnityEngine.Quaternion;
using Vector2 = UnityEngine.Vector2;
using Vector3 = UnityEngine.Vector3;

public class GrappleController : Singleton<GrappleController>
{
    [SerializeField] private float _grappleLength = 25.0f;
    [SerializeField] private float _grappleSpeed = 20.0f;
    [SerializeField] private float _grappleHangTime = 1.0f;
    
    [SerializeField] private GrappleVFX _grappleVFXPrefab;
    [SerializeField] private GameObject _grappleTargetGO;
    [SerializeField] private float _grappleCollisionBuffer = 0.005f;
    [SerializeField] private Camera _camera;
    
    public InputActionReference shootGrapple;

    public struct GrappleSection
    {
        public Vector3 Base;
        public Vector3 Tip;
        public Vector3 CollideNormal;
    }

    private enum GrappleState
    {
        Inactive,
        Shooting,
        Hooked,
    }

    const int GRAPPLE_MASK = Utilities.GrappleCollisionMask | Utilities.BlockerCollisionMask;
    private const int MAX_GRAPPLE_VFX = 100;
    private List<GrappleVFX> _grappleVFXs = new List<GrappleVFX>(MAX_GRAPPLE_VFX);
    private List<GrappleSection> _grappleSections = new List<GrappleSection>();

    private bool _shootThisFrame;
    private bool _releaseThisFrame;

    private GrappleState _grappleState = GrappleState.Inactive;
    private Vector2 _grappleAngle;
    private float _grappleExtension;
    private float _grappleHangTimer;

    public List<GrappleSection> ActiveGrappleSections => _grappleSections;
    public bool IsGrappling => _grappleState == GrappleState.Hooked;
    
    [SerializeField] private InputActionReference aim;

    protected override void Awake()
    {
        base.Awake();
        
        for (int i = 0; i < MAX_GRAPPLE_VFX; i++)
        {
            _grappleVFXs.Add(Instantiate(_grappleVFXPrefab));
            _grappleVFXs[^1].Off();
        }
        _grappleTargetGO.SetActive(false);
    }

    private void Update()
    {
        _shootThisFrame |= shootGrapple.action.WasPressedThisFrame();
        _releaseThisFrame |= shootGrapple.action.WasReleasedThisFrame();
    }

    private void FixedUpdate()
    {
        if (GSM.Instance.CurrentState is GameStateDead && _grappleState != GrappleState.Inactive)
        {
            ReleaseGrapple();
        }
        
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;

        if (!Game.Instance.ShouldGrapple)
        {
            if (_grappleState != GrappleState.Inactive)
                ReleaseGrapple();
            return;
        }

        switch (_grappleState)
        {

            case GrappleState.Inactive:
                ProcessGrappleInactive();
                break;
            case GrappleState.Shooting:
                ProcessGrappleShooting();
                break;
            case GrappleState.Hooked:
                ProcessGrappleHooked();
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
        
        for (int i = 0; i < _grappleVFXs.Count; i++)
        {
            if (i < _grappleSections.Count)
            {
                _grappleVFXs[i].On(_grappleSections[i].Tip, _grappleSections[i].Base);
            }
            else
            {
                _grappleVFXs[i].Off();
            }
        }

        _releaseThisFrame = false;
        _shootThisFrame = false;
    }

    private void ProcessGrappleInactive()
    {
        Vector3 dir = CalculateShootDirection();
        bool didHit = CalculateGrappleHit(dir, 99999.0f, out RaycastHit hit);
        _grappleTargetGO.SetActive(didHit);
        //if (didHit)
        {
            Vector3 grapplePoint = Game.WrapAroundTower ? Game.ProjectOnTower(hit.point) : hit.point;
            _grappleTargetGO.transform.position = grapplePoint;
            
            if (_shootThisFrame)
            {
                _shootThisFrame = false;
                ShootGrapple();
            }
        }
    }
    
    private void ProcessGrappleShooting()
    {
        if (_releaseThisFrame)
        {
            ReleaseGrapple();
            return;
        }

        Vector3 dir = PlayerForward() * _grappleAngle.x + Vector3.up * _grappleAngle.y;
        bool didHit = CalculateGrappleHit(dir, _grappleExtension, out RaycastHit hit);
        if (didHit)
        {
            _grappleSections[^1] = new GrappleSection() { Tip = transform.position, Base = hit.point, CollideNormal = _grappleSections[^1].CollideNormal};
            _grappleState = GrappleState.Hooked;
            Player.Instance.SFXGrappleImpact();
            return;
        }
        
        Vector3 shootPos = transform.position + dir * _grappleExtension;
        _grappleSections[^1] = new GrappleSection() { Tip = transform.position, Base = shootPos, CollideNormal = _grappleSections[^1].CollideNormal};
        
        if (_grappleExtension > _grappleLength)
        {
            _grappleHangTimer += Time.deltaTime;
            if (_grappleHangTimer > _grappleHangTime)
            {
                ReleaseGrapple();
            }
        }
        else
        {
            _grappleExtension += _grappleSpeed * Time.deltaTime;
        }
    }
    
    private void ProcessGrappleHooked()
    {
        if (_releaseThisFrame)
        {
            ReleaseGrapple();
            return;
        }

        _grappleSections[^1] = new GrappleSection() { Tip = transform.position, Base = _grappleSections[^1].Base, CollideNormal = _grappleSections[^1].CollideNormal};
            
        // Detect when a new grapple section needs to be made due to a grapple collision.
        {
            GrappleSection lastSection = _grappleSections[^1];
            Vector3 dir = (lastSection.Base - lastSection.Tip).normalized;
            float mag = (lastSection.Base - lastSection.Tip).magnitude;
            bool didHit = Physics.Raycast(lastSection.Tip + dir * _grappleCollisionBuffer, dir, out RaycastHit hit, mag - _grappleCollisionBuffer * 2.0f, GRAPPLE_MASK);
            if (didHit)
            {
                // Modify the existing section.
                Vector3 perp = Game.WrapAroundTower ? Game.ProjectOnTower(hit.point).normalized : Vector3.back;
                Vector3 collideNormal = Vector3.Cross(perp, (_grappleSections[^1].Base - hit.point).normalized);
                
                if (Vector3.Dot(hit.normal, collideNormal) <= 0.0f) 
                    collideNormal = Vector3.Cross(-perp, (_grappleSections[^1].Base - hit.point).normalized);

                _grappleSections[^1] = new GrappleSection() { Tip = hit.point, Base = _grappleSections[^1].Base, CollideNormal = collideNormal };
                
                // Add new section.
                _grappleSections.Add(new GrappleSection() { Tip = transform.position, Base = hit.point });
            }
        }

        // Detect when two grapple sections can be collapsed because there is no longer a collision between them.
        if (_grappleSections.Count > 1)
        {
            GrappleSection farSection = _grappleSections[^2];
            GrappleSection closeSection = _grappleSections[^1];
            
            Vector3 closeToFar = farSection.Base - closeSection.Tip;
            
            Vector3 dir = closeToFar.normalized;
            float mag = closeToFar.magnitude;
            bool didHit = Physics.Raycast(closeSection.Tip + dir * _grappleCollisionBuffer, dir, out RaycastHit hit, mag - _grappleCollisionBuffer * 2.0f, GRAPPLE_MASK);
            
            if (!didHit)
            {
                Vector3 tipDir = (closeSection.Tip - closeSection.Base).normalized;
                if (Vector3.Dot(tipDir, farSection.CollideNormal) >= 0.0f)
                {
                    // Remove last section.
                    _grappleSections.RemoveAt(_grappleSections.Count - 1);
            
                    // Expand new last section.
                    _grappleSections[^1] = new GrappleSection() { Tip = closeSection.Tip, Base = farSection.Base, CollideNormal = farSection.CollideNormal};
                }
            }
        }

        Vector3 target = _grappleSections[^1].Base;
        Player.Instance.Controller.SetGrapple(target);

        _grappleTargetGO.transform.position = target;
    }

    private void ShootGrapple()
    {
        _grappleState = GrappleState.Shooting;
        _grappleSections.Add(new GrappleSection() { Tip = transform.position, Base = transform.position});
        _grappleTargetGO.SetActive(true);
        _grappleExtension = 0.0f;
        _grappleHangTimer = 0.0f;

        Vector3 dir = CalculateShootDirection();
        _grappleAngle.x = Vector3.Dot(dir, PlayerForward());
        _grappleAngle.y = Vector3.Dot(dir, Vector3.up);
        
        Player.Instance.SFXGrapple();
    }

    private void ReleaseGrapple()
    {
        for (int i = 0; i < _grappleVFXs.Count; i++)
        {
            _grappleVFXs[i].Off();
        }

        _grappleState = GrappleState.Inactive;
        _grappleTargetGO.SetActive(false);
        
        Player.Instance.Controller.StopGrapple();
        _grappleSections.Clear();
    }

    private Vector3 CalculateShootDirection()
    {
        if (Player.Instance.PlayerInput.currentControlScheme.Contains("Gamepad"))
        {
            // Rotate movement by current viewing angle
            Vector2 playerMove = aim.action.ReadValue<Vector2>();
            Quaternion viewYaw = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0);
            Vector3 rotatedVector = viewYaw * playerMove;
            return rotatedVector.normalized * Mathf.Min(rotatedVector.magnitude, 1.0f);
        }
        else
        {
            // Grapple target preview
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 mouseWorld = Vector3.one;

            if (!Game.WrapAroundTower)
            {
                Plane plane = new Plane(Vector3.forward, Vector3.zero);
                Ray ray = _camera.ScreenPointToRay(mousePos);
                if (plane.Raycast(ray, out float dist))
                {
                    mouseWorld = ray.GetPoint(dist);
                }
            }
            else
            {
                mouseWorld = _camera.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, (transform.position - _camera.transform.position).magnitude));
                mouseWorld = Game.ProjectOnTower(mouseWorld);
            }
            return (mouseWorld - transform.position).normalized;
        }
    }

    private bool CalculateGrappleHit(Vector3 dir, float dist, out RaycastHit hit)
    {
        bool didHit = Physics.Raycast(transform.position, dir, out hit, dist, GRAPPLE_MASK);
        return didHit && hit.collider.gameObject.layer != (int)Utilities.PhysicsLayers.Blocker;
    }

    private Vector3 PlayerForward()
    {
        Vector3 playerPosN = Utilities.Flatten(transform.position).normalized;
        return Vector3.Cross(playerPosN, Vector3.up);
    }
}
