using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ChestController : MonoBehaviour
{
    [Header("Configurações do Pulo e Giro (Juice Inicial)")]
    [SerializeField] private float jumpHeight = 1.5f;       // Altura do pulo
    [SerializeField] private float jumpDuration = 0.8f;     // Tempo total do pulo
    [SerializeField] private float totalSpins = 2f;         // Quantas voltas ele dá no ar
    [SerializeField] private float stretchAmount = 0.25f;   // O quanto ele acha e estica

    [Header("Configurações de Tamanho")]
    [SerializeField] private Vector3 finalScale = Vector3.one; // O tamanho que o baú vai ficar permanentemente após abrir

    [Header("Comportamento Pós-Abertura (Flutuar e Girar)")]
    [SerializeField] private float floatSpeed = 2f;         // Velocidade da flutuação contínua
    [SerializeField] private float floatAmplitude = 0.15f;  // Altura da flutuação (o quanto sobe e desce flutuando)
    [SerializeField] private float postSpinSpeed = 45f;     // Velocidade do giro contínuo depois de aberto

    [Header("Efeitos e Interação")]
    [SerializeField] private ParticleSystem sparkleParticles; // Referência ao sistema de partículas de brilho
    [SerializeField] private float maxInteractionDistance = 2.5f;
    [SerializeField] private Transform playerTransform;

    private bool isOpen = false;
    private bool isAnimating = false;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Update()
    {
        if (isOpen)
        {
            // Comportamento contínuo após abrir: Fica flutuando para cima e para baixo
            float newY = basePosition.y + Mathf.Sin(Time.time * floatSpeed) * floatAmplitude;
            transform.position = new Vector3(basePosition.x, newY, basePosition.z);

            // Fica girando devagar no próprio eixo
            transform.Rotate(Vector3.up * postSpinSpeed * Time.deltaTime, Space.World);
            return;
        }

        if (isAnimating) return;

        // Detecta clique do botão esquerdo OU direito do mouse
        if (Mouse.current != null &&
            (Mouse.current.leftButton.wasPressedThisFrame || Mouse.current.rightButton.wasPressedThisFrame))
        {
            TryOpenChest();
        }
    }

    private void TryOpenChest()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            // Verifica se clicou no baú ou em seus filhos
            if (hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform))
            {
                if (playerTransform != null)
                {
                    float distance = Vector3.Distance(transform.position, playerTransform.position);
                    if (distance > maxInteractionDistance) return;
                }

                StartCoroutine(ChestJuiceRoutine());
            }
        }
    }

    private IEnumerator ChestJuiceRoutine()
    {
        isAnimating = true;
        basePosition = transform.position;
        baseRotation = transform.rotation;

        // Dispara as partículas de brilho
        if (sparkleParticles != null)
        {
            sparkleParticles.Play();
        }

        float time = 0;
        Vector3 initialScale = transform.localScale;

        while (time < jumpDuration)
        {
            time += Time.deltaTime;
            float progress = time / jumpDuration;

            // 1. Movimento de Pulo Parabólico
            float heightOffset = Mathf.Sin(progress * Mathf.PI) * jumpHeight;
            transform.position = basePosition + new Vector3(0, heightOffset, 0);

            // 2. Rotação no ar baseada na quantidade de voltas configurada
            float currentYRotation = progress * 360f * totalSpins;
            transform.rotation = baseRotation * Quaternion.Euler(0, currentYRotation, 0);

            // 3. Efeito de Escala (Achatar/Esticar + Transição gradual para o tamanho final desejado)
            float scaleEffect = 1f + Mathf.Sin(progress * Mathf.PI) * stretchAmount;
            Vector3 targetScaleProgress = Vector3.Lerp(initialScale, finalScale, progress);
            transform.localScale = targetScaleProgress * scaleEffect;

            yield return null;
        }

        // Reseta rotação e define o tamanho final cravado
        transform.rotation = baseRotation;
        transform.localScale = finalScale;

        // Atualiza a posição base para o ponto onde ele terminou para a flutuação começar perfeitamente
        basePosition = transform.position;

        isAnimating = false;
        isOpen = true; // Ativa o estado flutuante e giratório contínuo

        Debug.Log("Baú aberto! Fase concluída com sucesso!");
    }
}