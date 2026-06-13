using System.Text.Json;
using BioscoopMAUI.Interfaces.Reservations;
using BioscoopMAUI.Models.DTOs;
using SQLite;

namespace BioscoopMAUI.Services.Reservations;

public class LocalReservationStore : ILocalReservationStore
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new(JsonSerializerDefaults.Web);
    private readonly SQLiteAsyncConnection _database;
    private readonly Lazy<Task> _initializeTask;

    public LocalReservationStore()
    {
        SQLitePCL.Batteries_V2.Init();

        var databasePath = Path.Combine(FileSystem.AppDataDirectory, "reservations.db3");
        _database = new SQLiteAsyncConnection(databasePath, SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create | SQLiteOpenFlags.SharedCache);
        _initializeTask = new Lazy<Task>(async () => await _database.CreateTableAsync<CachedReservationRecord>());
    }

    public async Task<List<ReservationResponseDto>> GetReservationsAsync()
    {
        await EnsureInitializedAsync();

        var records = await _database
            .Table<CachedReservationRecord>()
            .OrderByDescending(record => record.ShowtimeStartTime)
            .ToListAsync();

        return records
            .Select(record => DeserializeReservation(record.PayloadJson))
            .OfType<ReservationResponseDto>()
            .ToList();
    }

    public async Task SaveReservationsAsync(IEnumerable<ReservationResponseDto> reservations)
    {
        await EnsureInitializedAsync();

        var records = reservations.Select(CreateRecord).ToList();
        await _database.RunInTransactionAsync(connection =>
        {
            connection.DeleteAll<CachedReservationRecord>();
            connection.InsertAll(records);
        });
    }

    public async Task SaveReservationAsync(ReservationResponseDto reservation)
    {
        await EnsureInitializedAsync();
        await _database.InsertOrReplaceAsync(CreateRecord(reservation));
    }

    public async Task RemoveReservationAsync(int reservationId)
    {
        await EnsureInitializedAsync();
        await _database.ExecuteAsync("DELETE FROM CachedReservations WHERE ReservationId = ?", reservationId);
    }

    private Task EnsureInitializedAsync()
        => _initializeTask.Value;

    private static CachedReservationRecord CreateRecord(ReservationResponseDto reservation)
    {
        return new CachedReservationRecord
        {
            ReservationId = reservation.Id,
            ShowtimeStartTime = reservation.Showtime.StartTime,
            Status = reservation.Status,
            PayloadJson = JsonSerializer.Serialize(reservation, JsonSerializerOptions),
            UpdatedAtUtc = DateTime.UtcNow
        };
    }

    private static ReservationResponseDto? DeserializeReservation(string payloadJson)
    {
        try
        {
            return JsonSerializer.Deserialize<ReservationResponseDto>(payloadJson, JsonSerializerOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    [Table("CachedReservations")]
    private class CachedReservationRecord
    {
        [PrimaryKey]
        public int ReservationId { get; set; }

        [Indexed]
        public DateTime ShowtimeStartTime { get; set; }

        [MaxLength(32)]
        public string Status { get; set; } = string.Empty;

        public string PayloadJson { get; set; } = string.Empty;

        public DateTime UpdatedAtUtc { get; set; }
    }
}