using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class HP : MonoBehaviour
{
    public int hp = 1;

    [Header("血量 UI")]
    public Image hpImage;
    public string hpImageName = "HPImage";

    public Sprite hp_0;
    public Sprite hp_1_3;
    public Sprite hp_4_9;
    public Sprite hp_10;

    [Header("提示 UI（可選）")]
    public GameObject hpHintPanel;
    public GameObject hpItem;
    public string hpHintPanelName = "HPHintPanel";

    public float hintDuration = 3f;
    public float fadeTime = 0.5f;

    public bool hasShownHP = false;

    private Coroutine hintCoroutine;


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
        StartCoroutine(InitUI()); // 🔹 延遲初始化確保能抓到第一場景 UI
    }

    void Update()
    {
        if (hp < 0) hp = 0;
        if (hp >= 10) hp = 10;

        if (hpImage == null || hpImage.Equals(null))
        {
            FindHPUI();
            if (hpImage == null) return;
        }

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
        if (hpImage == null)
            FindHPUI();

        if (hpImage != null)
            hpImage.gameObject.SetActive(show);

        if (show)
        {
            hasShownHP = true;
            ShowHPHint();
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 🔹 延遲 0.1 秒再綁定，確保 Canvas 已載入
        StartCoroutine(DelayedFindUI());
    }

    IEnumerator DelayedFindUI()
    {
        yield return new WaitForSeconds(0.1f); // 等待新場景的 UI 初始化
        FindHPUI();
        FindHintUI();

        UpdateHpUI();

        if (hasShownHP && hpImage != null)
            hpImage.gameObject.SetActive(true);
    }

    IEnumerator InitUI()
    {
        // 等待第一個場景的 UI 載入
        yield return new WaitForSeconds(0.1f);
        FindHPUI();
        FindHintUI();

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


    void FindHintUI()
    {
        var foundHint = GameObject.Find(hpHintPanelName);
        if (foundHint != null)
        {
            hpHintPanel = foundHint;
            Debug.Log($"💬 綁定血量提示框：{hpHintPanel.name}");
        }
    }

    void ShowHPHint()
    {
        if (hpHintPanel == null) return;

        if (hintCoroutine != null)
            StopCoroutine(hintCoroutine);

        hintCoroutine = StartCoroutine(ShowHintCoroutine());
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
