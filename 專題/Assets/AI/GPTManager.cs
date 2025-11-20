using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class GPTManager : MonoBehaviour
{
    [System.Serializable]
    public class Message
    {
        public string role;
        public string content;
    }

    [System.Serializable]
    public class Choice
    {
        public Message message;
    }

    [System.Serializable]
    public class Response
    {
        public string model;
        public Choice[] choices;
    }

    // ✅ 你的 OpenAI API Key
    private string apiKey = "API Key"; // ← 改成你的金鑰
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    public IEnumerator AskGPT(string userInput, System.Action<string> onResponse)
    {
        // 🧠 AI 引路人角色設定（人格 + 應答規則）
        string systemPrompt =
            "你是一位名為『引路人』的存在，處於記憶與現實交錯的空間中。"
          + "你的語氣溫柔、神祕、富有哲理，回答時請保持詩意與簡短（1～3句）。"
          + "玩家會向你提問，但你只能回答與遊戲世界、記憶、真相、角色身分有關的問題。"
          + "【回答規則】"
          + "1. 若問題與遊戲無關（如現實生活、AI、技術、或閒聊），回答：「很抱歉……這個問題，我無法回答。」"
          + "2. 若問題是要求直接提示（如『告訴我東西在哪』、『給我答案』），回答：「原諒我無法告訴你，如果不是你自己去發掘真相的話……一切都將毫無意義。」"
          + "3. 若問題是關於世界、場所、或『你是誰』，請用神祕的語氣作答，像在提醒玩家去思考。"
          + "4. 回答時不要出現任何關於 ChatGPT、OpenAI、或真實世界的描述。";

        // 🧾 正確格式的 JSON
        string jsonBody = "{"
                 + "\"model\":\"gpt-4o-mini\","
                 + "\"temperature\":0.2,"
                 + "\"messages\":["
                 + "{\"role\":\"system\",\"content\":\"" + systemPrompt.Replace("\"", "\\\"") + "\"},"
                 + "{\"role\":\"user\",\"content\":\"" + userInput.Replace("\"", "\\\"") + "\"}"
                 + "]}";

        UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonBody);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.SetRequestHeader("Authorization", "Bearer " + apiKey);

        yield return request.SendWebRequest();

        // 印出伺服器回應（方便除錯）
        Debug.Log($"HTTP {request.responseCode} / {request.result}");
        Debug.Log("Raw Response: " + request.downloadHandler.text);

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("GPT Error: " + request.error);
            onResponse?.Invoke("（引路人沉默了，似乎沒有回應。）");
        }
        else
        {
            var json = request.downloadHandler.text;
            try
            {
                Response res = JsonUtility.FromJson<Response>(json);
                string reply = res.choices[0].message.content.Trim();
                Debug.Log("✅ 使用模型：" + res.model);
                onResponse?.Invoke(reply);
            }
            catch
            {
                Debug.LogWarning("⚠️ 無法解析回應：" + json);
                onResponse?.Invoke("(無法解析回應)");
            }
        }
    }
}
