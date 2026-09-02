using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PirateArrivalSequence : MonoBehaviour
{
    [Header("Configurações do Navio")]
    [SerializeField] private Transform[] shipSpawnPoints; // Pontos no convés do navio para cada pirata
    [SerializeField] private float jumpDuration = 0.6f;     // Duração do pulo do navio até o grid
    [SerializeField] private float jumpArcHeight = 1.8f;    // Altura do arco do pulo
    [SerializeField] private float delayBetweenPirates = 0.15f; // Delay do pulo entre cada pirata

    [Header("Configurações de Movimento")]
    [SerializeField] private float moveSpeed = 4f;          // Velocidade de caminhada no grid
    [SerializeField] private float stepSwayFrequency = 0.35f;// Intervalo para acionar o Juice de passo

    // Evento disparado quando todos os piratas chegam e estão prontos para o jogador
    public System.Action OnPiratesReady;

    /// <summary>
    /// Inicia a sequência de desembarque dos piratas.
    /// </summary>
    /// <param name="piratePrefabs">Prefabs dos piratas a instanciar.</param>
    /// <param name="landingPositions">Posição inicial no grid para onde vão pular.</param>
    /// <param name="finalPaths">Lista de caminhos (Nodes/Vector3) até a posição final de cada pirata.</param>
    public void StartDesembarkSequence(
        GameObject[] piratePrefabs,
        Vector3[] landingPositions,
        List<Vector3>[] finalPaths)
    {
        StartCoroutine(DesembarkRoutine(piratePrefabs, landingPositions, finalPaths));
    }

    private IEnumerator DesembarkRoutine(
        GameObject[] piratePrefabs,
        Vector3[] landingPositions,
        List<Vector3>[] finalPaths)
    {
        int totalPirates = Mathf.Min(piratePrefabs.Length, shipSpawnPoints.Length);
        List<GameObject> spawnedPirates = new List<GameObject>();
        List<Coroutine> activeWalkRoutines = new List<Coroutine>();

        // 1. Instanciar piratas nos pontos do navio
        for (int i = 0; i < totalPirates; i++)
        {
            GameObject pirateObj = Instantiate(piratePrefabs[i], shipSpawnPoints[i].position, shipSpawnPoints[i].rotation);
            spawnedPirates.Add(pirateObj);
        }

        // 2. Fazer cada pirata pular do navio para o grid (com delay leve entre eles)
        for (int i = 0; i < totalPirates; i++)
        {
            int index = i;
            GameObject pirate = spawnedPirates[index];
            Vector3 startPos = shipSpawnPoints[index].position;
            Vector3 targetLanding = landingPositions[index];

            // Inicia o pulo individual
            StartCoroutine(JumpToGridRoutine(pirate, startPos, targetLanding, () =>
            {
                // Quando terminar o pulo, inicia a caminhada até o destino final no grid
                if (finalPaths != null && index < finalPaths.Length && finalPaths[index] != null)
                {
                    Coroutine walk = StartCoroutine(WalkPathRoutine(pirate, finalPaths[index]));
                    activeWalkRoutines.Add(walk);
                }
            }));

            yield return new WaitForSeconds(delayBetweenPirates);
        }

        // Aguardar todos completarem a caminhada
        foreach (var walkRoutine in activeWalkRoutines)
        {
            if (walkRoutine != null) yield return walkRoutine;
        }

        // 3. Notificar que a equipe está pronta para o jogo começar
        OnPiratesReady?.Invoke();
    }

    /// <summary>
    /// Pulo em curva parabólica (Arco) do navio para o grid.
    /// </summary>
    private IEnumerator JumpToGridRoutine(GameObject pirate, Vector3 start, Vector3 end, System.Action onComplete)
    {
        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        float elapsed = 0f;

        while (elapsed < jumpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / jumpDuration);

            // Interpolação Linear de X e Z
            Vector3 currentPos = Vector3.Lerp(start, end, t);

            // Arco no Y usando Seno
            float yArc = Mathf.Sin(t * Mathf.PI) * jumpArcHeight;
            currentPos.y += yArc;

            pirate.transform.position = currentPos;

            // Rotacionar em direção ao destino
            Vector3 direction = (end - start);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                pirate.transform.rotation = Quaternion.Slerp(
                    pirate.transform.rotation,
                    Quaternion.LookRotation(direction),
                    Time.deltaTime * 10f
                );
            }

            yield return null;
        }

        pirate.transform.position = end;

        // Efeito de Impacto/Squash no pouso
        if (juice != null)
        {
            juice.TriggerDropSquash();
        }

        onComplete?.Invoke();
    }

    /// <summary>
    /// Caminhada nó a nó no grid ativando a inércia/Juice a cada passo.
    /// </summary>
    private IEnumerator WalkPathRoutine(GameObject pirate, List<Vector3> path)
    {
        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        float stepTimer = 0f;

        for (int i = 0; i < path.Count; i++)
        {
            Vector3 targetWaypoint = path[i];

            while (Vector3.Distance(pirate.transform.position, targetWaypoint) > 0.05f)
            {
                // Movimento
                pirate.transform.position = Vector3.MoveTowards(
                    pirate.transform.position,
                    targetWaypoint,
                    moveSpeed * Time.deltaTime
                );

                // Rotação suave no sentido do caminho
                Vector3 moveDir = (targetWaypoint - pirate.transform.position).normalized;
                moveDir.y = 0;
                if (moveDir != Vector3.zero)
                {
                    pirate.transform.rotation = Quaternion.Slerp(
                        pirate.transform.rotation,
                        Quaternion.LookRotation(moveDir),
                        Time.deltaTime * 12f
                    );
                }

                // Disparo de inércia do passo (Sway)
                stepTimer += Time.deltaTime;
                if (stepTimer >= stepSwayFrequency)
                {
                    stepTimer = 0f;
                    if (juice != null)
                    {
                        juice.TriggerStepSway(0f, 1f);
                    }
                }

                yield return null;
            }

            pirate.transform.position = targetWaypoint;
        }

        // Parar animações de caminhada ao chegar no destino final
        if (juice != null)
        {
            juice.StopSway();
            juice.TriggerBounce(); // Efeito suave de parada/chegada
        }
    }
}