using UnityEngine;

[CreateAssetMenu(fileName = "NewBlockData", menuName = "Grid System/Block Data")]
public class BlockData : ScriptableObject
{
    public string blockName = "Grama";
    public GameObject prefab;
    public bool isWalkable = true;
    public Color debugColor = Color.green;
}