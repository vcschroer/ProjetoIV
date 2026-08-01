using UnityEngine;

public class CursorManager : MonoBehaviour
{
    public static CursorManager Instance { get; private set; }

    [System.Serializable]
    public struct CursorData
    {
        public Texture2D texture;
        public Vector2 hotSpot;
    }

    [Header("Cursores do Jogo")]
    public CursorData defaultCursor;          
    public CursorData validPlacementCursor;   
    public CursorData pickupPirateCursor;     
    public CursorData invalidPlacementCursor; 
    public CursorData orbitCameraCursor;   
    private Texture2D currentTexture;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        SetCursor(defaultCursor);
    }

    public void SetCursorType(CursorState state)
    {
        switch (state)
        {
            case CursorState.OrbitingCamera:
                SetCursor(orbitCameraCursor);
                break;
            case CursorState.PickupPirate:
                SetCursor(pickupPirateCursor);
                break;
            case CursorState.ValidPlacement:
                SetCursor(validPlacementCursor);
                break;
            case CursorState.InvalidPlacement:
                SetCursor(invalidPlacementCursor);
                break;
            case CursorState.WalkToTile:
            case CursorState.Default:
            default:
                SetCursor(defaultCursor);
                break;
        }
    }

    private void SetCursor(CursorData data)
    {
        if (data.texture != currentTexture)
        {
            currentTexture = data.texture;
            Cursor.SetCursor(data.texture, data.hotSpot, CursorMode.Auto);
        }
    }
}

public enum CursorState
{
    Default,
    ValidPlacement,
    PickupPirate,
    InvalidPlacement,
    OrbitingCamera,
    WalkToTile
}