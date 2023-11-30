// Copyright (C) 2023 Nicholas Maltbie
//
// Permission is hereby granted, free of charge, to any person obtaining a copy of this software and
// associated documentation files (the "Software"), to deal in the Software without restriction,
// including without limitation the rights to use, copy, modify, merge, publish, distribute,
// sublicense, and/or sell copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED, INCLUDING
// BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY
// CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE,
// ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
// SOFTWARE.

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.OpenKCC.Demo
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class KinematicCharacterController : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
    
        [SerializeField] private InputActionReference movePlayer;
        [SerializeField] private InputActionReference lookAtPlayer;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference shootAction;
        
        [SerializeField] private int maxBounces = 5;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float anglePower = 0.5f;
        [SerializeField] private float verticalSnapDown = 0.45f;
        [SerializeField] private float groundDist = 0.01f;
        [SerializeField] private float maxWalkingAngle = 60f;
        [SerializeField] private float jumpVelocity = 5.0f;
        [SerializeField] private float maxJumpAngle = 80f;
        [SerializeField] private float jumpCooldown = 0.25f;
        [SerializeField] private float coyoteTime = 0.05f;
        [SerializeField] private float jumpBufferTime = 0.05f;
        [SerializeField] [Range(0, 1)] private float jumpAngleWeightFactor = 0.0f;
        [SerializeField] private float _groundVelocityRetardation = 0.9f;
        
        [Header("Grappling")]
        [SerializeField] private float _grappleLeanInfluence = 5.0f;
        [SerializeField] private float _grappleRetraction = 10.0f;
        [SerializeField] private float _maxForce = 1.0f;
        [SerializeField] private float _bounceGrapple = 1.0f;
        [SerializeField] private float _bounce = 1.0f;
        [SerializeField] private Transform _avatar;

        private const float EPSILON = 0.001f;
        
        private float jumpInputElapsed = Mathf.Infinity;
        private float timeSinceLastJump = 0.0f;
        private float elapsedFalling = 0f;
        private bool notSlidingSinceJump = true;
        private Vector3 _velocity;
        private Vector2 cameraAngle;
        private CapsuleCollider capsuleCollider;
        private bool jumpInputPressed => jumpAction.action.IsPressed();// || shootAction.action.WasPressedThisFrame();
        private float _roofTimer;
        private Collider _groundCollider;
        private Vector3 _grappleDir;
        
        private Vector3 _grappleTarget;
        private bool _grappling;
        private Vector3 _steering;
        private bool _falling;

        public bool _allowDepthMovement;
        public bool _disallowLeftMovement;

        public Collider GroundCollider => _groundCollider;
        public Vector3 Velocity => _velocity;
        
        public void ZeroVelocity()
        {
            _velocity = Vector3.zero;
        }

        public void ApplyPositionDelta(Vector3 delta)
        {
            transform.position += delta;
        }
        
        private void Start()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
        }

        public void SetGrapple(Vector3 target)
        {
            _grappleTarget = target;
            _grappling = true;
        }

        public void StopGrapple()
        {
            _grappling = false;
        }

        private static Vector3 Limit(Vector3 vec, float vMax)
        {
            float length = vec.magnitude;
            if (length == 0.0f)
            {
                return vec;
            }

            float i = vMax / length;
            i = Mathf.Min(i, 1.0f);
            vec *= i;
            return vec;
        }

        private Vector3 _swingDir;

        private void DoGrapple()
        {
            Vector3 playerPos = Player.Instance.transform.position;
            
            // Extend/retract the grapple.
            Vector2 playerMove;
            if (Player.Instance.PlayerInput.currentControlScheme.Contains("Gamepad"))
            {
                playerMove = lookAtPlayer.action.ReadValue<Vector2>();
            }
            else
            {
                playerMove = movePlayer.action.ReadValue<Vector2>();
            }

            // Rotate movement by current viewing angle
            Quaternion viewYaw = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0);
            Vector3 rotatedVector = viewYaw * playerMove;
            Vector3 normalizedInput = rotatedVector.normalized * Mathf.Min(rotatedVector.magnitude, 1.0f);
            
            Vector3 playerPosN = Utilities.Flatten(playerPos).normalized;
            Vector3 grapplePosN = Utilities.Flatten(_grappleTarget).normalized;
            Vector3 grapplePosProjectedForward = Vector3.zero;
            Vector3 playerForward = Vector3.Cross(playerPosN, Vector3.up).normalized;
            bool left = Vector3.Dot(playerForward, (_grappleTarget - playerPos).normalized) < 0.0f;
            if (left) playerForward = -playerForward;
            if (Game.WrapAroundTower)
            {
                /* This chunk of code un-projects the grapple point from the circular tower,
                 so forces can be accurately calculated as if on a 2D plane. Then, the direction of 
                 swing is calculated and current velocity is projected onto that vector, so player
                 moves restricted to the arc made by the grapple. */

                // Gets the distance to the grapple point on 2D plane.
                float angle = Mathf.Acos(Vector3.Dot(playerPosN, grapplePosN));
                float distance = Game.TowerCircumference * (angle / (2.0f * Mathf.PI));

                grapplePosProjectedForward = playerPos + playerForward * distance;
                grapplePosProjectedForward.y += (_grappleTarget - playerPos).y;
            }
            else
            {
                grapplePosProjectedForward = _grappleTarget;
            }
            
            // Calculate swing direction.
            _grappleDir = (grapplePosProjectedForward - playerPos).normalized;
            Vector3 fromCamera = Game.WrapAroundTower ? Utilities.Flatten(playerPos) : Vector3.forward;
            _swingDir = Vector3.Cross(_grappleDir, fromCamera).normalized;
            if (!left) _swingDir = -_swingDir;

            // Project swing velocity onto swing direction.
            Vector3 grappleVelocity = _velocity + normalizedInput * _grappleLeanInfluence * Time.fixedDeltaTime;
            float speed = Vector3.Dot(grappleVelocity, _swingDir);
            Vector3 desiredVelocity = _swingDir * speed;
            
            // Move based on grapple retraction.
            Vector3 retraction = (grapplePosProjectedForward - playerPos).normalized * _grappleRetraction;
            desiredVelocity += retraction * playerMove.y;

            // Use steering forces to apply some smoothing to the grapple forces.
            _steering = Limit(desiredVelocity - _velocity, _maxForce);
            _velocity += _steering;
        }

        public void ManualFixedUpdate()
        {
            if (_grappling)
            {
                DoGrapple();
            }

            // Read input values from player
            Vector2 playerMove = movePlayer.action.ReadValue<Vector2>();
            
            if (_grappling)
            {
                playerMove.x = 0.0f;
            }
            
            // If player is not allowed to move, stop player input
            if (Player.Instance.IsDead)
            {
                playerMove = Vector2.zero;
            }

            // Rotate player based on mouse input, ensure pitch is bounded to not overshoot
            transform.rotation = Quaternion.Euler(0, cameraAngle.y, 0);

            // Check if the player is falling
            (bool onGround, float groundAngle) = CheckGrounded(_velocity, out RaycastHit groundHit);
            
            bool falling = !(onGround && groundAngle <= maxWalkingAngle);
            if (!falling && _falling && elapsedFalling > 0.5f)
            {
                Player.Instance.SFXLand();
            }
            _falling = falling;

            _groundCollider = onGround ? groundHit.collider : null;

            // If falling, increase falling speed, otherwise stop falling.
            if (_falling)
            {
                playerMove.x = 0.0f;
                _velocity += Physics.gravity * Time.fixedDeltaTime;
                elapsedFalling += Time.fixedDeltaTime;
                
                // Re-project velocity onto the tower. Quick dirty way to make sure we don't retard speed when falling around
                // the tower.
                if (Game.WrapAroundTower)
                {
                    if (!Mathf.Approximately(_velocity.magnitude, 0.0f))
                    {
                        Vector3 reprojectionPoint = transform.position + _velocity.normalized * 1.0f;
                        reprojectionPoint = Game.ProjectOnTower(reprojectionPoint);
                        _velocity = (reprojectionPoint - transform.position).normalized * _velocity.magnitude;
                    }
                }
            }
            else
            {
                Vector3 retardation = -_velocity * _groundVelocityRetardation;
                _velocity += retardation * Time.fixedDeltaTime;
                elapsedFalling = 0;
                notSlidingSinceJump = true;
            }

            // If the player is attemtping to jump and can jump allow for player jump
            if (jumpInputPressed)
            {
                Player.Instance.SFXJump();
                jumpInputElapsed = 0.0f;
            }
            // Player is attempting to jump if they hit jump this frame or within the last buffer time.
            bool attemptingJump = jumpInputElapsed <= jumpBufferTime;

            // Player can jump if they are (1) on the ground, (2) within the ground jump angle,
            //  (3) has not jumped within the jump cooldown time period, and (4) has only jumped once while sliding
            bool canJump = (onGround || elapsedFalling <= coyoteTime) &&
                groundAngle <= maxJumpAngle &&
                timeSinceLastJump >= jumpCooldown &&
                (!_falling || notSlidingSinceJump);

            // Read player input movement
            var inputVector = new Vector3(playerMove.x, 0, playerMove.y);

            // Rotate movement by current viewing angle
            var viewYaw = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0);
            Vector3 rotatedVector = viewYaw * inputVector;
            Vector3 normalizedInput = rotatedVector.normalized * Mathf.Min(rotatedVector.magnitude, 1.0f);
            
            // Have player jump if they can jump and are attempting to jump
            if (canJump && attemptingJump)
            {
                _velocity += Vector3.up * jumpVelocity;
                timeSinceLastJump = 0.0f;
                jumpInputElapsed = Mathf.Infinity;

                // Mark if the player is jumping while they are sliding
                notSlidingSinceJump = false;
            }
            else
            {
                timeSinceLastJump += Time.fixedDeltaTime;
                jumpInputElapsed += Time.fixedDeltaTime;
            }
            
            // Scale movement by speed and time
            Vector3 movement = normalizedInput * moveSpeed * Time.fixedDeltaTime;

            // If the player is standing on the ground, project their movement onto that plane
            // This allows for walking down slopes smoothly.
            if (!_falling)
            {
                movement = Vector3.ProjectOnPlane(movement, groundHit.normal);
            }
            
            // Lock position to given radius.
            if (Game.WrapAroundTower)
            {
                transform.position = Game.ProjectOnTower(transform.position);
            }
            
            // Attempt to move the player based on player movement
            transform.position = MovePlayer(movement);
            
            if (!_falling && !attemptingJump)
            {
                if (!Mathf.Approximately(playerMove.x, 0.0f))
                {
                    _velocity = movement / Time.fixedDeltaTime;
                }
            }

            // Move player based on falling speed
            transform.position = MovePlayer(_velocity * Time.fixedDeltaTime);
            
            // If player was on ground at the start of the ground, snap the player down
            if (onGround && !attemptingJump)
            {
                SnapPlayerDown();
            }
            
            // Point character in the correct direction.
            Vector3 forward = Utilities.Flatten(_velocity).normalized;
            if (!Mathf.Approximately(forward.magnitude, 0.0f))
            {
                if (_grappling)
                {
                    forward = Utilities.Flatten(_grappleTarget - transform.position).normalized;
                    _animator.transform.forward = forward;
                }
                else
                {
                    _animator.transform.forward = forward;
                }
            }

            UpdateAnimator();
        }

        static public bool IsRunning;

        private void UpdateAnimator()
        {
            Vector3 forward = Utilities.Flatten(_velocity);
            float movement = Vector3.Dot(forward, _velocity);
            IsRunning = Mathf.Abs(movement) > 1.0f && !_falling;
            
            _animator.SetBool("Jump", _falling);
            _animator.SetFloat("Move", movement);
            _animator.SetBool("Running", IsRunning);
            _animator.SetBool("Grappling", _grappling);
            _animator.SetBool("Dead", Player.Instance.IsDead);
        }
        
        /// <summary>
        /// Check if the player is standing on the ground.
        /// </summary>
        /// <param name="groundHit">Hit event for standing on the ground.</param>
        /// <returns>A tuple of a (boolean, float), the boolean is whether the player is within groundDist of
        /// the ground, the float is the angle between the surface and the ground.</returns>
        private (bool, float) CheckGrounded(Vector3 velocity, out RaycastHit groundHit)
        {
            bool onGround = CastSelf(transform.position, transform.rotation, Vector3.down, groundDist, out groundHit);
            float angle = Vector3.Angle(groundHit.normal, Vector3.up);
            return (onGround, angle);
        }

        /// <summary>
        /// Snap the player down if they are within a specific distance of the ground.
        /// </summary>
        private void SnapPlayerDown()
        {
            bool closeToGround = CastSelf(
                transform.position,
                transform.rotation,
                Vector3.down,
                verticalSnapDown,
                out RaycastHit groundHit);

            // If within the threshold distance of the ground
            if (closeToGround && groundHit.distance > 0)
            {
                // Snap the player down the distance they are from the ground
                transform.position += Vector3.down * (groundHit.distance - EPSILON * 2);
            }
        }

        /// <summary>
        /// Move the player with a bounce and slide motion.
        /// </summary>
        /// <param name="movement">Movement of the player.</param>
        /// <returns>Final position of player after moving and bouncing.</returns>
        private Vector3 MovePlayer(Vector3 movement)
        {
            Vector3 position = transform.position;
            Quaternion rotation = transform.rotation;

            Vector3 remaining = movement;

            float bounceStrength = _grappling ? _bounceGrapple : _bounce;
            Vector3 reflectedVelocity = _velocity;
            int bounces = 0;

            while (bounces < maxBounces && remaining.magnitude > EPSILON)
            {
                // Do a cast of the collider to see if an object is hit during this
                // movement bounce
                float distance = remaining.magnitude;
                if (!CastSelf(position, rotation, remaining.normalized, distance, out RaycastHit hit))
                {
                    // If there is no hit, move to desired position
                    position += remaining;

                    // Exit as we are done bouncing
                    break;
                }
                
                // If we are overlapping with something, just exit.
                // if (hit.distance == 0)
                // {
                //     break;
                // }

                float fraction = hit.distance / distance;
                // Set the fraction of remaining movement (minus some small value)
                position += remaining * (fraction);
                // Push slightly along normal to stop from getting caught in walls
                position += hit.normal * EPSILON * 2;
                // Decrease remaining movement by fraction of movement remaining
                remaining *= (1 - fraction);

                // Plane to project rest of movement onto
                Vector3 planeNormal = hit.normal;

                // Only apply angular change if hitting something
                // Get angle between surface normal and remaining movement
                float angleBetween = Vector3.Angle(hit.normal, remaining) - 90.0f;

                // Normalize angle between to be between 0 and 1
                // 0 means no angle, 1 means 90 degree angle
                float MaxAngleShoveDegrees = 60.0f;
                angleBetween = Mathf.Min(MaxAngleShoveDegrees, Mathf.Abs(angleBetween));
                float normalizedAngle = angleBetween / MaxAngleShoveDegrees;

                // Reduce the remaining movement by the remaining movement that ocurred
                remaining *= Mathf.Pow(1 - normalizedAngle, anglePower) * 0.9f + 0.1f;

                // Rotate the remaining movement to be projected along the plane 
                // of the surface hit (emulate pushing against the object)
                Vector3 projected = Vector3.ProjectOnPlane(remaining, planeNormal).normalized * remaining.magnitude;

                // If projected remaining movement is less than original remaining movement (so if the projection broke
                // due to float operations), then change this to just project along the vertical.
                if (projected.magnitude + EPSILON < remaining.magnitude)
                {
                    remaining = Vector3.ProjectOnPlane(remaining, Vector3.up).normalized * remaining.magnitude;
                }
                else
                {
                    remaining = projected;
                }
                
                // Apply bounce force to velocity.
                float dot = Vector3.Dot(planeNormal, reflectedVelocity);
                reflectedVelocity -= planeNormal * dot * (1.0f + bounceStrength);
                
                // Track number of times the character has bounced
                bounces++;
            }
            
            // Apply bounce force to velocity.
            _velocity = reflectedVelocity;

            // We're done, player was moved as part of loop
            return position;
        }

        /// <summary>
        /// Cast self in a given direction and get the first object hit.
        /// </summary>
        /// <param name="position">Position of the object when it is being raycast.</param>
        /// <param name="rotation">Rotation of the objecting when it is being raycast.</param>
        /// <param name="direction">Direction of the raycast.</param>
        /// <param name="distance">Maximum distance of raycast.</param>
        /// <param name="hit">First object hit and related information, will have a distance of Mathf.Infinity if none
        /// is found.</param>
        /// <returns>True if an object is hit within distance, false otherwise.</returns>
        public bool CastSelf(Vector3 pos, Quaternion rot, Vector3 dir, float dist, out RaycastHit hit)
        {
            // Get Parameters associated with the KCC
            Vector3 center = rot * capsuleCollider.center + pos;
            float radius = capsuleCollider.radius;
            float height = capsuleCollider.height;

            // Get top and bottom points of collider
            Vector3 bottom = center + rot * Vector3.down * (height / 2 - radius);
            Vector3 top = center + rot * Vector3.up * (height / 2 - radius);

            // Check what objects this collider will hit when cast with this configuration excluding itself
            IEnumerable<RaycastHit> hits = Physics.CapsuleCastAll(
                top, bottom, radius, dir, dist, ~0, QueryTriggerInteraction.Ignore)
                .Where(hit => hit.collider.transform != transform);
            bool didHit = hits.Count() > 0;

            // Find the closest objects hit
            float closestDist = didHit ? Enumerable.Min(hits.Select(hit => hit.distance)) : 0;
            IEnumerable<RaycastHit> closestHit = hits.Where(hit => hit.distance == closestDist);

            // Get the first hit object out of the things the player collides with
            hit = closestHit.FirstOrDefault();

            // Return if any objects were hit
            return didHit;
        }
    }
}
