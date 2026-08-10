using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Referências UI")]
    [SerializeField] private Image imagemTransicao;

    [Header("Configurações")]
    [SerializeField] private float duracaoAnimacao = 0.5f;

    [SerializeField] private List<Sprite> spritesTransicao;

    private Canvas canvas;
    private bool estaOcorrendoTransicao = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            canvas = GetComponentInParent<Canvas>();
            if (canvas != null) canvas.sortingOrder = 999;
        }
        else if (Instance != this)
        {
            Destroy(transform.root.gameObject);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (Instance != this) return;

        string nomeCenaAtual = SceneManager.GetActiveScene().name;

        if (nomeCenaAtual != "Menu")
        {
            imagemTransicao.enabled = true;
            StartCoroutine(RotinaEntrada());
        }
        else
        {
            imagemTransicao.enabled = false;
        }
    }

    public void CarregarCena(string nomeDaCena)
    {
        if (estaOcorrendoTransicao) return;
        StartCoroutine(RotinaMudarCena(nomeDaCena));
    }


    private IEnumerator RotinaMudarCena(string nomeDaCena)
    {
        estaOcorrendoTransicao = true;

        yield return StartCoroutine(TocarAnimacao(reverso: false));

        AsyncOperation operacaoAsync = SceneManager.LoadSceneAsync(nomeDaCena);
        while (!operacaoAsync.isDone)
        {
            yield return null;
        }

        yield return StartCoroutine(RotinaEntrada());

        estaOcorrendoTransicao = false;
    }

    private IEnumerator RotinaEntrada()
    {
        yield return StartCoroutine(TocarAnimacao(reverso: true));

        imagemTransicao.enabled = false;
    }

    private IEnumerator TocarAnimacao(bool reverso)
    {

        imagemTransicao.enabled = true;
        int totalFrames = spritesTransicao.Count;
        float tempoPorFrame = duracaoAnimacao / totalFrames;
        float tempoDecorrido = 0f;
        int indexFrameAtual = 0;

        while (indexFrameAtual < totalFrames)
        {
            indexFrameAtual = Mathf.FloorToInt(tempoDecorrido / tempoPorFrame);

            if (indexFrameAtual < totalFrames)
            {
                int indexFinal = reverso ? (totalFrames - 1) - indexFrameAtual : indexFrameAtual;
                indexFinal = Mathf.Clamp(indexFinal, 0, totalFrames - 1);

                imagemTransicao.sprite = spritesTransicao[indexFinal];
            }

            tempoDecorrido += Time.deltaTime;
            yield return null;
        }

        int indexUltimo = reverso ? 0 : totalFrames - 1;
        imagemTransicao.sprite = spritesTransicao[indexUltimo];
    }
}