using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Ship : MonoBehaviour
{
    [Header("Configurações das Ondas / Flutuação")]
    [SerializeField] private bool autoStartFloating = true;
    [SerializeField] private float waveAmplitude = 0.25f;
    [SerializeField] private float waveFrequency = 1.5f;
    [SerializeField] private float rollAmplitude = 3.5f;
    [SerializeField] private float pitchAmplitude = 2.0f;
    [SerializeField] private float rotationFrequency = 1.1f;
    [SerializeField] private bool randomPhase = true;

    [Header("Piratas")]
    [SerializeField] private Transform[] shipSpawnPoints;       // 3 pontos no convés do navio
    [SerializeField] private GameObject playerInstance;          // Pirata Líder
    [SerializeField] private GameObject[] extraPirateInstances; // Os outros 2 piratas

    [Header("Referências de Chão (Tiles)")]
    [SerializeField] private Transform landingTile;              // Bloco de chão onde eles pousam ao pular do navio
    [SerializeField] private Transform startTile;                // Bloco de chão onde a fase realmente começa
    [SerializeField] private float yOffsetAboveTile = 1.0f;       // Altura acima do bloco para o pirata pisar

    [Header("Configurações de Animação")]
    [SerializeField] private float jumpFromShipDuration = 0.55f;
    [SerializeField] private float jumpFromShipArcHeight = 1.4f;
    [SerializeField] private float delayBetweenJumps = 0.15f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float stepJumpHeight = 0.4f;        // Altura do pulo tile a tile

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private float phaseOffset;
    private bool isFloating = false;

    private void Start()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;

        if (randomPhase)
        {
            phaseOffset = Random.Range(0f, 2f * Mathf.PI);
        }

        if (autoStartFloating)
        {
            StartFloating();
        }

        // 1. Posiciona os piratas nos pontos do navio
        PositionPiratesOnShip();

        // 2. Inicia a sequência de montagem e desembarque
        StartCoroutine(PirateLandingSequence());
    }

    private void Update()
    {
        if (!isFloating) return;
        ApplyFloatingEffect();
    }

    private void ApplyFloatingEffect()
    {
        float time = Time.time + phaseOffset;

        float waveY = Mathf.Sin(time * waveFrequency) * waveAmplitude;
        transform.localPosition = basePosition + new Vector3(0f, waveY, 0f);

        float pitch = Mathf.Sin(time * rotationFrequency) * pitchAmplitude;
        float roll = Mathf.Cos(time * rotationFrequency * 0.85f) * rollAmplitude;

        transform.localRotation = baseRotation * Quaternion.Euler(pitch, 0f, roll);
    }

    public void StartFloating() => isFloating = true;
    public void StopFloating() => isFloating = false;

    private void PositionPiratesOnShip()
    {
        // Posiciona o Líder no Ponto 0
        if (shipSpawnPoints.Length > 0 && playerInstance != null)
        {
            playerInstance.transform.position = shipSpawnPoints[0].position;
            playerInstance.transform.rotation = shipSpawnPoints[0].rotation;
            playerInstance.transform.SetParent(transform);
        }

        // Posiciona os Piratas Extras nos Pontos 1, 2, etc.
        for (int i = 0; i < extraPirateInstances.Length; i++)
        {
            if (i + 1 < shipSpawnPoints.Length && extraPirateInstances[i] != null)
            {
                Transform spawnPoint = shipSpawnPoints[i + 1];
                GameObject extraPirate = extraPirateInstances[i];

                extraPirate.transform.SetParent(transform); // Garante parentesco com o navio
                extraPirate.transform.position = spawnPoint.position;
                extraPirate.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    private IEnumerator PirateLandingSequence()
    {
        yield return null; // Aguarda a inicialização completa do GridManager

        if (landingTile == null)
        {
            Debug.LogError("O Landing Tile não foi atribuído no Inspector do Ship!");
            yield break;
        }

        Vector3 landingWorldPos = landingTile.position + Vector3.up * yOffsetAboveTile;
        PirateStackManager stackManager = playerInstance != null ? playerInstance.GetComponent<PirateStackManager>() : null;

        // 1. MONTAGEM DA PILHA AINDA NO NAVIO
        if (stackManager != null && extraPirateInstances != null)
        {
            float stepH = stackManager.StepHeight;

            for (int i = 0; i < extraPirateInstances.Length; i++)
            {
                GameObject extraPirate = extraPirateInstances[i];
                if (extraPirate == null) continue;

                // Cada pirata pula para o topo da pilha no barco
                yield return StartCoroutine(JumpPirateToStackOnShip(extraPirate, i + 1, stepH));
                yield return new WaitForSeconds(delayBetweenJumps);
            }
        }

        yield return new WaitForSeconds(0.2f); // Pequena pausa dramática com a torre pronta no barco

        // Desparenta todos do navio para moverem livremente no mundo
        if (playerInstance != null) playerInstance.transform.SetParent(null);
        foreach (var pirate in extraPirateInstances)
        {
            if (pirate != null) pirate.transform.SetParent(null);
        }

        // 2. PULO DA TORRE INTEIRA DO NAVIO PARA O LANDING TILE
        if (playerInstance != null)
        {
            yield return StartCoroutine(JumpPirateToGrid(playerInstance, landingWorldPos));

            // Impacto de pouso de toda a torre no chão
            if (stackManager != null)
            {
                stackManager.TriggerStackImpact(topToBottom: true);
            }
        }

        // 3. CAMINHADA TILE A TILE ATÉ O START TILE
        if (startTile != null && startTile != landingTile)
        {
            Vector3Int startStandGridPos = GetStandGridPosition(landingTile);
            Vector3Int targetStandGridPos = GetStandGridPosition(startTile);

            List<Vector3Int> gridPath = null;
            if (GridManager.Instance != null)
            {
                gridPath = GridManager.Instance.FindPath(startStandGridPos, targetStandGridPos);
            }

            if (gridPath != null && gridPath.Count > 0)
            {
                yield return StartCoroutine(WalkPiratesToStartAlongPath(gridPath));
            }
            else
            {
                Debug.LogWarning("GridManager não encontrou um caminho entre o LandingTile e o StartTile.");
            }
        }
    }

    private IEnumerator JumpPirateToStackOnShip(GameObject pirate, int stackIndex, float stepHeight)
    {
        Vector3 startPos = pirate.transform.position;
        float elapsed = 0f;

        while (elapsed < jumpFromShipDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpFromShipDuration;

            // Target acompanha a posição do líder no barco (mesmo flutuando)
            Vector3 targetPos = playerInstance.transform.position + Vector3.up * (stackIndex * stepHeight);

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpFromShipArcHeight;

            pirate.transform.position = currentPos;

            Vector3 lookDir = (targetPos - startPos);
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                pirate.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            yield return null;
        }

        // Encaixa oficialmente na pilha ao aterrissar no barco
        PirateStackManager stackManager = playerInstance.GetComponent<PirateStackManager>();
        if (stackManager != null)
        {
            stackManager.AddToStackDirectly(pirate);
        }
    }

    private IEnumerator JumpPirateToGrid(GameObject pirate, Vector3 targetPos)
    {
        Vector3 startPos = pirate.transform.position;
        float elapsed = 0f;

        while (elapsed < jumpFromShipDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpFromShipDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpFromShipArcHeight;

            pirate.transform.position = currentPos;

            Vector3 lookDir = (targetPos - startPos);
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                pirate.transform.rotation = Quaternion.LookRotation(lookDir);
            }

            yield return null;
        }

        pirate.transform.position = targetPos;
    }

    private IEnumerator WalkPiratesToStartAlongPath(List<Vector3Int> path)
    {
        PirateStackManager stackManager = playerInstance.GetComponent<PirateStackManager>();

        foreach (Vector3Int step in path)
        {
            Vector3 startPos = playerInstance.transform.position;
            Vector3 targetWorldPos = new Vector3(step.x, step.y, step.z);

            Vector3 moveDir = (targetWorldPos - startPos);
            moveDir.y = 0;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                playerInstance.transform.rotation = Quaternion.LookRotation(moveDir);
            }

            float distance = Vector3.Distance(startPos, targetWorldPos);
            float stepDuration = Mathf.Max(0.1f, distance / walkSpeed);
            float elapsed = 0f;

            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / stepDuration;

                // Arco de pulo tile a tile
                Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * stepJumpHeight;

                playerInstance.transform.position = currentPos;

                yield return null;
            }

            playerInstance.transform.position = targetWorldPos;

            // Dispara o Juice completo de toda a pilha a cada passo
            if (stackManager != null)
            {
                stackManager.OnPlayerStep();
            }
        }
    }

    private Vector3Int GetStandGridPosition(Transform tileTransform)
    {
        BlockTile blockTile = tileTransform.GetComponent<BlockTile>();
        if (blockTile != null)
        {
            return blockTile.gridPosition + Vector3Int.up;
        }

        return Vector3Int.RoundToInt(tileTransform.position) + Vector3Int.up;
    }
}