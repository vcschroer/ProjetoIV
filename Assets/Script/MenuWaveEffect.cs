using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuWaveEffect : MonoBehaviour
{
    [Header("Configurações dos Elementos")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform waveContainer;

    [Header("Conchas e Pedrinhas na Beira do Mar")]
    [SerializeField] private RectTransform[] shellTransforms;
    [SerializeField] private float shellPushDistance = 15f;
    [SerializeField] private float shellSmoothSpeed = 6f;   
    [Header("Movimento Horizontal (Vinda da Direita)")]
    [SerializeField] private float startX = 600f;
    [SerializeField] private float maxTravelDistance = 350f;
    [SerializeField] private float waveDuration = 4f;

    [Header("Movimento Secundário (Oscilação Vertical)")]
    [SerializeField] private float verticalAmplitude = 40f;
    [SerializeField] private float verticalSpeed = 2f;

    [Header("Geração Contínua de Ondas")]
    [SerializeField] private float spawnInterval = 1.2f;

    private class WaveData
    {
        public RectTransform rectTransform;
        public Image image;
        public float elapsedTime;
        public float initialY;
        public float randomSeed;
    }

    private List<WaveData> activeWaves = new List<WaveData>();
    private Dictionary<RectTransform, Vector2> shellsOriginalPositions = new Dictionary<RectTransform, Vector2>();
    private float spawnTimer = 0f;

    private void Start()
    {
        if (shellTransforms != null)
        {
            foreach (RectTransform shell in shellTransforms)
            {
                if (shell != null)
                {
                    shellsOriginalPositions[shell] = shell.anchoredPosition;
                }
            }
        }

        SpawnWave();
    }

    private void Update()
    {
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= spawnInterval)
        {
            spawnTimer = 0f;
            SpawnWave();
        }

        float maxWaveExtension = 0f;

        for (int i = activeWaves.Count - 1; i >= 0; i--)
        {
            WaveData wave = activeWaves[i];

            if (wave == null || wave.rectTransform == null)
            {
                activeWaves.RemoveAt(i);
                continue;
            }

            wave.elapsedTime += Time.deltaTime;
            float progress = Mathf.Clamp01(wave.elapsedTime / waveDuration);

            float waveCurve = Mathf.Sin(progress * Mathf.PI);

            if (waveCurve > maxWaveExtension)
            {
                maxWaveExtension = waveCurve;
            }

            float currentX = Mathf.Lerp(startX, startX - maxTravelDistance, waveCurve);
            float currentY = wave.initialY + Mathf.Sin((wave.elapsedTime * verticalSpeed) + wave.randomSeed) * verticalAmplitude;

            wave.rectTransform.anchoredPosition = new Vector2(currentX, currentY);

            float currentAlpha = Mathf.Lerp(1f, 0f, progress);
            Color color = wave.image.color;
            color.a = currentAlpha;
            wave.image.color = color;

            if (progress >= 1f)
            {
                Destroy(wave.image.gameObject);
                activeWaves.RemoveAt(i);
            }
        }

        UpdateShellsSync(maxWaveExtension);
    }

    private void UpdateShellsSync(float waveExtension)
    {
        if (shellTransforms == null) return;

        foreach (RectTransform shell in shellTransforms)
        {
            if (shell != null && shellsOriginalPositions.TryGetValue(shell, out Vector2 origPos))
            {

                float targetOffsetX = -waveExtension * shellPushDistance;
                Vector2 targetPos = origPos + new Vector2(targetOffsetX, 0f);

                shell.anchoredPosition = Vector2.Lerp(shell.anchoredPosition, targetPos, Time.deltaTime * shellSmoothSpeed);
            }
        }
    }

    private void SpawnWave()
    {
        if (wavePrefab == null || waveContainer == null) return;

        GameObject waveObj = Instantiate(wavePrefab, waveContainer);
        RectTransform rect = waveObj.GetComponent<RectTransform>();
        Image img = waveObj.GetComponent<Image>();

        if (rect != null && img != null)
        {
            float startY = rect.anchoredPosition.y;
            rect.anchoredPosition = new Vector2(startX, startY);

            Color color = img.color;
            color.a = 1f;
            img.color = color;

            WaveData newWave = new WaveData
            {
                rectTransform = rect,
                image = img,
                elapsedTime = 0f,
                initialY = startY,
                randomSeed = Random.Range(0f, 10f)
            };

            activeWaves.Add(newWave);
        }
    }
}