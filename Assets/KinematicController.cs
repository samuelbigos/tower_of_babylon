using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(PlayerInput))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class KinematicController : MonoBehaviour
{
    [SerializeField] private float _acceleration = 1.0f;
    [SerializeField] private float _deceleration = 0.9f;
    [SerializeField] private float _jumpImpulse = 1000.0f;
    
    private Rigidbody _rb;
    private CapsuleCollider _collider;
    private PlayerInput _input;
    
    private Vector2 _inputMovement;
    private Vector2 _inputLookAt;
    private Vector3 _velocity;
    private bool _freeze;
    private bool _doJump;
    
    private bool _grounded;
    private Vector3 _groundNormal;
    private Vector3 _groundPos;

    private float Height => _collider.height * 0.5f + _collider.radius;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<CapsuleCollider>();
        _input = GetComponent<PlayerInput>();
    }

    private void Start()
    {
    }

    private void Update()
    {
        // if (!Mathf.Approximately(0.0f, _inputMovement.x))
        // {
        //     Vector3 right = Vector3.right;
        //     Vector3 movement = _inputMovement.x * right;
        //     _velocity += movement.normalized * Time.deltaTime * _acceleration;
        // }
        
        _velocity += Physics.gravity * Time.deltaTime;

        _grounded = CheckGrounded(out RaycastHit hit);

        if (_grounded)
        {
            _groundNormal = hit.normal;
            _groundPos = hit.point;
            
            transform.position = _groundPos + new Vector3(0.0f, Height * 0.5f, 0.0f);
            
            if (_doJump)
            {
                if (_grounded)
                {
                    _velocity.y += _jumpImpulse;
                }
                _doJump = false;
            }

            _velocity.y = 0.0f;
        }
        else
        {
            transform.position += _velocity * Time.deltaTime;
        }
        
        // float y = _velocity.y;
        // _velocity = Vector3.Lerp(_velocity, Vector3.zero, 1.0f * _deceleration * Time.deltaTime);
        // _velocity.y = y;
    }
    
    private bool CheckGrounded(out RaycastHit groundHit)
    {
        bool onGround = CastSelf(transform.position, transform.rotation, Vector3.down, 0.1f, out groundHit);
        float angle = Vector3.Angle(groundHit.normal, Vector3.up);
        return onGround && angle < 30.0f;
    }
    
    // https://github.com/nicholas-maltbie/OpenKCC/blob/6c37fb120165ca0b96a2559ce1eb187c15a9625c/Assets/OpenKCC/Scripts/Demo/SimplifiedKCC.cs#L208
    private bool CastSelf(Vector3 pos, Quaternion rot, Vector3 dir, float dist, out RaycastHit hit)
    {
        // Get Parameters associated with the KCC
        Vector3 center = pos;
        float radius = _collider.radius;
        float height = _collider.height;

        // Get top and bottom points of collider
        Vector3 bottom = pos - new Vector3(0.0f, Height * 0.5f, 0.0f);
        Vector3 top = pos + new Vector3(0.0f, Height * 0.5f, 0.0f);

        // Check what objects this collider will hit when cast with this configuration excluding itself
        IEnumerable<RaycastHit> hits = Physics.CapsuleCastAll(
            top, bottom, radius, dir, dist, ~0, QueryTriggerInteraction.Ignore);

        int numHits = 0;
        foreach (RaycastHit h in hits)
        {
            if (h.transform == transform)
                continue;
            numHits++;
        }
        bool didHit = numHits > 0;

        // Find the closest objects hit
        float closestDist = didHit ? Enumerable.Min(hits.Select(hit => hit.distance)) : 0;
        IEnumerable<RaycastHit> closestHit = hits.Where(hit => hit.distance == closestDist);

        // Get the first hit object out of the things the player collides with
        hit = closestHit.FirstOrDefault();

        // Return if any objects were hit
        return didHit;
    }
    
    private void OnMovement(InputValue value)
    {
        _inputMovement = value.Get<Vector2>();
    }
    
    private void OnLookAt(InputValue value)
    {
        _inputLookAt = value.Get<Vector2>();
    }

    private void OnJump(InputValue value)
    {
        _doJump = true;
    }
}
