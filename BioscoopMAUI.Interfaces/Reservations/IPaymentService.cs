using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.Interfaces.Reservations;

public interface IPaymentService
{
    Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(Guid holdId);
    Task<PaymentStatusResponseDto> GetPaymentStatusAsync(string sessionId);
}