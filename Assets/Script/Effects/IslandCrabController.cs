using UnityEngine;

public class IslandCrabController : MonoBehaviour
{
    [Header("Referência do Player")]
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";

    [Header("Velocidades")]
    [SerializeField] private float wanderSpeed = 1.6f;
    [SerializeField] private float fleeSpeed = 3.4f;
    [SerializeField] private float turnSpeed = 12f;

    [Header("Patrulha Contínua pela Ilha")]
    [SerializeField] private float wanderRadius = 5.0f;
    [SerializeField] private float minStepDistance = 1.2f;

    [Header("Fuga do Player (Flee)")]
    [SerializeField] private float fleeTriggerDistance = 2.6f;
    [SerializeField] private float fleeSafeDistance = 4.2f;
    [SerializeField] private float fleeStepDistance = 2.0f;

    [Header("Animação Procedural (Andar de Lado)")]
    [SerializeField] private Transform visualModel;
    [SerializeField] private float sidewaysRotationOffset = 90f;
    [SerializeField] private float waddleFrequency = 20f;
    [SerializeField] private float waddleAngle = 11f;
    [SerializeField] private float stepHopHeight = 0.05f;

    private Vector3 targetPosition;
    private float surfaceY;
    private int groundGridY;

    private bool isFleeing;
    private float waddleTimer;
    private bool isMovingNow;

    private void Start()
    {
        if (visualModel == null)
            visualModel = transform;

        if (playerTransform == null)
        {
            PlayerController pc = FindFirstObjectByType<PlayerController>();
            if (pc != null)
            {
                playerTransform = pc.transform;
            }
            else
            {
                GameObject pObj = GameObject.FindGameObjectWithTag(playerTag);
                if (pObj != null) playerTransform = pObj.transform;
            }
        }

        surfaceY = transform.position.y;
        groundGridY = Mathf.RoundToInt(surfaceY) - 1;
        targetPosition = transform.position;

        PickNextWanderTarget();
    }

    private void Update()
    {
        UpdateFleeState();

        if (isFleeing)
        {
            HandleFleeMovement();
        }
        else
        {
            HandleWanderMovement();
        }

        UpdateCrabProceduralAnimation();
    }

    private void UpdateFleeState()
    {
        if (playerTransform == null)
        {
            isFleeing = false;
            return;
        }

        Vector3 flatCrab = new Vector3(transform.position.x, 0f, transform.position.z);
        Vector3 flatPlayer = new Vector3(playerTransform.position.x, 0f, playerTransform.position.z);
        float distanceToPlayer = Vector3.Distance(flatCrab, flatPlayer);

        if (!isFleeing && distanceToPlayer <= fleeTriggerDistance)
        {
            isFleeing = true;
            PickFleeTarget();
        }
        else if (isFleeing && distanceToPlayer >= fleeSafeDistance)
        {
            isFleeing = false;
            PickNextWanderTarget();
        }
    }

