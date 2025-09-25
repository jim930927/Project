using UnityEngine;
using UnityEngine.UI;

public class LetterInteraction : MonoBehaviour
{
    [Header("UI & Dialogue")]
    public GameObject letterImagePanel;   // 信件圖片
    public InkDialogueManager dialogueManager;
    public TextAsset inkJSON;

    [Header("Overlay UI")]
    public GameObject overlayPanel;       // 半透明遮罩
    public Text overlayText;              // 遮罩上的文字
    public Button returnButton;           // 返回按鈕

    private bool isPlayerNear = false;
    private bool isImageShowing = false;
    private bool hasRead = false;
    private bool isFinished = false;
    private bool isOverlayActive = false;

    void Start()
    {
        if (letterImagePanel != null) letterImagePanel.SetActive(false);
        if (overlayPanel != null) overlayPanel.SetActive(false);

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(false);
            returnButton.onClick.AddListener(OnReturnButtonPressed);
        }
    }

    void Update()
    {
        if (!isPlayerNear || isFinished) return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!hasRead)
            {
                if (!isImageShowing)
                {
                    // 第一次按下 → 顯示圖片
                    letterImagePanel.SetActive(true);
                    isImageShowing = true;
                }
                else
                {
                    // 第二次按下 → 播放對話，但圖片保持顯示
                    // ❌ 不要關閉圖片
                    // letterImagePanel.SetActive(false);
                    isImageShowing = true; // 保持 true
                    hasRead = true;

                    if (dialogueManager != null && inkJSON != null)
                    {
                        dialogueManager.EnterDialogueMode(inkJSON, "letter_content", OnDialogueFinished);
                    }
                }
            }
        }
    }

    // ✅ 對話播完後 → 顯示灰色遮罩 + 返回按鈕
    private void OnDialogueFinished()
    {
        if (letterImagePanel != null)
            letterImagePanel.SetActive(false);

        if (overlayPanel != null && overlayText != null && returnButton != null)
        {
            overlayPanel.SetActive(true);
            returnButton.gameObject.SetActive(true);
            isOverlayActive = true;
        }
    }

    // ✅ 按返回按鈕 → 關閉遮罩 → 進入 Ink 選項 → 信件物件消失
    private void OnReturnButtonPressed()
    {
        overlayPanel.SetActive(false);
        returnButton.gameObject.SetActive(false);
        isOverlayActive = false;

        if (dialogueManager != null && inkJSON != null)
        {
            dialogueManager.EnterDialogueMode(inkJSON, "letter_choices");
        }

        isFinished = true;

        // 🚩 信件物件消失
        gameObject.SetActive(false);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
            isPlayerNear = true;
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;

            if (letterImagePanel.activeSelf)
                letterImagePanel.SetActive(false);
        }
    }
}
