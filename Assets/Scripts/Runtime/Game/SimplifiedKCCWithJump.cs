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
using nickmaltbie.OpenKCC.Character;
using nickmaltbie.OpenKCC.Utils;
using UnityEngine;
using UnityEngine.InputSystem;

namespace nickmaltbie.OpenKCC.Demo
{
    [RequireComponent(typeof(CapsuleCollider))]
    public class SimplifiedKCCWithJump : MonoBehaviour
    {
        [SerializeField] private InputActionReference movePlayer;
        [SerializeField] private InputActionReference jumpAction;
        [SerializeField] private InputActionReference shootAction;
        
        [SerializeField] private int maxBounces = 5;
        [SerializeField] private float moveSpeed = 5.0f;
        [SerializeField] private float anglePower = 0.5f;
        [SerializeField] private float verticalSnapDown = 0.45f;
        [SerializeField] private Vector3 gravity = new Vector3(0, -20, 0);
        [SerializeField] private float groundDist = 0.01f;
        [SerializeField] private float maxWalkingAngle = 60f;
        [SerializeField] private float jumpVelocity = 5.0f;
        [SerializeField] private float maxJumpAngle = 80f;
        [SerializeField] private float jumpCooldown = 0.25f;
        [SerializeField] private float coyoteTime = 0.05f;
        [SerializeField] private float jumpBufferTime = 0.05f;
        [SerializeField] [Range(0, 1)] private float jumpAngleWeightFactor = 0.0f;
        [SerializeField] private float _grappleLeanInfluence = 5.0f;
        [SerializeField] private float _grappleRetraction = 10.0f;
        [SerializeField] private float _maxForce = 1.0f;
        
        private float jumpInputElapsed = Mathf.Infinity;
        private float timeSinceLastJump = 0.0f;
        private float elapsedFalling = 0f;
        private bool notSlidingSinceJump = true;
        private Vector3 velocity;
        private Vector2 cameraAngle;
        private CapsuleCollider capsuleCollider;
        private bool jumpInputPressed => jumpAction.action.IsPressed();// || shootAction.action.WasPressedThisFrame();
        private bool grappling;
        private float _roofTimer;

        private Vector3 _steering;

        private void Start()
        {
            capsuleCollider = GetComponent<CapsuleCollider>();
            Rigidbody rigidbody = GetComponent<Rigidbody>();
            rigidbody.isKinematic = true;
        }

        public void ApplyGrapple(Vector3 target)
        {
            Vector3 playerPos = Player.Instance.transform.position;
            
            // Extend/retract the grapple.
            Vector2 playerMove = movePlayer.action.ReadValue<Vector2>();
            // Rotate movement by current viewing angle
            Quaternion viewYaw = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0);
            Vector3 rotatedVector = viewYaw * playerMove;
            Vector3 normalizedInput = rotatedVector.normalized * Mathf.Min(rotatedVector.magnitude, 1.0f);

            /* This chunk of code un-projects the grapple point from the circular tower,
             so forces can be accurately calculated as if on a 2D plane. Then, the direction of 
             swing is calculated and current velocity is projected onto that vector, so player
             moves restricted to the arc made by the grapple. */
            Vector3 playerPosN = Utilities.Flatten(playerPos).normalized;
            Vector3 grapplePosN = Utilities.Flatten(target).normalized;
            
            // Gets the distance to the grapple point on 2D plane.
            float angle = Mathf.Acos(Vector3.Dot(playerPosN, grapplePosN));
            float distance = Utilities.TOWER_CIRCUMFERENCE * (angle / (2.0f * Mathf.PI));

            // Project the grapple post onto that 2D plane.
            Vector3 playerForward = Vector3.Cross(playerPosN, Vector3.up).normalized;
            bool left = Vector3.Dot(playerForward, (target - playerPos).normalized) < 0.0f;
            if (left) playerForward = -playerForward;
            
            Vector3 grapplePosProjectedForward = playerPos + playerForward * distance;
            grapplePosProjectedForward.y += (target - playerPos).y;

            // Calculate swing direction.
            Vector3 grappleDir = (grapplePosProjectedForward - playerPos).normalized;
            Vector3 swingDir = Vector3.Cross(grappleDir, Utilities.Flatten(playerPos)).normalized;
            if (!left) swingDir = -swingDir;

