using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Extensions;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class FeedbackController(BioscoopDbContext context) : ControllerBase
{
    [HttpPost]
    [Authorize]
    public async Task<ActionResult<FeedbackResponseDto>> Post([FromBody] FeedbackCreateDto? request)
    {
        var auth0UserId = User.GetAuth0UserId();
        if (string.IsNullOrWhiteSpace(auth0UserId))
            return Unauthorized();

        var feedbackText = request?.Feedback?.Trim();
        if (string.IsNullOrWhiteSpace(feedbackText))
            return BadRequest("Feedback is required.");

        if (feedbackText.Length > 2000)
            return BadRequest("Feedback must be 2000 characters or less.");

        var feedback = new UserFeedback
        {
            Auth0UserId = auth0UserId,
            CreatedAt = DateTime.UtcNow,
            Feedback = feedbackText
        };

        context.UserFeedback.Add(feedback);
        await context.SaveChangesAsync();

        return Ok(new FeedbackResponseDto(
            feedback.Id,
            feedback.Auth0UserId,
            feedback.CreatedAt,
            feedback.Feedback));
    }
}