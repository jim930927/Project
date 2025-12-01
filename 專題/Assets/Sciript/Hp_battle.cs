using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class Hp_battle : MonoBehaviour
{
    public int Bhp = 5;

    [Header("血量 UI")]
    public Image hpImage;
    public string hpImageName = "HPImage";

    public Sprite hp_5;
    public Sprite hp_4;
    public Sprite hp_3;
    public Sprite hp_2;
    public Sprite hp_1;
    public Sprite hp_0;

    public string SceneName = "FightAgain";

    [Header("特效設定")]
    public Image blackScreen; // 指向黑屏 UI 物件
    public GameObject blackScreenAct;
    public float blackScreenDuration = 1f; // 黑屏持續時間

    public bool hasShownHP = true;

    void Awake()
    {
        StartCoroutine(DelayedFindUI());
    }

    void Start()
    {
        StartCoroutine(InitUI());
        StartCoroutine(DelayedFindUI());
        blackScreenAct.SetActive(false);
    }

    void Update()
    {
        UpdateHpUI();
        if (Bhp <= 0)
        {
            TriggerFinalFailDialogue();
        }

    }

    void UpdateHpUI()
    {
        if (hpImage == null) return;

        if (Bhp == 5)
            hpImage.sprite = hp_5;
        else if (Bhp == 4)
            hpImage.sprite = hp_4;
        else if (Bhp == 3)
            hpImage.sprite = hp_3;
        else if (Bhp == 2)
            hpImage.sprite = hp_2;
        else if (Bhp == 1)
            hpImage.sprite = hp_1;

    }

    void TriggerFinalFailDialogue()
    {
        // 防止重複觸發
        if (Bhp != 0) return;

        Debug.Log("💀 HP 歸零，進入 FAILED 對話");

        var dm = FinalBattleDialogueManager.Instance;
        if (dm != null)
        {
            var hp = FindObjectOfType<HP>();
            if (hp != null) hp.hp -= 1;


            // 這裡假設你的 Ink 裡有 Knot: "FAILED"
            dm.EnterDialogueMode(dm.inkJSON, "FAILED", () =>
            {
                // 這個 callback 是對話結束後要做的事
                SceneManager.LoadScene(SceneName);
            });
        }
        else
        {
            Debug.LogWarning("⚠ 找不到 FinalBattleDialogueManager，改直接回主選單");
            SceneManager.LoadScene(SceneName);
        }

        Bhp = -999; // 防止 Update() 無限觸發
    }

    IEnumerator DelayedFindUI()
    {
        yield return new WaitForSeconds(0.1f); // 等待新場景的 UI 初始化
        FindHPUI();

        UpdateHpUI();

        if (hasShownHP && hpImage != null)
            hpImage.gameObject.SetActive(true);
    }

    IEnumerator InitUI()
    {
        // 等待第一個場景的 UI 載入
        yield return new WaitForSeconds(0.1f);
        FindHPUI();

        UpdateHpUI();

        if (!hasShownHP && hpImage != null)
            hpImage.gameObject.SetActive(false);

    }

    void FindHPUI()
    {
        Image[] allImages = FindObjectsOfType<Image>(true);
        foreach (var img in allImages)
        {
            if (img.name == hpImageName)
            {
                hpImage = img;
                Debug.Log($"🩸 在場景中找到血量圖像：{hpImage.name}");
                return;
            }
        }
        Debug.LogWarning("⚠️ 沒找到血量圖像：" + hpImageName);
    }

    public void PlayDamageEffect()
    {
        Debug.Log("💢 播放扣血特效！");

        // 🔹 讓血條閃爍紅色
        if (hpImage != null)
            StartCoroutine(FlashRed());

        // 🔹 黑屏
        if (blackScreen != null)
        {
            blackScreenAct.SetActive(true);
            StartCoroutine(Blackout());
        }
            

        // 🔹 震動螢幕（可選）
        var camShake = Camera.main?.GetComponent<Animator>();
        if (camShake != null)
            camShake.SetTrigger("Shake");

        /*// 🔹 播放音效（如果有）
        var audio = GetComponent<AudioSource>();
        if (audio != null)
            audio.Play();
        */
    }

    IEnumerator FlashRed()
    {
        Color originalColor = hpImage.color;
        hpImage.color = Color.black;
        yield return new WaitForSeconds(0.5f);
        hpImage.color = originalColor;
    }

    IEnumerator Blackout()
    {
        // 淡入黑屏
        blackScreen.gameObject.SetActive(true);
        Color c = blackScreen.color;
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(0, 1, t / 0.5f);
            blackScreen.color = c;
            yield return null;
        }
        c.a = 1;
        blackScreen.color = c;

        // 停留黑屏
        yield return new WaitForSeconds(blackScreenDuration);

        // 淡出黑屏
        for (float t = 0; t < 0.5f; t += Time.deltaTime)
        {
            c.a = Mathf.Lerp(1, 0, t / 0.5f);
            blackScreen.color = c;
            yield return null;
        }
        c.a = 0;
        blackScreen.color = c;
        blackScreen.gameObject.SetActive(false);
    }

}
