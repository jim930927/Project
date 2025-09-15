using UnityEngine;

public class LetterInteraction : MonoBehaviour
{
    [Header("UI & Dialogue")]
    public GameObject letterImagePanel;
    public InkDialogueManager dialogueManager;
    public TextAsset inkJSON;

    private bool isPlayerNear = false;
    private bool isImageShowing = false;
    private bool hasRead = false;
    private bool isFinished = false; // ✅ 新增：是否已完成整個互動

    void Start()
    {
        if (letterImagePanel != null)
            letterImagePanel.SetActive(false);
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
                    // 第二次按下 → 關閉圖片 + 觸發對話
                    letterImagePanel.SetActive(false);
                    isImageShowing = false;
                    hasRead = true;

                    if (dialogueManager != null && inkJSON != null)
                    {
                        dialogueManager.EnterDialogueMode(inkJSON, "letter_content");
                    }
                    else
                    {
                        Debug.LogWarning("請確認 DialogueManager 與 Ink JSON 已設定！");
                    }

                    isFinished = true; // ✅ 鎖定互動流程只發生一次
                }
            }
        }
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
