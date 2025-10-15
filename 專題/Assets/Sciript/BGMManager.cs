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
            audioSource.clip = clip;
            audioSource.loop = true;
            audioSource.Play();
        }
        else
        {
            Debug.LogWarning($"⚠️ 找不到音樂：{name}");
        }
    }
}
