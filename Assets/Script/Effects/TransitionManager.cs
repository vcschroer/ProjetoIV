using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class TransitionManager : MonoBehaviour
{
    public static TransitionManager Instance { get; private set; }

    [Header("Referências da Transição")]
    [Tooltip("O GameObject pai que cobre a tela inteira (com a cor preta e o componente Mask).")]
    [SerializeField] private GameObject painelPretoTransicao;

    [Tooltip("O RectTransform da imagem que contém o sprite do chapéu (filho do painel).")]
    [SerializeField] private RectTransform imagemChapeu;

    [Header("Configurações da Animação")]
    [SerializeField] private float duracaoAnimacao = 0.8f;
    [SerializeField] private float escalaMaxima = 15f; // Tamanho suficiente para cobrir a tela inteira

    private Canvas canvas;
    private bool estaOcorrendoTransicao = false;

    private void Awake()
    {
        Debug.Log("[TransitionManager] Awake iniciado.");

        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(transform.root.gameObject);

            // Configura o Canvas pai para ficar na camada mais alta
            canvas = GetComponentInChildren<Canvas>();
            if (canvas == null)
                canvas = GetComponentInParent<Canvas>();

            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 999;
                Debug.Log("[TransitionManager] Canvas configurado com SortingOrder = 999.");
            }
            else
            {
                Debug.LogWarning("[TransitionManager] Nenhum Canvas encontrado para a transição!");
            }
        }
        else if (Instance != this)
        {
            Debug.Log("[TransitionManager] Instância duplicada encontrada e destruída.");
            Destroy(transform.root.gameObject);
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        if (Instance != this) return;

        string nomeCenaAtual = SceneManager.GetActiveScene().name;
        Debug.Log($"[TransitionManager] Start executado na cena: '{nomeCenaAtual}'");

        if (nomeCenaAtual != "Menu")
        {
            Debug.Log("[TransitionManager] Cena diferente de 'Menu'. Iniciando transição de entrada...");
            if (painelPretoTransicao != null) painelPretoTransicao.SetActive(true);
            StartCoroutine(RotinaEntrada());
        }
        else
        {
            Debug.Log("[TransitionManager] Cena é 'Menu'. Desativando painel de transição.");
            if (painelPretoTransicao != null) painelPretoTransicao.SetActive(false);
        }
    }

    public void CarregarCena(string nomeDaCena)
    {
        Debug.Log($"[TransitionManager] Chamada para CarregarCena('{nomeDaCena}').");

        if (estaOcorrendoTransicao)
        {
            Debug.LogWarning("[TransitionManager] Bloqueado: Já existe uma transição em andamento!");
            return;
        }

        StartCoroutine(RotinaMudarCena(nomeDaCena));
    }

    private IEnumerator RotinaMudarCena(string nomeDaCena)
    {
        estaOcorrendoTransicao = true;
        Debug.Log("[TransitionManager] Iniciando animação de saída (Fechando a tela)...");

        // Fechar a tela: vai do tamanho máximo até zero
        yield return StartCoroutine(TocarAnimacaoEscala(escalaMaxima, 0f));

        Debug.Log($"[TransitionManager] Tela fechada. Carregando a cena assincronamente: '{nomeDaCena}'...");
        AsyncOperation operacaoAsync = SceneManager.LoadSceneAsync(nomeDaCena);

        while (!operacaoAsync.isDone)
        {
            yield return null;
        }

        Debug.Log($"[TransitionManager] Cena '{nomeDaCena}' carregada com sucesso. Iniciando transição de entrada...");
        yield return StartCoroutine(RotinaEntrada());

        estaOcorrendoTransicao = false;
        Debug.Log("[TransitionManager] Troca de cena finalizada.");
    }

    private IEnumerator RotinaEntrada()
    {
        Debug.Log("[TransitionManager] Executando RotinaEntrada (Abrindo a tela)...");
        if (painelPretoTransicao != null) painelPretoTransicao.SetActive(true);

        // Abrir a tela: vai de zero até o tamanho máximo
        yield return StartCoroutine(TocarAnimacaoEscala(0f, escalaMaxima));

        if (painelPretoTransicao != null) painelPretoTransicao.SetActive(false);
        Debug.Log("[TransitionManager] Tela totalmente aberta. Painel desativado.");
    }

    private IEnumerator TocarAnimacaoEscala(float escalaInicial, float escalaFinal)
    {
        if (imagemChapeu == null)
        {
            Debug.LogError("[TransitionManager] ERRO CRÍTICO: 'imagemChapeu' não está atribuída no Inspector!");
            yield break;
        }

        float tempoDecorrido = 0f;

        while (tempoDecorrido < duracaoAnimacao)
        {
            tempoDecorrido += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(tempoDecorrido / duracaoAnimacao);
            float smoothT = Mathf.SmoothStep(0f, 1f, t);

            float escalaAtual = Mathf.Lerp(escalaInicial, escalaFinal, smoothT);
            imagemChapeu.localScale = new Vector3(escalaAtual, escalaAtual, 1f);

            yield return null;
        }

        imagemChapeu.localScale = new Vector3(escalaFinal, escalaFinal, 1f);
    }
}