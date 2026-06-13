using System.Text.Json;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.DataModels;
using BioscoopMAUI.Models.DTOs;

namespace BioscoopMAUI.API.Services;

public class TicketPricingService(IWebHostEnvironment hostingEnvironment) : ITicketPricingService
{
    private TicketPricingConfig? _cachedConfig;

    public async Task<PriceQuoteDto> GetPriceQuoteAsync(Showtime showtime, int seatCount)
    {
        if (seatCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(seatCount), "Seat count must be greater than zero.");

        var config = await GetConfigAsync();
        var baseTicketPrice = showtime.Movie.DurationMinutes >= config.BasePrice.LongMovieThresholdMinutes
            ? config.BasePrice.LongMovie
            : config.BasePrice.Normal;

        var surchargePerTicket = showtime.Room.Has3D ? config.Surcharges.ThreeD : 0m;
        var ticketPriceBeforeDiscount = baseTicketPrice + surchargePerTicket;
        var subtotal = ticketPriceBeforeDiscount * seatCount;
        var discountAmount = Math.Round(subtotal * (showtime.DiscountPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        var totalPrice = Math.Max(0, subtotal - discountAmount);

        return new PriceQuoteDto(
            showtime.Id,
            seatCount,
            baseTicketPrice,
            surchargePerTicket,
            ticketPriceBeforeDiscount,
            subtotal,
            showtime.DiscountPercentage,
            discountAmount,
            totalPrice);
    }

    private async Task<TicketPricingConfig> GetConfigAsync()
    {
        if (_cachedConfig is not null)
            return _cachedConfig;

        var jsonFilePath = Path.Combine(hostingEnvironment.WebRootPath, "config", "ticketPricing.json");
        await using var stream = File.OpenRead(jsonFilePath);
        _cachedConfig = await JsonSerializer.DeserializeAsync<TicketPricingConfig>(
            stream,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new TicketPricingConfig();

        return _cachedConfig;
    }
}