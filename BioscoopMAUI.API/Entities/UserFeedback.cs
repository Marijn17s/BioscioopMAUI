namespace BioscoopMAUI.API.Entities;

public class UserFeedback
{
    public int Id { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string Feedback { get; set; } = string.Empty;
}