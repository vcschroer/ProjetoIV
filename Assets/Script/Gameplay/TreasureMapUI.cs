using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI; // Necessário para Image e Button
using UnityEngine.EventSystems; // Necessário para detectar cliques e arrastos na UI

public class TreasureMapUI : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IDragHandler
{
    // Singleton para acesso global pelos scripts de movimentação e câmera
    public static TreasureMapUI Instance { get; private set; }
    public bool IsExpanded => isExpanded;

    [Header("Referências da UI")]
    [SerializeField] private RectTransform mapParchment; // O painel/imagem do papel do mapa
    [SerializeField] private Image backgroundPanel;     // O painel preto de fundo (UI Image/Panel)
    [SerializeField] private GameObject pathDotPrefab;  // Prefab de um pontinho (UI Image)
    [SerializeField] private GameObject startMarkPrefab; // Prefab do Início (Ex: Barco Pirata)
    [SerializeField] private GameObject xMarkPrefab;    // Prefab do ícone 'X' Final (UI Image)

    [Header("Configurações do Fundo Escuro")]
    [Range(0f, 1f)]
    [SerializeField] private float maxBackgroundAlpha = 0.75f;

    [Header("Configurações do Desenho")]
    [SerializeField] private float dotSpacing = 22f;
    [SerializeField] private float startIconClearance = 45f;
    [SerializeField] private float endIconClearance = 35f;

    [SerializeField] private float padding = 20f;        // Margem para não colar na borda
    [SerializeField] private Vector2 mapCenterOffset = Vector2.zero;

    [Header("Ajustes de Diagonais e Rotação do Desenho")]
    [SerializeField] private bool smoothDiagonalSteps = true;
    [SerializeField] private float spriteRotationOffset = 0f;

    [Header("Configurações de Expansão (Clique)")]
    [SerializeField] private Vector3 minimizedScale = Vector3.one;
    [SerializeField] private Vector3 expandedScale = new Vector3(2.5f, 2.5f, 1f);
    [SerializeField] private float animationDuration = 0.25f;
    [SerializeField] private bool centerWhenExpanded = true;

    [Header("Configurações de Rotação pelo Jogador")]
    [SerializeField] private float clickThreshold = 10f;

    [Header("Configurações de Wobble (Minimizado)")]
    [SerializeField] private bool usarWobble = true;
    [SerializeField] private float velocidadeWobble = 4f;
    [SerializeField] private float anguloMaximoWobble = 8f;

    private bool isExpanded = false;
    private bool isAnimating = false;
    private Coroutine resizeCoroutine;
    private Vector2 originalAnchoredPosition;
    private Vector2 pointerDownPosition;
    private float initialAngleOffset;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (mapParchment != null)
        {
            originalAnchoredPosition = mapParchment.anchoredPosition;
        }

