using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeaTideController : MonoBehaviour
{
    public static SeaTideController Instance { get; private set; }

    [Header("Ativação da Mecânica")]
    [SerializeField] private bool isTideActive = true;

    [Header("Configurações de Turnos / Cliques")]
    [SerializeField] private int clicksToChangeTide = 3;
    private int remainingClicks;

    [Header("Movimento da Água")]
    [SerializeField] private float riseAmount = 1.0f;
    [SerializeField] private float transitionDuration = 1.0f;

    [Header("Alinhamento de Flutuação")]
    [SerializeField] private float floatSurfaceOffset = 0.0f;

    [Header("Segurança do Player")]
    [SerializeField] private PlayerController player;
    [SerializeField] private float drownHeightThreshold = 0.4f;

    private Ship sceneShip;
    private bool isTransitioning = false;
    private bool isHighTide = false;
    private float initialWaterY;

    private Dictionary<GameObject, Vector3> floatingObjectsOriginalPositions = new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        remainingClicks = clicksToChangeTide;
        initialWaterY = transform.position.y;
    }

    private void Start()
    {
        sceneShip = FindFirstObjectByType<Ship>();
    }

    public void RegisterPlayerMoveClick()
    {
        if (!isTideActive || isTransitioning) return;

        remainingClicks--;

        if (remainingClicks <= 0)
        {
            if (!isHighTide)
            {
                StartCoroutine(RiseTideRoutine());
            }
            else
            {
                StartCoroutine(LowerTideRoutine());
            }
        }
    }

    private IEnumerator RiseTideRoutine()
    {
        isTransitioning = true;

        if (player != null)
        {
            player.SetInputLock(true);
            while (player.IsMoving)
            {
                yield return null;
            }
        }

        if (sceneShip == null)
        {
            sceneShip = FindFirstObjectByType<Ship>();
        }

        Vector3 startWaterPos = transform.position;
        Vector3 targetWaterPos = new Vector3(startWaterPos.x, initialWaterY + riseAmount, startWaterPos.z);

        float targetFloatY = targetWaterPos.y + floatSurfaceOffset;

        List<GameObject> objectsToFloat = FindDroppedObjectsToFloat(targetFloatY);
        Dictionary<GameObject, Vector3> startPositions = new Dictionary<GameObject, Vector3>();
        Dictionary<GameObject, Vector3> targetPositions = new Dictionary<GameObject, Vector3>();

        floatingObjectsOriginalPositions.Clear();

        foreach (GameObject obj in objectsToFloat)
        {
            Vector3 objPos = obj.transform.position;
            floatingObjectsOriginalPositions[obj] = objPos;
            startPositions[obj] = objPos;
            targetPositions[obj] = new Vector3(objPos.x, targetFloatY, objPos.z);
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            transform.position = Vector3.Lerp(startWaterPos, targetWaterPos, smoothProgress);

            if (sceneShip != null)
            {
                sceneShip.SetTideOffset(Mathf.Lerp(0f, riseAmount, smoothProgress));
            }

            foreach (GameObject obj in objectsToFloat)
            {
                if (obj != null && (obj.CompareTag("DroppedCrate") || obj.CompareTag("DroppedPirate")))
                {
                    obj.transform.position = Vector3.Lerp(startPositions[obj], targetPositions[obj], smoothProgress);
                }
            }

            yield return null;
        }

        transform.position = targetWaterPos;

        if (sceneShip != null)
        {
            sceneShip.SetTideOffset(riseAmount);
        }

        foreach (GameObject obj in objectsToFloat)
        {
            if (obj != null && (obj.CompareTag("DroppedCrate") || obj.CompareTag("DroppedPirate")))
            {
                obj.transform.position = targetPositions[obj];
                UpdateObjectTileData(obj, targetPositions[obj], isFloating: true);
            }
        }

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isHighTide = true;
        CheckPlayerSafety();
    }

    private IEnumerator LowerTideRoutine()
    {
        isTransitioning = true;

        if (player != null)
        {
            player.SetInputLock(true);
            while (player.IsMoving)
            {
                yield return null;
            }
        }

        if (sceneShip == null)
        {
            sceneShip = FindFirstObjectByType<Ship>();
        }

        Vector3 startWaterPos = transform.position;
        Vector3 targetWaterPos = new Vector3(startWaterPos.x, initialWaterY, startWaterPos.z);

        List<GameObject> activeFloatingObjects = new List<GameObject>();
        Dictionary<GameObject, Vector3> startPositions = new Dictionary<GameObject, Vector3>();

        foreach (var kvp in floatingObjectsOriginalPositions)
        {
            GameObject obj = kvp.Key;
            if (obj != null && (obj.CompareTag("DroppedCrate") || obj.CompareTag("DroppedPirate")))
            {
                activeFloatingObjects.Add(obj);
                startPositions[obj] = obj.transform.position;
            }
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, elapsed / transitionDuration);

            transform.position = Vector3.Lerp(startWaterPos, targetWaterPos, smoothProgress);

            if (sceneShip != null)
            {
                sceneShip.SetTideOffset(Mathf.Lerp(riseAmount, 0f, smoothProgress));
            }

            foreach (GameObject obj in activeFloatingObjects)
            {
                if (obj != null && (obj.CompareTag("DroppedCrate") || obj.CompareTag("DroppedPirate")))
                {
                    Vector3 originalPos = floatingObjectsOriginalPositions[obj];
                    obj.transform.position = Vector3.Lerp(startPositions[obj], originalPos, smoothProgress);
                }
            }

            yield return null;
        }

        transform.position = targetWaterPos;

        if (sceneShip != null)
        {
            sceneShip.SetTideOffset(0f);
        }

        foreach (GameObject obj in activeFloatingObjects)
        {
            if (obj != null && (obj.CompareTag("DroppedCrate") || obj.CompareTag("DroppedPirate")))
            {
                Vector3 originalPos = floatingObjectsOriginalPositions[obj];
                obj.transform.position = originalPos;
                UpdateObjectTileData(obj, originalPos, isFloating: false);
            }
        }

        floatingObjectsOriginalPositions.Clear();

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isHighTide = false;
        remainingClicks = clicksToChangeTide;
        isTransitioning = false;

        if (player != null)
        {
            player.SetInputLock(false);
        }
    }

    private List<GameObject> FindDroppedObjectsToFloat(float targetFloatY)
    {
        List<GameObject> result = new List<GameObject>();
        HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();

        GameObject[] crates = GameObject.FindGameObjectsWithTag("DroppedCrate");
        GameObject[] pirates = GameObject.FindGameObjectsWithTag("DroppedPirate");

        List<GameObject> allTagged = new List<GameObject>();
        allTagged.AddRange(crates);
        allTagged.AddRange(pirates);

        foreach (GameObject item in allTagged)
        {
            Transform root = item.transform;
            while (root.parent != null && (root.parent.CompareTag("DroppedCrate") || root.parent.CompareTag("DroppedPirate")))
            {
                root = root.parent;
            }

            GameObject rootObj = root.gameObject;
            if (!uniqueRoots.Contains(rootObj))
            {
                uniqueRoots.Add(rootObj);

                if (rootObj.transform.position.y < targetFloatY - 0.05f)
                {
                    result.Add(rootObj);
                }
            }
        }

        return result;
    }

    private void UpdateObjectTileData(GameObject obj, Vector3 newWorldPos, bool isFloating)
    {
        BlockTile tile = obj.GetComponent<BlockTile>();
        if (tile != null && tile.data != null)
        {
            Vector3Int newGridPos = Vector3Int.RoundToInt(newWorldPos);
            tile.Setup(newGridPos, tile.data);
        }

        PirateJuice juice = obj.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            if (isFloating)
            {
                juice.StartFloating();
            }
            else
            {
                juice.StopFloating();
            }
        }
    }

    private void CheckPlayerSafety()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
        }

        Collider waterCol = GetComponent<Collider>();
        Collider playerCol = player.GetComponentInChildren<Collider>();

        bool isSubmerged = false;

        if (waterCol != null && playerCol != null)
        {
            isSubmerged = waterCol.bounds.Intersects(playerCol.bounds);
        }

        float waterSurfaceY = waterCol != null ? waterCol.bounds.max.y : transform.position.y;
        float playerFeetY = player.transform.position.y;

        if (isSubmerged || (waterSurfaceY - playerFeetY) >= drownHeightThreshold)
        {
            player.DrownInWater();
        }
        else
        {
            remainingClicks = clicksToChangeTide;
            isTransitioning = false;
            player.SetInputLock(false);
        }
    }

    public int GetRemainingClicks() => remainingClicks;
    public bool IsHighTide => isHighTide;
}