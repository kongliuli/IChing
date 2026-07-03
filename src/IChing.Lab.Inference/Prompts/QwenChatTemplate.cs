namespace IChing.Lab.Inference.Prompts;

public static class QwenChatTemplate
{
    public static string Wrap(string system, string user) =>
        $"""
        <|im_start|>system
        {system.Trim()}
        
        <|im_start|>user
        {user.Trim()}
        
        <|im_start|>assistant
        """;
}
