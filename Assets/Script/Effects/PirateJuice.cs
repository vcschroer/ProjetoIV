using System.Collections;
using UnityEngine;

public class PirateJuice : MonoBehaviour
{
    [Header("Configurações do Bounce")]
    [SerializeField] private float bounceIntensity = 0.15f;
    [SerializeField] private float bounceDuration = 0.2f;

    [Header("Configurações de Inércia do Passo")]
    [SerializeField] private float swayDuration = 0.45f;

    [Space]
    [SerializeField] private float tiltAngle = 18f;
    [SerializeField] private float forwardOvershootRatio = 0.6f;
    [SerializeField] private Vector3 tiltAxis = new Vector3(1f, 0f, 0f);

    [Space]
    [SerializeField] private float upLiftIntensity = 0.08f;
    [SerializeField] private float backShiftIntensity = 0.15f;

    [Header("Configurações do Squash & Stretch")]
    [SerializeField] private float dropSquashAmount = 0.35f;
    [SerializeField] private float dropDuration = 0.25f;

    [SerializeField] private float pickupStretchAmount = 0.4f;
    [SerializeField] private float pickupDuration = 0.25f;

    [Header("Configurações da Flutuação")]
    [SerializeField] private float floatAmplitude = 0.08f;
    [SerializeField] private float floatSpeed = 2.0f;
    [SerializeField] private float floatTiltAngle = 5.0f;

    private Vector3 initialLocalPos;
    private Vector3 initialScale;
    private Quaternion initialRotation;

    private Coroutine floatCoroutine;
    private Coroutine swayCoroutine;

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
        if (bounceDuration <= 0f) yield break;

        float elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / bounceDuration);

            float yOffset = Mathf.Sin(progress * Mathf.PI) * bounceIntensity;

            Vector3 currentPos = transform.localPosition;
            transform.localPosition = new Vector3(currentPos.x, initialLocalPos.y + yOffset, currentPos.z);

            yield return null;
        }

        transform.localPosition = initialLocalPos;
    }

    public void TriggerStepSway(float delay = 0f, float heightMultiplier = 1f)
    {
        if (swayCoroutine != null) StopCoroutine(swayCoroutine);
        swayCoroutine = StartCoroutine(StepSwayRoutine(delay, heightMultiplier));
    }

    private IEnumerator StepSwayRoutine(float delay, float heightMultiplier)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);

        // Previne divisão por zero se swayDuration for 0 no Inspector
        if (swayDuration <= 0f)
        {
            transform.localRotation = initialRotation;
            transform.localPosition = initialLocalPos;
            swayCoroutine = null;
            yield break;
        }

        float elapsed = 0f;

        float maxTilt = tiltAngle * heightMultiplier;
        float maxLift = upLiftIntensity * heightMultiplier;
        float maxShift = backShiftIntensity * heightMultiplier;

        // Garante que a linha de eixo não seja Vector3.zero
        Vector3 safeAxis = tiltAxis.sqrMagnitude > 0.0001f ? tiltAxis.normalized : Vector3.right;

        while (elapsed < swayDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / swayDuration);

            float rawOvershoot = Mathf.Sin(progress * Mathf.PI * 2f);
            float damping = Mathf.Pow(1f - progress, 1.2f);

            float curve;
            if (rawOvershoot >= 0)
            {
                curve = rawOvershoot * damping;
            }
            else
            {
                curve = rawOvershoot * damping * forwardOvershootRatio;
            }

            Quaternion tiltRot = Quaternion.Euler(safeAxis * (curve * maxTilt));
            transform.localRotation = initialRotation * tiltRot;

            float yLift = Mathf.Abs(curve) * maxLift;
            float zShift = -curve * maxShift;

            Vector3 offset = new Vector3(0f, yLift, zShift);
            transform.localPosition = initialLocalPos + offset;

            yield return null;
        }

        transform.localRotation = initialRotation;
        transform.localPosition = initialLocalPos;
        swayCoroutine = null;
    }

    public void StopSway()
    {
        if (swayCoroutine != null)
        {
            StopCoroutine(swayCoroutine);
            swayCoroutine = null;
        }
        transform.localPosition = initialLocalPos;
        transform.localRotation = initialRotation;
    }

    public void TriggerDropSquash(float delay = 0f)
    {
        StopSway();
        StartCoroutine(DropSquashRoutine(delay));
    }

    private IEnumerator DropSquashRoutine(float delay)
    {
        if (delay > 0) yield return new WaitForSeconds(delay);
        if (dropDuration <= 0f) yield break;

        float elapsed = 0f;

        while (elapsed < dropDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / dropDuration);

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
        StopSway();
        StopAllCoroutines();
        StartCoroutine(PickupStretchRoutine());
    }

    private IEnumerator PickupStretchRoutine()
    {
        if (pickupDuration <= 0f) yield break;

        float elapsed = 0f;

        while (elapsed < pickupDuration)
        {
            elapsed += Time.deltaTime;
            float progress = Mathf.Clamp01(elapsed / pickupDuration);

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

            float tiltX = Mathf.Sin(time * 0.8f) * floatTiltAngle;
            float tiltZ = Mathf.Cos(time * 0.6f) * floatTiltAngle;
            transform.localRotation = initialRotation * Quaternion.Euler(tiltX, 0f, tiltZ);

            yield return null;
        }
    }
}