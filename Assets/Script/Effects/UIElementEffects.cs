using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class UIElementEffects : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Wobble")]
    [SerializeField] private bool usarWobble = false;

    [SerializeField] private float velocidadeWobble = 4f;

    [SerializeField] private float anguloMaximo = 8f;


    [Header("Hover Scale")]
    [SerializeField] private bool usarHoverScale = false;

    [SerializeField] private float escalaHover = 1.12f;

    [SerializeField] private float duracaoTransicaoScale = 0.1f;


    [Header("Text Wave")]
    [SerializeField] private bool usarTextWave = false;

    [SerializeField] private float velocidadeWave = 5f;

    [SerializeField] private float alturaOnda = 6f;

    [SerializeField] private float frequenciaOnda = 0.35f;


    private RectTransform rectTransform;
    private Quaternion rotacaoInicial;
    private Vector3 escalaOriginal;
    private Coroutine coroutineEscala;
    private TMP_Text textComponent;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        rotacaoInicial = rectTransform.localRotation;
        escalaOriginal = transform.localScale;

        textComponent = GetComponent<TMP_Text>();
    }

    private void Update()
    {
        if (usarWobble)
        {
            float angulo = Mathf.Sin(Time.time * velocidadeWobble) * anguloMaximo;
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, angulo);
        }
        else
        {
            rectTransform.localRotation = Quaternion.Lerp(rectTransform.localRotation, rotacaoInicial, Time.deltaTime * 10f);
        }

        if (usarTextWave && textComponent != null)
        {
            AplicarOndaNoTexto();
        }
    }

    private void AplicarOndaNoTexto()
    {
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;
            Vector3[] vertices = textInfo.meshInfo[materialIndex].vertices;

            float offsetVertical = Mathf.Sin(Time.time * velocidadeWave + i * frequenciaOnda) * alturaOnda;

            Vector3 deslocamento = new Vector3(0, offsetVertical, 0);

            vertices[vertexIndex + 0] += deslocamento;
            vertices[vertexIndex + 1] += deslocamento;
            vertices[vertexIndex + 2] += deslocamento;
            vertices[vertexIndex + 3] += deslocamento;
        }

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
            textComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!usarHoverScale) return;
        MudarEscala(escalaOriginal * escalaHover);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!usarHoverScale) return;
        MudarEscala(escalaOriginal);
    }

    private void MudarEscala(Vector3 escalaAlvo)
    {
        if (coroutineEscala != null) StopCoroutine(coroutineEscala);
        coroutineEscala = StartCoroutine(RotinaMudarEscala(escalaAlvo));
    }

    private IEnumerator RotinaMudarEscala(Vector3 escalaAlvo)
    {
        Vector3 escalaInicial = transform.localScale;
        float tempo = 0f;

        while (tempo < duracaoTransicaoScale)
        {
            tempo += Time.deltaTime;
            transform.localScale = Vector3.Lerp(escalaInicial, escalaAlvo, tempo / duracaoTransicaoScale);
            yield return null;
        }

        transform.localScale = escalaAlvo;
    }

    private void OnDisable()
    {
        if (coroutineEscala != null) StopCoroutine(coroutineEscala);

        if (rectTransform != null)
        {
            rectTransform.localRotation = rotacaoInicial;
            transform.localScale = escalaOriginal;
        }

        if (textComponent != null)
        {
            textComponent.ForceMeshUpdate();
        }
    }
}