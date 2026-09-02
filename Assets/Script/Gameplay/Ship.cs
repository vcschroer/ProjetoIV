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
    [SerializeField] private Transform landingTile;              // Arraste o bloco de chão onde eles vão pousar
    [SerializeField] private Transform startTile;                // Arraste o bloco de chão onde a fase começa
    [SerializeField] private float yOffsetAboveTile = 1.0f;       // Altura acima do bloco para o pirata pisar

    [Header("Configurações de Animação")]
    [SerializeField] private float jumpDuration = 0.55f;
    [SerializeField] private float jumpArcHeight = 1.4f;
    [SerializeField] private float delayBetweenJumps = 0.15f;
    [SerializeField] private float walkSpeed = 4f;

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

        // 2. Inicia o desembarque direto para os blocos referenciados
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
        if (shipSpawnPoints.Length > 0 && playerInstance != null)
        {
            playerInstance.transform.position = shipSpawnPoints[0].position;
            playerInstance.transform.rotation = shipSpawnPoints[0].rotation;
            playerInstance.transform.SetParent(transform);
        }

        for (int i = 0; i < extraPirateInstances.Length; i++)
        {
            if (i + 1 < shipSpawnPoints.Length && extraPirateInstances[i] != null)
            {
                extraPirateInstances[i].transform.position = shipSpawnPoints[i + 1].position;
                extraPirateInstances[i].transform.rotation = shipSpawnPoints[i + 1].rotation;
                extraPirateInstances[i].transform.SetParent(transform);
            }
        }
    }

    private IEnumerator PirateLandingSequence()
    {
        yield return null; // Aguarda a inicialização dos objetos

        if (landingTile == null)
        {
            Debug.LogError("O Landing Tile não foi atribuído no Inspector do Ship!");
            yield break;
        }

        // Posição exata 1 unidade (ou offset ajustável) acima do bloco de aterrisagem
        Vector3 landingWorldPos = landingTile.position + Vector3.up * yOffsetAboveTile;

        List<GameObject> allPirates = new List<GameObject> { playerInstance };
        allPirates.AddRange(extraPirateInstances);

        // Desparenta todos do navio
        foreach (var pirate in allPirates)
        {
            if (pirate != null) pirate.transform.SetParent(null);
        }

        // 1. Pulo de cada pirata para cima do Landing Tile
        for (int i = 0; i < allPirates.Count; i++)
        {
            GameObject pirate = allPirates[i];
            if (pirate == null) continue;

            StartCoroutine(JumpPirateToGrid(pirate, landingWorldPos));
            yield return new WaitForSeconds(delayBetweenJumps);
        }

        yield return new WaitForSeconds(jumpDuration);

        // 2. Se houver um Start Tile configurado e diferente do Landing Tile, eles caminham até ele
        if (startTile != null && startTile != landingTile)
        {
            Vector3Int startGridCoord = Vector3Int.RoundToInt(landingTile.position);
            Vector3Int targetGridCoord = Vector3Int.RoundToInt(startTile.position);

            List<Vector3Int> gridPath = null;
            if (GridManager.Instance != null)
            {
                gridPath = GridManager.Instance.FindPath(startGridCoord, targetGridCoord);
            }

            if (gridPath != null && gridPath.Count > 0)
            {
                yield return StartCoroutine(WalkPiratesToStart(gridPath));
            }
            else
            {
                // Caminhada direta simples caso não use/ache pathfinding
                Vector3 startWorldPos = startTile.position + Vector3.up * yOffsetAboveTile;
                yield return StartCoroutine(WalkDirectToStart(startWorldPos));
            }
        }

        // 3. Monta a pilha inicial
        AssembleStartingStack();
    }

    private IEnumerator JumpPirateToGrid(GameObject pirate, Vector3 targetPos)
    {
        Vector3 startPos = pirate.transform.position;
        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * jumpArcHeight;

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

        if (juice != null)
        {
            juice.TriggerDropSquash();
        }
    }

    private IEnumerator WalkPiratesToStart(List<Vector3Int> path)
    {
        PirateJuice leaderJuice = playerInstance.GetComponentInChildren<PirateJuice>();

        foreach (Vector3Int step in path)
        {
            Vector3 targetWorldPos = new Vector3(step.x, step.y + yOffsetAboveTile, step.z);
            Vector3 startPos = playerInstance.transform.position;

            Vector3 moveDir = (targetWorldPos - startPos);
            moveDir.y = 0;

            if (leaderJuice != null) leaderJuice.TriggerStepSway(0f, 0.8f);

            float distance = Vector3.Distance(startPos, targetWorldPos);
            float stepDuration = Mathf.Max(0.1f, distance / walkSpeed);
            float elapsed = 0f;

            while (elapsed < stepDuration)
            {
                elapsed += Time.deltaTime;
                float progress = elapsed / stepDuration;

                Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.15f;

                playerInstance.transform.position = currentPos;

                if (moveDir.sqrMagnitude > 0.001f)
                {
                    playerInstance.transform.rotation = Quaternion.LookRotation(moveDir);
                }

                // Piratas extras acompanham
                for (int i = 0; i < extraPirateInstances.Length; i++)
                {
                    if (extraPirateInstances[i] != null)
                    {
                        extraPirateInstances[i].transform.position = currentPos;
                        extraPirateInstances[i].transform.rotation = playerInstance.transform.rotation;
                    }
                }

                yield return null;
            }

            playerInstance.transform.position = targetWorldPos;
        }

        if (leaderJuice != null) leaderJuice.StopSway();
    }

    private IEnumerator WalkDirectToStart(Vector3 targetWorldPos)
    {
        PirateJuice leaderJuice = playerInstance.GetComponentInChildren<PirateJuice>();
        Vector3 startPos = playerInstance.transform.position;

        Vector3 moveDir = (targetWorldPos - startPos);
        moveDir.y = 0;

        float distance = Vector3.Distance(startPos, targetWorldPos);
        float duration = distance / walkSpeed;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / duration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, progress);
            playerInstance.transform.position = currentPos;

            if (moveDir.sqrMagnitude > 0.001f)
            {
                playerInstance.transform.rotation = Quaternion.LookRotation(moveDir);
            }

            for (int i = 0; i < extraPirateInstances.Length; i++)
            {
                if (extraPirateInstances[i] != null)
                {
                    extraPirateInstances[i].transform.position = currentPos;
                    extraPirateInstances[i].transform.rotation = playerInstance.transform.rotation;
                }
            }

            yield return null;
        }

        playerInstance.transform.position = targetWorldPos;
        if (leaderJuice != null) leaderJuice.StopSway();
    }

    private void AssembleStartingStack()
    {
        if (playerInstance == null) return;

        PirateStackManager stackManager = playerInstance.GetComponent<PirateStackManager>();
        if (stackManager != null)
        {
            foreach (GameObject extraPirate in extraPirateInstances)
            {
                if (extraPirate != null)
                {
                    stackManager.AddToStackDirectly(extraPirate);
                }
            }
        }
    }
}