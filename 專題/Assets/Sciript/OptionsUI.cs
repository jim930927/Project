using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    public AudioMixer audioMixer;  // 你的 AudioMixer
    public Slider volumeSlider;

    void Start()
    {
        float value;
        audioMixer.GetFloat("MasterVolume", out value);
        volumeSlider.value = Mathf.Pow(10, value / 20);
    }

    public void OnVolumeChanged(float value)
    {
        // Slider 0~1 轉換成 dB
        audioMixer.SetFloat("MasterVolume", Mathf.Log10(value) * 20);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
