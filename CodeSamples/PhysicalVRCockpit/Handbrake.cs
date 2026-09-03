using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class Handbrake : MonoBehaviour
{
    #region Inspector

    [Header("XR")]
    [SerializeField] private XRGrabInteractable _grab;

    [Header("Rotation")]
    [SerializeField] private Transform _root;
    [SerializeField, Tooltip("Lever X angle when fully engaged (pulled).")]
    private float _minAngle = 60f;
    [SerializeField, Tooltip("Lever X angle when fully released.")]
    private float _maxAngle = 90f;
    [SerializeField] private float _followSpeed = 20f;

    [Header("State")]
    [SerializeField] private float _engageThreshold = 0.1f;

    #endregion

    #region Runtime State

    private XRBaseInteractor _interactor;
    private float _startAngle;
    private float _startHandAngle;

    #endregion

    #region Public API

    public float BrakeForce01 =>
        Mathf.InverseLerp(_maxAngle, _minAngle, GetLocalXAngle());

    public bool IsEngaged => BrakeForce01 >= _engageThreshold;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!_grab)
            _grab = GetComponent<XRGrabInteractable>();

        if (!_root)
            _root = transform.parent;
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
            UpdateRotation();
    }

    #endregion

    #region XR Events

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject as XRBaseInteractor;

        _startAngle = GetLocalXAngle();
        _startHandAngle = GetHandAngle(_interactor.transform.position);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
    }

    #endregion

    #region Logic

    private void UpdateRotation()
    {
        var handAngle = GetHandAngle(_interactor.transform.position);
        var delta = _startHandAngle - handAngle;

        var targetAngle = Mathf.Clamp(
            _startAngle + delta,
            _minAngle,
            _maxAngle
        );

        var current = GetLocalXAngle();
        SetLocalXAngle(Mathf.Lerp(current, targetAngle, Time.deltaTime * _followSpeed));
    }

    #endregion

    #region Angle Helpers

    private float GetHandAngle(Vector3 handWorldPos)
    {
        var localHandPos = _root.InverseTransformPoint(handWorldPos);
        return Mathf.Atan2(localHandPos.y, localHandPos.z) * Mathf.Rad2Deg;
    }

    private float GetLocalXAngle()
    {
        var x = transform.localEulerAngles.x;
        return x > 180f ? x - 360f : x;
    }

    private void SetLocalXAngle(float angle)
    {
        var euler = transform.localEulerAngles;
        euler.x = angle;
        transform.localEulerAngles = euler;
    }

    #endregion
}
