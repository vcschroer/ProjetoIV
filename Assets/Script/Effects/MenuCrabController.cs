using UnityEngine;
using UnityEngine.InputSystem;

public class MenuCrabController : MonoBehaviour
{
    [Header("Partes do Caranguejo (UI)")]
    [SerializeField] private RectTransform crabBody;
    [SerializeField] private RectTransform leftLegs;
    [SerializeField] private RectTransform rightLegs;

    [Header("Patrulha pelo Menu")]
    [SerializeField] private float patrolSpeed = 90f;
    [SerializeField] private float minMoveTime = 2.0f;
    [SerializeField] private float maxMoveTime = 5.0f;
    [SerializeField] private float maxAllowedDistanceOutside = 70f;

    [Header("Fuga do Mouse")]
    [SerializeField] private float detectionRadius = 180f;
    [SerializeField] private float fleeSpeed = 380f;
    [SerializeField] private float acceleration = 8f;

    [Header("Animação das Patinhas")]
    [SerializeField] private float patrolLegSpeed = 8f;
    [SerializeField] private float patrolLegAngle = 6f;
    [SerializeField] private float runLegSpeed = 24f;
    [SerializeField] private float runLegAngle = 14f;
    [SerializeField] private float legBobAmount = 3.5f;

    [Header("Inclinação do Corpo")]
    [SerializeField] private float bodyTiltAngle = 6f;

    [Header("Rotação Suave")]
    [SerializeField] private float rotationSpeed = 2.0f;

    private RectTransform crabRect;
    private RectTransform parentRect;
    private Canvas parentCanvas;

    private float currentSpeed = 0f;
    private float targetSpeed = 90f;
    private float moveTimer = 0f;
    private float stuckTimer = 0f;
    private float legAnimationTimer = 0f;
    private float currentRotationAngle = 0f;
    private float targetRotationAngle = 0f;

    private Quaternion initialLeftLegRot;
    private Quaternion initialRightLegRot;
    private Vector2 initialLeftLegPos;
    private Vector2 initialRightLegPos;
    private Quaternion initialBodyRot;

    private void Awake()
    {
        crabRect = GetComponent<RectTransform>();
        parentRect = crabRect.parent as RectTransform;
        parentCanvas = GetComponentInParent<Canvas>();

        if (crabBody == null) crabBody = crabRect;

        if (leftLegs != null)
        {
            initialLeftLegRot = leftLegs.localRotation;
            initialLeftLegPos = leftLegs.anchoredPosition;
        }

        if (rightLegs != null)
        {
            initialRightLegRot = rightLegs.localRotation;
            initialRightLegPos = rightLegs.anchoredPosition;
        }

        if (crabBody != null)
        {
            initialBodyRot = crabBody.localRotation;
        }

        PickNewPatrolAngle();
    }

    private void Update()
    {
        HandleMovementAndRotation();
        AnimateLegsAndBody();
    }

    private void HandleMovementAndRotation()
    {
        if (parentRect == null) return;

        Vector2 currentPos = crabRect.anchoredPosition;
        float halfWidth = parentRect.rect.width * 0.5f;
        float halfHeight = parentRect.rect.height * 0.5f;

        bool isFleeing = false;
        targetSpeed = patrolSpeed;

        if (Mouse.current != null)
        {
            Vector2 mouseScreenPos = Mouse.current.position.ReadValue();
            Camera uiCam = (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                ? parentCanvas.worldCamera
                : null;

            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, mouseScreenPos, uiCam, out Vector2 localMousePos))
            {
                Vector2 diff = currentPos - localMousePos;
                float distance = diff.magnitude;

                if (distance < detectionRadius)
                {
                    isFleeing = true;
                    Vector2 fleeDir = diff.normalized;
                    if (fleeDir.sqrMagnitude < 0.01f)
                    {
                        fleeDir = new Vector2(Mathf.Cos(targetRotationAngle * Mathf.Deg2Rad), Mathf.Sin(targetRotationAngle * Mathf.Deg2Rad));
                    }

                    targetRotationAngle = Mathf.Atan2(fleeDir.y, fleeDir.x) * Mathf.Rad2Deg;
                    ResetMoveTimer();

                    float panicFactor = 1f - (distance / detectionRadius);
                    targetSpeed = fleeSpeed * Mathf.Lerp(0.7f, 1.2f, panicFactor);
                }
            }
        }

