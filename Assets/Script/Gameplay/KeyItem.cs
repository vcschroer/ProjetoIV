using System.Collections;
using UnityEngine;

public class KeyItem : MonoBehaviour
{
    [Header("Flutuar e Giro (Idle)")]
    [SerializeField] private float floatSpeed = 2f;
    [SerializeField] private float floatAmplitude = 0.12f;
    [SerializeField] private float spinSpeed = 90f;
    [SerializeField] private Vector3 spinAxis = new Vector3(0, 1, 0);

    [Header("Animação de Coleta (Pop-up)")]
    [SerializeField] private float popScaleMultiplier = 1.5f;
    [SerializeField] private float animationDuration = 0.5f;
    [SerializeField] private ParticleSystem pickupParticles;

    [Header("Referências e Regras")]
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
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isCollected) return;

        if (other.CompareTag(playerTag))
        {
            CollectKey();
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

        if (pickupParticles != null)
        {
            pickupParticles.Play();
        }

        StartCoroutine(CollectAnimationRoutine());
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

            transform.localScale = Vector3.Lerp(startScale, maxScale, smoothProgress);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < downDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / downDuration;
            float smoothProgress = Mathf.SmoothStep(0f, 1f, progress);

            transform.localScale = Vector3.Lerp(maxScale, Vector3.zero, smoothProgress);
            yield return null;
        }

        Destroy(gameObject);
    }
}