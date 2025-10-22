using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("輸入欄位 (擇一填)")]
    public TMP_InputField tmpInputField;   // 新版 TextMeshPro
    public InputField legacyInputField;    // 舊版 Unity UI

    [Header("對話顯示區 (擇一填)")]
    public TMP_Text tmpChatDisplay;        // 新版 TextMeshPro
    public Text legacyChatDisplay;         // 舊版 Unity UI

    [Header("送出按鈕 (Button)")]
    public Button sendButton;

    [Header("GPT 管理器 (GPTManager)")]
    public GPTManager gpt;

    private void Start()
    {
        if (sendButton != null) sendButton.onClick.AddListener(Send);
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
        string line = highlight ? $"\n<color=#00BFFF>{who}：</color> {content}" : $"\n{who}：{content}";

        if (tmpChatDisplay != null) tmpChatDisplay.text += line;
        if (legacyChatDisplay != null) legacyChatDisplay.text += line.Replace("<color=#00BFFF>", "").Replace("</color>", "");
    }

    private void Send()
    {
        string text = GetInputText();
        if (string.IsNullOrEmpty(text) || gpt == null) return;

        // 顯示玩家輸入
        AppendChat("你", text, highlight: true);
        ClearInput();

        // 呼叫 GPT
        StartCoroutine(gpt.AskGPT(text, (reply) =>
        {
            AppendChat("引路人", reply);
        }));
    }
}
