using System.Collections;
using UnityEngine;

public class WindParticleSpawner : MonoBehaviour
{
    [Header("Prefabs de Vento")]
    [SerializeField] private GameObject[] prefabsVento;

    [Header("Configurações de Spawn Inicial")]
    [SerializeField] private int quantidadeInicial = 10;

    [Header("Configurações de Tempo Contínuo")]
    [SerializeField] private float tempoMinimo = 1.5f;

    [SerializeField] private float tempoMaximo = 3.5f;

    [SerializeField] private float tempoDeVidaParticula = 5f;

    [Header("Configurações de Rotação / Direção")]
    [SerializeField] private bool usarRotacaoAleatoria = true;

    [Tooltip("Ângulo Y mínimo")]
    [SerializeField] private float anguloMinimoY = 0f;

    [Tooltip("Ângulo Y máximo")]
    [SerializeField] private float anguloMaximoY = 360f;

    [Header("Gizmos")]
    [SerializeField] private Vector3 tamanhoArea = new Vector3(10f, 5f, 10f);

    [SerializeField] private Vector3 offsetArea = Vector3.zero;

    [SerializeField] private Color corGizmo = new Color(0f, 0.8f, 1f, 0.8f);

    private Coroutine corrotinaSpawn;

    private void Start()
    {
        SpawnParticulasIniciais();
    }

    private void OnEnable()
    {
        corrotinaSpawn = StartCoroutine(RotinaSpawn());
    }

    private void OnDisable()
    {
        if (corrotinaSpawn != null)
        {
            StopCoroutine(corrotinaSpawn);
        }
    }

    private void SpawnParticulasIniciais()
    {
        for (int i = 0; i < quantidadeInicial; i++)
        {
            SpawnParticulaVento();
        }
    }

    private IEnumerator RotinaSpawn()
    {
        while (true)
        {
            float tempoEspera = Random.Range(tempoMinimo, tempoMaximo);
            yield return new WaitForSeconds(tempoEspera);

            SpawnParticulaVento();
        }
    }

    private void SpawnParticulaVento()
    {
        if (prefabsVento == null || prefabsVento.Length == 0) return;

        int indexSorteado = Random.Range(0, prefabsVento.Length);
        GameObject prefabSorteado = prefabsVento[indexSorteado];

        if (prefabSorteado == null) return;

        Vector3 posicaoAleatoria = transform.position + offsetArea + new Vector3(
            Random.Range(-tamanhoArea.x / 2f, tamanhoArea.x / 2f),
            Random.Range(-tamanhoArea.y / 2f, tamanhoArea.y / 2f),
            Random.Range(-tamanhoArea.z / 2f, tamanhoArea.z / 2f)
        );

        Quaternion rotacaoFinal = transform.rotation;

        if (usarRotacaoAleatoria)
        {
            float anguloYSorteado = Random.Range(anguloMinimoY, anguloMaximoY);
            rotacaoFinal = Quaternion.Euler(0f, anguloYSorteado, 0f);
        }

        GameObject ventoInstanciado = Instantiate(prefabSorteado, posicaoAleatoria, rotacaoFinal);

        Destroy(ventoInstanciado, tempoDeVidaParticula);
    }

    private void OnDrawGizmos()
    {
        Vector3 centro = transform.position + offsetArea;

        Gizmos.color = corGizmo;
        Gizmos.DrawWireCube(centro, tamanhoArea);

        Color corPreenchimento = corGizmo;
        corPreenchimento.a = 0.15f;
        Gizmos.color = corPreenchimento;
        Gizmos.DrawCube(centro, tamanhoArea);
    }
}