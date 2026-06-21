using BioscoopMAUI.API.Entities;
using BioscoopMAUI.API.Services;
using Microsoft.AspNetCore.Hosting;
using NSubstitute;
using NUnit.Framework;

namespace BioscoopMAUI.API.Tests.Services;

[TestFixture]
public class TicketPricingServiceTests
{
    private string _tempWebRoot = string.Empty;
    private TicketPricingService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _tempWebRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(_tempWebRoot, "config"));

        var pricingJson = """
            {
              "basePrice": {
                "normal": 8.50,
                "longMovie": 9.00,
                "longMovieThresholdMinutes": 130
              },
              "surcharges": {
                "threeD": 2.50
              }
            }
            """;

        File.WriteAllText(Path.Combine(_tempWebRoot, "config", "ticketPricing.json"), pricingJson);

        var hostingEnvironment = Substitute.For<IWebHostEnvironment>();
        hostingEnvironment.WebRootPath.Returns(_tempWebRoot);

        _service = new TicketPricingService(hostingEnvironment);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempWebRoot))
            Directory.Delete(_tempWebRoot, recursive: true);
    }

    [Test]
    public async Task GetPriceQuoteAsync_UsesLongMoviePriceWhenDurationMeetsThreshold()
    {
        var showtime = CreateShowtime(durationMinutes: 130, has3D: false, discountPercentage: 0);

        var quote = await _service.GetPriceQuoteAsync(showtime, seatCount: 2);

        Assert.Multiple(() =>
        {
            Assert.That(quote.BaseTicketPrice, Is.EqualTo(9.00m));
            Assert.That(quote.SurchargePerTicket, Is.EqualTo(0m));
            Assert.That(quote.Subtotal, Is.EqualTo(18.00m));
            Assert.That(quote.TotalPrice, Is.EqualTo(18.00m));
        });
    }

    [Test]
    public async Task GetPriceQuoteAsync_AppliesThreeDSurchargeAndDiscountRounding()
    {
        var showtime = CreateShowtime(durationMinutes: 90, has3D: true, discountPercentage: 10);

        var quote = await _service.GetPriceQuoteAsync(showtime, seatCount: 2);

        Assert.Multiple(() =>
        {
            Assert.That(quote.BaseTicketPrice, Is.EqualTo(8.50m));
            Assert.That(quote.SurchargePerTicket, Is.EqualTo(2.50m));
            Assert.That(quote.TicketPriceBeforeDiscount, Is.EqualTo(11.00m));
            Assert.That(quote.Subtotal, Is.EqualTo(22.00m));
            Assert.That(quote.DiscountAmount, Is.EqualTo(2.20m));
            Assert.That(quote.TotalPrice, Is.EqualTo(19.80m));
        });
    }

    [Test]
    public void GetPriceQuoteAsync_ThrowsWhenSeatCountIsZero()
    {
        var showtime = CreateShowtime(durationMinutes: 90, has3D: false, discountPercentage: 0);

        Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => _service.GetPriceQuoteAsync(showtime, seatCount: 0));
    }

    private static Showtime CreateShowtime(int durationMinutes, bool has3D, decimal discountPercentage)
    {
        return new Showtime
        {
            Id = 1,
            MovieId = 1,
            RoomId = 1,
            DiscountPercentage = discountPercentage,
            Movie = new Movie
            {
                Id = 1,
                Title = "Test Movie",
                Description = "Description",
                PosterUrl = "poster.jpg",
                Actors = "Actor",
                TrailerUrl = "trailer.mp4",
                Genres = "Action",
                AgeRating = 12,
                DurationMinutes = durationMinutes,
                ReleaseDate = DateTime.UtcNow
            },
            Room = new Room
            {
                Id = 1,
                Number = 1,
                Name = "Room 1",
                Has3D = has3D,
                IsWheelchairAccessible = true
            }
        };
    }
}