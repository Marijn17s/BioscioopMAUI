using System.Net.Http.Json;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Services.Reservations;

public class PaymentService(IHttpClientFactory httpClientFactory) : IPaymentService
{
    public async Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid holdId)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.PostAsJsonAsync("api/payment/checkout-session", new CheckoutSessionRequestDto(holdId));
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<CheckoutSessionResponseDto>()
            ?? throw new InvalidOperationException("The checkout session response was empty.");
    }

    public async Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string sessionId)
    {
        var client = httpClientFactory.CreateClient("BioscoopAPI");
        var response = await client.GetAsync($"api/payment/status/{Uri.EscapeDataString(sessionId)}");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PaymentStatusResponseDto>()
            ?? new PaymentStatusResponseDto("unknown", null, "Payment status is unavailable.");
    }
}