    private void HandleFleeMovement()
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            PickFleeTarget();
            toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude > 0.001f)
        {
            MoveTowards(targetPosition, fleeSpeed);
        }
        else
        {
            isMovingNow = false;
        }
    }

    private void HandleWanderMovement()
    {
        Vector3 toTarget = targetPosition - transform.position;
        toTarget.y = 0f;

        if (toTarget.sqrMagnitude <= 0.04f)
        {
            PickNextWanderTarget();
            toTarget = targetPosition - transform.position;
            toTarget.y = 0f;
        }

        if (toTarget.sqrMagnitude > 0.001f)
        {
            MoveTowards(targetPosition, wanderSpeed);
        }
        else
        {
            isMovingNow = false;
        }
    }

    private void MoveTowards(Vector3 destination, float speed)
    {
        Vector3 direction = destination - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.0001f)
        {
            isMovingNow = false;
            return;
        }

        Vector3 moveDir = direction.normalized;
        Vector3 nextPos = transform.position + moveDir * (speed * Time.deltaTime);
        nextPos.y = surfaceY;

        if (!IsWalkableSandPosition(nextPos))
        {
            isMovingNow = false;
            if (isFleeing) PickFleeTarget();
            else PickNextWanderTarget();
            return;
        }

        isMovingNow = true;
        transform.position = nextPos;

        Quaternion lookRot = Quaternion.LookRotation(moveDir) * Quaternion.Euler(0f, sidewaysRotationOffset, 0f);
        transform.rotation = Quaternion.Slerp(transform.rotation, lookRot, turnSpeed * Time.deltaTime);
    }

    private void PickNextWanderTarget()
    {
        for (int i = 0; i < 20; i++)
        {
            Vector2 randomDir = Random.insideUnitCircle.normalized;
            if (randomDir.sqrMagnitude < 0.01f) randomDir = Vector2.right;

            float dist = Random.Range(minStepDistance, wanderRadius);
            Vector3 candidate = new Vector3(
                transform.position.x + randomDir.x * dist,
                surfaceY,
                transform.position.z + randomDir.y * dist
            );

            if (IsPathClearOnIsland(transform.position, candidate))
            {
                targetPosition = candidate;
                return;
            }
        }

        float step = 1.0f;
        Vector3[] fallbackDirs =
        {
            new Vector3(step, 0f, 0f),
            new Vector3(-step, 0f, 0f),
            new Vector3(0f, 0f, step),
            new Vector3(0f, 0f, -step),
            new Vector3(step, 0f, step),
            new Vector3(-step, 0f, step),
            new Vector3(step, 0f, -step),
            new Vector3(-step, 0f, -step)
        };

        int startIdx = Random.Range(0, fallbackDirs.Length);
        for (int i = 0; i < fallbackDirs.Length; i++)
        {
            Vector3 candidate = transform.position + fallbackDirs[(startIdx + i) % fallbackDirs.Length];
            candidate.y = surfaceY;

            if (IsPathClearOnIsland(transform.position, candidate))
            {
                targetPosition = candidate;
                return;
            }
        }

        targetPosition = transform.position;
    }

    private void PickFleeTarget()
    {
        if (playerTransform == null)
        {
            isFleeing = false;
            return;
        }

        Vector3 awayDir = transform.position - playerTransform.position;
        awayDir.y = 0f;

        if (awayDir.sqrMagnitude < 0.001f)
            awayDir = Random.insideUnitSphere;

        awayDir.y = 0f;
        awayDir.Normalize();

        float[] testAngles = { 0f, 30f, -30f, 60f, -60f, 95f, -95f, 130f, -130f, 165f, -165f };

        foreach (float angle in testAngles)
        {
            Vector3 rotatedDir = Quaternion.Euler(0f, angle, 0f) * awayDir;
            Vector3 candidate = transform.position + rotatedDir * fleeStepDistance;
            candidate.y = surfaceY;

            if (IsPathClearOnIsland(transform.position, candidate))
            {
                targetPosition = candidate;
                return;
            }
        }

        PickNextWanderTarget();
    }

    private bool IsPathClearOnIsland(Vector3 from, Vector3 to)
    {
        float dist = Vector3.Distance(from, to);
        int samples = Mathf.Max(2, Mathf.CeilToInt(dist / 0.35f));

        for (int i = 1; i <= samples; i++)
        {
            float t = (float)i / samples;
            Vector3 point = Vector3.Lerp(from, to, t);
            if (!IsWalkableSandPosition(point))
            {
                return false;
            }
        }

        return true;
    }

    private bool IsWalkableSandPosition(Vector3 worldPos)
    {
        if (GridManager.Instance == null) return true;

        int cellX = Mathf.RoundToInt(worldPos.x);
        int cellZ = Mathf.RoundToInt(worldPos.z);

        Vector3Int groundPos = new Vector3Int(cellX, groundGridY, cellZ);
        Vector3Int bodyPos = new Vector3Int(cellX, groundGridY + 1, cellZ);

        BlockTile groundTile = GridManager.Instance.GetTileAt(groundPos);
        if (groundTile == null || groundTile.data == null || !groundTile.data.isWalkable || groundTile.isWaterTile)
        {
            return false;
        }

        if (GridManager.Instance.GetTileAt(bodyPos) != null)
        {
            return false;
        }

        return true;
    }

    private void UpdateCrabProceduralAnimation()
    {
        if (visualModel == null) return;

        if (isMovingNow)
        {
            float speedMultiplier = isFleeing ? 1.8f : 1.15f;
            waddleTimer += Time.deltaTime * waddleFrequency * speedMultiplier;

            float tiltZ = Mathf.Sin(waddleTimer) * waddleAngle;
            float hopY = Mathf.Abs(Mathf.Sin(waddleTimer)) * stepHopHeight;

            if (visualModel != transform)
            {
                visualModel.localRotation = Quaternion.Euler(0f, 0f, tiltZ);
                visualModel.localPosition = new Vector3(0f, hopY, 0f);
            }
            else
            {
                Vector3 pos = transform.position;
                pos.y = surfaceY + hopY;
                transform.position = pos;
            }
        }
        else
        {
            waddleTimer = 0f;
            if (visualModel != transform)
            {
                visualModel.localRotation = Quaternion.Slerp(visualModel.localRotation, Quaternion.identity, 12f * Time.deltaTime);
                visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, Vector3.zero, 12f * Time.deltaTime);
            }
            else
            {
                Vector3 pos = transform.position;
                pos.y = surfaceY;
                transform.position = pos;
            }
        }
    }
}