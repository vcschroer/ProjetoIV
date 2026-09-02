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
    [SerializeField] private Transform[] shipSpawnPoints;       // Pontos distintos no convés do navio
    [SerializeField] private GameObject playerInstance;          // Pirata Líder
    [SerializeField] private GameObject[] extraPirateInstances; // Os outros piratas

    [Header("Referências de Chão (Tiles)")]
    [SerializeField] private Transform landingTile;              // Bloco de chão onde eles pousam ao pular do navio
    [SerializeField] private Transform startTile;                // Bloco de chão onde a fase realmente começa
    [SerializeField] private float yOffsetAboveTile = 1.0f;       // Altura acima do bloco para o pirata pisar

    [Header("Configurações de Animação / Cutscene")]
    [SerializeField] private float startDelay = 0.5f;            // Delay antes de iniciar desembarque
    [SerializeField] private float jumpFromShipDuration = 0.55f;
    [SerializeField] private float jumpFromShipArcHeight = 1.4f;
    [SerializeField] private float delayBetweenJumps = 0.15f;
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float stepJumpHeight = 0.4f;        // Altura do pulo tile a tile

    [Header("Câmera / Zoom Cutscene")]
    [SerializeField] private float cutsceneZoomDistance = 14.0f; // Distância durante a animação
    [SerializeField] private float normalZoomDistance = 60.0f;   // Distância normal após a animação

    [Header("Cutscene do Mapa do Tesouro")]
    [SerializeField] private float mapDisplayDelay = 0.2f;        // Pequena pausa antes de exibir o mapa
    [SerializeField] private float autoMinimizeDelay = 2.5f;     // Tempo em segundos que o mapa fica aberto

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

        PositionPiratesOnShip();
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
        PirateStackManager stackManager = playerInstance != null ? playerInstance.GetComponent<PirateStackManager>() : null;
        if (stackManager != null)
        {
            stackManager.enabled = false;
        }

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
                Transform spawnPoint = shipSpawnPoints[i + 1];
                GameObject extraPirate = extraPirateInstances[i];

                extraPirate.transform.SetParent(transform);
                extraPirate.transform.position = spawnPoint.position;
                extraPirate.transform.rotation = spawnPoint.rotation;
            }
        }
    }

    private IEnumerator PirateLandingSequence()
    {
        // Trava controles do jogador
        PlayerController playerController = playerInstance != null ? playerInstance.GetComponent<PlayerController>() : null;
        if (playerController != null)
        {
            playerController.SetInputLock(true);
        }

        // Aplica o zoom aproximado na câmera
        IsometricOrbitCamera orbitCamera = Camera.main != null ? Camera.main.GetComponent<IsometricOrbitCamera>() : null;
        if (orbitCamera != null)
        {
            orbitCamera.SetZoomLock(true, cutsceneZoomDistance);
        }

        yield return null;

        if (startDelay > 0f)
        {
            yield return new WaitForSeconds(startDelay);
        }

        if (landingTile == null)
        {
            Debug.LogError("O Landing Tile não foi atribuído no Inspector do Ship!");
            if (playerController != null) playerController.SetInputLock(false);
            if (orbitCamera != null) orbitCamera.SetZoomLock(false, normalZoomDistance);
            yield break;
        }

        Vector3 landingWorldPos = landingTile.position + Vector3.up * yOffsetAboveTile;
        PirateStackManager stackManager = playerInstance != null ? playerInstance.GetComponent<PirateStackManager>() : null;

        // 1. PULO DO LÍDER PARA O LANDING TILE
        if (playerInstance != null)
        {
            playerInstance.transform.SetParent(null);
            yield return StartCoroutine(JumpPirateToGrid(playerInstance, landingWorldPos));
        }

        // 2. DEMAIS PIRATAS PULAM PARA A PILHA
        if (extraPirateInstances != null)
        {
            float stepH = stackManager != null ? stackManager.StepHeight : 1.0f;

            for (int i = 0; i < extraPirateInstances.Length; i++)
            {
                GameObject extraPirate = extraPirateInstances[i];
                if (extraPirate == null) continue;

                extraPirate.transform.SetParent(null);

                Vector3 targetStackPos = landingWorldPos + Vector3.up * ((i + 1) * stepH);
                yield return StartCoroutine(JumpPirateToGrid(extraPirate, targetStackPos));

                if (stackManager != null)
                {
                    stackManager.AddToStackDirectly(extraPirate);
                }

                yield return new WaitForSeconds(delayBetweenJumps);
            }

            if (stackManager != null)
            {
                stackManager.enabled = true;
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

        // 4. EXIBE O MAPA COM FADE IN E AGUARDA A MINIMIZAÇÃO COMPLETA
        if (TreasureMapUI.Instance != null)
        {
            if (mapDisplayDelay > 0f)
            {
                yield return new WaitForSeconds(mapDisplayDelay);
            }

            yield return TreasureMapUI.Instance.ShowIntroSequence(autoMinimizeDelay);
        }

        // 5. REMOVE O ZOOM DA CÂMERA SOMENTE APÓS O MAPA MINIMIZAR
        if (orbitCamera != null)
        {
            orbitCamera.SetZoomLock(false, normalZoomDistance);
        }

        // 6. LIBERA OS CONTROLES DO JOGADOR
        if (playerController != null)
        {
            playerController.SetInputLock(false);
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
        PirateStackManager stackManager = playerInstance != null ? playerInstance.GetComponent<PirateStackManager>() : null;

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

                Vector3 currentPos = Vector3.Lerp(startPos, targetWorldPos, progress);
                currentPos.y += Mathf.Sin(progress * Mathf.PI) * stepJumpHeight;

                playerInstance.transform.position = currentPos;

                yield return null;
            }

            playerInstance.transform.position = targetWorldPos;

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