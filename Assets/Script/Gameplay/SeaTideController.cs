
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class SeaTideController : MonoBehaviour
{
    public static SeaTideController Instance { get; private set; }

    [Header("Ativação da Mecânica")]
    [SerializeField] private bool isTideActive = true;

    [Header("Configurações de Turnos / Cliques")]
    [SerializeField] private int clicksToChangeTide = 3;
    private int remainingClicks;

    [Header("UI do Contador de Movimentos")]
    [SerializeField] private TMP_Text tideClicksText;
    [SerializeField] private string risingPrefix = "";
    [SerializeField] private string loweringPrefix = "";

    [Header("Movimento da Água")]
    [SerializeField] private float riseAmount = 1f;
    [SerializeField] private float transitionDuration = 1f;

    [Header("Alinhamento de Flutuação")]
    [SerializeField] private float floatSurfaceOffset = 0f;

    [Header("Segurança do Player")]
    [SerializeField] private PlayerController player;
    [SerializeField] private Collider playerBodyCollider;
    [SerializeField] private float drownHeightThreshold = 0.4f;

    [Header("Detecção de Apoio")]
    [SerializeField] private float supportProbeDistance = 0.35f;
    [SerializeField] private float supportRayStartOffset = 0.05f;
    [SerializeField] private float supportMaxPenetration = 0.15f;

    private Ship sceneShip;

    private bool isTransitioning;
    private bool isHighTide;

    private float initialWaterY;

    private readonly Dictionary<GameObject, Vector3>
        floatingObjectsOriginalPositions =
        new Dictionary<GameObject, Vector3>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
        {
            Destroy(gameObject);
            return;
        }

        remainingClicks = clicksToChangeTide;
        initialWaterY = transform.position.y;

        UpdateTideUI();
    }

    private void Start()
    {
        sceneShip = FindFirstObjectByType<Ship>();

        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        UpdateTideUI();
    }



    public void RegisterPlayerMoveClick()
    {
        if (!isTideActive || isTransitioning)
            return;

        remainingClicks = Mathf.Max(0, remainingClicks - 1);

        UpdateTideUI();

        if (remainingClicks <= 0)
        {
            if (!isHighTide)
                StartCoroutine(RiseTideRoutine());
            else
                StartCoroutine(LowerTideRoutine());
        }
    }

    private void UpdateTideUI()
    {
        if (tideClicksText == null)
            return;

        string prefix = isHighTide ? loweringPrefix : risingPrefix;

        tideClicksText.text = $"{prefix}{remainingClicks}";
    }


    private IEnumerator RiseTideRoutine()
    {
        isTransitioning = true;

        yield return null;

        if (player != null)
        {
            player.SetInputLock(true);

            while (player.IsMoving)
                yield return null;
        }

        if (sceneShip == null)
            sceneShip = FindFirstObjectByType<Ship>();

        Vector3 startWaterPosition = transform.position;

        Vector3 targetWaterPosition = new Vector3(
            startWaterPosition.x,
            initialWaterY + riseAmount,
            startWaterPosition.z
        );

        float waterDeltaY =
            targetWaterPosition.y - startWaterPosition.y;


        float currentWaterSurface = GetWaterSurfaceY();

        float targetFloatY =
            currentWaterSurface + waterDeltaY + floatSurfaceOffset;

        List<GameObject> objectsToFloat =
            BuildFloatingTargets(
                targetFloatY,
                out Dictionary<GameObject, Vector3> startPositions,
                out Dictionary<GameObject, Vector3> targetPositions
            );

        floatingObjectsOriginalPositions.Clear();

        foreach (GameObject obj in objectsToFloat)
        {
            if (obj != null)
                floatingObjectsOriginalPositions[obj] = startPositions[obj];
        }

        HashSet<GameObject> movingRoots =
            new HashSet<GameObject>(objectsToFloat);

        GameObject playerSupport = null;
        Vector3 lastSupportPosition = Vector3.zero;

        if (player != null)
        {
            playerSupport = FindPlayerDroppedSupport(movingRoots);

            if (playerSupport != null)
                lastSupportPosition = playerSupport.transform.position;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / transitionDuration)
            );

            transform.position = Vector3.Lerp(
                startWaterPosition,
                targetWaterPosition,
                progress
            );

            if (sceneShip != null)
            {
                sceneShip.SetTideOffset(
                    Mathf.Lerp(0f, riseAmount, progress)
                );
            }

            foreach (GameObject obj in objectsToFloat)
            {
                if (obj == null)
                    continue;

                obj.transform.position = Vector3.Lerp(
                    startPositions[obj],
                    targetPositions[obj],
                    progress
                );
            }


            MovePlayerWithPlatform(
                playerSupport,
                ref lastSupportPosition
            );

            yield return null;
        }

        transform.position = targetWaterPosition;

        if (sceneShip != null)
            sceneShip.SetTideOffset(riseAmount);

        foreach (GameObject obj in objectsToFloat)
        {
            if (obj == null)
                continue;

            obj.transform.position = targetPositions[obj];

            UpdateObjectTileData(
                obj,
                targetPositions[obj],
                true
            );
        }

        if (GridManager.Instance != null)
            GridManager.Instance.ScanGrid();

        isHighTide = true;

        CheckPlayerSafety();
    }



    private IEnumerator LowerTideRoutine()
    {
        isTransitioning = true;

        yield return null;

        if (player != null)
        {
            player.SetInputLock(true);

            while (player.IsMoving)
                yield return null;
        }

        if (sceneShip == null)
            sceneShip = FindFirstObjectByType<Ship>();

        Vector3 startWaterPosition = transform.position;

        Vector3 targetWaterPosition = new Vector3(
            startWaterPosition.x,
            initialWaterY,
            startWaterPosition.z
        );

        List<GameObject> activeFloatingObjects =
            new List<GameObject>();

        Dictionary<GameObject, Vector3> startPositions =
            new Dictionary<GameObject, Vector3>();

        foreach (var entry in floatingObjectsOriginalPositions)
        {
            GameObject obj = entry.Key;

            if (obj == null)
                continue;

            activeFloatingObjects.Add(obj);
            startPositions[obj] = obj.transform.position;
        }

        HashSet<GameObject> movingRoots =
            new HashSet<GameObject>(activeFloatingObjects);

        GameObject playerSupport = null;
        Vector3 lastSupportPosition = Vector3.zero;

        if (player != null)
        {
            playerSupport = FindPlayerDroppedSupport(movingRoots);

            if (playerSupport != null)
                lastSupportPosition = playerSupport.transform.position;
        }

        float elapsed = 0f;

        while (elapsed < transitionDuration)
        {
            elapsed += Time.deltaTime;

            float progress = Mathf.SmoothStep(
                0f,
                1f,
                Mathf.Clamp01(elapsed / transitionDuration)
            );

            transform.position = Vector3.Lerp(
                startWaterPosition,
                targetWaterPosition,
                progress
            );

            if (sceneShip != null)
            {
                sceneShip.SetTideOffset(
                    Mathf.Lerp(riseAmount, 0f, progress)
                );
            }

            foreach (GameObject obj in activeFloatingObjects)
            {
                if (obj == null)
                    continue;

                Vector3 originalPosition =
                    floatingObjectsOriginalPositions[obj];

                obj.transform.position = Vector3.Lerp(
                    startPositions[obj],
                    originalPosition,
                    progress
                );
            }

            MovePlayerWithPlatform(
                playerSupport,
                ref lastSupportPosition
            );

            yield return null;
        }

        transform.position = targetWaterPosition;

        if (sceneShip != null)
            sceneShip.SetTideOffset(0f);

        foreach (GameObject obj in activeFloatingObjects)
        {
            if (obj == null)
                continue;

            Vector3 originalPosition =
                floatingObjectsOriginalPositions[obj];

            obj.transform.position = originalPosition;

            UpdateObjectTileData(
                obj,
                originalPosition,
                false
            );
        }

        floatingObjectsOriginalPositions.Clear();

        if (GridManager.Instance != null)
            GridManager.Instance.ScanGrid();

        isHighTide = false;
        remainingClicks = clicksToChangeTide;

        UpdateTideUI();

        CheckPlayerSafety();
    }

    private void MovePlayerWithPlatform(
        GameObject support,
        ref Vector3 lastSupportPosition)
    {
        if (player == null || support == null)
            return;

        if (support == null)
            return;

        Vector3 currentSupportPosition = support.transform.position;

        Vector3 platformDelta =
            currentSupportPosition - lastSupportPosition;

        player.transform.position += platformDelta;

        lastSupportPosition = currentSupportPosition;
    }


    private List<GameObject> BuildFloatingTargets(
        float targetFloatY,
        out Dictionary<GameObject, Vector3> startPositions,
        out Dictionary<GameObject, Vector3> targetPositions)
    {
        List<GameObject> result = new List<GameObject>();

        startPositions = new Dictionary<GameObject, Vector3>();
        targetPositions = new Dictionary<GameObject, Vector3>();

        List<GameObject> allObjects = GetAllDroppedRoots();

        Dictionary<Vector2Int, List<GameObject>> columns =
            new Dictionary<Vector2Int, List<GameObject>>();

        foreach (GameObject obj in allObjects)
        {
            if (obj == null)
                continue;

            Vector2Int key = new Vector2Int(
                Mathf.RoundToInt(obj.transform.position.x),
                Mathf.RoundToInt(obj.transform.position.z)
            );

            if (!columns.ContainsKey(key))
                columns[key] = new List<GameObject>();

            columns[key].Add(obj);
        }

        foreach (List<GameObject> column in columns.Values)
        {
            float lowestY = float.MaxValue;

            foreach (GameObject obj in column)
            {
                if (obj == null)
                    continue;

                lowestY = Mathf.Min(
                    lowestY,
                    obj.transform.position.y
                );
            }

            if (lowestY == float.MaxValue)
                continue;

            if (lowestY >= targetFloatY - 0.05f)
                continue;

            float columnOffset = targetFloatY - lowestY;

            foreach (GameObject obj in column)
            {
                if (obj == null)
                    continue;

                Vector3 originalPosition = obj.transform.position;

                Vector3 targetPosition =
                    originalPosition + Vector3.up * columnOffset;

                if (result.Contains(obj))
                    continue;

                result.Add(obj);

                startPositions[obj] = originalPosition;
                targetPositions[obj] = targetPosition;
            }
        }

        return result;
    }

    private List<GameObject> GetAllDroppedRoots()
    {
        HashSet<GameObject> uniqueRoots = new HashSet<GameObject>();

        GameObject[] crates =
            GameObject.FindGameObjectsWithTag("DroppedCrate");

        GameObject[] pirates =
            GameObject.FindGameObjectsWithTag("DroppedPirate");

        foreach (GameObject item in crates)
        {
            GameObject root = GetDroppedRoot(item.transform);

            if (root != null)
                uniqueRoots.Add(root);
        }

        foreach (GameObject item in pirates)
        {
            GameObject root = GetDroppedRoot(item.transform);

            if (root != null)
                uniqueRoots.Add(root);
        }

        return new List<GameObject>(uniqueRoots);
    }

    private GameObject GetDroppedRoot(Transform current)
    {
        GameObject root = null;

        while (current != null)
        {
            if (current.CompareTag("DroppedCrate") ||
                current.CompareTag("DroppedPirate"))
            {
                root = current.gameObject;
            }

            current = current.parent;
        }

        return root;
    }


    private Collider GetPlayerCollider()
    {
        if (playerBodyCollider != null)
            return playerBodyCollider;

        if (player == null)
            return null;

        playerBodyCollider = player.GetComponent<Collider>();

        if (playerBodyCollider == null)
            playerBodyCollider =
                player.GetComponentInChildren<Collider>();

        return playerBodyCollider;
    }

    private bool TryGetSupportBelowPlayer(
        out RaycastHit bestHit,
        HashSet<GameObject> allowedDroppedRoots = null,
        bool requireDroppedObject = false)
    {
        bestHit = default;

        if (player == null)
            return false;

        Collider body = GetPlayerCollider();

        float feetY = body != null
            ? body.bounds.min.y
            : player.transform.position.y;

        Vector3 rayOrigin = body != null
            ? new Vector3(
                body.bounds.center.x,
                feetY + supportRayStartOffset,
                body.bounds.center.z
            )
            : player.transform.position +
              Vector3.up * supportRayStartOffset;

        float rayDistance =
            supportProbeDistance + supportRayStartOffset;

        RaycastHit[] hits = Physics.RaycastAll(
            rayOrigin,
            Vector3.down,
            rayDistance,
            Physics.DefaultRaycastLayers,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = float.MaxValue;
        bool foundSupport = false;

        Collider waterCollider = GetComponent<Collider>();

        foreach (RaycastHit hit in hits)
        {
            Collider hitCollider = hit.collider;

            if (hitCollider == null)
                continue;

            if (hitCollider.transform == player.transform ||
                hitCollider.transform.IsChildOf(player.transform))
            {
                continue;
            }

            if (waterCollider != null &&
                hitCollider == waterCollider)
            {
                continue;
            }

            if (hitCollider.GetComponentInParent<SeaTideController>() != null)
                continue;

            float verticalGap = feetY - hit.point.y;

            if (verticalGap < -supportMaxPenetration ||
                verticalGap > supportProbeDistance)
            {
                continue;
            }

            GameObject droppedRoot =
                GetDroppedRoot(hitCollider.transform);

            if (droppedRoot != null)
            {
                if (allowedDroppedRoots != null &&
                    !allowedDroppedRoots.Contains(droppedRoot))
                {
                    continue;
                }
            }
            else
            {

                if (requireDroppedObject || allowedDroppedRoots != null)
                    continue;

                BlockTile tile =
                    hitCollider.GetComponentInParent<BlockTile>();

                if (tile == null || tile.isWaterTile)
                    continue;
            }

            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                bestHit = hit;
                foundSupport = true;
            }
        }

        return foundSupport;
    }

    private GameObject FindPlayerDroppedSupport(
        HashSet<GameObject> allowedPlatforms)
    {
        if (!TryGetSupportBelowPlayer(
                out RaycastHit hit,
                allowedPlatforms,
                requireDroppedObject: true))
        {
            return null;
        }

        return GetDroppedRoot(hit.collider.transform);
    }



    private void UpdateObjectTileData(
        GameObject obj,
        Vector3 newWorldPosition,
        bool isFloating)
    {
        if (obj == null)
            return;

        BlockTile tile = obj.GetComponent<BlockTile>();

        if (tile != null && tile.data != null)
        {
            Vector3Int newGridPosition =
                Vector3Int.RoundToInt(newWorldPosition);

            tile.Setup(newGridPosition, tile.data);
        }

        PirateJuice juice =
            obj.GetComponentInChildren<PirateJuice>();

        if (juice != null)
        {
            if (isFloating)
                juice.StartFloating();
            else
                juice.StopFloating();
        }
    }

    private float GetWaterSurfaceY()
    {
        Collider waterCollider = GetComponent<Collider>();

        if (waterCollider != null)
            return waterCollider.bounds.max.y;

        return transform.position.y;
    }



    private void CheckPlayerSafety()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();

        if (player == null)
        {
            isTransitioning = false;
            return;
        }


        bool hasSupport = TryGetSupportBelowPlayer(
            out RaycastHit supportHit
        );

        if (!hasSupport)
        {
            isTransitioning = false;

            player.DrownInWater();
            return;
        }

        Collider body = GetPlayerCollider();

        float playerFeetY = body != null
            ? body.bounds.min.y
            : player.transform.position.y;

        float waterSurfaceY = GetWaterSurfaceY();

        bool isSubmerged =
            (waterSurfaceY - playerFeetY) >= drownHeightThreshold;

        if (isSubmerged)
        {
            isTransitioning = false;
            player.DrownInWater();
            return;
        }

        remainingClicks = clicksToChangeTide;
        isTransitioning = false;

        player.SetInputLock(false);

        UpdateTideUI();
    }

    public int GetRemainingClicks()
    {
        return remainingClicks;
    }

    public bool IsHighTide => isHighTide;
}
