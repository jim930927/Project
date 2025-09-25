using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class Journal : MonoBehaviour
{
    [Header("UI & Dialogue")]
    public GameObject journalImagePanel;
    public InkDialogueManager dialogueManager;
    public TextAsset inkJSON;

    [Header("Overlay UI")]
    public GameObject overlayPanel;
    public Text overlayText;
    public Button returnButton;

    [Header("翻頁按鈕")]
    public Button nextPageButton;
    public Button prevPageButton;

    [Header("自訂每頁內容")]
    [TextArea(3, 10)]
    public List<string> pages = new List<string>(); // 自訂每頁內容

    private bool isPlayerNear = false;
    private bool isImageShowing = false;
    private bool hasRead = false;
    private bool isFinished = false;
    private bool isOverlayActive = false;

    private int currentPage = 0;

    void Start()
    {
        if (journalImagePanel != null) journalImagePanel.SetActive(false);
        if (overlayPanel != null) overlayPanel.SetActive(false);

        if (returnButton != null)
        {
            returnButton.gameObject.SetActive(false);
            returnButton.onClick.AddListener(OnReturnButtonPressed);
        }

        if (nextPageButton != null)
        {
            nextPageButton.onClick.AddListener(OnNextPagePressed);
            nextPageButton.gameObject.SetActive(false);
        }

        if (prevPageButton != null)
        {
            prevPageButton.onClick.AddListener(OnPrevPagePressed);
            prevPageButton.gameObject.SetActive(false);
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
                    journalImagePanel.SetActive(true);
                    isImageShowing = true;
                }
                else
                {
                    isImageShowing = true;
                    hasRead = true;

                    if (dialogueManager != null && inkJSON != null)
                        dialogueManager.EnterDialogueMode(inkJSON, "journal_content", OnDialogueFinished);
                }
            }
        }
    }

    private void OnDialogueFinished()
    {
        if (journalImagePanel != null)
            journalImagePanel.SetActive(false);

        if (overlayPanel != null && overlayText != null && returnButton != null)
        {
            overlayPanel.SetActive(true);
            isOverlayActive = true;

            currentPage = 0;
            ShowCurrentPage();
        }
    }

    private void ShowCurrentPage()
    {
        if (pages.Count > 0 && currentPage >= 0 && currentPage < pages.Count)
        {
            overlayText.text = pages[currentPage];

            // 控制按鈕顯示
            prevPageButton.gameObject.SetActive(currentPage > 0);
            nextPageButton.gameObject.SetActive(currentPage < pages.Count - 1);
            returnButton.gameObject.SetActive(currentPage == pages.Count - 1);
        }
    }

    private void OnNextPagePressed()
    {
        if (currentPage < pages.Count - 1)
        {
            currentPage++;
            ShowCurrentPage();
        }
    }

    private void OnPrevPagePressed()
    {
        if (currentPage > 0)
        {
            currentPage--;
            ShowCurrentPage();
        }
    }

    private void OnReturnButtonPressed()
    {
        overlayPanel.SetActive(false);
        returnButton.gameObject.SetActive(false);
        nextPageButton.gameObject.SetActive(false);
        prevPageButton.gameObject.SetActive(false);
        isOverlayActive = false;

        if (dialogueManager != null && inkJSON != null)
            dialogueManager.EnterDialogueMode(inkJSON, "journal_choices");

        isFinished = true;
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
            if (journalImagePanel.activeSelf)
                journalImagePanel.SetActive(false);
        }
    }
}
