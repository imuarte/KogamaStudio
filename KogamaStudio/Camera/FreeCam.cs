using UnityEngine;

namespace KogamaStudio.Camera;

public static class Freecam
{
    public static bool IsEnabled { get; private set; }

    public static float Speed         = 20f;
    public static float Sensitivity   = 2f;
    public static bool  RequireRightClick = true;

    private static float _yaw;
    private static float _pitch;
    private static Vector3 _position;
    private static bool _initialized;
    private static bool _wasActive;

    public static void Enable()
    {
        IsEnabled = true;
        _initialized = false;
    }

    public static void Disable()
    {
        IsEnabled = false;
        _initialized = false;
    }

    public static void ProcessInput()
    {
        if (!IsEnabled) return;

        var cam = MVGameControllerBase.MainCameraManager?.MainCamera;
        if (cam == null) return;

        if (!_initialized)
        {
            _position = cam.transform.position;
            var euler = cam.transform.eulerAngles;
            _yaw = euler.y;
            _pitch = euler.x;
            if (_pitch > 180f) _pitch -= 360f;
            _initialized = true;
        }

        // Rotation — only when RMB held (if required) or always
        bool canRotate = !RequireRightClick || Input.GetMouseButton(1);
        if (canRotate)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");
            _yaw   += mouseX * Sensitivity;
            _pitch -= mouseY * Sensitivity;
            _pitch  = Mathf.Clamp(_pitch, -89f, 89f);
        }

        // Movement — WASD + Q/E for up/down
        var rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        var forward  = rotation * Vector3.forward;
        var right    = rotation * Vector3.right;
        var up       = Vector3.up;

        float dt = Time.unscaledDeltaTime;
        float moveSpeed = Speed * dt;

        // Sprint with shift
        if (Input.GetKey(KeyCode.LeftShift))
            moveSpeed *= 2f;

        Vector3 move = Vector3.zero;
        if (Input.GetKey(KeyCode.W)) move += forward;
        if (Input.GetKey(KeyCode.S)) move -= forward;
        if (Input.GetKey(KeyCode.D)) move += right;
        if (Input.GetKey(KeyCode.A)) move -= right;
        if (Input.GetKey(KeyCode.E)) move += up;
        if (Input.GetKey(KeyCode.Q)) move -= up;

        if (move.sqrMagnitude > 0f)
            _position += move.normalized * moveSpeed;
    }

    public static void Apply(MainCameraManager mgr)
    {
        if (IsEnabled && _initialized)
        {
            if (!_wasActive)
            {
                if (mgr.cameraController != null)
                    ((Behaviour)(object)mgr.cameraController).enabled = false;
                _wasActive = true;
            }

            var cam = mgr.MainCamera;
            if (cam == null) return;

            cam.transform.position = _position;
            cam.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
        else if (_wasActive)
        {
            if (mgr.cameraController != null)
                ((Behaviour)(object)mgr.cameraController).enabled = true;
            _wasActive = false;
        }
    }
}
