using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Necessário para verificar se o cursor está sobre a UI

public class IsometricOrbitCamera : MonoBehaviour
{
    [Header("Alvo de Foco")]
    public Transform target;
    public Vector3 targetOffset = new Vector3(0, 1.5f, 0);

    [Header("Configurações de Zoom (Scroll)")]
    public float distance = 60.0f;
    public float minDistance = 10.0f;
    public float maxDistance = 75.0f; // Aumentado para permitir a distância de 60
    public float zoomSensitivity = 2.0f;
    public float zoomSmoothSpeed = 5.0f; // Velocidade da transição do zoom

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

    private float targetDistance;
    private bool isZoomLocked = false;

    void Start()
    {
        if (target != null) groundedYPosition = target.position.y;
        targetDistance = distance;
    }

    void LateUpdate()
    {
        if (target == null || Mouse.current == null) return;

        // TRAVA 1: Se o mapa do tesouro estiver expandido, cancela a órbita e ignora comandos de câmera
        if (TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded)
        {
            if (isOrbiting)
            {
                isOrbiting = false;
                if (CursorManager.Instance != null)
                    CursorManager.Instance.SetCursorType(CursorState.Default);
            }

            UpdateCameraPosition();
            return;
        }

        // Início da rotação (botão do meio) -> Funciona durante a cutscene!
        if (Mouse.current.middleButton.wasPressedThisFrame)
        {
            if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
            {
                isOrbiting = true;
                if (CursorManager.Instance != null)
                    CursorManager.Instance.SetCursorType(CursorState.OrbitingCamera);
            }
        }

        // Término da rotação
        if (Mouse.current.middleButton.wasReleasedThisFrame)
        {
            isOrbiting = false;
            if (CursorManager.Instance != null)
                CursorManager.Instance.SetCursorType(CursorState.Default);
        }

        // Aplica a rotação se estiver orbitando
        if (isOrbiting)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            currentYaw += mouseDelta.x * rotationSensitivityX;
            currentPitch -= mouseDelta.y * rotationSensitivityY;
            currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
        }

        // Controle de Zoom pelo Scroll (Bloqueado durante cutscenes)
        if (!isZoomLocked)
        {
            float scrollValue = Mouse.current.scroll.ReadValue().y;
            if (Mathf.Abs(scrollValue) > 0.01f)
            {
                if (EventSystem.current == null || !EventSystem.current.IsPointerOverGameObject())
                {
                    targetDistance -= (scrollValue / 120.0f) * zoomSensitivity;
                    targetDistance = Mathf.Clamp(targetDistance, minDistance, maxDistance);
                }
            }
        }

        // Transição suave do zoom para a targetDistance
        distance = Mathf.Lerp(distance, targetDistance, Time.deltaTime * zoomSmoothSpeed);

        UpdateCameraPosition();
    }

    /// <summary>
    /// Bloqueia ou libera o zoom do scroll. Se passar uma nova distância, transiciona suavemente até ela.
    /// </summary>
    public void SetZoomLock(bool locked, float newDistance = -1f)
    {
        isZoomLocked = locked;
        if (newDistance > 0f)
        {
            targetDistance = Mathf.Clamp(newDistance, minDistance, maxDistance);
        }
    }

    private void UpdateCameraPosition()
    {
        groundedYPosition = Mathf.Lerp(groundedYPosition, target.position.y, Time.deltaTime * 3.0f);
        Quaternion finalRotation = Quaternion.Euler(currentPitch, currentYaw, 0);
        Vector3 focusPoint = new Vector3(target.position.x, groundedYPosition, target.position.z) + targetOffset;

        transform.rotation = finalRotation;
        transform.position = focusPoint - (finalRotation * Vector3.forward * distance);
    }
}