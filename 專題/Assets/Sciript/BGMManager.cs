using UnityEngine;

public class BGMManager : MonoBehaviour
{
    public AudioSource audioSource;
    public AudioClip[] bgmClips;

    private void Awake()
    {
        //DontDestroyOnLoad(gameObject);

        // 若已有 BGMManager 存在，刪掉後來的
        var objs = FindObjectsOfType<BGMManager>();
        if (objs.Length > 1)
        {
            Destroy(gameObject);
            return;
        }

        Debug.Log("🎧 BGMManager 啟動（唯一實例）");
    }

    public void PlayMusic(string name)
    {
        Debug.Log($"🎯 Step4: PlayMusic() 被呼叫，clipName = {name}");

        if (audioSource == null)
        {
            Debug.LogError("❌ Step4: audioSource == NULL，無法播放音樂！");
            return;
        }

        // 嘗試抓取音檔
        AudioClip clip = System.Array.Find(bgmClips, c => c != null && c.name == name);

        if (clip == null)
        {
            Debug.LogError($"❌ Step4: 找不到音樂檔案：{name}（確認 bgmClips 有沒有放這個 Clip）");
            return;
        }

        // 顯示 debug 訊息
        Debug.Log($"🎵 Step4: 找到音樂：{clip.name}，開始播放！");

        // 播放
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.Play();

        if (!audioSource.isPlaying)
        {
            Debug.LogError("❌ Step4: audioSource.Play() 執行後仍未播放，可能音量=0 或 Mute=true！");
        }
    }

    public void PauseMusic()
    {
        if (audioSource != null && audioSource.isPlaying)
        {
            audioSource.Pause();
            Debug.Log("⏸ 已暫停音樂");
        }
    }

    public void ResumeMusic()
    {
        if (audioSource != null && !audioSource.isPlaying)
        {
            audioSource.UnPause();
            Debug.Log("▶️ 已繼續播放音樂");
        }
    }
}
