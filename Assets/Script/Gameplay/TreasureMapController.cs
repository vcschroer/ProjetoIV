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
    [Tooltip("Coloque GameObjects vazios aqui para forçar a linha a passar por eles antes do baú.")]
    [SerializeField] private Transform[] waypoints;

    private void Start()
    {
        StartCoroutine(GenerateMapRoutine());
    }

    private IEnumerator GenerateMapRoutine()
    {
        yield return null; 

        if (GridManager.Instance == null || playerStart == null || chestTarget == null || mapUI == null)
        {
            Debug.LogWarning("TreasureMapController: Faltam referências no Inspector!");
            yield break;
        }

        List<Vector3Int> fullPath = new List<Vector3Int>();
        Vector2Int currentStartXZ = new Vector2Int(Mathf.RoundToInt(playerStart.position.x), Mathf.RoundToInt(playerStart.position.z));

        List<Vector2Int> allTargets = new List<Vector2Int>();

        foreach (var wp in waypoints)
        {
            if (wp != null)
                allTargets.Add(new Vector2Int(Mathf.RoundToInt(wp.position.x), Mathf.RoundToInt(wp.position.z)));
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
}