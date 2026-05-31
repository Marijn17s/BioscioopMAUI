using BioscoopMAUI.API.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/payment")]
public class PaymentController(BioscoopDbContext context) : ControllerBase
{
    [HttpPost("pin")]
    public async Task<ActionResult> ValidatePin([FromBody] string pinCode)
    {
        var isValid = await context.PinCards.AnyAsync(p => p.PinCode == pinCode);
        if (!isValid)
            return BadRequest(new { message = "Ongeldige pincode" });
        return Ok(new {message = "Betaling voltooid" });
    }
}
