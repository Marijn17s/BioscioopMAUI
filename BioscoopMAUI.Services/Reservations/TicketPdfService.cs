using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Models.Helpers;

namespace BioscoopMAUI.Services.Reservations;

public class TicketPdfService(QrCodeHelper qrCodeHelper) : ITicketPdfService
{
    public async Task<string> GenerateReservationTicketsAsync(ReservationResponseDto reservation)
    {
        var fileName = $"reservation-{reservation.Id}-tickets.pdf";
        var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

        var document = new ReservationTicketsDocument(reservation, qrCodeHelper);
        await document.SaveAsync(filePath);

        var pdfFile = new FileInfo(filePath);
        if (!pdfFile.Exists || pdfFile.Length is 0)
            throw new InvalidOperationException("The ticket PDF could not be generated.");

        return filePath;
    }
}