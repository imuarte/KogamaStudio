using HarmonyLib;
using UnityEngine;

namespace KogamaStudio.Camera;

public static class Freecam
{
    private static bool _enabled = false;
    private static float _yaw;
    private static float _pitch;
    private static float _targetYaw;
    private static float _targetPitch;
    public static float Speed = 20f;
    public static float Sensitivity = 2f;
    public static bool RequireRightClick = true;
    public static Vector3 Position;
    public static Quaternion Rotation;

    public static bool IsEnabled => _enabled;

    public static void Enable()
    {
        var cam = MVGameControllerBase.MainCameraManager?.MainCamera;
        if (cam == null) return;

        Position = cam.transform.position;
        var e = cam.transform.rotation.eulerAngles;
        _targetYaw = _yaw = e.y;
        _targetPitch = _pitch = e.x > 180f ? e.x - 360f : e.x;
        Rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        _enabled = true;
    }

    public static void Disable()
    {
        _enabled = false;
    }

    public static void Update()
    {
        if (!_enabled) return;

        float speed = Speed * Time.deltaTime;

        if (Input.GetKey(KeyCode.W)) Position += Rotation * Vector3.forward * speed;
        if (Input.GetKey(KeyCode.S)) Position += Rotation * Vector3.back * speed;
        if (Input.GetKey(KeyCode.A)) Position += Rotation * Vector3.left * speed;
        if (Input.GetKey(KeyCode.D)) Position += Rotation * Vector3.right * speed;
        if (Input.GetKey(KeyCode.Space)) Position += Vector3.up * speed;
        if (Input.GetKey(KeyCode.C)) Position += Vector3.down * speed;

        if (!RequireRightClick || Input.GetMouseButton(1))
        {
            _targetYaw   += Input.GetAxis("Mouse X") * Sensitivity;
            _targetPitch -= Input.GetAxis("Mouse Y") * Sensitivity;
            _targetPitch  = Mathf.Clamp(_targetPitch, -90f, 90f);
        }

        float smooth = Time.deltaTime * 20f;
        _yaw   = Mathf.LerpAngle(_yaw, _targetYaw, smooth);
        _pitch = Mathf.Lerp(_pitch, _targetPitch, smooth);

        Rotation = Quaternion.Euler(_pitch, _yaw, 0f);
    }
}


[HarmonyPatch(typeof(MainCameraManager), "UpdateCamera")]
internal class FreecamBlockCameraPatch
{
    [HarmonyPrefix]
    private static bool Pre() => !Freecam.IsEnabled;
}

[HarmonyPatch(typeof(InputToPlayerMovement), "HandleInputState")]
internal class FreecamInputPatch
{
    [HarmonyPrefix]
    private static bool Pre() => !Freecam.IsEnabled;
}
