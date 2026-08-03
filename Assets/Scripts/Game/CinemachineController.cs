using UnityEngine;
using Cinemachine; // Cinemachine 2.x namespace (was "Unity.Cinemachine" in CM3/Unity 6)

/// <summary>
/// A Cinemachine 2.7 extension that adds a Hollow Knight style horizontal
/// look-ahead to a CinemachineVirtualCamera. Attach this to the same
/// GameObject as your CinemachineVirtualCamera (it will show up in the
/// vcam's Extensions list, or you can just drag it on directly).
///
/// Recommended setup on the CinemachineVirtualCamera:
///   - Follow: your player
///   - Body: Framing Transposer (or Transposer) - handles normal smooth follow
///   - This extension only adds the extra "push ahead" offset in the facing
///     direction, and reacts instantly with a fast smoothing time the moment
///     facing direction changes.
/// </summary>
[AddComponentMenu("Cinemachine/Extensions/Controller Look Ahead")]
[SaveDuringPlay]
public class CinemachineControllerLookAhead : CinemachineExtension
{
    [Header("Facing Source")]
    [Tooltip("If true, reads Horizontal input axis to determine facing. " +
             "If false, call SetFacingDirection() externally from your player controller instead.")]
    public bool useInputFallback = true;
    [Tooltip("Ignore input below this magnitude when using input fallback.")]
    public float inputDeadzone = 0.1f;

    [Header("Look Ahead")]
    [Tooltip("How far the camera pushes ahead in the facing direction.")]
    public float lookAheadDistance = 3f;
    [Tooltip("Smoothing time while facing direction stays the same.")]
    public float lookAheadSmoothTime = 0.35f;
    [Tooltip("Smoothing time the instant facing direction changes (keep small for a snappy reaction).")]
    public float directionChangeSmoothTime = 3f;

    private float currentLookAheadX;
    private float lookAheadVelocityX;
    private int facingDirection = 1;
    private int lastFacingDirection = 1;

    /// <summary>
    /// Call this from your player controller whenever facing direction changes
    /// (e.g. when you flip the sprite). More reliable than polling input.
    /// </summary>
    public void SetFacingDirection(int direction)
    {
        if (direction != 0)
            facingDirection = direction > 0 ? 1 : -1;
    }

    protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase vcam, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
    {
        // Inject the offset right after Body so it composes correctly with
        // whatever damping the Transposer/Framing Transposer already applied.
        if (stage != CinemachineCore.Stage.Body)
            return;

        if (useInputFallback)
        {
            float h = Input.GetAxisRaw("Horizontal");
            if (Mathf.Abs(h) > inputDeadzone)
                facingDirection = h > 0f ? 1 : -1;
        }

        bool directionChanged = facingDirection != lastFacingDirection;
        float smoothTime = directionChanged ? directionChangeSmoothTime : lookAheadSmoothTime;
        float targetLookAheadX = facingDirection * lookAheadDistance;

        if (deltaTime < 0f)
        {
            // deltaTime < 0 signals a cut (e.g. camera just activated) - snap immediately, no smoothing.
            currentLookAheadX = targetLookAheadX;
            lookAheadVelocityX = 0f;
        }
        else
        {
            currentLookAheadX = Mathf.SmoothDamp(
                currentLookAheadX,
                targetLookAheadX,
                ref lookAheadVelocityX,
                smoothTime,
                Mathf.Infinity,
                deltaTime
            );
        }

        lastFacingDirection = facingDirection;

        // PositionCorrection is added on top of the Body stage's raw result,
        // so it doesn't interfere with Cinemachine's own damping calculations.
        state.PositionCorrection += new Vector3(currentLookAheadX, 0f, 0f);
    }
}