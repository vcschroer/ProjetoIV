using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PirateStackManager : MonoBehaviour
{
    [Header("Configurações da Pilha")]
    [SerializeField] private List<GameObject> stackedPirates = new List<GameObject>();
    [SerializeField] private float stepHeight = 1.0f;
    [SerializeField] private float jumpDuration = 0.25f;

    [Header("Configurações do Bloco Solto")]
    [SerializeField] private BlockData defaultWalkableData;
    [SerializeField] private string tileLayerName = "Tile";

    private bool isBusy = false;
    private BlockTile currentHoveredTile;

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            ClearTileHighlight();
            return;
        }

        UpdateCursorState();

        if (isBusy) return;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    private void LateUpdate()
    {
        if (!isBusy)
        {
            MaintainStackPositions();
        }
    }

    private void UpdateCursorState()
    {
        if (CursorManager.Instance == null || Mouse.current == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            bool isDroppedPirate = hitObject.CompareTag("DroppedPirate") ||
                                   (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("DroppedPirate"));

            if (isDroppedPirate)
            {
                BlockTile pirateTile = hitObject.GetComponentInParent<BlockTile>();
                UpdateTileHighlight(pirateTile);
                CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                return;
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            UpdateTileHighlight(clickedTile);

            if (clickedTile != null)
            {
                if (clickedTile.isWaterTile)
                {
                    Collider[] collidersInWater = Physics.OverlapSphere(clickedTile.transform.position, 0.4f);
                    foreach (Collider col in collidersInWater)
                    {
                        if (col.CompareTag("DroppedPirate") || (col.transform.parent != null && col.transform.parent.CompareTag("DroppedPirate")))
                        {
                            CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                            return;
                        }
                    }
                }

                int currentTotalHeight = 1 + GetStackCount();
                if (clickedTile.maxAllowedHeight > 0 && currentTotalHeight > clickedTile.maxAllowedHeight)
                {
                    CursorManager.Instance.SetCursorType(CursorState.InvalidPlacement);
                    return;
                }

                Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
                Vector3Int groundGridPos = Vector3Int.RoundToInt(clickedTile.transform.position);

                int distanceX = Mathf.Abs(playerPos.x - groundGridPos.x);
                int distanceZ = Mathf.Abs(playerPos.z - groundGridPos.z);
                int distanceY = Mathf.Abs(playerPos.y - groundGridPos.y);

                bool isWithinRange = (distanceX <= 1 && distanceZ <= 1) && (distanceX > 0 || distanceZ > 0) && (distanceY <= 2);

                if (isWithinRange && stackedPirates.Count > 0)
                {
                    if (clickedTile.isWaterTile)
                    {
                        CursorManager.Instance.SetCursorType(CursorState.ValidPlacement);
                        return;
                    }

                    if (GridManager.Instance != null)
                    {
                        Vector3Int airPosAbove = groundGridPos + Vector3Int.up;
                        if (GridManager.Instance.GetTileAt(airPosAbove) == null)
                        {
                            CursorManager.Instance.SetCursorType(CursorState.ValidPlacement);
                            return;
                        }
                    }
                }

                if (clickedTile.data != null && clickedTile.data.isWalkable && !clickedTile.isWaterTile)
                {
                    CursorManager.Instance.SetCursorType(CursorState.WalkToTile);
                    return;
                }

                CursorManager.Instance.SetCursorType(CursorState.InvalidPlacement);
                return;
            }
        }
        else
        {
            ClearTileHighlight();
        }

        CursorManager.Instance.SetCursorType(CursorState.Default);
    }

    private void UpdateTileHighlight(BlockTile newTile)
    {
        if (currentHoveredTile != newTile)
        {
            if (currentHoveredTile != null)
            {
                currentHoveredTile.SetHighlight(false);
            }

            currentHoveredTile = newTile;

            if (currentHoveredTile != null)
            {
                currentHoveredTile.SetHighlight(true);
            }
        }
    }

    private void ClearTileHighlight()
    {
        if (currentHoveredTile != null)
        {
            currentHoveredTile.SetHighlight(false);
            currentHoveredTile = null;
        }
    }

    private void MaintainStackPositions()
    {
        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                Vector3 targetPos = transform.position + new Vector3(0, (i + 1) * stepHeight, 0);
                stackedPirates[i].transform.position = targetPos;
                stackedPirates[i].transform.rotation = transform.rotation;
            }
        }
    }

    public void OnPlayerStep()
    {
        PirateJuice baseJuice = GetComponentInChildren<PirateJuice>();
        if (baseJuice != null)
        {
            baseJuice.TriggerBounce(0f);
            baseJuice.TriggerStepSway(delay: 0f, heightMultiplier: 0.7f);
        }

        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                PirateJuice juice = stackedPirates[i].GetComponentInChildren<PirateJuice>();
                if (juice != null)
                {
                    float delay = (i + 1) * 0.035f;
                    float heightMultiplier = 1f + (i * 0.45f);

                    juice.TriggerBounce(delay);
                    juice.TriggerStepSway(delay, heightMultiplier);
                }
            }
        }
    }

    public void TriggerStackImpact(bool topToBottom = true)
    {
        PirateJuice baseJuice = GetComponentInChildren<PirateJuice>();
        if (baseJuice != null)
        {
            float baseDelay = topToBottom ? stackedPirates.Count * 0.04f : 0f;
            baseJuice.TriggerDropSquash(baseDelay);
        }

        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                PirateJuice juice = stackedPirates[i].GetComponentInChildren<PirateJuice>();
                if (juice != null)
                {
                    int hierarchyIndex = topToBottom ? (stackedPirates.Count - 1 - i) : (i + 1);
                    float delay = hierarchyIndex * 0.04f;
                    juice.TriggerDropSquash(delay);
                }
            }
        }
    }

    private void HandleRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("DroppedPirate") || (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("DroppedPirate")))
            {
                GameObject rootPirate = hitObject.CompareTag("DroppedPirate") ? hitObject : hitObject.transform.parent.gameObject;
                TryPickupPirate(rootPirate);
                return;
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            if (clickedTile != null)
            {
                if (clickedTile.isWaterTile)
                {
                    Collider[] collidersInWater = Physics.OverlapSphere(clickedTile.transform.position, 0.4f);
                    foreach (Collider col in collidersInWater)
                    {
                        if (col.CompareTag("DroppedPirate") || (col.transform.parent != null && col.transform.parent.CompareTag("DroppedPirate")))
                        {
                            GameObject rootPirate = col.CompareTag("DroppedPirate") ? col.gameObject : col.transform.parent.gameObject;
                            TryPickupPirate(rootPirate);
                            return;
                        }
                    }
                }

                TryDropPirate(clickedTile);
            }
        }
    }

    private void TryPickupPirate(GameObject targetPirate)
    {
        PirateIdentity identity = targetPirate.GetComponent<PirateIdentity>();

        if (identity != null)
        {
            int expectedID = stackedPirates.Count + 1;

            if (identity.pirateID != expectedID)
            {
                return;
            }
        }

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int piratePos = Vector3Int.RoundToInt(targetPirate.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - piratePos.x);
        int distanceZ = Mathf.Abs(playerPos.z - piratePos.z);
        int distanceY = Mathf.Abs(playerPos.y - piratePos.y);

        if (distanceX <= 1 && distanceZ <= 1 && (distanceX > 0 || distanceZ > 0) && distanceY <= 2)
        {
            StartCoroutine(PickupRoutine(targetPirate));
        }
    }

    private IEnumerator PickupRoutine(GameObject pirate)
    {
        isBusy = true;

        Vector3Int currentPirateGridPos = Vector3Int.RoundToInt(pirate.transform.position);
        if (GridManager.Instance != null)
        {
            BlockTile waterTile = GridManager.Instance.GetTileAt(currentPirateGridPos);
            if (waterTile != null && waterTile.isWaterTile)
            {
                if (waterTile.data != null)
                {
                    waterTile.data.isWalkable = false;
                }
            }
        }

        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            juice.StopFloating();
            juice.TriggerPickupStretch();
        }

        BlockTile tileScript = pirate.GetComponent<BlockTile>();
        if (tileScript != null)
        {
            tileScript.SetHighlight(false);
            tileScript.enabled = false;
        }

        Collider col = pirate.GetComponent<Collider>();
        if (col != null)
        {
            col.enabled = false;
        }

        pirate.tag = "Untagged";
        SetLayerRecursively(pirate, LayerMask.NameToLayer("Default"));

        Vector3 startPos = pirate.transform.position;
        int targetIndex = stackedPirates.Count;
        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 topTargetPos = transform.position + new Vector3(0, (targetIndex + 1) * stepHeight, 0);
            Vector3 currentPos = Vector3.Lerp(startPos, topTargetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            pirate.transform.position = currentPos;
            yield return null;
        }

        stackedPirates.Add(pirate);

        TriggerStackImpact(topToBottom: true);

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    private void TryDropPirate(BlockTile targetTile)
    {
        if (stackedPirates.Count == 0) return;

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int groundGridPos = Vector3Int.RoundToInt(targetTile.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - groundGridPos.x);
        int distanceZ = Mathf.Abs(playerPos.z - groundGridPos.z);
        int distanceY = Mathf.Abs(playerPos.y - groundGridPos.y);

        bool isWithinDropRange = (distanceX <= 1 && distanceZ <= 1) && (distanceX > 0 || distanceZ > 0) && (distanceY <= 2);

        if (!isWithinDropRange)
        {
            return;
        }

        int topIndex = stackedPirates.Count - 1;
        GameObject pirateToDrop = stackedPirates[topIndex];
        stackedPirates.RemoveAt(topIndex);

        StartCoroutine(DropRoutine(pirateToDrop, groundGridPos, isWater: targetTile.isWaterTile));
    }

    private IEnumerator DropRoutine(GameObject pirate, Vector3Int targetGridPos, bool isWater)
    {
        isBusy = true;

        Vector3 startPos = pirate.transform.position;

        Vector3Int finalGridPos = isWater ? targetGridPos : new Vector3Int(targetGridPos.x, targetGridPos.y + 1, targetGridPos.z);
        Vector3 targetPos = new Vector3(finalGridPos.x, finalGridPos.y, finalGridPos.z);

        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            pirate.transform.position = currentPos;
            yield return null;
        }

        pirate.transform.position = targetPos;
        pirate.transform.rotation = Quaternion.identity;

        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        pirate.tag = "DroppedPirate";

        int tileLayerIndex = LayerMask.NameToLayer(tileLayerName);
        if (tileLayerIndex != -1)
        {
            SetLayerRecursively(pirate, tileLayerIndex);
        }

        Collider col = pirate.GetComponent<Collider>();
        if (col == null) col = pirate.AddComponent<BoxCollider>();
        col.enabled = true;

        if (isWater)
        {
            BlockTile waterTile = GridManager.Instance.GetTileAt(targetGridPos);
            if (waterTile != null && waterTile.data != null)
            {
                waterTile.data = Instantiate(waterTile.data);
                waterTile.data.isWalkable = true;
            }

            if (juice != null)
            {
                juice.StartFloating();
            }
        }
        else
        {
            if (juice != null)
            {
                juice.TriggerDropSquash();
            }

            BlockTile tile = pirate.GetComponent<BlockTile>();
            if (tile != null)
            {
                tile.enabled = true;
                if (tile.data == null) tile.data = defaultWalkableData;
                tile.Setup(finalGridPos, tile.data);
            }
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    public int GetStackCount()
    {
        return stackedPirates.Count;
    }

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }

    public void AddToStackDirectly(GameObject pirate)
    {
        if (pirate == null) return;

        Collider col = pirate.GetComponent<Collider>();
        if (col != null) col.enabled = false;

        pirate.tag = "Untagged";
        SetLayerRecursively(pirate, LayerMask.NameToLayer("Default"));

        stackedPirates.Add(pirate);
        TriggerStackImpact(topToBottom: true);
    }
}