        float limitX = halfWidth + maxAllowedDistanceOutside;
        float limitY = halfHeight + maxAllowedDistanceOutside;

        if (Mathf.Abs(currentPos.x) > limitX || Mathf.Abs(currentPos.y) > limitY)
        {
            stuckTimer += Time.deltaTime;

            Vector2 dirToCenter = (Vector2.zero - currentPos).normalized;
            targetRotationAngle = Mathf.Atan2(dirToCenter.y, dirToCenter.x) * Mathf.Rad2Deg;

            if (stuckTimer >= 1.0f)
            {
                crabRect.anchoredPosition = Vector2.Lerp(currentPos, Vector2.zero, Time.deltaTime * 2f);
            }
        }
        else
        {
            stuckTimer = 0f;
            if (!isFleeing)
            {
                moveTimer -= Time.deltaTime;
                if (moveTimer <= 0f)
                {
                    PickNewPatrolAngle();
                }
            }
        }

        currentRotationAngle = Mathf.LerpAngle(currentRotationAngle, targetRotationAngle, Time.deltaTime * rotationSpeed);
        crabRect.localRotation = Quaternion.Euler(0f, 0f, currentRotationAngle);

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * acceleration);

        float rad = currentRotationAngle * Mathf.Deg2Rad;
        Vector2 moveDirection = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

        crabRect.anchoredPosition = currentPos + moveDirection * currentSpeed * Time.deltaTime;
    }

    private void PickNewPatrolAngle()
    {
        targetRotationAngle = Random.Range(0f, 360f);
        ResetMoveTimer();
    }

    private void ResetMoveTimer()
    {
        moveTimer = Random.Range(minMoveTime, maxMoveTime);
    }

    private void AnimateLegsAndBody()
    {
        float speedRatio = Mathf.Clamp01((currentSpeed - patrolSpeed) / Mathf.Max(1f, fleeSpeed - patrolSpeed));

        float currentLegSpeed = Mathf.Lerp(patrolLegSpeed, runLegSpeed, speedRatio);
        float currentLegAngle = Mathf.Lerp(patrolLegAngle, runLegAngle, speedRatio);

        legAnimationTimer += Time.deltaTime * currentLegSpeed;

        float leftWave = Mathf.Sin(legAnimationTimer);
        float rightWave = Mathf.Sin(legAnimationTimer + Mathf.PI);

        if (leftLegs != null)
        {
            leftLegs.localRotation = initialLeftLegRot * Quaternion.Euler(0f, 0f, leftWave * currentLegAngle);
            float offsetY = Mathf.Abs(leftWave) * legBobAmount * Mathf.Lerp(0.5f, 1f, speedRatio);
            leftLegs.anchoredPosition = initialLeftLegPos + new Vector2(0f, offsetY);
        }

        if (rightLegs != null)
        {
            rightLegs.localRotation = initialRightLegRot * Quaternion.Euler(0f, 0f, rightWave * currentLegAngle);
            float offsetY = Mathf.Abs(rightWave) * legBobAmount * Mathf.Lerp(0.5f, 1f, speedRatio);
            rightLegs.anchoredPosition = initialRightLegPos + new Vector2(0f, offsetY);
        }

        if (crabBody != null)
        {
            float targetTilt = Mathf.Sin(legAnimationTimer * 2f) * bodyTiltAngle * speedRatio;
            Quaternion targetBodyRot = initialBodyRot * Quaternion.Euler(0f, 0f, targetTilt);
            crabBody.localRotation = Quaternion.Slerp(crabBody.localRotation, targetBodyRot, Time.deltaTime * 10f);
        }
    }
}