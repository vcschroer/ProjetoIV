using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuWaveEffect : MonoBehaviour
{
    [Header("Configurações dos Elementos")]
    [SerializeField] private GameObject wavePrefab;
    [SerializeField] private Transform waveContainer;

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
    private float spawnTimer = 0f;

    private void Start()
    {
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

            float currentX = Mathf.Lerp(startX, startX - maxTravelDistance, Mathf.Sin(progress * Mathf.PI));

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