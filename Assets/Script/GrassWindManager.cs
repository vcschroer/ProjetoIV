using System.Collections.Generic;
using UnityEngine;

public class GrassWindManager : MonoBehaviour
{
    private class GrassInstance
    {
        public Transform transform;
        public Quaternion initialLocalRotation;
        public Vector3 initialLocalPosition;
        public Vector3 initialLocalScale;
        public float phaseOffset;
        public float pivotToBottomHeight;
        public Vector3 currentBendEuler;
        public float currentSquash;
    }

    [Header("Busca na Cena")]
    [SerializeField] private string grassTag = "grass";
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private Transform playerTransform;

    [Header("Configurações do Vento")]
    [SerializeField] private float windSpeed = 2.2f;
    [SerializeField] private float windAngleX = 7f;
    [SerializeField] private float windAngleZ = 10f;
    [SerializeField] private float windWaveScale = 0.75f;

    [Header("Interação com o Player")]
    [SerializeField] private float trampleRadius = 0.75f;
    [SerializeField] private float maxHeightDifference = 1.2f;
    [SerializeField] private float trampleBendAngle = 52f;
    [SerializeField] private float trampleSquashY = 0.25f;
    [SerializeField] private float bendDownSpeed = 16f;
    [SerializeField] private float recoverSpeed = 5.5f;

    [Header("Compensação de Pivô")]
    [SerializeField] private bool anchorBaseToGround = true;

    private readonly List<GrassInstance> grasses = new List<GrassInstance>();

    private void Start()
    {
        FindPlayerByTag();
        CollectGrasses();
    }

    private void FindPlayerByTag()
    {
        if (playerTransform != null) return;

        try
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag(playerTag);
            if (playerObj != null)
            {
                playerTransform = playerObj.transform;
            }
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[GrassWindManager] A tag '{playerTag}' não existe no Tag Manager da Unity.");
        }
    }

    public void CollectGrasses()
    {
        grasses.Clear();

        GameObject[] foundObjects;
        try
        {
            foundObjects = GameObject.FindGameObjectsWithTag(grassTag);
        }
        catch (UnityException)
        {
            Debug.LogWarning($"[GrassWindManager] A tag '{grassTag}' não existe no Tag Manager da Unity.");
            return;
        }

        foreach (GameObject obj in foundObjects)
        {
            if (obj == null) continue;

            Transform t = obj.transform;
            float pivotOffset = 0f;

            if (anchorBaseToGround)
            {
                Renderer rend = obj.GetComponentInChildren<Renderer>();
                if (rend != null)
                {
                    pivotOffset = Mathf.Max(0f, t.position.y - rend.bounds.min.y);
                }
            }

            float spatialPhase = (t.position.x + t.position.z) * windWaveScale + Random.Range(-0.35f, 0.35f);

            grasses.Add(new GrassInstance
            {
                transform = t,
                initialLocalRotation = t.localRotation,
                initialLocalPosition = t.localPosition,
                initialLocalScale = t.localScale,
                phaseOffset = spatialPhase,
                pivotToBottomHeight = pivotOffset,
                currentBendEuler = Vector3.zero,
                currentSquash = 0f
            });
        }
    }

    private void Update()
    {
        if (grasses.Count == 0) return;

        if (playerTransform == null)
        {
            FindPlayerByTag();
        }

        float time = Time.time * windSpeed;
        bool hasPlayer = playerTransform != null;
        Vector3 playerPos = hasPlayer ? playerTransform.position : Vector3.zero;

        for (int i = grasses.Count - 1; i >= 0; i--)
        {
            GrassInstance g = grasses[i];
            if (g.transform == null)
            {
                grasses.RemoveAt(i);
                continue;
            }

            Vector3 grassWorldPos = g.transform.position;

            Vector3 targetBendEuler = Vector3.zero;
            float targetSquash = 0f;
            bool isTrampled = false;

            if (hasPlayer)
            {
                float verticalDiff = Mathf.Abs(playerPos.y - grassWorldPos.y);
                if (verticalDiff <= maxHeightDifference)
                {
                    Vector3 awayFromPlayer = grassWorldPos - playerPos;
                    awayFromPlayer.y = 0f;
                    float dist = awayFromPlayer.magnitude;

                    if (dist < trampleRadius)
                    {
                        isTrampled = true;
                        float factor = 1f - Mathf.Clamp01(dist / trampleRadius);
                        factor = Mathf.SmoothStep(0f, 1f, factor);

                        Vector3 pushDir = dist > 0.001f ? (awayFromPlayer / dist) : Vector3.forward;

                        Vector3 localPushDir = g.transform.parent != null
                            ? g.transform.parent.InverseTransformDirection(pushDir)
                            : pushDir;

                        float bendX = localPushDir.z * trampleBendAngle * factor;
                        float bendZ = -localPushDir.x * trampleBendAngle * factor;

                        targetBendEuler = new Vector3(bendX, 0f, bendZ);
                        targetSquash = trampleSquashY * factor;
                    }
                }
            }

            float transitionSpeed = isTrampled ? bendDownSpeed : recoverSpeed;
            g.currentBendEuler = Vector3.Lerp(g.currentBendEuler, targetBendEuler, transitionSpeed * Time.deltaTime);
            g.currentSquash = Mathf.Lerp(g.currentSquash, targetSquash, transitionSpeed * Time.deltaTime);

            float windDampen = 1f - Mathf.Clamp01(g.currentSquash / Mathf.Max(0.01f, trampleSquashY));
            float wavePrimary = Mathf.Sin(time + g.phaseOffset);
            float waveSecondary = Mathf.Cos(time * 0.7f + g.phaseOffset * 1.3f);

            float windRotX = waveSecondary * windAngleX * windDampen;
            float windRotZ = wavePrimary * windAngleZ * windDampen;

            Quaternion windAndBendRot = Quaternion.Euler(
                g.currentBendEuler.x + windRotX,
                0f,
                g.currentBendEuler.z + windRotZ
            );

            g.transform.localRotation = windAndBendRot * g.initialLocalRotation;

            Vector3 scale = g.initialLocalScale;
            scale.y = g.initialLocalScale.y * (1f - g.currentSquash);
            g.transform.localScale = scale;

            if (anchorBaseToGround && g.pivotToBottomHeight > 0.001f)
            {
                float tiltAngleRad = Quaternion.Angle(Quaternion.identity, windAndBendRot) * Mathf.Deg2Rad;
                float sinkCompensation = g.pivotToBottomHeight * (1f - Mathf.Cos(tiltAngleRad));
                g.transform.localPosition = g.initialLocalPosition - new Vector3(0f, sinkCompensation, 0f);
            }
        }
    }
}