        if (backgroundPanel != null)
        {
            Color c = backgroundPanel.color;
            c.a = 0f;
            backgroundPanel.color = c;
            backgroundPanel.raycastTarget = false;

            Button bgButton = backgroundPanel.GetComponent<Button>();
            if (bgButton == null)
            {
                bgButton = backgroundPanel.gameObject.AddComponent<Button>();
            }
            bgButton.transition = Selectable.Transition.None; 
            bgButton.onClick.AddListener(OnBackgroundClicked);
        }
    }

    private void Update()
    {
        if (usarWobble && !isExpanded && !isAnimating && mapParchment != null)
        {
            float angulo = Mathf.Sin(Time.time * velocidadeWobble) * anguloMaximoWobble;
            mapParchment.localRotation = Quaternion.Euler(0f, 0f, angulo);
        }
    }

    private void OnBackgroundClicked()
    {
        if (isExpanded && !isAnimating)
        {
            ToggleExpand();
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pointerDownPosition = eventData.position;

        if (isExpanded && mapParchment != null)
        {
            Vector2 mapScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, mapParchment.position);
            Vector2 dir = eventData.position - mapScreenPos;

            float mouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

            initialAngleOffset = mapParchment.eulerAngles.z - mouseAngle;
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isExpanded || mapParchment == null) return;

        Vector2 mapScreenPos = RectTransformUtility.WorldToScreenPoint(eventData.pressEventCamera, mapParchment.position);
        Vector2 dir = eventData.position - mapScreenPos;
        float currentMouseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        mapParchment.rotation = Quaternion.Euler(0f, 0f, currentMouseAngle + initialAngleOffset);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Vector2.Distance(pointerDownPosition, eventData.position) > clickThreshold)
        {
            return;
        }

        if (!isExpanded)
        {
            ToggleExpand();
        }
    }


    public void ToggleExpand()
    {
        isExpanded = !isExpanded;

        if (resizeCoroutine != null)
        {
            StopCoroutine(resizeCoroutine);
        }

        Vector3 targetScale = isExpanded ? expandedScale : minimizedScale;
        Vector2 targetPos = (isExpanded && centerWhenExpanded) ? Vector2.zero : originalAnchoredPosition;

        Quaternion targetRot = isExpanded ? mapParchment.localRotation : Quaternion.identity;

        resizeCoroutine = StartCoroutine(AnimateMap(targetScale, targetPos, targetRot));
    }

    private IEnumerator AnimateMap(Vector3 targetScale, Vector2 targetPosition, Quaternion targetRotation)
    {
        isAnimating = true;

        if (isExpanded && backgroundPanel != null)
        {
            backgroundPanel.raycastTarget = true;
        }

        Vector3 startScale = mapParchment.localScale;
        Vector2 startPos = mapParchment.anchoredPosition;
        Quaternion startRot = mapParchment.localRotation;

        float startAlpha = backgroundPanel != null ? backgroundPanel.color.a : 0f;
        float targetAlpha = isExpanded ? maxBackgroundAlpha : 0f;

        float elapsed = 0f;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / animationDuration;
            t = Mathf.SmoothStep(0f, 1f, t); 

            mapParchment.localScale = Vector3.Lerp(startScale, targetScale, t);
            mapParchment.anchoredPosition = Vector2.Lerp(startPos, targetPosition, t);
            mapParchment.localRotation = Quaternion.Slerp(startRot, targetRotation, t);

            if (backgroundPanel != null)
            {
                Color c = backgroundPanel.color;
                c.a = Mathf.Lerp(startAlpha, targetAlpha, t);
                backgroundPanel.color = c;
            }

            yield return null;
        }

        mapParchment.localScale = targetScale;
        mapParchment.anchoredPosition = targetPosition;
        mapParchment.localRotation = targetRotation;

        if (backgroundPanel != null)
        {
            Color finalColor = backgroundPanel.color;
            finalColor.a = targetAlpha;
            backgroundPanel.color = finalColor;

            if (!isExpanded)
            {
                backgroundPanel.raycastTarget = false;
            }
        }

        isAnimating = false;
    }

    public void GenerateMapPath(List<Vector3Int> gridPath)
    {
        foreach (Transform child in mapParchment)
        {
            Destroy(child.gameObject);
        }

        if (gridPath == null || gridPath.Count < 2) return;

        List<Vector2Int> path2D = new List<Vector2Int>();
        foreach (var pos3D in gridPath)
        {
            Vector2Int point2D = new Vector2Int(pos3D.x, pos3D.z);
            if (path2D.Count == 0 || path2D[path2D.Count - 1] != point2D)
            {
                path2D.Add(point2D);
            }
        }

        if (path2D.Count < 2) return;

        if (smoothDiagonalSteps)
        {
            path2D = SimplifyZigzags(path2D);
        }

        int minX = int.MaxValue, maxX = int.MinValue;
        int minZ = int.MaxValue, maxZ = int.MinValue;

        foreach (var pos in path2D)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minZ) minZ = pos.y;
            if (pos.y > maxZ) maxZ = pos.y;
        }

        float pathWidth = Mathf.Max(1, maxX - minX);
        float pathHeight = Mathf.Max(1, maxZ - minZ);

        float availableWidth = mapParchment.rect.width - (padding * 2f);
        float availableHeight = mapParchment.rect.height - (padding * 2f);

        float scaleX = availableWidth / pathWidth;
        float scaleZ = availableHeight / pathHeight;
        float scale = Mathf.Min(scaleX, scaleZ, 35f);

        Vector2 pathCenter = new Vector2((minX + maxX) / 2f, (minZ + maxZ) / 2f);

        List<Vector2> waypoints = new List<Vector2>();
        for (int i = 0; i < path2D.Count; i++)
        {
            Vector2Int gridPos = path2D[i];
            Vector2 canvasPos = new Vector2(
                (gridPos.x - pathCenter.x) * scale,
                (gridPos.y - pathCenter.y) * scale
            ) + mapCenterOffset;

            waypoints.Add(canvasPos);
        }

        float totalPathLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            totalPathLength += Vector2.Distance(waypoints[i], waypoints[i + 1]);
        }

        List<Vector2> dotPositions = new List<Vector2>();
        List<Vector2> dotDirections = new List<Vector2>();

        float accumulatedDist = 0f;
        float nextDotDist = startIconClearance;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector2 pStart = waypoints[i];
            Vector2 pEnd = waypoints[i + 1];
            float segLength = Vector2.Distance(pStart, pEnd);

            if (segLength < 0.001f) continue;

            Vector2 segDir = (pEnd - pStart).normalized;

            while (nextDotDist <= accumulatedDist + segLength && nextDotDist <= totalPathLength - endIconClearance)
            {
                float t = (nextDotDist - accumulatedDist) / segLength;
                Vector2 pos = Vector2.Lerp(pStart, pEnd, t);

                dotPositions.Add(pos);
                dotDirections.Add(segDir);

                nextDotDist += dotSpacing;
            }

            accumulatedDist += segLength;
        }

        for (int i = 0; i < dotPositions.Count; i++)
        {
            GameObject spawnedDot = Instantiate(pathDotPrefab, mapParchment);
            RectTransform rect = spawnedDot.GetComponent<RectTransform>();
            rect.anchoredPosition = dotPositions[i];

            Vector2 dir = dotDirections[i];
            if (dir != Vector2.zero)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                rect.localRotation = Quaternion.Euler(0f, 0f, angle + spriteRotationOffset);
            }
        }

        if (waypoints.Count > 0)
        {
            if (startMarkPrefab != null)
            {
                GameObject spawnedStart = Instantiate(startMarkPrefab, mapParchment);
                RectTransform startRect = spawnedStart.GetComponent<RectTransform>();
                startRect.anchoredPosition = waypoints[0];
                startRect.localRotation = Quaternion.identity;
            }

            if (xMarkPrefab != null)
            {
                GameObject spawnedX = Instantiate(xMarkPrefab, mapParchment);
                RectTransform xRect = spawnedX.GetComponent<RectTransform>();
                xRect.anchoredPosition = waypoints[waypoints.Count - 1];
                xRect.localRotation = Quaternion.identity;
            }
        }
    }

    private List<Vector2Int> SimplifyZigzags(List<Vector2Int> path)
    {
        if (path.Count < 3) return path;

        List<Vector2Int> simplified = new List<Vector2Int> { path[0] };

        for (int i = 1; i < path.Count - 1; i++)
        {
            Vector2Int prev = simplified[simplified.Count - 1];
            Vector2Int curr = path[i];
            Vector2Int next = path[i + 1];

            Vector2Int dir1 = curr - prev;
            Vector2Int dir2 = next - curr;

            if (Mathf.Abs(dir1.x) + Mathf.Abs(dir1.y) == 1 &&
                Mathf.Abs(dir2.x) + Mathf.Abs(dir2.y) == 1 &&
                dir1 != dir2)
            {
                continue;
            }

            simplified.Add(curr);
        }

        simplified.Add(path[path.Count - 1]);
        return simplified;
    }
}