using UnityEngine;

public class BlockTile : MonoBehaviour
{
    [Header("Dados do Bloco")]
    public BlockData data;
    public Vector3Int gridPosition;

    private MeshRenderer meshRenderer;

    private void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
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
}