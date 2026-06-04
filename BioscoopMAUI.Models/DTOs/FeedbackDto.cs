namespace BioscoopMAUI.Models.DTOs;

public record FeedbackCreateDto(string Feedback);

public record FeedbackResponseDto(
    int Id,
    string Auth0UserId,
    DateTime CreatedAt,
    string Feedback);