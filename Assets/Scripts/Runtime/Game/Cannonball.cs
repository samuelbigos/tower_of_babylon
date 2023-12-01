using System;
using System.Collections;
using System.Collections.Generic;
using System.Numerics;
using UnityEngine;
using Vector3 = UnityEngine.Vector3;

public class Cannonball : MonoBehaviour
{
    private Rigidbody _rb;

    [SerializeField] private ParticleSystem _poof;
    
    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }
    
    public void Launch(Vector3 velocity)
    {
        _rb.AddForce(velocity, ForceMode.Impulse);
    }

    private void FixedUpdate()
    {
        Vector3 velocity = _rb.velocity;
        
        // Re-project velocity onto the tower. Quick dirty way to make sure we don't retard speed when falling around
        // the tower.
        if (Game.WrapAroundTower)
        {
            if (!Mathf.Approximately(velocity.magnitude, 0.0f))
            {
                Vector3 reprojectionPoint = transform.position + velocity.normalized * 1.0f;
                reprojectionPoint = Game.ProjectOnTower(reprojectionPoint);
                _rb.velocity = (reprojectionPoint - transform.position).normalized * velocity.magnitude;
            }
            
            // Lock position to given radius.
            _rb.position = Game.ProjectOnTower(_rb.position);
        }
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.layer == (int)Utilities.PhysicsLayers.Monument || other.gameObject.layer == (int)Utilities.PhysicsLayers.KillZone)
        {
            ParticleSystem ps = Instantiate(_poof);
            ps.transform.parent = Game.Instance.transform;
            ps.transform.position = gameObject.transform.position;
            
            Destroy(gameObject);
        }
    }
}
