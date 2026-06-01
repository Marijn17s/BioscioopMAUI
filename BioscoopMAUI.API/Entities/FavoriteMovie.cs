namespace BioscoopMAUI.API.Entities;

public class FavoriteMovie
{
    public int Id { get; set; }
    public string Auth0UserId { get; set; } = string.Empty;
    public int MovieId { get; set; }
    public DateTime CreatedAt { get; set; }

    public Movie Movie { get; set; }
}