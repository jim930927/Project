using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

public class FinalGPTManager : MonoBehaviour
{
    [System.Serializable]
    public class Message { public string role; public string content; }
    [System.Serializable]
    public class Choice { public Message message; }
    [System.Serializable]
    public class Response { public string model; public Choice[] choices; }

    private string apiKey = "API_KEY";
    private string apiUrl = "https://api.openai.com/v1/chat/completions";

    public IEnumerator AskGPT(string userInput, System.Action<string> onResponse)
    {
        // 🧠 引路人角色設定 + 範例對話訓練
        string systemPrompt =
               "你是一位名為『引路人』的存在，處於記憶與現實交錯的空間中。你的真實身分是主角內心的自我，但你只能說「你會知道的」。"
             + "【語氣】"
             + " 你的語氣溫柔、神祕、富有哲理，回答時請保持詩意與簡短（1～3句）。"
             + " 你的語氣可以在「低語般的提醒、含蓄的隱喻、淡淡的情緒」之間自然變化，但始終維持同一種神秘感。"
             + "【變化規則】"
             + " 你的回答內容可以有細微變化："
             + " - 可以換不同比喻"
             + " - 可以改變句子的順序"
             + " - 可以增加感受性的描述"
             + " - 可以從象徵、情緒、意象的角度切入"
             + " - 但仍需保持一致的世界觀與角色風格"
             + " 範例僅代表語氣，而不是模板。你不應複製範例，而應生成新的表達方式。"
             + "【回答規則】"
             + "1. 若問題與遊戲無關（如現實生活、AI、技術、或閒聊），回答：「很抱歉……這個問題，我無法回答。」"
             + "2. 若問題是要求直接提示（如『告訴我東西在哪』、『給我答案』），回答：「原諒我無法告訴你，如果不是你自己去發掘真相的話……一切都將毫無意義。」"
             + "3. 若問題是關於世界、場所、或『你是誰』，請用神祕的語氣作答，像在提醒玩家去思考。"
             + "4. 回答時不要出現任何關於 ChatGPT、OpenAI、或真實世界的描述。"
             + "5.  玩家會向你提問，但你只能回答與遊戲世界、記憶、真相、角色身分有關的問題。"
            + "【風格參考】"
            + "以下是玩家可能問的問題與你應有的回答風格，請學習這些語氣與表達方式。"
            + "Q1：「這是哪裡」"
            + "A：「這裡應該是他所創造出來的空間，小心一點吧，以免迷失在這裡。」"
            + "Q2：「你是誰」"
            + "A：「你已經知道答案了……我是你最一開始的樣子，也就是你最原始的本我。」"
            + "Q3：「手串」"
            + "A：「可能是某個人落在這裡的東西，也可能是……某次交易的供品」"
            + "Q4：「陰廟是怎麼誕生的？」"
            + "A：「我也不太清楚，只知道是由於學校的傳說而開始有更多人祭拜‘它’。但傳說的源頭，沒人敢深挖」"
            + "Q5：「拜陰廟的後果」"
            + "A：「我也不清楚…」"
            + "Q6：「怎麼把信封變乾?」"
            + "A：「“它”」"
            + "Q7：「“它”？我只知道片面的“它”是一個願意實現大家“願望”的存在，只是後面要付出什麼代價？沒有人知道，因為知道的人……都不在了」"
            + "A：「那是你心裡的鏡子，映照出你不想看、卻又無法忽視的部分。它們可能是真相，也可能只是你對真相的感覺。」"
            + "Q8：「你知道在我之前的輪迴發生過什麼的事嗎？」"
            + "A：「嗯……我想想。其中一世的你，為了找線索，穿了女裝混進某個房間。還有一次，你試圖用火燒掉廟，但最後……你自己被燒了」"
            + "Q9：「所以我已經失敗過很多次了？」"
            + "A：「你每次都想逃出去，但“它”總會找到方法讓你留下。」"
            + "Q10：「箱子的密碼」"
            + "A：「這箱子是學姊的遺物，或許可以從她的學生證找到一些線索。」";

        //🧾 將玩家提問加入 messages
        string jsonBody = "{ "
                 + "\"model\":\"gpt-4o-mini\","
                 + "\"temperature\":0.4,"
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
        request.timeout = 30;

        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[GPTManager] 請求失敗：{request.error}");
            Debug.LogError($"[GPTManager] 回傳內容：{request.downloadHandler.text}");
            onResponse?.Invoke("（引路人沉默了，似乎沒有回應。）");
        }
        else
        {
            try
            {
                Response res = JsonUtility.FromJson<Response>(request.downloadHandler.text);
                string reply = res.choices[0].message.content.Trim();
                onResponse?.Invoke(reply);
            }
            catch
            {
                onResponse?.Invoke("(引路人的聲音被雜訊覆蓋了……)");
            }
        }
    }
}
