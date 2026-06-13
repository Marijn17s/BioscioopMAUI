using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Models.Helpers;
using SkiaSharp;

namespace BioscoopMAUI.Services.Reservations;

public class ReservationTicketsDocument(ReservationResponseDto reservation, QrCodeHelper qrCodeHelper)
{
    private const float PageWidth = 595;
    private const float PageHeight = 842;

    private static readonly SKColor PageBackgroundColor = new(23, 23, 31);
    private static readonly SKColor PanelColor = new(46, 46, 56);
    private static readonly SKColor AccentColor = new(230, 10, 21);
    private static readonly SKColor MutedTextColor = new(185, 185, 199);
    private static readonly SKColor BorderColor = new(74, 74, 86);

    public async Task SaveAsync(string filePath)
    {
        await Task.Run(() => Save(filePath));
    }

    private void Save(string filePath)
    {
        using var document = SKDocument.CreatePdf(filePath, new SKDocumentPdfMetadata
        {
            Title = $"Reservation {reservation.Id} tickets",
            Author = "BioscoopMAUI"
        });

        foreach (var seat in reservation.Seats.OrderBy(seat => seat.Row).ThenBy(seat => seat.SeatNumber))
            AddTicketPage(document, seat);

        document.Close();
    }

    private void AddTicketPage(SKDocument document, SeatDto seat)
    {
        var canvas = document.BeginPage(PageWidth, PageHeight);
        canvas.Clear(PageBackgroundColor);

        DrawTicketShell(canvas);

        // Header
        DrawText(canvas, "BioscoopMAUI Ticket", 64, 140, SKColors.White, 28, true);
        DrawText(canvas, reservation.MovieTitle, 64, 190, SKColors.White, 20, true);
        DrawText(canvas, reservation.Showtime.StartTime.ToString("dddd d MMMM yyyy HH:mm"), 64, 218, MutedTextColor, 11);

        DrawReservationInfo(canvas, seat);
        DrawQrCode(canvas, seat);
        
        // Footer
        DrawText(canvas, "Show this ticket at the entrance. Keep it available until your seat is checked.", 64, 762,MutedTextColor, 11);

        document.EndPage();
    }

    private static void DrawTicketShell(SKCanvas canvas)
    {
        using var panelPaint = CreateFillPaint(PanelColor);
        using var borderPaint = CreateStrokePaint(BorderColor, 1);
        using var accentPaint = CreateFillPaint(AccentColor);

        canvas.DrawRect(36, 36, 523, 770, panelPaint);
        canvas.DrawRect(36, 36, 523, 770, borderPaint);
        canvas.DrawRect(64, 82, 467, 4, accentPaint);
    }

    private void DrawReservationInfo(SKCanvas canvas, SeatDto seat)
    {
        using var panelPaint = CreateFillPaint(PageBackgroundColor);
        canvas.DrawRect(64, 258, 467, 205, panelPaint);

        DrawInfoBlock(canvas, "Date", reservation.Showtime.StartTime.ToString("dddd d MMMM yyyy"), 84, 298);
        DrawInfoBlock(canvas, "Time", reservation.Showtime.StartTime.ToString("HH:mm"), 318, 298);
        DrawInfoBlock(canvas, "Room", reservation.RoomName, 84, 364);
        DrawInfoBlock(canvas, "Seat", $"Row {seat.Row}, Seat {seat.SeatNumber}", 318, 364);
        DrawInfoBlock(canvas, "Reservation", reservation.Id.ToString(), 84, 430);
        DrawInfoBlock(canvas, "Paid", reservation.TotalPrice.ToString("C"), 318, 430);
    }

    private static void DrawInfoBlock(SKCanvas canvas, string label, string value, float x, float y)
    {
        DrawText(canvas, label.ToUpperInvariant(), x, y, MutedTextColor, 9);
        DrawText(canvas, value, x, y + 22, SKColors.White, 13, true);
    }

    private void DrawQrCode(SKCanvas canvas, SeatDto seat)
    {
        var qrCodeText = qrCodeHelper.GetSeatQrCodeString(reservation, seat);
        var qrCodeBytes = Convert.FromBase64String(qrCodeHelper.GetQrCodeImage(qrCodeText));

        using var panelPaint = CreateFillPaint(PageBackgroundColor);
        using var whitePaint = CreateFillPaint(SKColors.White);
        using var qrCodeBitmap = SKBitmap.Decode(qrCodeBytes);

        canvas.DrawRect(64, 506, 467, 180, panelPaint);
        DrawText(canvas, "Scan this ticket", 84, 552, SKColors.White, 16, true);
        DrawText(canvas, "Show this QR code at the entrance.", 84, 586, MutedTextColor, 11);
        canvas.DrawRect(348, 526, 148, 148, whitePaint);
        canvas.DrawBitmap(qrCodeBitmap, new SKRect(358, 536, 486, 664));
    }

    private static SKPaint CreateFillPaint(SKColor color)
        => new()
        {
            Color = color,
            IsAntialias = true,
            Style = SKPaintStyle.Fill
        };

    private static SKPaint CreateStrokePaint(SKColor color, float strokeWidth)
        => new()
        {
            Color = color,
            IsAntialias = true,
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke
        };

    private static void DrawText(SKCanvas canvas, string text, float x, float y, SKColor color, float textSize, bool isBold = false)
    {
        using var typeface = SKTypeface.FromFamilyName("Arial", isBold ? SKFontStyle.Bold : SKFontStyle.Normal);
        using var font = new SKFont(typeface, textSize);
        using var paint = new SKPaint();
        paint.Color = color;
        paint.IsAntialias = true;

        canvas.DrawText(text, x, y, SKTextAlign.Left, font, paint);
    }
}