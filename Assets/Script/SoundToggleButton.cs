using UnityEngine;
using UnityEngine.UI;

public class SoundToggleButton : MonoBehaviour
{
    private FMOD.Studio.Bus masterBus;
    private bool isMuted = false;

    [Header("Opcional: Ícones do Botão")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Sprite soundOnSprite;
    [SerializeField] private Sprite soundOffSprite;

    [Header("Indicador de Mute")]
    [SerializeField] private GameObject muteIndicatorObject;

    private void Start()
    {
        masterBus = FMODUnity.RuntimeManager.GetBus("bus:/");

        UpdateUI();
    }

    public void ToggleSound()
    {
        isMuted = !isMuted;

        if (masterBus.isValid())
        {
            float targetVolume = isMuted ? 0f : 1f;
            FMOD.RESULT result = masterBus.setVolume(targetVolume);

            Debug.Log("[FMOD Debug] Volume alterado para: " + targetVolume + " | Resultado: " + result);
        }
        else
        {
            Debug.LogError("[FMOD Debug] ERRO: masterBus inválido!");
        }

        UpdateUI();
    }

    private void UpdateUI()
    {
        if (iconImage != null)
        {
            iconImage.sprite = isMuted ? soundOffSprite : soundOnSprite;
        }

        if (muteIndicatorObject != null)
        {
            muteIndicatorObject.SetActive(isMuted);
        }
    }
}