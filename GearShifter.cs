using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public enum Gear
{
    Neutral = 0,
    G1 = 1,
    G2 = 2,
    G3 = 3,
    G4 = 4,
    G5 = 5,
    Reverse = -1
}

public class GearShifter : MonoBehaviour
{
    private const float MAX_ANGLE = 25f;

    #region Inspector

    [Header("XR")]
    [SerializeField] private XRGrabInteractable _grab;
    [SerializeField] private Transform _root;

    [Header("Gate Positions (Z Axis)")]
    [SerializeField, Tooltip("Ordered ascending: [-Z gate (5/R), center gate (3/4), +Z gate (1/2)].")]
    private float[] _zGates = { -25f, 0f, 25f };
    [SerializeField] private float _gateSnapEpsilon = 5f;

    [Header("Center Window (Bigger = Easier Movement)")]
    [SerializeField] private float _centerXWindow = 10f;
    [SerializeField] private float _deadzone = 1.5f;

    [Header("Gear Engagement")]
    [SerializeField, Tooltip("Minimum X deflection (degrees) required to engage a gear.")]
    private float _xEngageAngle = 15f;
    [SerializeField, Tooltip("Z-angle tolerance when detecting the gear gate (degrees).")]
    private float _zGateEpsilon = 8f;

    [Header("Tuning")]
    [SerializeField] private float _followSpeed = 25f;

    [Header("Auto Return To Center")]
    [SerializeField] private float _returnSpeed = 6f;

    #endregion

    #region Runtime State

    private XRBaseInteractor _interactor;

    private float _startHandAngleX;
    private float _startHandAngleZ;

    private float _startX;
    private float _startZ;

    private Gear _currentGear = Gear.Neutral;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        if (!_grab)
            _grab = GetComponent<XRGrabInteractable>();
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
        if (_interactor == null)
        {
            TickNoInteractor();
            return;
        }

        TickGrabbed();
    }

    #endregion

    #region XR Events

    private void OnGrab(SelectEnterEventArgs args)
    {
        _interactor = args.interactorObject as XRBaseInteractor;

        _startX = GetLocalSignedX();
        _startZ = GetLocalSignedZ();

        _startHandAngleX = HandAngleForX(_interactor.transform.position);
        _startHandAngleZ = HandAngleForZ(_interactor.transform.position);

        EventSystem.OnShifterGrabChanged?.Invoke(true);
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        _interactor = null;
        EventSystem.OnShifterGrabChanged?.Invoke(false);
    }

    #endregion

    #region Ticking

    private void TickNoInteractor()
    {
        if (_currentGear == Gear.Neutral)
            ReturnToCenter();
    }

    private void TickGrabbed()
    {
        var handWorldPos = _interactor.transform.position;

        var handX = HandAngleForX(handWorldPos);
        var handZ = HandAngleForZ(handWorldPos);

        var deltaX = _startHandAngleX - handX;
        var deltaZ = handZ - _startHandAngleZ;

        if (Mathf.Abs(deltaX) < _deadzone) deltaX = 0f;
        if (Mathf.Abs(deltaZ) < _deadzone) deltaZ = 0f;

        var targetX = Mathf.Clamp(_startX + deltaX, -MAX_ANGLE, MAX_ANGLE);
        var targetZ = ComputeTargetZ(targetX, deltaZ);

        SmoothFollowTo(targetX, targetZ);
        TryUpdateGear();
    }

    #endregion

    #region Target / Movement

    private float ComputeTargetZ(float targetX, float deltaZ)
    {
        if (Mathf.Abs(targetX) <= _centerXWindow)
        {
            var rawZ = Mathf.Clamp(_startZ + deltaZ, -MAX_ANGLE, MAX_ANGLE);

            var snapped = SnapToGate(rawZ);
            var snapDist = Mathf.Abs(Mathf.DeltaAngle(rawZ, snapped));

            return snapDist <= _gateSnapEpsilon ? snapped : rawZ;
        }

        return SnapToGate(GetLocalSignedZ());
    }

    private void SmoothFollowTo(float targetX, float targetZ)
    {
        var curX = GetLocalSignedX();
        var curZ = GetLocalSignedZ();

        var smX = Mathf.Lerp(curX, targetX, Time.deltaTime * _followSpeed);
        var smZ = Mathf.Lerp(curZ, targetZ, Time.deltaTime * _followSpeed);

        SetLocalXZ(smX, smZ);
    }

    private void ReturnToCenter()
    {
        var x = GetLocalSignedX();
        var z = GetLocalSignedZ();

        var targetX = Mathf.Lerp(x, 0f, Time.deltaTime * _returnSpeed);
        var targetZ = Mathf.Lerp(z, 0f, Time.deltaTime * _returnSpeed);

        SetLocalXZ(targetX, targetZ);
    }

    #endregion

    #region Gear Logic

    private void TryUpdateGear()
    {
        var newGear = ComputeGear();

        if (newGear == _currentGear)
            return;

        _currentGear = newGear;
        EventSystem.OnGearChanged?.Invoke(_currentGear);
    }

    private Gear ComputeGear()
    {
        var x = GetLocalSignedX();
        var z = GetLocalSignedZ();

        if (Mathf.Abs(x) < _xEngageAngle)
            return Gear.Neutral;

        var forward = x > 0f;
        var gateIndex = FindGateIndex(z);

        return GearForGate(gateIndex, forward);
    }

    private int FindGateIndex(float z)
    {
        for (var i = 0; i < _zGates.Length; i++)
        {
            if (Mathf.Abs(Mathf.DeltaAngle(z, _zGates[i])) <= _zGateEpsilon)
                return i;
        }

        return -1;
    }

    private Gear GearForGate(int gateIndex, bool forward)
    {
        switch (gateIndex)
        {
            case 2: return forward ? Gear.G1 : Gear.G2;      // +Z gate (left column)
            case 1: return forward ? Gear.G3 : Gear.G4;      // center gate
            case 0: return forward ? Gear.G5 : Gear.Reverse; // -Z gate (right column)
            default: return Gear.Neutral;
        }
    }

    #endregion

    #region Angle Helpers

    private float HandAngleForX(Vector3 handWorldPos)
    {
        var p = _root.InverseTransformPoint(handWorldPos);
        return Mathf.Atan2(p.y, p.z) * Mathf.Rad2Deg;
    }

    private float HandAngleForZ(Vector3 handWorldPos)
    {
        var p = _root.InverseTransformPoint(handWorldPos);
        return Mathf.Atan2(p.y, p.x) * Mathf.Rad2Deg;
    }

    private float GetLocalSignedX()
    {
        var a = transform.localEulerAngles.x;
        return a > 180f ? a - 360f : a;
    }

    private float GetLocalSignedZ()
    {
        var a = transform.localEulerAngles.z;
        return a > 180f ? a - 360f : a;
    }

    private void SetLocalXZ(float xDeg, float zDeg)
    {
        var e = transform.localEulerAngles;
        e.x = xDeg;
        e.z = zDeg;
        e.y = 0f;
        transform.localEulerAngles = e;
    }

    #endregion

    #region Gate Snapping

    private float SnapToGate(float z)
    {
        var best = _zGates[0];
        var bestDist = Mathf.Abs(Mathf.DeltaAngle(z, best));

        for (var i = 1; i < _zGates.Length; i++)
        {
            var d = Mathf.Abs(Mathf.DeltaAngle(z, _zGates[i]));
            if (d >= bestDist)
                continue;

            bestDist = d;
            best = _zGates[i];
        }

        return Mathf.Clamp(best, -MAX_ANGLE, MAX_ANGLE);
    }

    #endregion
}
