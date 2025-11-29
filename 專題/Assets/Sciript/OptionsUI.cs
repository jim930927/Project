using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class OptionsUI : MonoBehaviour
{
    public AudioMixer audioMixer;  // 你的 AudioMixer
    public Slider volumeSlider;
    private bool _isUpdating = false;

    private void OnEnable()
    {
        float db;

        // 從 Mixer 取值
        if (audioMixer.GetFloat("MasterVolume", out db))
        {
            float linear = Mathf.Pow(10, db / 20);

            _isUpdating = true;
            volumeSlider.SetValueWithoutNotify(linear);
            _isUpdating = false;
        }
    }

    public void OnVolumeChanged(float value)
    {
        if (_isUpdating) return;   // ← 防止遞迴 (最重要的一行)

        value = Mathf.Clamp(value, 0.0001f, 1f);

        float db = Mathf.Log10(value) * 20;

        audioMixer.SetFloat("MasterVolume", db);
    }

    public void ClosePanel()
    {
        gameObject.SetActive(false);
    }
}
