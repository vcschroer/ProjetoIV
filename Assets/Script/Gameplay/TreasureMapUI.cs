using System.Collections.Generic;
using UnityEngine;

public class TreasureMapUI : MonoBehaviour
{
    [Header("Referências da UI")]
    [SerializeField] private RectTransform mapParchment; // O painel/imagem do papel do mapa
    [SerializeField] private GameObject pathDotPrefab;  // Prefab de um pontinho (UI Image)
    [SerializeField] private GameObject startMarkPrefab; // Prefab do Início (Ex: Barco Pirata)
    [SerializeField] private GameObject xMarkPrefab;    // Prefab do ícone 'X' Final (UI Image)

    [Header("Configurações do Desenho")]
    [Tooltip("Distância exata em pixels entre o centro de cada pontinho.")]
    [SerializeField] private float dotSpacing = 22f;

    [Tooltip("Margem livre entre o Barco Inicial e o primeiro pontinho.")]
    [SerializeField] private float startIconClearance = 45f;

    [Tooltip("Margem livre entre o último pontinho e o X Final.")]
    [SerializeField] private float endIconClearance = 35f;

    [SerializeField] private float padding = 20f;        // Margem para não colar na borda
    [SerializeField] private Vector2 mapCenterOffset = Vector2.zero;

    [Header("Ajustes de Diagonais e Rotação")]
    [Tooltip("Simplifica passos em formato de 'escada' transformando-os em diagonais retas.")]
    [SerializeField] private bool smoothDiagonalSteps = true;

    [Tooltip("Ajuste se o sprite do seu pontinho estiver na vertical no Prefab (ex: 90 ou -90).")]
    [SerializeField] private float spriteRotationOffset = 0f;

    public void GenerateMapPath(List<Vector3Int> gridPath)
    {
        // 1. Limpa o mapa antigo
        foreach (Transform child in mapParchment)
        {
            Destroy(child.gameObject);
        }

        if (gridPath == null || gridPath.Count < 2) return;

        // 2. Projeta o caminho 3D para 2D (X, Z), removendo duplicatas
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

        // 3. Suaviza os zig-zags de 1 bloco em linhas diagonais diretas
        if (smoothDiagonalSteps)
        {
            path2D = SimplifyZigzags(path2D);
        }

        // 4. Encontra os limites (Mínimo e Máximo) do trajeto
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

        // 5. Calcula a escala ideal para encaixar no pergaminho
        float availableWidth = mapParchment.rect.width - (padding * 2f);
        float availableHeight = mapParchment.rect.height - (padding * 2f);

        float scaleX = availableWidth / pathWidth;
        float scaleZ = availableHeight / pathHeight;
        float scale = Mathf.Min(scaleX, scaleZ, 35f);

        Vector2 pathCenter = new Vector2((minX + maxX) / 2f, (minZ + maxZ) / 2f);

        // 6. Converte os waypoints do grid para coordenadas no Canvas
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

        // --- CÁLCULO DE DISTÂNCIA TOTAL ---
        float totalPathLength = 0f;
        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            totalPathLength += Vector2.Distance(waypoints[i], waypoints[i + 1]);
        }

        // 7. Amostra os pontinhos com distância uniforme, respeitando as áreas livres
        List<Vector2> dotPositions = new List<Vector2>();
        List<Vector2> dotDirections = new List<Vector2>();

        float accumulatedDist = 0f;

        // Começa a colocar pontos apenas DEPOIS da margem do ícone inicial
        float nextDotDist = startIconClearance;

        for (int i = 0; i < waypoints.Count - 1; i++)
        {
            Vector2 pStart = waypoints[i];
            Vector2 pEnd = waypoints[i + 1];
            float segLength = Vector2.Distance(pStart, pEnd);

            if (segLength < 0.001f) continue;

            Vector2 segDir = (pEnd - pStart).normalized;

            // O ponto precisa estar dentro do segmento atual E antes da margem do ícone final
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

        // 8. Instancia e rotaciona os pontinhos
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

        // 9. Instancia os ícones de Início e Fim (Barco e X)
        if (waypoints.Count > 0)
        {
            // Ícone Inicial
            if (startMarkPrefab != null)
            {
                GameObject spawnedStart = Instantiate(startMarkPrefab, mapParchment);
                RectTransform startRect = spawnedStart.GetComponent<RectTransform>();
                startRect.anchoredPosition = waypoints[0];
                startRect.localRotation = Quaternion.identity; // Mantém reto
            }

            // Ícone Final (X)
            if (xMarkPrefab != null)
            {
                GameObject spawnedX = Instantiate(xMarkPrefab, mapParchment);
                RectTransform xRect = spawnedX.GetComponent<RectTransform>();
                xRect.anchoredPosition = waypoints[waypoints.Count - 1];
                xRect.localRotation = Quaternion.identity; // Mantém reto
            }
        }
    }

    /// <summary>
    /// Converte passos alternados em "escada" (ex: Cima -> Direita -> Cima)
    /// em um segmento diagonal reto e contínuo.
    /// </summary>
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