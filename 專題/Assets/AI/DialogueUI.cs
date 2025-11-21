using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("主對話框 (要開關的物件)")]
    public GameObject dialoguePanel;   // << 將對話框 Panel 拖進來

    [Header("輸入欄位 (擇一填)")]
    public TMP_InputField tmpInputField;
    public InputField legacyInputField;

    [Header("對話顯示區 (擇一填)")]
    public TMP_Text tmpChatDisplay;
    public Text legacyChatDisplay;

    [Header("卷軸 (ScrollRect)")]
    public ScrollRect scrollRect;

    [Header("按鈕")]
    public Button sendButton;
    public Button closeButton;    // << 新增關閉按鈕

    [Header("GPT 管理器")]
    public GPTManager gpt;

    private bool isPlayerInside = false;
    private GameObject player;

    private bool isOpen = false;

    public InkDialogueManager InkDialogueManager;

    private void Start()
    {
        if (sendButton != null) sendButton.onClick.AddListener(Send);
        if (closeButton != null) closeButton.onClick.AddListener(CloseDialogue);

        CloseDialogue(); // 開場關閉
    }

    private void Update()
    {
        if (!isPlayerInside || player == null)
            return;

        // 按空白鍵 → 開啟
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OpenDialogue();
        }

        // 按 ESC → 關閉
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            CloseDialogue();
        }
    }

    public void OpenDialogue()
    {

        dialoguePanel.SetActive(true);
        isOpen = true;
        InkDialogueManager.SetPlayerCanMove(false);
    }

    public void CloseDialogue()
    {
        dialoguePanel.SetActive(false);
        isOpen = false;
        InkDialogueManager.SetPlayerCanMove(true);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            player = other.gameObject;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            player = null;
        }
    }


    private string GetInputText()
    {
        if (tmpInputField != null) return tmpInputField.text?.Trim();
        if (legacyInputField != null) return legacyInputField.text?.Trim();
        return string.Empty;
    }

    private void ClearInput()
    {
        if (tmpInputField != null) tmpInputField.text = string.Empty;
        if (legacyInputField != null) legacyInputField.text = string.Empty;
    }

    private void AppendChat(string who, string content, bool highlight = false)
    {
        string line = highlight ?
            $"\n<color=#00BFFF>{who}：</color> {content}" :
            $"\n{who}：{content}";

        if (tmpChatDisplay != null) tmpChatDisplay.text += line;
        if (legacyChatDisplay != null)
            legacyChatDisplay.text += line.Replace("<color=#00BFFF>", "").Replace("</color>", "");

        ScrollToBottom();
    }

    private void ScrollToBottom()
    {
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();

            // 強制移到最底部
            scrollRect.verticalNormalizedPosition = 0f;

            Canvas.ForceUpdateCanvases();
        }
    }

    private void Send()
    {
        string text = GetInputText();
        if (string.IsNullOrEmpty(text) || gpt == null) return;

        AppendChat("你", text, highlight: true);
        ClearInput();

        StartCoroutine(gpt.AskGPT(text, (reply) =>
        {
            AppendChat("引路人", reply);
        }));
    }
}
