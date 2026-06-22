using BioscoopMAUI.Models.DTOs;
using BioscoopMAUI.Models.Helpers;
using NUnit.Framework;

namespace BioscoopMAUI.Models.Tests.Helpers;

[TestFixture]
public class QrCodeHelperTests
{
    private readonly QrCodeHelper _helper = new();

    private static ReservationResponseDto CreateSampleReservation(params int[] seatIds)
    {
        var seats = seatIds
            .Select(seatId => new SeatDto(seatId, 5, seatId))
            .ToList();

        var showtime = new ShowtimeResponseDto(
            42,
            7,
            3,
            "Room 1",
            new DateTime(2026, 6, 21, 20, 0, 0),
            8.50,
            0);

        return new ReservationResponseDto(
            100,
            showtime,
            seats,
            "Sample Movie",
            "Room 1",
            17.00m,
            ReservationStatus.Confirmed,
            new DateTime(2026, 6, 20, 12, 0, 0));
    }

    [Test]
    public void GetReservationQrCodeString_ParsesCorrectly()
    {
        var reservation = CreateSampleReservation(11, 12);

        var qrCodeString = _helper.GetReservationQrCodeString(reservation);
        var parsed = _helper.ParseQrCode(qrCodeString);

        Assert.That(parsed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(parsed?.ReservationId, Is.EqualTo(100));
            Assert.That(parsed?.ShowtimeId, Is.EqualTo(42));
            Assert.That(parsed?.RoomId, Is.EqualTo(3));
            Assert.That(parsed?.SeatIds, Is.EquivalentTo([11, 12]));
            Assert.That(parsed?.Checksum, Is.Not.Null.And.Not.Empty);
        });
    }

    [Test]
    public void VerifyChecksum_ReturnsFalseWhenSeatIdIsTampered()
    {
        var reservation = CreateSampleReservation(11);
        var qrCodeString = _helper.GetReservationQrCodeString(reservation);
        var parsed = _helper.ParseQrCode(qrCodeString);

        Assert.That(parsed, Is.Not.Null);
        Assert.That(_helper.VerifyChecksum(qrCodeString), Is.True);

        var tamperedSeatIds = parsed.SeatIds.Select(seatId => seatId + 1).ToList();
        var tamperedPayload = $"RE:{parsed.ReservationId}|SH:{parsed.ShowtimeId}|RO:{parsed.RoomId}|SE:{string.Join(",", tamperedSeatIds)}|CH:{parsed.Checksum}";
        var tamperedQrCode = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(tamperedPayload));

        Assert.That(_helper.VerifyChecksum(tamperedQrCode), Is.False);
    }

    [Test]
    public void ParseQrCode_ReturnsNullForInvalidInput()
    {
        Assert.Multiple(() =>
        {
            Assert.That(_helper.ParseQrCode(string.Empty), Is.Null);
            Assert.That(_helper.ParseQrCode("not-valid-base64"), Is.Null);
            Assert.That(_helper.ParseQrCode(Convert.ToBase64String("RE:1|SH:2"u8.ToArray())), Is.Null);
            Assert.That(_helper.ParseQrCode(Convert.ToBase64String("RE:abc|SH:2|RO:3|SE:4|CH:abcd"u8.ToArray())), Is.Null);
            Assert.That(_helper.ParseQrCode(Convert.ToBase64String("RE:1|SH:2|RO:3|SE:|CH:abcd"u8.ToArray())), Is.Null);
        });
    }
}