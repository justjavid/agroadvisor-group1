namespace Service.Services.ChatBot;

public class ChatAiOptions
{
    public string Endpoint { get; set; } = "https://generativelanguage.googleapis.com/v1beta/models/{model}:generateContent";
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gemini-1.5-flash";
    public string SystemPrompt { get; set; } =
        "You are AgroAdvisor's agronomy assistant. Give concise, practical guidance for farming questions. Mention uncertainty when appropriate and end with: This is general guidance.";
}
