using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrappleController : MonoBehaviour
{
    [SerializeField] private float _grappleLength = 25.0f;
    
    [SerializeField] private GrappleVFX _grappleVFXPrefab;
    [SerializeField] private GameObject _grappleTargetGO;
    [SerializeField] private float _grappleCollisionBuffer = 0.005f;
    [SerializeField] private Camera _camera;
    
    public InputActionReference shootGrapple;

    private bool _grappleActive;

    private struct GrappleSection
    {
        public Vector3 Base;
        public Vector3 Tip;
        public Vector3 CollideNormal;
    }

    private const int MAX_GRAPPLE_VFX = 100;
    private List<GrappleVFX> _grappleVFXs = new List<GrappleVFX>(MAX_GRAPPLE_VFX);
    private List<GrappleSection> _grappleSections = new List<GrappleSection>();

    private bool _shootThisFrame;
    private bool _releaseThisFrame;

    private void Awake()
    {
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
        if (GSM.Instance.CurrentState is not GameStateAlive)
            return;

        if (!Game.Instance.ShouldGrapple)
        {
            if (_grappleActive)
                ReleaseGrapple();
            return;
        }
        
        bool shoot = _shootThisFrame;
        bool release = _releaseThisFrame;
        _shootThisFrame = false;
        _releaseThisFrame = false;

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
        
        const int mask = Utilities.GrappleCollisionMask | Utilities.BlockerCollisionMask;
        bool didHit = Physics.Raycast(transform.position, (mouseWorld - transform.position).normalized, out RaycastHit hit, _grappleLength, mask);
        if (didHit && hit.collider.gameObject.layer == (int)Utilities.PhysicsLayers.Blocker)
        {
            didHit = false;
        }
        _grappleTargetGO.SetActive(didHit);
        if (didHit)
        {
            Vector3 grapplePoint = Game.WrapAroundTower ? Game.ProjectOnTower(hit.point) : hit.point;
            _grappleTargetGO.transform.position = grapplePoint;
            
            if (shoot)
            {
                ShootGrapple(hit);
            }
        }
        
        if (release)
        {
            ReleaseGrapple();
        }

        if (_grappleActive)
        {
            _grappleSections[^1] = new GrappleSection() { Tip = transform.position, Base = _grappleSections[^1].Base, CollideNormal = _grappleSections[^1].CollideNormal};
            
            // Detect when a new grapple section needs to be made due to a grapple collision.
            {
                GrappleSection lastSection = _grappleSections[^1];
                Vector3 dir = (lastSection.Base - lastSection.Tip).normalized;
                float mag = (lastSection.Base - lastSection.Tip).magnitude;
                didHit = Physics.Raycast(lastSection.Tip + dir * _grappleCollisionBuffer, dir, out hit, mag - _grappleCollisionBuffer * 2.0f, mask);
                if (didHit)
                {
                    // Modify the existing section.
                    Vector3 perp = Game.WrapAroundTower ? Game.ProjectOnTower(hit.point).normalized : Vector3.back;
                    Vector3 collideNormal = Vector3.Cross(perp, (_grappleSections[^1].Base - hit.point).normalized);
                    
                    if (Vector3.Dot(hit.normal, collideNormal) <= 0.0f) 
                        collideNormal = Vector3.Cross(-perp, (_grappleSections[^1].Base - hit.point).normalized);
                    
                    //DebugSphere.Instance.transform.position = hit.point + collideNormal;
                    
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
                didHit = Physics.Raycast(closeSection.Tip + dir * _grappleCollisionBuffer, dir, out hit, mag - _grappleCollisionBuffer * 2.0f, mask);
                
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

            Vector3 target = _grappleSections[^1].Base;
            Player.Instance.Controller.SetGrapple(target);

            _grappleTargetGO.transform.position = target;
        }
    }

    private void ShootGrapple(RaycastHit hit)
    {
        _grappleSections.Add(new GrappleSection() { Tip = transform.position, Base = hit.point});
        _grappleActive = true;
        _grappleTargetGO.SetActive(true);
    }

    private void ReleaseGrapple()
    {
        for (int i = 0; i < _grappleVFXs.Count; i++)
        {
            _grappleVFXs[i].Off();
        }

        _grappleActive = false;
        _grappleTargetGO.SetActive(false);
        
        Player.Instance.Controller.StopGrapple();
        _grappleSections.Clear();
    }
}
