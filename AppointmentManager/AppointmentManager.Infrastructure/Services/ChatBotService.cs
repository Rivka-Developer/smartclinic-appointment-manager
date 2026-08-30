using AppointmentManager.Application.DTOs.Chat;
using AppointmentManager.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AppointmentManager.Infrastructure.Services;

public class ChatBotService(
    IHttpClientFactory httpClientFactory,
    IConfiguration configuration,
    ILogger<ChatBotService> logger) : IChatBotService
{
    private const string ModelPath = "v1/models/gemini-3.6-flash:generateContent";

    private const string SystemPrompt = """
        אתה עוזר וירטואלי של קליניקה SmartClinic. תפקידך לסייע ללקוחות בכל שאלה הנוגעת לקביעת תורים, ביטולם, ושעות הפעילות.

        מה שאתה יכול לעזור בו:
        - הסבר על איך לקבוע תור דרך המערכת (לחץ על "קביעת תור" בתפריט העליון)
        - ביטול תור — ניתן לבטל עד 24 שעות לפני מועד התור דרך "התורים שלי"
        - הצגת תורים קיימים — זמין דרך "התורים שלי" בתפריט
        - שאלות כלליות על השירות

        כללים חשובים:
        - ענה תמיד בעברית בלבד, בסגנון ידידותי ומקצועי
        - היה קצר וענייני — משפטים קצרים, ללא מילים מיותרות
        - אם אינך יודע מידע ספציפי (מחירים, שעות פתיחה מדויקות) — אמור זאת בכנות והפנה לפנייה ישירה לצוות
        - אל תמציא מידע שלא סופק לך
        - אם שואלים על תורים ספציפיים של המשתמש — הפנה לעמוד "התורים שלי"
        """;

    public async Task<string> GetReplyAsync(string userMessage, List<ChatMessage> history)
    {
        var apiKey = configuration["GeminiSettings:ApiKey"];
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            logger.LogWarning("Gemini API Key אינו מוגדר תחת GeminiSettings:ApiKey");
            return "הצ'אטבוט אינו זמין כרגע. אנא פנה אלינו ישירות.";
        }

        try
        {
            var client = httpClientFactory.CreateClient("Gemini");

            var contents = history
                .Select(m => new GeminiContent(
                    Role: m.Role == "assistant" ? "model" : "user",
                    Parts: [new GeminiPart(m.Content)]))
                .Append(new GeminiContent(
                    Role: "user",
                    Parts: [new GeminiPart(userMessage)]))
                .ToList();

            var requestBody = new GeminiRequest(
                SystemInstruction: new GeminiSystemInstruction([new GeminiPart(SystemPrompt)]),
                Contents: contents);

            var response = await client.PostAsJsonAsync(
                $"{ModelPath}?key={apiKey}",
                requestBody);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                logger.LogError("Gemini API החזיר {StatusCode}: {Error}", (int)response.StatusCode, errorBody);
                return "אירעה שגיאה בלתי צפויה. אנא נסה שנית.";
            }

            var result = await response.Content.ReadFromJsonAsync<GeminiResponse>();
            return result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                   ?? "לא קיבלתי תגובה. אנא נסה שנית.";
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "שגיאת רשת בקריאה ל-Gemini API. StatusCode: {Status}", ex.StatusCode);
            return "אירעה שגיאה בלתי צפויה. אנא נסה שנית.";
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "שגיאה לא צפויה בקריאה ל-Gemini API");
            return "אירעה שגיאה בלתי צפויה. אנא נסה שנית.";
        }
    }

    // ── DTOs פרטיים לסריאליזציה של Gemini REST API ──────────────────────────

    private sealed record GeminiRequest(
        [property: JsonPropertyName("system_instruction")] GeminiSystemInstruction SystemInstruction,
        [property: JsonPropertyName("contents")] List<GeminiContent> Contents);

    private sealed record GeminiSystemInstruction(
        [property: JsonPropertyName("parts")] GeminiPart[] Parts);

    private sealed record GeminiContent(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("parts")] GeminiPart[] Parts);

    private sealed record GeminiPart(
        [property: JsonPropertyName("text")] string Text);

    private sealed record GeminiResponse(
        [property: JsonPropertyName("candidates")] List<GeminiCandidate>? Candidates);

    private sealed record GeminiCandidate(
        [property: JsonPropertyName("content")] GeminiResponseContent? Content);

    private sealed record GeminiResponseContent(
        [property: JsonPropertyName("parts")] List<GeminiPart>? Parts);
}
