using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class GrappleController : MonoBehaviour
{
    [SerializeField] private float _grappleForce = 10.0f;
    [SerializeField] private GameObject _grappleTargetGO;
    
    [SerializeField] private GameObject _grappleVFX;
    [SerializeField] private Transform _grappleP1;
    [SerializeField] private Transform _grappleP2;
    [SerializeField] private Transform _grappleP3;
    [SerializeField] private Transform _grappleP4;
    
    public InputActionReference shootGrapple;

    private bool _grappleActive;
    private Vector3 _grappleTarget;

    private void Awake()
    {
        _grappleVFX.SetActive(false);
        _grappleTargetGO.SetActive(false);
    }

    private void Update()
    {
        bool shoot = shootGrapple.action.WasPressedThisFrame();
        bool release = shootGrapple.action.WasReleasedThisFrame();

        // Grapple target preview
        Camera cam = Camera.main;
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Vector3 mouseWorld = Vector3.one;

        if (!Game.WrapAroundTower)
        {
            Plane plane = new Plane(Vector3.forward, Vector3.zero);
            Ray ray = cam.ScreenPointToRay(mousePos);
            if (plane.Raycast(ray, out float dist))
            {
                mouseWorld = ray.GetPoint(dist);
            }
        }
        else
        {
            mouseWorld = cam.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, (transform.position - cam.transform.position).magnitude));
            mouseWorld = Utilities.ProjectOnTower(mouseWorld);
        }
        
        const int mask = 1 << (int) Utilities.PhysicsLayers.Grapple;
        bool didHit = Physics.Raycast(transform.position, (mouseWorld - transform.position).normalized, out RaycastHit hit, 100.0f, mask);
        _grappleTargetGO.SetActive(didHit);
        if (didHit)
        {
            Vector3 grapplePoint = Game.WrapAroundTower ? Utilities.ProjectOnTower(hit.point) : hit.point;
            _grappleTargetGO.transform.position = grapplePoint;
            
            if (shoot)
            {
                ShootGrapple(grapplePoint);
            }
        }
        
        if (release)
        {
            ReleaseGrapple();
        }

        if (_grappleActive)
        {
            _grappleP1.position = transform.position;
            _grappleP2.position = transform.position;

            Player.Instance.Controller.ApplyGrapple(_grappleTarget);

            _grappleTargetGO.transform.position = _grappleTarget;
        }
    }

    private void ShootGrapple(Vector3 hit)
    {
        _grappleVFX.SetActive(true);
        _grappleTarget = hit;
        _grappleActive = true;

        _grappleVFX.transform.position = transform.position;
        _grappleP3.position = _grappleTarget;
        _grappleP4.position = _grappleTarget;
        
        _grappleTargetGO.SetActive(true);
    }

    private void ReleaseGrapple()
    {
        _grappleVFX.SetActive(false);
        _grappleActive = false;
        _grappleTargetGO.SetActive(false);
    }
}
