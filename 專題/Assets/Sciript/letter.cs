using UnityEngine;
using UnityEngine.UI;
using static ClueData;

public class LetterInteraction : MonoBehaviour
{
    [Header("UI & Dialogue")]
    public GameObject letterImagePanel;
    public InkDialogueManager dialogueManager;
    public TextAsset inkJSON;

    [Header("Overlay UI")]
    public GameObject overlayPanel;
    public Text overlayText;
    public Button returnButton;

    [Header("模式設定")]
    public bool isReReading = false;

    [Header("線索設定")]
    public string clueID;
    public string clueName; // 可自訂顯示文字

    private bool isPlayerNear = false;
    private bool isImageShowing = false;
    private bool hasRead = false;
    private bool isFinished = false;
    private bool isOverlayActive = false;

    void Start()
    {
        if (letterImagePanel) letterImagePanel.SetActive(false);
        if (overlayPanel) overlayPanel.SetActive(false);

        if (returnButton)
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
                    // 第二次按下 → 播放對話
                    isImageShowing = true;
                    hasRead = true;

                    if (dialogueManager && inkJSON)
                        dialogueManager.EnterDialogueMode(inkJSON, "letter_content", OnDialogueFinished);
                }
            }
        }
    }

    private void OnDialogueFinished() { 
        if (letterImagePanel != null) 
            letterImagePanel.SetActive(false); 
        if (overlayPanel != null && overlayText != null && returnButton != null) 
        { 
            overlayPanel.SetActive(true); returnButton.gameObject.SetActive(true); 
            isOverlayActive = true; } 
        var clueData = Resources.Load<ClueData>("ClueDatabase"); 
        if (clueData != null) 
        { clueData.AddClue(clueID); 
            Debug.Log($"🔍 玩家獲得線索：{clueID}");
        }
    }

    private void OnReturnButtonPressed()
    {
        overlayPanel.SetActive(false);
        returnButton.gameObject.SetActive(false);

        if (!isReReading && dialogueManager != null && inkJSON != null)
            dialogueManager.EnterDialogueMode(inkJSON, "letter_choices");

        isFinished = true;
        isReReading = true;
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