            // Project swing velocity onto swing direction.
            Vector3 grappleVelocity = velocity + normalizedInput * _grappleLeanInfluence * Time.deltaTime;
            float speed = Vector3.Dot(grappleVelocity, swingDir);
            Vector3 desiredVelocity = swingDir * speed;
            
            // Move based on grapple retraction.
            Vector3 retraction = (grapplePosProjectedForward - playerPos).normalized * _grappleRetraction * Time.deltaTime;
            desiredVelocity += retraction;

            _steering = Limit(desiredVelocity - velocity, _maxForce);
            //_steering = desiredVelocity - velocity;

            velocity += _steering;
            //velocity = desiredVelocity;

            grappling = true;
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
        
        private void Update()
        {
            // Read input values from player
            Vector2 playerMove = movePlayer.action.ReadValue<Vector2>();
            
            if (grappling)
            {
                playerMove.x = 0.0f;
            }
            playerMove.y = 0.0f;

            grappling = false;

            // If player is not allowed to move, stop player input
            if (PlayerInputUtils.playerMovementState == PlayerInputState.Deny)
            {
                playerMove = Vector2.zero;
            }

            // Rotate player based on mouse input, ensure pitch is bounded to not overshoot
            transform.rotation = Quaternion.Euler(0, cameraAngle.y, 0);

            // Check if the player is falling
            (bool onGround, float groundAngle) = CheckGrounded(velocity, out RaycastHit groundHit);
            bool falling = !(onGround && groundAngle <= maxWalkingAngle);
            
            // Check if the player hit their head
            (bool onRoof, float roofAngle) = CheckRoofed(velocity, out RaycastHit roofHit);
            _roofTimer -= Time.deltaTime;
            if (onRoof && _roofTimer < 0.0f)
            {
                velocity.y = 0.0f;
                _roofTimer = 0.5f;
            }

            // If falling, increase falling speed, otherwise stop falling.
            if (falling)
            {
                playerMove.x = 0.0f;
                velocity += gravity * Time.deltaTime;
                elapsedFalling += Time.deltaTime;
            }
            else
            {
                velocity = Vector3.zero;
                elapsedFalling = 0;
                notSlidingSinceJump = true;
            }

            // If the player is attemtping to jump and can jump allow for player jump
            if (jumpInputPressed)
            {
                jumpInputElapsed = 0.0f;
            }
            // Player is attempting to jump if they hit jump this frame or within the last buffer time.
            bool attemptingJump = jumpInputElapsed <= jumpBufferTime;

            // Player can jump if they are (1) on the ground, (2) within the ground jump angle,
            //  (3) has not jumped within the jump cooldown time period, and (4) has only jumped once while sliding
            bool canJump = (onGround || elapsedFalling <= coyoteTime) &&
                groundAngle <= maxJumpAngle &&
                timeSinceLastJump >= jumpCooldown &&
                (!falling || notSlidingSinceJump);

            // Read player input movement
            var inputVector = new Vector3(playerMove.x, 0, playerMove.y);

            // Rotate movement by current viewing angle
            var viewYaw = Quaternion.Euler(0, Camera.main.transform.rotation.eulerAngles.y, 0);
            Vector3 rotatedVector = viewYaw * inputVector;
            Vector3 normalizedInput = rotatedVector.normalized * Mathf.Min(rotatedVector.magnitude, 1.0f);
            
            // Have player jump if they can jump and are attempting to jump
            if (canJump && attemptingJump)
            {
                velocity = Vector3.Lerp(Vector3.up + new Vector3(normalizedInput.x, 0.0f, 0.0f), groundHit.normal, jumpAngleWeightFactor) * jumpVelocity;
                timeSinceLastJump = 0.0f;
                jumpInputElapsed = Mathf.Infinity;

                // Mark if the player is jumping while they are sliding
                notSlidingSinceJump = false;
            }
            else
            {
                timeSinceLastJump += Time.deltaTime;
                jumpInputElapsed += Time.deltaTime;
            }
            
            // Scale movement by speed and time
            Vector3 movement = normalizedInput * moveSpeed * Time.deltaTime;

            // If the player is standing on the ground, project their movement onto that plane
            // This allows for walking down slopes smoothly.
            if (!falling)
            {
                movement = Vector3.ProjectOnPlane(movement, groundHit.normal);
            }
            
            // Lock position to given radius.
            transform.position = Utilities.ProjectOnTower(transform.position);

            // Attempt to move the player based on player movement
            transform.position = MovePlayer(movement);

            // Move player based on falling speed
            transform.position = MovePlayer(velocity * Time.deltaTime);
            
            // If player was on ground at the start of the ground, snap the player down
            if (onGround && !attemptingJump)
            {
                SnapPlayerDown();
            }
        }

