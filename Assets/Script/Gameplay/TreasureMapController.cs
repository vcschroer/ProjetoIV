using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TreasureMapController : MonoBehaviour
{
    [Header("Referências")]
    [SerializeField] private TreasureMapUI mapUI;
    [SerializeField] private Transform playerStart;
    [SerializeField] private Transform chestTarget;

    [Header("Caminho Forçado (Opcional)")]
    [SerializeField] private Transform[] waypoints;

    [Header("Modo Manual por GameObjects (Fases Específicas)")]
    [SerializeField] private bool useManualPoints = false;
    [SerializeField] private Transform[] manualMapPoints;

    private void Start()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    private IEnumerator GenerateMapRoutine()
    {
        yield return null;

        if (mapUI == null)
        {
            Debug.LogWarning("TreasureMapController: Falta a referência do TreasureMapUI no Inspector!");
            yield break;
        }

        if (useManualPoints)
        {
            GenerateManualMap();
            yield break;
        }

        if (GridManager.Instance == null || playerStart == null || chestTarget == null)
        {
            Debug.LogWarning("TreasureMapController: Faltam referências no Inspector!");
            yield break;
        }

        List<Vector3Int> fullPath = new List<Vector3Int>();
        Vector2Int currentStartXZ = new Vector2Int(Mathf.RoundToInt(playerStart.position.x), Mathf.RoundToInt(playerStart.position.z));

        List<Vector2Int> allTargets = new List<Vector2Int>();

        if (waypoints != null)
        {
            foreach (var wp in waypoints)
            {
                if (wp != null)
                    allTargets.Add(new Vector2Int(Mathf.RoundToInt(wp.position.x), Mathf.RoundToInt(wp.position.z)));
            }
        }

        allTargets.Add(new Vector2Int(Mathf.RoundToInt(chestTarget.position.x), Mathf.RoundToInt(chestTarget.position.z)));

        foreach (var targetXZ in allTargets)
        {
            List<Vector3Int> segment = GridManager.Instance.Find2DPathWithHeightLimit(currentStartXZ, targetXZ);

            if (segment == null || segment.Count == 0)
            {
                Debug.LogWarning($"Não foi possível encontrar caminho no trecho até o alvo {targetXZ}!");
                yield break;
            }

            if (fullPath.Count > 0)
            {
                segment.RemoveAt(0);
            }

            fullPath.AddRange(segment);

            currentStartXZ = targetXZ;
        }

        if (fullPath.Count > 0)
        {
            mapUI.GenerateMapPath(fullPath);
            Debug.Log("Mapa do tesouro gerado com sucesso usando waypoints!");
        }
    }

    private void GenerateManualMap()
    {
        if (manualMapPoints == null || manualMapPoints.Length == 0)
        {
            Debug.LogWarning("TreasureMapController: Modo manual ativado, mas nenhum ponto foi atribuído em 'manualMapPoints'!");
            return;
        }

        List<Vector3Int> manualPath = new List<Vector3Int>();

        foreach (var point in manualMapPoints)
        {
            if (point != null)
            {
                Vector3Int gridPoint = new Vector3Int(
                    Mathf.RoundToInt(point.position.x),
                    Mathf.RoundToInt(point.position.y),
                    Mathf.RoundToInt(point.position.z)
                );
                manualPath.Add(gridPoint);
            }
        }

        if (manualPath.Count == 0)
        {
            Debug.LogWarning("TreasureMapController: Todos os pontos manuais estão nulos!");
            return;
        }

        mapUI.GenerateMapPath(manualPath);
        Debug.Log("Mapa do tesouro gerado com sucesso usando pontos manuais!");
    }
}