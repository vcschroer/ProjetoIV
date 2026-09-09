using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Referências da Transição")]
    [Tooltip("O GameObject pai 'chapeu' que possui o componente Mask e o script CutoutMaskUI.")]
    [SerializeField] private GameObject objetoChapeuTransicao;

    [Header("Configurações da Animação")]
    [Tooltip("Duração da transição (em segundos) para entrada e saída.")]
    [SerializeField] private float duracaoAnimacao = 0.8f;

    [Tooltip("Tempo (em segundos) que a tela fica travada na escala mínima (fechada) antes de abrir.")]
    [SerializeField] private float tempoEsperaPreAbertura = 1.0f;

    [Tooltip("Escala mínima do chapéu (tamanho do furo quando fechado/início).")]
    [SerializeField] private float escalaMinima = 0f;

    [Tooltip("Escala máxima do chapéu (tamanho suficiente para cobrir a tela inteira quando aberto).")]
    [SerializeField] private float escalaMaxima = 15f;

    private CanvasGroup canvasGroupTransition;
    private bool estaOcorrendoTransicao = false;

    private void Awake()
    {
        // Define a instância local para esta cena
        Instance = this;

        ConfigurarComponentes();

        // Toda cena nasce com a tela 100% FECHADA no primeiro frame (escala mínima)
        // para esconder o carregamento e a geração do mapa do tesouro.
        ConfigurarEstado(escalaMinima, 1f);
    }

    private IEnumerator Start()
    {
        Debug.Log($"[TransitionManager] Cena iniciada. Tela fechada. Aguardando {tempoEsperaPreAbertura}s para o mapa/jogo estabilizar...");

        // 1. Aguarda o tempo configurado para a cena e gerador de mapa terminarem de carregar
        yield return new WaitForSecondsRealtime(tempoEsperaPreAbertura);

        Debug.Log("[TransitionManager] Abrindo a tela...");
        // 2. Executa a animação de ABERTURA (de fechado para aberto)
        yield return StartCoroutine(TocarAnimacaoEscala(escalaMinima, escalaMaxima));

        // 3. Esconde a UI para liberar os cliques do jogador
        ConfigurarEstado(escalaMaxima, 0f);
        Debug.Log("[TransitionManager] Tela aberta e transição finalizada!");
    }

    public void CarregarCena(string nomeDaCena)
    {
        if (estaOcorrendoTransicao) return;
        StartCoroutine(RotinaMudarCena(nomeDaCena));
    }

    private IEnumerator RotinaMudarCena(string nomeDaCena)
    {
        estaOcorrendoTransicao = true;

        // 1. Reativa a transição na escala máxima
        ConfigurarEstado(escalaMaxima, 1f);

        Debug.Log($"[TransitionManager] Fechando a tela para carregar '{nomeDaCena}'...");
        // 2. Anima do tamanho máximo para o mínimo (fecha a tela)
        yield return StartCoroutine(TocarAnimacaoEscala(escalaMaxima, escalaMinima));

        // 3. Carrega a próxima cena.
        // A nova cena terá o seu próprio TransitionManager que nascerá FECHADO no Awake()
        // e fará a abertura no Start() de forma perfeita!
        SceneManager.LoadScene(nomeDaCena);
    }

    private void ConfigurarComponentes()
    {
        if (objetoChapeuTransicao != null && canvasGroupTransition == null)
        {
            canvasGroupTransition = objetoChapeuTransicao.GetComponent<CanvasGroup>();
            if (canvasGroupTransition == null)
            {
                canvasGroupTransition = objetoChapeuTransicao.AddComponent<CanvasGroup>();
            }
        }
    }

    private void ConfigurarEstado(float escala, float alpha)
    {
        if (objetoChapeuTransicao == null) return;

        objetoChapeuTransicao.SetActive(true);
        objetoChapeuTransicao.transform.localScale = new Vector3(escala, escala, 1f);

        if (canvasGroupTransition != null)
        {
            canvasGroupTransition.alpha = alpha;
            canvasGroupTransition.blocksRaycasts = (alpha > 0f);
        }
    }

    private IEnumerator TocarAnimacaoEscala(float escalaInicial, float escalaFinal)
    {
        if (objetoChapeuTransicao == null) yield break;

        Transform chapeuTransform = objetoChapeuTransicao.transform;
        float tempoDecorrido = 0f;

        chapeuTransform.localScale = new Vector3(escalaInicial, escalaInicial, 1f);

        while (tempoDecorrido < duracaoAnimacao)
        {
            // Limita o delta time por frame a no máximo 0.05s para impedir saltos
            // caso a geração do mapa trave o processador brevemente.
            float deltaTime = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            tempoDecorrido += deltaTime;

            float t = Mathf.Clamp01(tempoDecorrido / duracaoAnimacao);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float escalaAtual = Mathf.Lerp(escalaInicial, escalaFinal, smoothT);
            chapeuTransform.localScale = new Vector3(escalaAtual, escalaAtual, 1f);

            yield return null;
        }

        chapeuTransform.localScale = new Vector3(escalaFinal, escalaFinal, 1f);
    }
}