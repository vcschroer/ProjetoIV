using UnityEngine;

public class NuvemBehavior : MonoBehaviour
{
    [Header("Flutuação Vertical (Cima / Baixo)")]
    [SerializeField] private float floatSpeedY = 1.2f;       // Velocidade de subida e descida
    [SerializeField] private float floatAmplitudeY = 0.3f;   // Distância da subida e descida

    [Header("Balanço Lateral (Lado a Lado)")]
    [SerializeField] private float swaySpeedX = 0.8f;        // Velocidade do balanço lateral
    [SerializeField] private float swayAmplitudeX = 0.6f;    // Distância do movimento para os lados

    [Header("Balanço Frente / Trás (Opcional)")]
    [SerializeField] private float swaySpeedZ = 0.5f;        // Velocidade do balanço em profundidade
    [SerializeField] private float swayAmplitudeZ = 0.2f;    // Distância do movimento frente/trás

    [Header("Inclinação Suave (Tilt)")]
    [SerializeField] private bool usarInclinacao = true;
    [SerializeField] private float tiltSpeed = 0.7f;         // Velocidade com que ela tomba levemente
    [SerializeField] private float tiltAngleMax = 4f;        // Ângulo máximo de inclinação em graus

    // Posições e rotações de referência
    private Vector3 startPosition;
    private Quaternion startRotation;
    private float randomOffset;

    private void Start()
    {
        // Salva onde você colocou a nuvem na cena como ponto ancorado
        startPosition = transform.position;
        startRotation = transform.rotation;

        // Offset aleatório para que múltiplas nuvens na mesma cena não se movam idênticas
        randomOffset = Random.Range(0f, 100f);
    }

    private void Update()
    {
        float time = Time.time + randomOffset;

        // Calculando as ondas senoidais para movimentos suaves e contínuos
        float offsetY = Mathf.Sin(time * floatSpeedY) * floatAmplitudeY;
        float offsetX = Mathf.Sin(time * swaySpeedX) * swayAmplitudeX;
        float offsetZ = Mathf.Cos(time * swaySpeedZ) * swayAmplitudeZ;

        // Aplica o movimentoIdle relativo à posição inicial
        transform.position = startPosition + new Vector3(offsetX, offsetY, offsetZ);

        // Inclina a nuvem levemente para acompanhar o balanço
        if (usarInclinacao)
        {
            float tiltZ = Mathf.Sin(time * tiltSpeed) * tiltAngleMax;
            float tiltX = Mathf.Cos(time * tiltSpeed * 0.8f) * (tiltAngleMax * 0.5f);

            transform.rotation = startRotation * Quaternion.Euler(tiltX, 0f, tiltZ);
        }
    }
}