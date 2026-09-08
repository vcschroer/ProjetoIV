using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class CrateStackManager : MonoBehaviour
{
    [Header("Configurações da Pilha")]
    [SerializeField] private List<GameObject> stackedCrates = new List<GameObject>();
    [SerializeField] private float stepHeight = 1.0f;
    [SerializeField] private float jumpDuration = 0.25f;

    [Header("Configurações do Bloco Solto")]
    [SerializeField] private BlockData defaultWalkableData;
    [SerializeField] private string tileLayerName = "Tile";

    public float StepHeight => stepHeight;
    public bool IsBusy => isBusy;

    private bool isBusy = false;
    private PirateStackManager pirateStackManager;

    private void Awake()
    {
        pirateStackManager = GetComponent<PirateStackManager>();
    }

    private void Update()
    {
        if (isBusy || (pirateStackManager != null && pirateStackManager.IsBusy)) return;

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

    public GameObject GetDroppedCrateRoot(GameObject hitObject)
    {
        if (hitObject == null) return null;

        Transform current = hitObject.transform;
        while (current != null)
        {
            if (current.CompareTag("DroppedCrate"))
            {
                GameObject crateObj = current.gameObject;
                if (stackedCrates.Contains(crateObj)) return null;
                return crateObj;
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
                if (stackedCrates.Contains(obj)) return null;
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

    public bool CanPickupCrate(GameObject targetCrate)
    {
        if (targetCrate == null || stackedCrates.Contains(targetCrate)) return false;
        if (IsPlayerStandingOn(targetCrate)) return false;

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int cratePos = Vector3Int.RoundToInt(targetCrate.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - cratePos.x);
        int distanceZ = Mathf.Abs(playerPos.z - cratePos.z);
        int distanceY = Mathf.Abs(playerPos.y - cratePos.y);

        return (distanceX <= 1 && distanceZ <= 1 && distanceY <= 2);
    }

    public void MaintainStackPositions()
    {
        int pirateOffset = (pirateStackManager != null) ? pirateStackManager.GetStackCount() : 0;
        float baseOffsetHeight = pirateOffset * stepHeight;

        for (int i = 0; i < stackedCrates.Count; i++)
        {
            if (stackedCrates[i] == null) continue;
            Vector3 targetPos = transform.position + Vector3.up * (baseOffsetHeight + (i + 1) * stepHeight);
            stackedCrates[i].transform.position = targetPos;
            stackedCrates[i].transform.rotation = transform.rotation;
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

            GameObject rootCrate = GetDroppedCrateRoot(hitObject);

            if (rootCrate != null && !IsPlayerStandingOn(rootCrate))
            {
                bool pickedUp = TryPickupCrate(rootCrate);
                if (pickedUp) return;
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            if (clickedTile != null)
            {
                Collider[] collidersOnTile = Physics.OverlapSphere(clickedTile.transform.position + Vector3.up * 0.5f, 0.5f);
                foreach (Collider col in collidersOnTile)
                {
                    GameObject crateOnTile = GetDroppedCrateRoot(col.gameObject);
                    if (crateOnTile != null && !IsPlayerStandingOn(crateOnTile))
                    {
                        bool pickedUp = TryPickupCrate(crateOnTile);
                        if (pickedUp) return;
                    }
                }

                if (pirateStackManager != null && pirateStackManager.GetStackCount() > 0)
                {
                    return;
                }

                if (stackedCrates.Count > 0)
                {
                    TryDropCrate(clickedTile);
                }
            }
        }
    }

    private bool TryPickupCrate(GameObject targetCrate)
    {
        if (!CanPickupCrate(targetCrate)) return false;

        StartCoroutine(PickupRoutine(targetCrate));
        return true;
    }

    private IEnumerator PickupRoutine(GameObject crate)
    {
        if (stackedCrates.Contains(crate)) yield break;

        isBusy = true;

        Vector3Int currentCrateGridPos = Vector3Int.RoundToInt(crate.transform.position);

        BlockTile tileScript = crate.GetComponent<BlockTile>();
        if (tileScript != null)
        {
            tileScript.SetHighlight(false);
            tileScript.enabled = false;
        }

        if (GridManager.Instance != null)
        {
            BlockTile tileAtPos = GridManager.Instance.GetTileAt(currentCrateGridPos);
            Vector3Int waterGridPos = currentCrateGridPos;

            if (tileAtPos == null || !tileAtPos.isWaterTile)
            {
                Vector3Int posBelow = currentCrateGridPos + Vector3Int.down;
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
                    if (root != null && root != crate)
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

        SetCollidersEnabled(crate, false);
        SetTagRecursively(crate, "Untagged");
        SetLayerRecursively(crate, LayerMask.NameToLayer("Default"));

        Vector3 startPos = crate.transform.position;
        int targetIndex = stackedCrates.Count;
        int pirateOffset = (pirateStackManager != null) ? pirateStackManager.GetStackCount() : 0;
        float totalOffset = (pirateOffset + targetIndex + 1) * stepHeight;

        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 topTargetPos = transform.position + new Vector3(0, totalOffset, 0);
            Vector3 currentPos = Vector3.Lerp(startPos, topTargetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            crate.transform.position = currentPos;
            yield return null;
        }

        if (!stackedCrates.Contains(crate))
        {
            stackedCrates.Add(crate);
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    private void TryDropCrate(BlockTile targetTile)
    {
        if (stackedCrates.Count == 0) return;

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int groundGridPos = Vector3Int.RoundToInt(targetTile.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - groundGridPos.x);
        int distanceZ = Mathf.Abs(playerPos.z - groundGridPos.z);
        int distanceY = Mathf.Abs(playerPos.y - groundGridPos.y);

        bool isWithinDropRange = (distanceX <= 1 && distanceZ <= 1) && (distanceX > 0 || distanceZ > 0) && (distanceY <= 2);

        if (!isWithinDropRange) return;

        int topIndex = stackedCrates.Count - 1;
        GameObject crateToDrop = stackedCrates[topIndex];
        stackedCrates.RemoveAt(topIndex);

        Vector3Int finalGridPos = CalculateDropGridPos(targetTile, out bool isWaterDrop);

        StartCoroutine(DropRoutine(crateToDrop, targetTile, finalGridPos, isWater: isWaterDrop));
    }

    private IEnumerator DropRoutine(GameObject crate, BlockTile targetTile, Vector3Int finalGridPos, bool isWater)
    {
        isBusy = true;

        Vector3 startPos = crate.transform.position;
        Vector3 targetPos = new Vector3(finalGridPos.x, finalGridPos.y, finalGridPos.z);

        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            crate.transform.position = currentPos;
            yield return null;
        }

        crate.transform.position = targetPos;
        crate.transform.rotation = Quaternion.identity;

        SetTagRecursively(crate, "DroppedCrate");

        int tileLayerIndex = LayerMask.NameToLayer(tileLayerName);
        if (tileLayerIndex != -1)
        {
            SetLayerRecursively(crate, tileLayerIndex);
        }

        SetCollidersEnabled(crate, true);

        BlockTile tile = crate.GetComponent<BlockTile>();
        if (tile == null) tile = crate.AddComponent<BlockTile>();

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
        }
        else
        {
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

    public int GetCrateCount() => stackedCrates.Count;

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
}