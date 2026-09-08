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

    public float StepHeight => stepHeight;
    public bool IsBusy => isBusy;

    private bool isBusy = false;
    private BlockTile currentHoveredTile;
    private CrateStackManager crateStackManager;

    private void Awake()
    {
        crateStackManager = GetComponent<CrateStackManager>();
    }

    private void Update()
    {
        if (Mouse.current != null && Mouse.current.middleButton.isPressed)
        {
            ClearTileHighlight();
            return;
        }

        UpdateCursorState();

        if (isBusy || (crateStackManager != null && crateStackManager.IsBusy)) return;

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

    private bool IsPlayerStandingOn(GameObject obj)
    {
        if (obj == null) return false;
        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int objPos = Vector3Int.RoundToInt(obj.transform.position);
        return (playerPos.x == objPos.x && playerPos.z == objPos.z);
    }

    public GameObject GetDroppedPirateRoot(GameObject hitObject)
    {
        if (hitObject == null) return null;

        Transform current = hitObject.transform;
        while (current != null)
        {
            if (current.CompareTag("DroppedPirate"))
            {
                GameObject pirateObj = current.gameObject;
                if (stackedPirates.Contains(pirateObj)) return null;
                return pirateObj;
            }
            current = current.parent;
        }
        return null;
    }

    private GameObject GetAnyDroppedRoot(GameObject hitObject)
    {
        if (hitObject == null) return null;

        Transform current = hitObject.transform;
        while (current != null)
        {
            if (current.CompareTag("DroppedPirate") || current.CompareTag("DroppedCrate"))
            {
                GameObject obj = current.gameObject;
                if (stackedPirates.Contains(obj)) return null;
                return obj;
            }
            current = current.parent;
        }
        return null;
    }

    private Vector3Int CalculateDropGridPos(BlockTile targetTile, out bool isWaterDrop)
    {
        Vector3Int tileGridPos = Vector3Int.RoundToInt(targetTile.transform.position);

        if (targetTile.isWaterTile)
        {
            Collider[] collidersInWater = Physics.OverlapSphere(new Vector3(tileGridPos.x, tileGridPos.y + 0.5f, tileGridPos.z), 0.35f);
            bool objectInWater = false;
            foreach (var col in collidersInWater)
            {
                if (GetAnyDroppedRoot(col.gameObject) != null)
                {
                    objectInWater = true;
                    break;
                }
            }

            if (!objectInWater)
            {
                isWaterDrop = true;
                return tileGridPos;
            }
            else
            {
                isWaterDrop = false;
                int height = GetColumnStackHeight(tileGridPos);
                return new Vector3Int(tileGridPos.x, tileGridPos.y + height, tileGridPos.z);
            }
        }
        else
        {
            isWaterDrop = false;
            int height = GetColumnStackHeight(tileGridPos + Vector3Int.up);
            return new Vector3Int(tileGridPos.x, tileGridPos.y + 1 + height, tileGridPos.z);
        }
    }

    private int GetColumnStackHeight(Vector3Int baseGridPos)
    {
        int height = 0;
        HashSet<GameObject> countedObjects = new HashSet<GameObject>();

        while (height < 20)
        {
            Vector3 checkCenter = new Vector3(baseGridPos.x, baseGridPos.y + height + 0.5f, baseGridPos.z);
            Collider[] colliders = Physics.OverlapSphere(checkCenter, 0.25f);

            GameObject newObjectFound = null;
            foreach (var col in colliders)
            {
                GameObject root = GetAnyDroppedRoot(col.gameObject);
                if (root != null && !countedObjects.Contains(root))
                {
                    newObjectFound = root;
                    break;
                }
            }

            if (newObjectFound != null)
            {
                countedObjects.Add(newObjectFound);
                height++;
            }
            else
            {
                break;
            }
        }

        return height;
    }

    public bool CanPickupPirate(GameObject targetPirate)
    {
        if (targetPirate == null || stackedPirates.Contains(targetPirate)) return false;
        if (IsPlayerStandingOn(targetPirate)) return false;

        PirateIdentity identity = targetPirate.GetComponent<PirateIdentity>();
        if (identity != null)
        {
            int expectedID = stackedPirates.Count + 1;
            if (identity.pirateID != expectedID) return false;
        }

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int piratePos = Vector3Int.RoundToInt(targetPirate.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - piratePos.x);
        int distanceZ = Mathf.Abs(playerPos.z - piratePos.z);
        int distanceY = Mathf.Abs(playerPos.y - piratePos.y);

        return (distanceX <= 1 && distanceZ <= 1 && distanceY <= 2);
    }

    private void UpdateCursorState()
    {
        if (CursorManager.Instance == null || Mouse.current == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            GameObject droppedPirate = GetDroppedPirateRoot(hitObject);
            if (droppedPirate != null && CanPickupPirate(droppedPirate))
            {
                BlockTile pirateTile = droppedPirate.GetComponentInParent<BlockTile>();
                UpdateTileHighlight(pirateTile);
                CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                return;
            }

            if (crateStackManager != null)
            {
                GameObject droppedCrate = crateStackManager.GetDroppedCrateRoot(hitObject);
                if (droppedCrate != null && crateStackManager.CanPickupCrate(droppedCrate))
                {
                    BlockTile crateTile = droppedCrate.GetComponentInParent<BlockTile>();
                    UpdateTileHighlight(crateTile);
                    CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                    return;
                }
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            UpdateTileHighlight(clickedTile);

            if (clickedTile != null)
            {
                Collider[] colliders = Physics.OverlapSphere(clickedTile.transform.position + Vector3.up * 0.5f, 0.5f);
                foreach (Collider col in colliders)
                {
                    GameObject pirateOnTile = GetDroppedPirateRoot(col.gameObject);
                    if (pirateOnTile != null && CanPickupPirate(pirateOnTile))
                    {
                        CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                        return;
                    }

                    if (crateStackManager != null)
                    {
                        GameObject crateOnTile = crateStackManager.GetDroppedCrateRoot(col.gameObject);
                        if (crateOnTile != null && crateStackManager.CanPickupCrate(crateOnTile))
                        {
                            CursorManager.Instance.SetCursorType(CursorState.PickupPirate);
                            return;
                        }
                    }
                }

                int totalCarriedCount = GetStackCount() + (crateStackManager != null ? crateStackManager.GetCrateCount() : 0);

                if (clickedTile.maxAllowedHeight > 0 && (1 + totalCarriedCount) > clickedTile.maxAllowedHeight)
                {
                    CursorManager.Instance.SetCursorType(CursorState.InvalidPlacement);
                    return;
                }

                Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
                Vector3Int groundGridPos = Vector3Int.RoundToInt(clickedTile.transform.position);

                int distanceX = Mathf.Abs(playerPos.x - groundGridPos.x);
                int distanceZ = Mathf.Abs(playerPos.z - groundGridPos.z);
                int distanceY = Mathf.Abs(playerPos.y - groundGridPos.y);

                bool isWithinRange = (distanceX <= 1 && distanceZ <= 1) && (distanceY <= 2);

                if (isWithinRange && totalCarriedCount > 0)
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

                // PRIORIDADE 3: ANDAR
                if (clickedTile.data != null && clickedTile.data.isWalkable)
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
            if (currentHoveredTile != null) currentHoveredTile.SetHighlight(false);
            currentHoveredTile = newTile;
            if (currentHoveredTile != null) currentHoveredTile.SetHighlight(true);
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

    public void MaintainStackPositions()
    {
        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] == null) continue;
            Vector3 targetPos = transform.position + Vector3.up * ((i + 1) * stepHeight);
            stackedPirates[i].transform.position = targetPos;
            stackedPirates[i].transform.rotation = transform.rotation;
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
        if (Camera.main == null) return;
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject == gameObject || hitObject.transform.IsChildOf(transform))
                return;

            GameObject rootPirate = GetDroppedPirateRoot(hitObject);

            if (rootPirate != null && !IsPlayerStandingOn(rootPirate))
            {
                bool pickedUp = TryPickupPirate(rootPirate);
                if (pickedUp) return;
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            if (clickedTile != null)
            {
                Collider[] collidersOnTile = Physics.OverlapSphere(clickedTile.transform.position + Vector3.up * 0.5f, 0.5f);
                foreach (Collider col in collidersOnTile)
                {
                    GameObject pirateOnTile = GetDroppedPirateRoot(col.gameObject);
                    if (pirateOnTile != null && !IsPlayerStandingOn(pirateOnTile))
                    {
                        bool pickedUp = TryPickupPirate(pirateOnTile);
                        if (pickedUp) return;
                    }
                }

                if (crateStackManager != null && crateStackManager.GetCrateCount() > 0 && stackedPirates.Count == 0)
                {
                    return;
                }

                if (stackedPirates.Count > 0)
                {
                    TryDropPirate(clickedTile);
                }
            }
        }
    }

    private bool TryPickupPirate(GameObject targetPirate)
    {
        if (!CanPickupPirate(targetPirate)) return false;

        StartCoroutine(PickupRoutine(targetPirate));
        return true;
    }

    private IEnumerator PickupRoutine(GameObject pirate)
    {
        if (stackedPirates.Contains(pirate)) yield break;

        isBusy = true;

        Vector3Int currentPirateGridPos = Vector3Int.RoundToInt(pirate.transform.position);

        BlockTile tileScript = pirate.GetComponent<BlockTile>();
        if (tileScript != null)
        {
            tileScript.SetHighlight(false);
            tileScript.enabled = false;
        }

        if (GridManager.Instance != null)
        {
            BlockTile tileAtPos = GridManager.Instance.GetTileAt(currentPirateGridPos);
            Vector3Int waterGridPos = currentPirateGridPos;

            if (tileAtPos == null || !tileAtPos.isWaterTile)
            {
                Vector3Int posBelow = currentPirateGridPos + Vector3Int.down;
                BlockTile tileBelow = GridManager.Instance.GetTileAt(posBelow);
                if (tileBelow != null && tileBelow.isWaterTile)
                {
                    tileAtPos = tileBelow;
                    waterGridPos = posBelow;
                }
                else if (tileBelow != null && tileBelow.data != null && !tileBelow.isWaterTile)
                {
                    tileBelow.data = Instantiate(tileBelow.data);
                    tileBelow.data.isWalkable = true;
                }
            }

            if (tileAtPos != null && tileAtPos.isWaterTile)
            {
                bool objectRemainsInWater = false;
                Collider[] colliders = Physics.OverlapSphere(new Vector3(waterGridPos.x, waterGridPos.y + 0.2f, waterGridPos.z), 0.35f);
                foreach (var col in colliders)
                {
                    GameObject root = GetAnyDroppedRoot(col.gameObject);
                    if (root != null && root != pirate)
                    {
                        objectRemainsInWater = true;
                        break;
                    }
                }

                if (!objectRemainsInWater && tileAtPos.data != null)
                {
                    tileAtPos.data = Instantiate(tileAtPos.data);
                    tileAtPos.data.isWalkable = false;
                }
            }
        }

        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            juice.StopFloating();
            juice.TriggerPickupStretch();
        }

        SetCollidersEnabled(pirate, false);
        SetTagRecursively(pirate, "Untagged");
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

        if (!stackedPirates.Contains(pirate))
        {
            stackedPirates.Add(pirate);
        }

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

        if (!isWithinDropRange) return;

        int topIndex = stackedPirates.Count - 1;
        GameObject pirateToDrop = stackedPirates[topIndex];
        stackedPirates.RemoveAt(topIndex);

        Vector3Int finalGridPos = CalculateDropGridPos(targetTile, out bool isWaterDrop);

        StartCoroutine(DropRoutine(pirateToDrop, targetTile, finalGridPos, isWater: isWaterDrop));
    }

    private IEnumerator DropRoutine(GameObject pirate, BlockTile targetTile, Vector3Int finalGridPos, bool isWater)
    {
        isBusy = true;

        Vector3 startPos = pirate.transform.position;
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
        SetTagRecursively(pirate, "DroppedPirate");

        int tileLayerIndex = LayerMask.NameToLayer(tileLayerName);
        if (tileLayerIndex != -1)
        {
            SetLayerRecursively(pirate, tileLayerIndex);
        }

        SetCollidersEnabled(pirate, true);

        BlockTile tile = pirate.GetComponent<BlockTile>();
        if (tile == null) tile = pirate.AddComponent<BlockTile>();

        tile.enabled = true;
        if (tile.data == null) tile.data = defaultWalkableData != null ? Instantiate(defaultWalkableData) : ScriptableObject.CreateInstance<BlockData>();
        else tile.data = Instantiate(tile.data);

        tile.data.isWalkable = true;
        tile.Setup(finalGridPos, tile.data);

        if (isWater)
        {
            BlockTile waterTile = GridManager.Instance != null ? GridManager.Instance.GetTileAt(finalGridPos) : targetTile;
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

            if (targetTile != null && targetTile.data != null && !targetTile.isWaterTile && targetTile != tile)
            {
                targetTile.data = Instantiate(targetTile.data);
                targetTile.data.isWalkable = false;
            }
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    public int GetStackCount() => stackedPirates.Count;

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null) SetLayerRecursively(child.gameObject, newLayer);
        }
    }

    private void SetTagRecursively(GameObject obj, string newTag)
    {
        if (obj == null) return;
        obj.tag = newTag;

        foreach (Transform child in obj.transform)
        {
            if (child != null) SetTagRecursively(child.gameObject, newTag);
        }
    }

    private void SetCollidersEnabled(GameObject obj, bool enabledState)
    {
        if (obj == null) return;
        Collider[] colliders = obj.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders)
        {
            col.enabled = enabledState;
        }
    }

    public void AddToStackDirectly(GameObject pirate)
    {
        if (pirate == null) return;

        SetCollidersEnabled(pirate, false);
        SetTagRecursively(pirate, "Untagged");
        SetLayerRecursively(pirate, LayerMask.NameToLayer("Default"));

        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            juice.StopFloating();
            juice.StopSway();
        }

        if (!stackedPirates.Contains(pirate))
        {
            stackedPirates.Add(pirate);
        }

        int index = stackedPirates.IndexOf(pirate);
        pirate.transform.position = transform.position + Vector3.up * ((index + 1) * stepHeight);
        pirate.transform.rotation = transform.rotation;

        TriggerStackImpact(topToBottom: true);
    }
}