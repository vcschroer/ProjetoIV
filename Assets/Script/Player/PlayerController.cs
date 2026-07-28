using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 0.4f;
    [SerializeField] private LayerMask tileLayer;

    private bool isMoving = false;

    private void Update()
    {
        if (isMoving) return;

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
                    StopAllCoroutines();
                    StartCoroutine(MoveAlongPath(path));
                }
                else
                {
                    Debug.LogWarning($"[PlayerController] Caminho inválido até a posição {targetPos}!");
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
            if (stackManager != null)
            {
                stackManager.OnPlayerStep();
            }

            Vector3 startPos = transform.position;
            Vector3 targetWorldPos = new Vector3(step.x, step.y, step.z);

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

            if (stackManager != null)
            {
                stackManager.TriggerStackImpact(topToBottom: false);
            }
        }

        isMoving = false;
    }
}