        /// <summary>
        /// Check if the player is standing on the ground.
        /// </summary>
        /// <param name="groundHit">Hit event for standing on the ground.</param>
        /// <returns>A tuple of a (boolean, float), the boolean is whether the player is within groundDist of
        /// the ground, the float is the angle between the surface and the ground.</returns>
        private (bool, float) CheckGrounded(Vector3 velocity, out RaycastHit groundHit)
        {
            groundHit = new RaycastHit();
            if (Vector3.Dot(velocity.normalized, Vector3.up) > 0.5f)
                return (false, 0.0f);
            
            bool onGround = CastSelf(transform.position, transform.rotation, Vector3.down, groundDist, out groundHit);
            float angle = Vector3.Angle(groundHit.normal, Vector3.up);
            return (onGround, angle);
        }
        
        private (bool, float) CheckRoofed(Vector3 velocity, out RaycastHit roofHit)
        {
            roofHit = new RaycastHit();
            if (Vector3.Dot(velocity.normalized, Vector3.down) > 0.0f)
                return (false, 0.0f);
            
            bool onRoof = CastSelf(transform.position, transform.rotation, Vector3.up, groundDist, out roofHit);
            float angle = Vector3.Angle(roofHit.normal, Vector3.down);
            return (onRoof, angle);
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
                transform.position += Vector3.down * (groundHit.distance - KCCUtils.Epsilon * 2);
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

            int bounces = 0;

            while (bounces < maxBounces && remaining.magnitude > KCCUtils.Epsilon)
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
                if (hit.distance == 0)
                {
                    break;
                }

                float fraction = hit.distance / distance;
                // Set the fraction of remaining movement (minus some small value)
                position += remaining * (fraction);
                // Push slightly along normal to stop from getting caught in walls
                position += hit.normal * KCCUtils.Epsilon * 2;
                // Decrease remaining movement by fraction of movement remaining
                remaining *= (1 - fraction);

                // Plane to project rest of movement onto
                Vector3 planeNormal = hit.normal;

                // Only apply angular change if hitting something
                // Get angle between surface normal and remaining movement
                float angleBetween = Vector3.Angle(hit.normal, remaining) - 90.0f;

                // Normalize angle between to be between 0 and 1
                // 0 means no angle, 1 means 90 degree angle
                angleBetween = Mathf.Min(KCCUtils.MaxAngleShoveDegrees, Mathf.Abs(angleBetween));
                float normalizedAngle = angleBetween / KCCUtils.MaxAngleShoveDegrees;

                // Reduce the remaining movement by the remaining movement that ocurred
                remaining *= Mathf.Pow(1 - normalizedAngle, anglePower) * 0.9f + 0.1f;

                // Rotate the remaining movement to be projected along the plane 
                // of the surface hit (emulate pushing against the object)
                Vector3 projected = Vector3.ProjectOnPlane(remaining, planeNormal).normalized * remaining.magnitude;

                // If projected remaining movement is less than original remaining movement (so if the projection broke
                // due to float operations), then change this to just project along the vertical.
                if (projected.magnitude + KCCUtils.Epsilon < remaining.magnitude)
                {
                    remaining = Vector3.ProjectOnPlane(remaining, Vector3.up).normalized * remaining.magnitude;
                }
                else
                {
                    remaining = projected;
                }

                // Track number of times the character has bounced
                bounces++;
            }

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
        private bool CastSelf(Vector3 pos, Quaternion rot, Vector3 dir, float dist, out RaycastHit hit)
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
