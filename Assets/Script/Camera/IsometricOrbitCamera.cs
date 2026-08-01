using UnityEngine;
using UnityEngine.InputSystem;

public class IsometricOrbitCamera : MonoBehaviour
{
    [Header("Alvo de Foco")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Configurações de Zoom (Scroll)")]
    public float distance = 25.0f;
    public float minDistance = 10.0f;
    public float maxDistance = 45.0f;
    public float zoomSensitivity = 2.0f;

    [Header("Sensibilidade do Mouse")]
    public float rotationSensitivityX = 0.2f;
    public float rotationSensitivityY = 0.2f;

    [Header("Limites de Ângulo Vertical (Pitch)")]
    [Range(5f, 85f)] public float minPitch = 15.0f;
    [Range(5f, 85f)] public float maxPitch = 60.0f;

    private float currentYaw = 45.0f;
    private float currentPitch = 30.0f;
    private float groundedYPosition;
    private bool isOrbiting = false;

    void Start()
    {
        if (target != null) groundedYPosition = target.position.y;
    }

    void LateUpdate()
    {
        if (target == null || Mouse.current == null) return;

        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            isOrbiting = true;
            if (CursorManager.Instance != null)
                CursorManager.Instance.SetCursorType(CursorState.OrbitingCamera);
        }

        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isOrbiting = false;
            if (CursorManager.Instance != null)
                CursorManager.Instance.SetCursorType(CursorState.Default);
        }

        if (isOrbiting)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentYaw += mouseDelta.x * rotationSensitivityX;
            currentPitch -= mouseDelta.y * rotationSensitivityY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        float scrollValue = Mouse.current.scroll.ReadValue().y;
        if (Mathf.Abs(scrollValue) > 0.01f)
        {
            distance -= (scrollValue / 120.0f) * zoomSensitivity;
            distance = Mathf.Clamp(distance, minDistance, maxDistance);
        }

        groundedYPosition = Mathf.Lerp(groundedYPosition, target.position.y, Time.deltaTime * 3.0f);
        Quaternion finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 focusPoint = new Vector3(target.position.x, groundedYPosition, target.position.z) + targetOffset;

        transform.rotation = finalRotation;
        transform.position = focusPoint - (finalRotation * Vector3.forward * distance);
    }
}