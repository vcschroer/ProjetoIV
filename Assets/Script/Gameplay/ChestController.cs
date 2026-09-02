using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ChestController : MonoBehaviour
{
    [Header("Configurações de Baú Soterrado / Desenterrar")]
    [SerializeField] private bool startsBuried = true;
    [SerializeField] private float buriedYOffset = -0.8f;          // Quão fundo o baú começa
    [SerializeField] private Vector3 buriedTiltOffset = new Vector3(-20f, 0f, 10f); // Inclinado torto na terra
    [SerializeField] private Vector3 pryTiltOffset = new Vector3(15f, 0f, -5f);    // Ângulo de alavanca da pá (subindo o lado)
    [SerializeField] private Vector3 digAreaSize = new Vector3(2.5f, 1.5f, 2.5f); // Tamanho da área do clique
    [SerializeField] private Vector3 digAreaOffset = Vector3.zero;  // Deslocamento da área
    [SerializeField] private float digDuration = 1.4f;            // Duração do movimento de escavar
    [SerializeField] private ParticleSystem digParticles;         // Partícula fixa (opcional)

    [Header("Partículas de Escavação (Instanciação de Prefabs)")]
    [SerializeField] private List<GameObject> dustParticlePrefabs = new List<GameObject>();
    [SerializeField] private int dustParticleCount = 12;          // Quantidade de prefabs a serem instanciados
    [SerializeField] private float dustSpawnRadius = 1.2f;        // Distância/Raio para instanciar nas LATERAIS do baú
    [SerializeField] private float dustMinHeight = 0.0f;
    [SerializeField] private float dustMaxHeight = 0.4f;
    [SerializeField] private float dustLifetime = 2.5f;           // Tempo para destruir os prefabs

    [Header("Eixos e Direções de Rotação")]
    [SerializeField] private Vector3 idleSpinAxis = new Vector3(0, 1, 0);
    [SerializeField] private Vector3 openSpinAxis = new Vector3(0, 1, 0);
    [SerializeField] private Vector3 postSpinAxis = new Vector3(0, 1, 0);

    [Header("Configurações do Flutuar Idle (Antes de Abrir)")]
    [SerializeField] private float idleFloatSpeed = 1.5f;
    [SerializeField] private float idleFloatAmplitude = 0.08f;
    [SerializeField] private float idleSpinSpeed = 0f;

    [Header("Configurações do Pulo e Giro")]
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.8f;
    [SerializeField] private float totalSpins = 2f;
    [SerializeField] private float stretchAmount = 0.25f;

    [Header("Configurações de Tamanho")]
    [SerializeField] private Vector3 finalScale = Vector3.one;

    [Header("Comportamento Pós-Abertura")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float postSpinSpeed = 45f;

    [Header("Partículas de Abertura")]
    [SerializeField] private List<GameObject> particlePrefabs = new List<GameObject>();
    [SerializeField] private int randomParticleCount = 20;

    [Header("Área de Spawn das Partículas de Abertura")]
    [SerializeField] private float minSpawnDistance = 0.5f;
    [SerializeField] private float maxSpawnDistance = 1.8f;
    [SerializeField] private float minSpawnHeight = 0.0f;
    [SerializeField] private float maxSpawnHeight = 2.0f;

    [SerializeField] private ParticleSystem sparkleParticles;

    [Header("Interação")]
    [SerializeField] private float maxInteractionDistance = 2.5f;
    [SerializeField] private Transform playerTransform;

    [Header("Fim de Fase / Transição")]
    [SerializeField] private string nomeCenaDestino = "Menu";
    [SerializeField] private float delayAntesTransicao = 0.2f;

    private bool isBuried = false;
    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private Vector3 startScale;
    private Vector3 basePosition;

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
        startScale = transform.localScale;

        isBuried = startsBuried;

        if (isBuried)
        {
            // Posiciona o baú fundo e inclinado na terra
            transform.position = startPosition + new Vector3(0, buriedYOffset, 0);
            transform.rotation = startRotation * Quaternion.Euler(buriedTiltOffset);
        }
    }

    private void Update()
    {
        // Trava para evitar cliques através de menus ou mapa da UI
        if ((TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        if (isAnimating) return;

        // ESTADO 1: Soterrado (Aguardando clique com o Botão Direito na área)
        if (isBuried)
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryDigChest();
            }
        }
        // ESTADO 2: Desenterrado e Fechado (Flutuando em Idle, aguardando clique para abrir)
        else if (!isOpen)
        {
            float idleY = startPosition.y + Mathf.Sin(Time.time * idleFloatSpeed) * idleFloatAmplitude;
            transform.position = new Vector3(startPosition.x, idleY, startPosition.z);

            if (idleSpinSpeed != 0)
            {
                transform.Rotate(idleSpinAxis.normalized * idleSpinSpeed * Time.deltaTime, Space.Self);
            }

            if (Mouse.current != null &&
               (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
            {
                TryOpenChest();
            }
        }
        // ESTADO 3: Aberto (Animação final pós-abertura)
        else
        {
            float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);
            transform.Rotate(postSpinAxis.normalized * postSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    /// <summary>
    /// Verifica se o clique com botão direito acertou a área configurada para desenterrar.
    /// </summary>
    private void TryDigChest()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (playerTransform != null)
            {
                float distance = Vector3.Distance(transform.position, playerTransform.position);
                if (distance > maxInteractionDistance) return;
            }

            // Verifica se o ponto do clique está dentro da Bounding Box da área de escavação
            Bounds digBounds = new Bounds(startPosition + digAreaOffset, digAreaSize);

            if (digBounds.Contains(hit.point) || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                StartCoroutine(UnearthRoutine());
            }
        }
    }

    /// <summary>
    /// Animação do baú sendo alavancado suavemente pela pá até sair do chão.
    /// </summary>
    private IEnumerator UnearthRoutine()
    {
        isAnimating = true;

        if (digParticles != null)
        {
            digParticles.Play();
        }

        // Instancia a poeira/terra nas LATERAIS do baú
        SpawnDustParticles();

        Vector3 buriedPosition = startPosition + new Vector3(0, buriedYOffset, 0);
        Quaternion initialRotation = startRotation * Quaternion.Euler(buriedTiltOffset);
        Quaternion leverRotation = startRotation * Quaternion.Euler(pryTiltOffset);

        float elapsed = 0f;

        while (elapsed < digDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / digDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            // Subida suave da posição Y
            transform.position = Vector3.Lerp(buriedPosition, startPosition, smoothProgress);

            // Rotação em 2 etapas de alavanca:
            if (progress < 0.5f)
            {
                float subProgress = progress / 0.5f;
                float smoothSubProgress = Mathf.SmoothStep(0f, 1f, subProgress);
                transform.rotation = Quaternion.Slerp(initialRotation, leverRotation, smoothSubProgress);
            }
            else
            {
                float subProgress = (progress - 0.5f) / 0.5f;
                float smoothSubProgress = Mathf.SmoothStep(0f, 1f, subProgress);
                transform.rotation = Quaternion.Slerp(leverRotation, startRotation, smoothSubProgress);
            }

            yield return null;
        }

        transform.position = startPosition;
        transform.rotation = startRotation;
        isBuried = false;
        isAnimating = false;
    }

    /// <summary>
    /// Instancia os prefabs de poeira no raio das LATERAIS do baú.
    /// </summary>
    private void SpawnDustParticles()
    {
        if (dustParticlePrefabs == null || dustParticlePrefabs.Count == 0) return;

        for (int i = 0; i < dustParticleCount; i++)
        {
            GameObject selectedPrefab = dustParticlePrefabs[Random.Range(0, dustParticlePrefabs.Count)];
            if (selectedPrefab == null) continue;

            // .normalized força a posição a ficar na BORDA do raio (nas laterais)
            Vector2 sideCircle = Random.insideUnitCircle.normalized;
            float height = Random.Range(dustMinHeight, dustMaxHeight);

            Vector3 spawnOffset = new Vector3(sideCircle.x * dustSpawnRadius, height, sideCircle.y * dustSpawnRadius);
            Vector3 spawnPos = startPosition + spawnOffset;

            GameObject particle = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            Destroy(particle, dustLifetime);
        }
    }

    /// <summary>
    /// Tenta abrir o baú após ser desenterrado.
    /// </summary>
    private void TryOpenChest()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                if (playerTransform != null)
                {
                    float distance = Vector3.Distance(transform.position, playerTransform.position);
                    if (distance > maxInteractionDistance) return;
                }
                StartCoroutine(ChestJuiceRoutine());
            }
        }
    }

    private IEnumerator ChestJuiceRoutine()
    {
        isAnimating = true;
        isOpen = false;

        transform.position = startPosition;
        transform.rotation = startRotation;
        transform.localScale = startScale;
        basePosition = startPosition;

        if (sparkleParticles != null) sparkleParticles.Play();
        SpawnRandomParticles();

        float time = 0;
        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;
            float heightOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = basePosition + new Vector3(0, heightOffset, 0);
            float currentAngle = progress * 360f * totalSpins;
            transform.rotation = startRotation * Quaternion.AngleAxis(currentAngle, openSpinAxis.normalized);
            float scaleEffect = 1f + Mathf.Sin(progress * Mathf.PI) * stretchAmount;
            Vector3 targetScaleProgress = Vector3.Lerp(startScale, finalScale, progress);
            transform.localScale = targetScaleProgress * scaleEffect;
            yield return null;
        }

        transform.rotation = startRotation;
        transform.localScale = finalScale;
        basePosition = transform.position;
        isAnimating = false;
        isOpen = true;

        yield return new WaitForSeconds(delayAntesTransicao);
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.CarregarCena(nomeCenaDestino);
        }
    }

    private void SpawnRandomParticles()
    {
        if (particlePrefabs == null || particlePrefabs.Count == 0) return;
        for (int i = 0; i < randomParticleCount; i++)
        {
            GameObject selectedPrefab = particlePrefabs[Random.Range(0, particlePrefabs.Count)];
            if (selectedPrefab == null) continue;
            Vector2 randomCircle = Random.insideUnitCircle.normalized;
            float randomDistance = Random.Range(minSpawnDistance, maxSpawnDistance);
            float randomHeight = Random.Range(minSpawnHeight, maxSpawnHeight);
            Vector3 spawnOffset = new Vector3(randomCircle.x * randomDistance, randomHeight, randomCircle.y * randomDistance);
            Vector3 spawnPos = transform.position + spawnOffset;
            GameObject particle = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
            Destroy(particle, 2.5f);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Desenha a caixa delimitadora da área de escavação na Scene View
        Gizmos.color = new Color(1f, 0.92f, 0.015f, 0.5f);
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        center += digAreaOffset;
        Gizmos.DrawWireCube(center, digAreaSize);

        // Desenha o anel das partículas de escavação nas laterais
        Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(center, dustSpawnRadius);
    }
}