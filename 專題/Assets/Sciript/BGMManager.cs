using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] bgmClips;

    public void PlayMusic(string name)
    {
        AudioClip clip = System.Array.Find(bgmClips, c => c.name == name);
        if (clip != null)
        {
            Debug.LogWarning("播放音樂：");
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到音樂：{name}");
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Pause();
    }

    // 🟢 新增：繼續音樂
    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
            audioSource.UnPause();
    }
}
