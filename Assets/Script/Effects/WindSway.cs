using UnityEngine;

public class WindSway : MonoBehaviour
{
    [Header("Configurações do Vento")]
    [SerializeField] private float windSpeed = 2f;    
    [SerializeField] private float windStrength = 3f;  
    [SerializeField] private Vector3 swayAxis = new Vector3(1, 0, 1); 

    private Quaternion initialRotation;
    private float randomOffset;

    private void Start()
    {
        initialRotation = transform.localRotation;
        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float time = (Time.time + randomOffset) * windSpeed;

        float angleX = Mathf.Sin(time) * windStrength;
        float angleZ = (Mathf.PerlinNoise(time, 0f) - 0.5f) * 2f * windStrength;

        Quaternion swayRotation = Quaternion.Euler(
            angleX * swayAxis.x,
            0f,
            angleZ * swayAxis.z
        );

        transform.localRotation = initialRotation * swayRotation;
    }
}