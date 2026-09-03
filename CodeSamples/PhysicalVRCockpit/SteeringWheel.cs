using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class SteeringWheel : MonoBehaviour
{
    private const float MIN_WHEEL_ANGLE = -450f;
    private const float MAX_WHEEL_ANGLE = 450f;

    #region Inspector

    [Header("Grab")]
    [SerializeField] private XRGrabInteractable _grab;

    [Header("Reference")]
    [Tooltip("Offset / Root – hand direction is calculated in its local space.")]
    [SerializeField] private Transform _root;

    [Header("Auto Return To Center")]
    [SerializeField] private float _returnSpeed = 3f;
    [SerializeField] private float _centerEpsilon = 0.5f;

    [Header("Anti-Jerk")]
    [Tooltip("Maximum step in degrees per frame.")]
    [SerializeField] private float _maxStepPerFrame = 5f;

    [Tooltip("Step smoothing (higher = smoother).")]
    [SerializeField] private float _stepSmooth = 10f;

    #endregion

    #region Runtime State

    private XRBaseInteractor _interactor;

    private float _wheelAngle;
    private float _lastHandAngle;
    private float _smoothedStep;

    #endregion

    #region Public API

    public float WheelAngleSignedDeg => _wheelAngle;

    public float Steer01 =>
        Mathf.Clamp(-WheelAngleSignedDeg / MAX_WHEEL_ANGLE, -1f, 1f);

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!_grab)
            _grab = GetComponent<XRGrabInteractable>();

        if (!_root)
            _root = transform.parent;

        _wheelAngle = GetSignedLocalZ();
    }

    private void OnEnable()
    {
        _grab.selectEntered.AddListener(OnGrab);
        _grab.selectExited.AddListener(OnRelease);
    }

    private void OnDisable()
    {
        _grab.selectEntered.RemoveListener(OnGrab);
        _grab.selectExited.RemoveListener(OnRelease);
    }

    private void Update()
    {
        if (_interactor != null)
        {
            RotateWheel();
            return;
        }

        ReturnToCenter();
    }

    #endregion

    #region XR Events

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject as XRBaseInteractor;

        // _wheelAngle is the source of truth – do not re-read it from the transform here,
        // localEulerAngles wraps at ±180° while the wheel range is ±450°.
        _lastHandAngle = FindHandAngle();
        _smoothedStep = 0f;
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        _smoothedStep = 0f;
    }

    #endregion

    #region Wheel Logic

    private void RotateWheel()
    {
        var handAngle = FindHandAngle();

        var step = Mathf.DeltaAngle(_lastHandAngle, handAngle);
        _lastHandAngle = handAngle;

        step = Mathf.Clamp(step, -_maxStepPerFrame, _maxStepPerFrame);
        _smoothedStep = Mathf.Lerp(_smoothedStep, step, Time.deltaTime * _stepSmooth);

        _wheelAngle = Mathf.Clamp(
            _wheelAngle + _smoothedStep,
            MIN_WHEEL_ANGLE,
            MAX_WHEEL_ANGLE
        );

        SetLocalZOnly(_wheelAngle);
    }

    private void ReturnToCenter()
    {
        var target = Mathf.Lerp(_wheelAngle, 0f, Time.deltaTime * _returnSpeed);

        if (Mathf.Abs(target) < _centerEpsilon)
            target = 0f;

        _wheelAngle = target;
        SetLocalZOnly(_wheelAngle);
    }

    #endregion

    #region Hand Angle Helpers

    private float FindHandAngle()
    {
        if (_interactor == null)
            return 0f;

        var dir = FindLocalDirection2D(_interactor.transform.position);
        return Vector2.SignedAngle(Vector2.up, dir);
    }

    private Vector2 FindLocalDirection2D(Vector3 handWorldPos)
    {
        var space = _root != null ? _root : transform;
        var local = space.InverseTransformPoint(handWorldPos);

        var dir = new Vector2(local.x, local.y);
        if (dir.sqrMagnitude < 0.0001f)
            return Vector2.up;

        return dir.normalized;
    }

    #endregion

    #region Transform Helpers

    private float GetSignedLocalZ()
    {
        var z = transform.localEulerAngles.z;
        return z > 180f ? z - 360f : z;
    }

    private void SetLocalZOnly(float angle)
    {
        transform.localEulerAngles = new Vector3(0f, 0f, angle);
    }

    #endregion
}
