using System.Collections;
using UnityEngine;

public class PirateJuice : MonoBehaviour
{
    [Header("Configurações do Bounce (Andando)")]
    [SerializeField] private float bounceIntensity = 0.15f;
    [SerializeField] private float bounceDuration = 0.2f;

    [Header("Configurações do Squash & Stretch")]
    [SerializeField] private float dropSquashAmount = 0.35f;
    [SerializeField] private float dropDuration = 0.25f;

    [SerializeField] private float pickupStretchAmount = 0.4f;
    [SerializeField] private float pickupDuration = 0.25f;

    [Header("Configurações da Flutuação na Água")]
    [SerializeField] private float floatAmplitude = 0.08f;
    [SerializeField] private float floatSpeed = 2.0f;
    [SerializeField] private float tiltAngle = 5.0f;

    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    private Coroutine floatCoroutine;

    private void Awake()
    {
        initialLocalPos = transform.localPosition;
        initialScale = transform.localScale;
        initialRotation = transform.localRotation;
    }


    public void TriggerBounce(float delay = 0f)
    {
        StartCoroutine(BounceRoutine(delay));
    }

    private IEnumerator BounceRoutine(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / bounceDuration;

            float yOffset = Mathf.Sin(progress * Mathf.PI) * bounceIntensity;
            transform.localPosition = initialLocalPos + new Vector3(0, yOffset, 0);

            yield return null;
        }

        transform.localPosition = initialLocalPos;
    }

    public void TriggerDropSquash(float delay = 0f)
    {
        StartCoroutine(DropSquashRoutine(delay));
    }

    private IEnumerator DropSquashRoutine(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / dropDuration;

            float squashFactor = Mathf.Sin(progress * Mathf.PI) * dropSquashAmount;

            float newY = initialScale.y * (1f - squashFactor);
            float newXZ = initialScale.x * (1f + (squashFactor * 0.5f));

            transform.localScale = new Vector3(newXZ, newY, newXZ);
            yield return null;
        }

        transform.localScale = initialScale;
    }

    public void TriggerPickupStretch()
    {
        StopFloating();
        StopAllCoroutines();
        StartCoroutine(PickupStretchRoutine());
    }

    private IEnumerator PickupStretchRoutine()
    {
        float elapsed = 0f;

        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float progress = elapsed / pickupDuration;

            float stretchFactor = Mathf.Sin(progress * Mathf.PI) * pickupStretchAmount;

            float newY = initialScale.y * (1f + stretchFactor);
            float newXZ = initialScale.x * (1f - (stretchFactor * 0.5f));

            transform.localScale = new Vector3(newXZ, newY, newXZ);
            yield return null;
        }

        transform.localScale = initialScale;
    }



    public void StartFloating()
    {
        StopFloating(); 
        floatCoroutine = StartCoroutine(FloatRoutine());
    }

    public void StopFloating()
    {
        if (floatCoroutine != null)
        {
            StopCoroutine(floatCoroutine);
            floatCoroutine = null;
        }

        transform.localPosition = initialLocalPos;
        transform.localRotation = initialRotation;
    }

    private IEnumerator FloatRoutine()
    {
        float timeOffset = Random.Range(0f, 100f);

        while (true)
        {
            float time = (Time.time + timeOffset) * floatSpeed;

            float newY = initialLocalPos.y + (Mathf.Sin(time) * floatAmplitude);
            transform.localPosition = new Vector3(initialLocalPos.x, newY, initialLocalPos.z);

            float tiltX = Mathf.Sin(time * 0.8f) * tiltAngle;
            float tiltZ = Mathf.Cos(time * 0.6f) * tiltAngle;
            transform.localRotation = initialRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

            yield return null;
        }
    }

}