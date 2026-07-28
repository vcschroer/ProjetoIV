using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PirateStackManager : MonoBehaviour
{
    [Header("Configurações da Pilha")]
    [Tooltip("Piratas que estão acima do líder (do meio para o topo).")]
    [SerializeField] private List<GameObject> stackedPirates = new List<GameObject>();
    [SerializeField] private float stepHeight = 1.0f;
    [SerializeField] private float jumpDuration = 0.25f;

    [Header("Configurações do Bloco Solto")]
    [SerializeField] private BlockData defaultWalkableData;
    [SerializeField] private string tileLayerName = "Tile";

    private bool isBusy = false;

    private void Update()
    {
        if (isBusy) return;

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            HandleRightClick();
        }
    }

    private void LateUpdate()
    {
        if (!isBusy)
        {
            MaintainStackPositions();
        }
    }

    private void MaintainStackPositions()
    {
        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                Vector3 targetPos = transform.position + new Vector3(0, (i + 1) * stepHeight, 0);
                stackedPirates[i].transform.position = targetPos;
                stackedPirates[i].transform.rotation = transform.rotation;
            }
        }
    }

    public void OnPlayerStep()
    {
        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                PirateJuice juice = stackedPirates[i].GetComponentInChildren<PirateJuice>();
                if (juice != null)
                {
                    float delay = (i + 1) * 0.04f;
                    juice.TriggerBounce(delay);
                }
            }
        }
    }

    public void TriggerStackImpact(bool topToBottom = true)
    {
        PirateJuice baseJuice = GetComponentInChildren<PirateJuice>();
        if (baseJuice != null)
        {
            float baseDelay = topToBottom ? stackedPirates.Count * 0.04f : 0f;
            baseJuice.TriggerDropSquash(baseDelay);
        }

        for (int i = 0; i < stackedPirates.Count; i++)
        {
            if (stackedPirates[i] != null)
            {
                PirateJuice juice = stackedPirates[i].GetComponentInChildren<PirateJuice>();
                if (juice != null)
                {
                    int hierarchyIndex = topToBottom ? (stackedPirates.Count - 1 - i) : (i + 1);
                    float delay = hierarchyIndex * 0.04f;
                    juice.TriggerDropSquash(delay);
                }
            }
        }
    }

    private void HandleRightClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            GameObject hitObject = hit.collider.gameObject;

            if (hitObject.CompareTag("DroppedPirate") || (hitObject.transform.parent != null && hitObject.transform.parent.CompareTag("DroppedPirate")))
            {
                GameObject rootPirate = hitObject.CompareTag("DroppedPirate") ? hitObject : hitObject.transform.parent.gameObject;
                TryPickupPirate(rootPirate);
                return;
            }

            BlockTile clickedTile = hitObject.GetComponentInParent<BlockTile>();
            if (clickedTile != null)
            {
                TryDropPirate(clickedTile);
            }
        }
    }

    #region --- RECOLHER PIRATA ---

    private void TryPickupPirate(GameObject targetPirate)
    {
        PirateIdentity identity = targetPirate.GetComponent<PirateIdentity>();
        if (identity != null)
        {
            int expectedID = stackedPirates.Count + 1;
            if (identity.pirateID != expectedID)
            {
                Debug.Log($"[Totem] Ordem incorreta! Pegue o Pirata {expectedID} primeiro.");
                return;
            }
        }

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int piratePos = Vector3Int.RoundToInt(targetPirate.transform.position);

        int distanceX = Mathf.Abs(playerPos.x - piratePos.x);
        int distanceZ = Mathf.Abs(playerPos.z - piratePos.z);

        if (distanceX + distanceZ == 1)
        {
            StartCoroutine(PickupRoutine(targetPirate));
        }
        else
        {
            Debug.Log("[Totem] Chegue mais perto para pegar o pirata!");
        }
    }

    private IEnumerator PickupRoutine(GameObject pirate)
    {
        isBusy = true;

        // Dispara o Stretch ao ser puxado do chão
        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            juice.TriggerPickupStretch();
        }

        BlockTile tileScript = pirate.GetComponent<BlockTile>();
        if (tileScript != null)
        {
            tileScript.enabled = false;
        }

        pirate.tag = "Untagged";
        SetLayerRecursively(pirate, LayerMask.NameToLayer("Default"));

        Vector3 startPos = pirate.transform.position;
        int targetIndex = stackedPirates.Count;
        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 topTargetPos = transform.position + new Vector3(0, (targetIndex + 1) * stepHeight, 0);
            Vector3 currentPos = Vector3.Lerp(startPos, topTargetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            pirate.transform.position = currentPos;
            yield return null;
        }

        stackedPirates.Add(pirate);

        TriggerStackImpact(topToBottom: true);

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    #endregion

    #region --- SOLTAR PIRATA ---

    private void TryDropPirate(BlockTile targetTile)
    {
        if (stackedPirates.Count == 0) return;

        Vector3Int playerPos = Vector3Int.RoundToInt(transform.position);
        Vector3Int groundGridPos = targetTile.gridPosition;

        if (GridManager.Instance != null)
        {
            BlockTile tileOnGround = GridManager.Instance.GetTileAt(groundGridPos);
            if (tileOnGround == null)
            {
                Debug.LogWarning("[Totem] Não há bloco de chão nesta posição!");
                return;
            }

            Vector3Int airPosAbove = groundGridPos + Vector3Int.up;
            if (GridManager.Instance.GetTileAt(airPosAbove) != null)
            {
                Debug.LogWarning("[Totem] Posição acima já está ocupada por um bloco!");
                return;
            }
        }

        int distanceX = Mathf.Abs(playerPos.x - groundGridPos.x);
        int distanceZ = Mathf.Abs(playerPos.z - groundGridPos.z);

        if (distanceX + distanceZ == 1)
        {
            int topIndex = stackedPirates.Count - 1;
            GameObject pirateToDrop = stackedPirates[topIndex];
            stackedPirates.RemoveAt(topIndex);

            StartCoroutine(DropRoutine(pirateToDrop, groundGridPos));
        }
        else
        {
            Debug.Log("[Totem] Você precisa estar ao lado do bloco para soltar o pirata!");
        }
    }

    private IEnumerator DropRoutine(GameObject pirate, Vector3Int targetGridPos)
    {
        isBusy = true;

        Vector3 startPos = pirate.transform.position;
        Vector3Int finalGridPos = new Vector3Int(targetGridPos.x, targetGridPos.y + 1, targetGridPos.z);
        Vector3 targetPos = new Vector3(finalGridPos.x, finalGridPos.y, finalGridPos.z);
        float time = 0;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            Vector3 currentPos = Vector3.Lerp(startPos, targetPos, progress);
            currentPos.y += Mathf.Sin(progress * Mathf.PI) * 0.8f;

            pirate.transform.position = currentPos;
            yield return null;
        }

        pirate.transform.position = targetPos;
        pirate.transform.rotation = Quaternion.identity;

        PirateJuice juice = pirate.GetComponentInChildren<PirateJuice>();
        if (juice != null)
        {
            juice.TriggerDropSquash();
        }

        pirate.tag = "DroppedPirate";

        int tileLayerIndex = LayerMask.NameToLayer(tileLayerName);
        if (tileLayerIndex != -1)
        {
            SetLayerRecursively(pirate, tileLayerIndex);
        }

        BoxCollider col = pirate.GetComponent<BoxCollider>();
        if (col == null) col = pirate.AddComponent<BoxCollider>();
        col.enabled = true;
        col.size = Vector3.one;
        col.center = Vector3.zero;

        BlockTile tile = pirate.GetComponent<BlockTile>();
        if (tile == null) tile = pirate.AddComponent<BlockTile>();

        tile.enabled = true;

        if (tile.data == null)
        {
            tile.data = defaultWalkableData;
        }

        tile.Setup(finalGridPos, tile.data);

        if (GridManager.Instance != null)
        {
            GridManager.Instance.ScanGrid();
        }

        isBusy = false;
    }

    #endregion

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;

        foreach (Transform child in obj.transform)
        {
            if (child != null)
            {
                SetLayerRecursively(child.gameObject, newLayer);
            }
        }
    }
}