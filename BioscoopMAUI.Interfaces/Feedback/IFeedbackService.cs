using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Feedback;

public interface IFeedbackService
{
    Task<FeedbackResponseDto> SubmitFeedbackAsync(string feedback);
}