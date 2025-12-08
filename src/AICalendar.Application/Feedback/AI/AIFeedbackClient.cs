using System.Net.Http.Json;
using Microsoft.Extensions.Configuration; 

namespace AICalendar.Application.Feedback.AI;

public class AIFeedbackClient : IAIFeedbackClient
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _config;

    public AIFeedbackClient(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _config = config;
    }

    public async Task SendFeedbackAsync(AiFeedbackRequestDto request, CancellationToken cancellationToken)
    {
        var url = _config["AiService:FeedbackUrl"];
        var functionKey = _config["AiService:FunctionKey"];

        var response = await _httpClient.PostAsJsonAsync(
            $"{url}?code={functionKey}",
            request,
            cancellationToken
        );

        response.EnsureSuccessStatusCode();
    }
}
