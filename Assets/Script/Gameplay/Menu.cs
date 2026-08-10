using UnityEngine;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    [SerializeField] private Button botaoJogar;

    private void Start()
    {
        botaoJogar.onClick.AddListener(IniciarJogo);
    }

    private void IniciarJogo()
    {
        if (TransitionManager.Instance != null)
        {
            TransitionManager.Instance.CarregarCena("GameTest 1");
        }
    }
}
