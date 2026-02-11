using HarmonyLib;
using Il2Cpp;
using UnityEngine;

namespace KogamaStudio.Camera;

public static class Freecam
{
    private static bool _enabled = false;
    private static Vector3 _pos;
    private static Quaternion _rot;
    private static MVCameraBase _currentCamera;

    public static bool IsEnabled => _enabled;

    public static void Enable()
    {
        var cam = MVGameControllerBase.MainCameraManager;
        if (cam == null) return;

        _currentCamera = cam.CurrentCamera;
        if (_currentCamera == null) return;

        _pos = _currentCamera.transform.position;
        _rot = _currentCamera.transform.rotation;
        _enabled = true;

        Debug.Log($"FREECAM: Enabled at {_pos}");
    }

    public static void Disable()
    {
        _enabled = false;
        Debug.Log("FREECAM: Disabled");
    }

    public static void Update()
    {
        if (!_enabled || _currentCamera == null) return;

        if (Input.GetKey(KeyCode.W)) _pos += _rot * Vector3.forward * 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.S)) _pos += _rot * Vector3.back * 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.A)) _pos += _rot * Vector3.left * 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.D)) _pos += _rot * Vector3.right * 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.Space)) _pos += Vector3.up * 20f * Time.deltaTime;
        if (Input.GetKey(KeyCode.C)) _pos += Vector3.down * 20f * Time.deltaTime;

        float mx = Input.GetAxis("Mouse X") * 100f * 0.01f;
        float my = Input.GetAxis("Mouse Y") * 100f * 0.01f;

        Vector3 e = _rot.eulerAngles;
        e.y += mx;
        e.x -= my;
        e.x = Mathf.Clamp(e.x > 180f ? e.x - 360f : e.x, -90f, 90f);

        _rot = Quaternion.Euler(e);
        _currentCamera.transform.position = _pos;
        _currentCamera.transform.rotation = _rot;
    }
}

[HarmonyPatch(typeof(MainCameraManager), "UpdateCamera")]
internal class Patch
{
    [HarmonyPostfix]
    private static void Post(MainCameraManager __instance)
    {
        Freecam.Update();
    }
}

[HarmonyPatch(typeof(InputToPlayerMovement), "HandleInputState")]
internal class Patch2
{
    [HarmonyPrefix]
    private static bool Pre() => !Freecam.IsEnabled;
}