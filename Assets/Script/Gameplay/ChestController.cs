using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChestController : MonoBehaviour
{
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

    [Header("Partículas")]
    [SerializeField] private List<GameObject> particlePrefabs = new List<GameObject>();
    [SerializeField] private int randomParticleCount = 20;

    [Header("Área de Spawn das Partículas")]
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
    }

    private void Update()
    {
        if (!isOpen && !isAnimating)
        {
            float idleY = startPosition.y + Mathf.Sin(Time.time * idleFloatSpeed) * idleFloatAmplitude;
            transform.position = new Vector3(startPosition.x, idleY, startPosition.z);
            if (idleSpinSpeed != 0) transform.Rotate(idleSpinAxis.normalized * idleSpinSpeed * Time.deltaTime, Space.Self);
        }
        else if (isOpen && !isAnimating)
        {
            float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);
            transform.Rotate(postSpinAxis.normalized * postSpinSpeed * Time.deltaTime, Space.Self);
        }

        if (isAnimating || isOpen) return; 

        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            TryOpenChest();
        }
    }

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
}