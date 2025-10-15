using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HP : MonoBehaviour
{
    public int hp = 4;

    [Header("血量 UI")]
    public Image hpImage;
    public Sprite hp_0;
    public Sprite hp_1_3;
    public Sprite hp_4_9;
    public Sprite hp_10;

    [Header("提示 UI")]
    public GameObject hpHintPanel;         // 👉 指向提示框的Panel
    public float hintDuration = 3f;        // 顯示時間
    public float fadeTime = 0.5f;          // 淡入淡出時間

    private static HP instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        if (hpImage != null)
            hpImage.gameObject.SetActive(false);  // 一開始隱藏

        if (hpHintPanel != null)
            hpHintPanel.SetActive(false);         // 一開始隱藏
    }

    void Update()
    {
        if (hp < 0) hp = 0;
        UpdateHpUI();
    }

    void UpdateHpUI()
    {
        if (hpImage == null) return;

        if (hp <= 0)
            hpImage.sprite = hp_0;
        else if (hp <= 3)
            hpImage.sprite = hp_1_3;
        else if (hp <= 9)
            hpImage.sprite = hp_4_9;
        else
            hpImage.sprite = hp_10;
    }

    public void ShowHPUI(bool show)
    {
        if (hpImage != null)
            hpImage.gameObject.SetActive(show);

        if (show)
            ShowHPHint(); // 顯示提示框
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (hpImage == null)
        {
            var found = GameObject.Find("HPImage");
            if (found != null)
                hpImage = found.GetComponent<Image>();
        }

        if (hpHintPanel == null)
        {
            var foundHint = GameObject.Find("HPHintPanel");
            if (foundHint != null)
                hpHintPanel = foundHint;
        }

        UpdateHpUI();
    }

    // 🔹 顯示提示框
    void ShowHPHint()
    {
        if (hpHintPanel == null) return;
        StopAllCoroutines();
        StartCoroutine(ShowHintCoroutine());
    }

    IEnumerator ShowHintCoroutine()
    {
        hpHintPanel.SetActive(true);
        CanvasGroup cg = hpHintPanel.GetComponent<CanvasGroup>();
        if (cg == null)
            cg = hpHintPanel.AddComponent<CanvasGroup>();

        // 淡入
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            cg.alpha = Mathf.Lerp(0, 1, t / fadeTime);
            yield return null;
        }
        cg.alpha = 1;

        // 停留
        yield return new WaitForSeconds(hintDuration);

        // 淡出
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            cg.alpha = Mathf.Lerp(1, 0, t / fadeTime);
            yield return null;
        }

        cg.alpha = 0;
        hpHintPanel.SetActive(false);
    }
}
