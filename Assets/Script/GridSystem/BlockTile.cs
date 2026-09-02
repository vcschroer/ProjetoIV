using UnityEngine;

public class BlockTile : MonoBehaviour
{
    [Header("Dados do Bloco")]
    public BlockData data;
    public Vector3Int gridPosition;

    private MeshRenderer meshRenderer;

    [Header("Obstáculos / Altura")]
    public int maxAllowedHeight = 0;
    public bool isWaterTile = false;
    public bool isQuicksand = false;

    [Header("Marcação Visual")]
    [SerializeField] private GameObject outlineObject;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();

        if (outlineObject != null)
        {
            outlineObject.SetActive(false);
        }
    }

    private void Start()
    {
        gridPosition = Vector3Int.RoundToInt(transform.position);
        ApplyColorFromData();
    }

    public void Setup(Vector3Int pos, BlockData blockData)
    {
        gridPosition = pos;
        data = blockData;
        ApplyColorFromData();
    }

    public void ApplyColorFromData()
    {
        if (data != null && meshRenderer != null)
        {
            meshRenderer.material.color = data.debugColor;
        }
    }

    public void SetHighlight(bool state)
    {
        if (outlineObject != null)
        {
            if (state && TreasureMapUI.Instance != null && TreasureMapUI.Instance.IsExpanded)
            {
                outlineObject.SetActive(false);
                return;
            }

            outlineObject.SetActive(state);
        }
    }
}