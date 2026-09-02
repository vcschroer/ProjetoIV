using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems; // Necessário para verificar interações com a UI

public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 0.4f;
    [SerializeField] private LayerMask tileLayer;

    [Header("Efeitos Visuais / Partículas")]
    [SerializeField] private ParticleSystem dustParticles;

    private bool isMoving = false;

    private void Update()
    {
        if (isMoving) return;

        // TRAVA DE UI: Se o mapa do tesouro estiver expandido ou o cursor estiver sobre a UI, ignora cliques de movimentação no mundo 3D
        if ((TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            HandleLeftClick();
        }
    }

    private void HandleLeftClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            BlockTile clickedTile = hit.collider.GetComponentInParent<BlockTile>();

            if (clickedTile != null)
            {
                Vector3Int startPos = Vector3Int.RoundToInt(transform.position);
                Vector3Int targetPos = clickedTile.gridPosition + Vector3Int.up;

                if (startPos == targetPos) return;

                List<Vector3Int> path = GridManager.Instance.FindPath(startPos, targetPos);

                if (path != null && path.Count > 0)
                {
                    bool isPathValid = true;
                    foreach (Vector3Int step in path)
                    {
                        Vector3Int groundCheckPos = new Vector3Int(step.x, step.y - 1, step.z);
                        if (GridManager.Instance.GetTileAt(groundCheckPos) == null)
                        {
                            isPathValid = false;
                            break;
                        }
                    }

                    if (isPathValid)
                    {
                        StopAllCoroutines();
                        StartCoroutine(MoveAlongPath(path));
                    }
                }
            }
        }
    }

    private IEnumerator MoveAlongPath(List<Vector3Int> path)
    {
        isMoving = true;
        PirateStackManager stackManager = GetComponent<PirateStackManager>();

        foreach (Vector3Int step in path)
        {
            Vector3Int groundPos = new Vector3Int(step.x, step.y - 1, step.z);
            BlockTile groundTile = GridManager.Instance != null ? GridManager.Instance.GetTileAt(groundPos) : null;

            if (groundTile == null)
            {
                isMoving = false;
                yield break;
            }

            if (groundTile.maxAllowedHeight > 0)
            {
                int currentHeight = 1 + (stackManager != null ? stackManager.GetStackCount() : 0);

                if (currentHeight > groundTile.maxAllowedHeight)
                {
                    isMoving = false;
                    yield break;
                }
            }

            Vector3 startPos = transform.position;
            Vector3 targetWorldPos = new Vector3(step.x, step.y, step.z);

            Vector3 moveDirection = (targetWorldPos - startPos);
            moveDirection.y = 0;

            if (moveDirection.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.LookRotation(moveDirection);
            }

            PlayDustParticles();

            if (stackManager != null)
            {
                stackManager.OnPlayerStep();
            }

            float distance = Vector3.Distance(startPos, targetWorldPos);
            float stepDuration = Mathf.Max(0.1f, distance / moveSpeed);
            float elapsed = 0f;

            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / stepDuration;

                Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

                transform.position = currentPos;
                yield return null;
            }

            transform.position = targetWorldPos;

            PlayDustParticles();

            if (groundTile.isQuicksand)
            {
                if (stackManager != null && stackManager.GetStackCount() > 0)
                {
                    StartCoroutine(SinkInQuicksandRoutine());
                    yield break;
                }
            }

            if (stackManager != null)
            {
                stackManager.TriggerStackImpact(topToBottom: false);
            }
        }

        isMoving = false;
    }

    private IEnumerator SinkInQuicksandRoutine()
    {
        isMoving = true;

        Vector3 startPos = transform.position;
        Vector3 sinkTargetPos = startPos + new Vector3(0, -0.65f, 0);

        Quaternion startRot = transform.rotation;
        Quaternion sinkRot = startRot * Quaternion.Euler(12f, 0f, -8f);

        float sinkDuration = 1.2f;
        float elapsed = 0f;

        PlayDustParticles();

        while (elapsed < sinkDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / sinkDuration;

            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(startPos, sinkTargetPos, smoothProgress);
            transform.rotation = Quaternion.Slerp(startRot, sinkRot, smoothProgress);

            yield return null;
        }

        transform.position = sinkTargetPos;
        isMoving = false;
    }

    private void PlayDustParticles()
    {
        if (dustParticles != null)
        {
            dustParticles.Play();
        }
    }
}