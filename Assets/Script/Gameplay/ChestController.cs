using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class ChestController : MonoBehaviour
{
    [Header("Chave")]
    [SerializeField] private bool requiresKey = false;
    [SerializeField] private bool hasKey = false;
    [SerializeField] private bool consumeKeyOnUse = true;

    [Header("Soterrado 100% (Proximidade)")]
    [SerializeField] private bool startsFullyBuried = true;
    [SerializeField] private float fullyBuriedYOffset = -2.5f;
    [SerializeField] private float revealDistance = 3.5f;
    [SerializeField] private float revealSpeed = 2f;

    [Header("Meio Soterrado / Escavação")]
    [SerializeField] private bool startsBuried = true;
    [SerializeField] private float buriedYOffset = -0.8f;
    [SerializeField] private Vector3 buriedTiltOffset = new Vector3(-20f, 0f, 10f);
    [SerializeField] private Vector3 pryTiltOffset = new Vector3(15f, 0f, -5f);
    [SerializeField] private Vector3 digAreaSize = new Vector3(2.5f, 1.5f, 2.5f);
    [SerializeField] private Vector3 digAreaOffset = Vector3.zero;
    [SerializeField] private float digDelay = 0f;
    [SerializeField] private float digDuration = 1.4f;
    [SerializeField] private ParticleSystem digParticles;

    [Header("Poeira da Escavação")]
    [SerializeField] private List<GameObject> dustParticlePrefabs = new List<GameObject>();
    [SerializeField] private int revealDustParticleCount = 8;
    [SerializeField] private int digDustParticleCount = 16;
    [SerializeField] private float dustSpawnRadius = 1.2f;
    [SerializeField] private float dustMinHeight = 0.0f;
    [SerializeField] private float dustMaxHeight = 0.4f;
    [SerializeField] private float dustLifetime = 2.5f;

    [Header("Flutuar e Giro (Idle e Abertura)")]
    [SerializeField] private Vector3 idleSpinAxis = new Vector3(0, 1, 0);
    [SerializeField] private Vector3 openSpinAxis = new Vector3(0, 1, 0);
    [SerializeField] private Vector3 postSpinAxis = new Vector3(0, 1, 0);

    [SerializeField] private float idleFloatSpeed = 1.5f;
    [SerializeField] private float idleFloatAmplitude = 0.08f;
    [SerializeField] private float idleSpinSpeed = 0f;

    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float jumpDuration = 0.8f;
    [SerializeField] private float totalSpins = 2f;
    [SerializeField] private float stretchAmount = 0.25f;

    [SerializeField] private Vector3 finalScale = Vector3.one;

    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.15f;
    [SerializeField] private float postSpinSpeed = 45f;

    [Header("Partículas de Abertura")]
    [SerializeField] private List<GameObject> particlePrefabs = new List<GameObject>();
    [SerializeField] private int randomParticleCount = 20;
    [SerializeField] private float minSpawnDistance = 0.5f;
    [SerializeField] private float maxSpawnDistance = 1.8f;
    [SerializeField] private float minSpawnHeight = 0.0f;
    [SerializeField] private float maxSpawnHeight = 2.0f;
    [SerializeField] private ParticleSystem sparkleParticles;

    [Header("Interação e Regras")]
    [SerializeField] private float maxInteractionDistance = 2.5f;
    [SerializeField] private Transform playerTransform;

    [Header("Transição de Cena")]
    [SerializeField] private string nomeCenaDestino = "Menu";
    [SerializeField] private float delayAntesTransicao = 0.2f;

    private bool isFullyBuried = false;
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

        isFullyBuried = startsFullyBuried;
        isBuried = startsBuried || startsFullyBuried;

        if (isFullyBuried)
        {
            transform.position = startPosition + new Vector3(0, fullyBuriedYOffset, 0);
            transform.rotation = startRotation * Quaternion.Euler(buriedTiltOffset);
        }
        else if (isBuried)
        {
            transform.position = startPosition + new Vector3(0, buriedYOffset, 0);
            transform.rotation = startRotation * Quaternion.Euler(buriedTiltOffset);
        }
    }

    private void Update()
    {
        if ((TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        if (isAnimating) return;

        if (isFullyBuried)
        {
            CheckProximityToReveal();
            return;
        }

        if (isBuried)
        {
            if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
            {
                TryDigChest();
            }
        }
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
        else
        {
            float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);
            transform.Rotate(postSpinAxis.normalized * postSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    private void CheckProximityToReveal()
    {
        if (playerTransform == null) return;

        float distance = Vector3.Distance(startPosition, playerTransform.position);
        if (distance <= revealDistance)
        {
            StartCoroutine(RevealFromGroundRoutine());
        }
    }

    private IEnumerator RevealFromGroundRoutine()
    {
        isAnimating = true;

        if (digParticles != null) digParticles.Play();

        Vector3 fullyBuriedPos = startPosition + new Vector3(0, fullyBuriedYOffset, 0);
        Vector3 partiallyBuriedPos = startPosition + new Vector3(0, buriedYOffset, 0);

        float elapsed = 0f;
        float duration = 1f / revealSpeed;
        float spawnTimer = 0f;
        float spawnInterval = (revealDustParticleCount > 0 && duration > 0f) ? duration / revealDustParticleCount : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            if (spawnInterval > 0f)
            {
                spawnTimer += Time.deltaTime;
                while (spawnTimer >= spawnInterval)
                {
                    spawnTimer -= spawnInterval;
                    SpawnSingleDustParticle();
                }
            }

            float progress = elapsed / duration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(fullyBuriedPos, partiallyBuriedPos, smoothProgress);
            yield return null;
        }

        transform.position = partiallyBuriedPos;

        if (digParticles != null) digParticles.Stop();

        isFullyBuried = false;
        isAnimating = false;
    }

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

            Bounds digBounds = new Bounds(startPosition + digAreaOffset, digAreaSize);

            if (digBounds.Contains(hit.point) || hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                StartCoroutine(UnearthRoutine());
            }
        }
    }

    private IEnumerator UnearthRoutine()
    {
        isAnimating = true;

        if (digDelay > 0f)
        {
            yield return new WaitForSeconds(digDelay);
        }

        if (digParticles != null) digParticles.Play();

        Vector3 buriedPosition = startPosition + new Vector3(0, buriedYOffset, 0);
        Quaternion initialRotation = startRotation * Quaternion.Euler(buriedTiltOffset);
        Quaternion leverRotation = startRotation * Quaternion.Euler(pryTiltOffset);

        float elapsed = 0f;
        float spawnTimer = 0f;
        float spawnInterval = (digDustParticleCount > 0 && digDuration > 0f) ? digDuration / digDustParticleCount : 0f;

        while (elapsed < digDuration)
        {
            elapsed += Time.deltaTime;

            if (spawnInterval > 0f)
            {
                spawnTimer += Time.deltaTime;
                while (spawnTimer >= spawnInterval)
                {
                    spawnTimer -= spawnInterval;
                    SpawnSingleDustParticle();
                }
            }

            float progress = elapsed / digDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.position = Vector3.Lerp(buriedPosition, startPosition, smoothProgress);

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

        if (digParticles != null) digParticles.Stop();

        isBuried = false;
        isAnimating = false;
    }

    private void SpawnSingleDustParticle()
    {
        if (dustParticlePrefabs == null || dustParticlePrefabs.Count == 0) return;

        GameObject selectedPrefab = dustParticlePrefabs[Random.Range(0, dustParticlePrefabs.Count)];
        if (selectedPrefab == null) return;

        Vector2 sideCircle = Random.insideUnitCircle.normalized;
        float height = Random.Range(dustMinHeight, dustMaxHeight);

        Vector3 spawnOffset = new Vector3(sideCircle.x * dustSpawnRadius, height, sideCircle.y * dustSpawnRadius);
        Vector3 spawnPos = startPosition + spawnOffset;

        GameObject particle = Instantiate(selectedPrefab, spawnPos, Quaternion.identity);
        Destroy(particle, dustLifetime);
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

                if (requiresKey && !hasKey)
                {
                    Debug.Log("O baú está trancado! Você precisa de uma chave para abri-lo.");
                    StartCoroutine(LockedShakeRoutine());
                    return;
                }

                if (requiresKey && consumeKeyOnUse)
                {
                    hasKey = false;
                }

                StartCoroutine(ChestJuiceRoutine());
            }
        }
    }

    private IEnumerator LockedShakeRoutine()
    {
        isAnimating = true;
        Quaternion originalRot = transform.rotation;
        float shakeDuration = 0.4f;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            elapsed += Time.deltaTime;
            float angle = Mathf.Sin(elapsed * 40f) * 5f;
            transform.rotation = originalRot * Quaternion.Euler(0, 0, angle);
            yield return null;
        }

        transform.rotation = originalRot;
        isAnimating = false;
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

    public void GiveKey()
    {
        hasKey = true;
        Debug.Log("Chave concedida ao jogador!");
    }

    public void SetHasKey(bool state)
    {
        hasKey = state;
    }

    public bool HasKey => hasKey;
    public bool RequiresKey => requiresKey;

    private void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireSphere(center, revealDistance);

        Gizmos.color = new Color(1f, 0.92f, 0.015f, 0.5f);
        Gizmos.DrawWireCube(center + digAreaOffset, digAreaSize);

        Gizmos.color = new Color(0.6f, 0.4f, 0.2f, 0.6f);
        Gizmos.DrawWireSphere(center, dustSpawnRadius);
    }
}