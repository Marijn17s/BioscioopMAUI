using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Feedback;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Feedback;

public class FeedbackService(IHttpClientFactory httpClientFactory) : IFeedbackService
{
    public async Task<FeedbackResponseDto> SubmitFeedbackAsync(string feedback)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PostAsJsonAsync("api/feedback", new FeedbackCreateDto(feedback));
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<FeedbackResponseDto>();
        return result ?? throw new InvalidOperationException("Feedback response was empty.");
    }
}