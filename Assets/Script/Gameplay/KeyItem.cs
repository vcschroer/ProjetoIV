using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class KeyItem : MonoBehaviour
{
    [Header("Flutuar e Giro (Idle)")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.12f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private Vector3 spinAxis = new Vector3(0, 1, 0);

    [Header("Animação de Coleta (Squash & Stretch)")]
    [SerializeField] private float popScaleMultiplier = 1.5f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField, Range(0f, 1f)] private float squashAmount = 0.35f; 
    [SerializeField] private ParticleSystem pickupParticlesPrefab;
    [SerializeField] private int particleCount = 25;

    [Header("Interação e Proximidade")]
    [SerializeField] private float maxInteractionDistance = 2.5f;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private ChestController targetChest;

    private Vector3 startPosition;
    private Vector3 startScale;
    private bool isCollected = false;
    private Collider itemCollider;

    private void Awake()
    {
        startPosition = transform.position;
        startScale = transform.localScale;
        itemCollider = GetComponent<Collider>();

        if (playerTransform == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
    }

    private void Update()
    {
        if (isCollected) return;

        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        if (spinSpeed != 0f)
        {
            transform.Rotate(spinAxis.normalized * spinSpeed * Time.deltaTime, Space.Self);
        }

        if ((TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded) ||
            (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()))
        {
            return;
        }

        if (Mouse.current != null &&
           (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            TryCollectKey();
        }
    }

    private void TryCollectKey()
    {
        if (Camera.main == null) return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                if (playerTransform != null)
                {
                    float distance = Vector3.Distance(transform.position, playerTransform.position);
                    if (distance > maxInteractionDistance)
                    {
                        Debug.Log("Jogador está muito longe para pegar a chave!");
                        return;
                    }
                }

                CollectKey();
            }
        }
    }

    public void CollectKey()
    {
        if (isCollected) return;

        isCollected = true;

        if (itemCollider != null)
        {
            itemCollider.enabled = false;
        }

        if (targetChest != null)
        {
            targetChest.GiveKey();
        }

        SpawnPickupParticles();
        StartCoroutine(CollectAnimationRoutine());
    }

    private void SpawnPickupParticles()
    {
        if (pickupParticlesPrefab != null)
        {
            ParticleSystem psInstance = Instantiate(pickupParticlesPrefab, transform.position, Quaternion.identity);
            psInstance.Emit(particleCount);

            float maxLifetime = psInstance.main.duration + psInstance.main.startLifetime.constantMax;
            Destroy(psInstance.gameObject, maxLifetime);
        }
    }

    private IEnumerator CollectAnimationRoutine()
    {
        Vector3 maxScale = startScale * popScaleMultiplier;
        float upDuration = animationDuration * 0.4f;
        float downDuration = animationDuration * 0.6f;
        float elapsed = 0f;

        while (elapsed < upDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / upDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 baseScale = Vector3.Lerp(startScale, maxScale, smoothProgress);

            float stretchFactor = Mathf.Sin(progress * Mathf.PI) * squashAmount;
            Vector3 deformMultiplier = new Vector3(1f - stretchFactor, 1f + stretchFactor, 1f - stretchFactor);

            transform.localScale = Vector3.Scale(baseScale, deformMultiplier);
            yield return null;
        }

        elapsed = 0f;

        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / downDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            Vector3 baseScale = Vector3.Lerp(maxScale, Vector3.zero, smoothProgress);

            float squashFactor = Mathf.Sin(progress * Mathf.PI) * squashAmount;
            Vector3 deformMultiplier = new Vector3(1f + squashFactor, 1f - squashFactor, 1f + squashFactor);

            transform.localScale = Vector3.Scale(baseScale, deformMultiplier);
            yield return null;
        }

        Destroy(gameObject);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, maxInteractionDistance);
    }
}