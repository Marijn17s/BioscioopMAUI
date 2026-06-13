using BioscoopMAUI.API.Data;
using BioscoopMAUI.API.Entities;
using BioscoopMAUI.Models.Auth;
using BioscoopMAUI.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BioscoopMAUI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RoomsController(BioscoopDbContext context) : ControllerBase
{
    // GET /api/rooms
    [HttpGet]
    public async Task<ActionResult<IEnumerable<RoomResponseDto>>> GetRooms()
    {
        var rooms = await context.Rooms
            .Include(r => r.Rows)
            .OrderBy(r => r.Number)
            .Select(r => new RoomResponseDto(
                r.Id,
                r.Number,
                r.Name,
                r.Has3D,
                r.IsWheelchairAccessible,
                r.Rows.OrderBy(row => row.RowNumber).Select(row => new RowResponseDto(
                    row.Id,
                    row.RowNumber,
                    row.SeatCount
                )).ToList()
            ))
            .ToListAsync();

        return Ok(rooms);
    }

    // GET /api/rooms/{id}
    [HttpGet("{id}")]
    public async Task<ActionResult<RoomResponseDto>> GetRoom(int id)
    {
        var room = await context.Rooms
            .Include(r => r.Rows)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return NotFound();

        var response = new RoomResponseDto(
            room.Id,
            room.Number,
            room.Name,
            room.Has3D,
            room.IsWheelchairAccessible,
            room.Rows.OrderBy(row => row.RowNumber).Select(row => new RowResponseDto(
                row.Id,
                row.RowNumber,
                row.SeatCount
            )).ToList()
        );

        return Ok(response);
    }

    // POST /api/rooms
    [HttpPost]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<ActionResult<RoomResponseDto>> CreateRoom(RoomCreateDto dto)
    {
        // First check if a room with this number already exists
        if (await context.Rooms.AnyAsync(r => r.Number == dto.Number))
        {
            return BadRequest($"Room number {dto.Number} already exists.");
        }

        var room = new Room
        {
            Number = dto.Number,
            Name = dto.Name,
            Has3D = dto.Has3D,
            IsWheelchairAccessible = dto.IsWheelchairAccessible
        };

        // Create rows based on the DTO layout
        foreach (var rowDto in dto.Rows)
        {
            room.Rows.Add(new Row
            {
                RoomId = room.Id,
                RowNumber = rowDto.RowNumber,
                SeatCount = rowDto.SeatCount
            });
        }

        AddSeatsForRoomLayout(room);

        context.Rooms.Add(room);
        await context.SaveChangesAsync();

        var response = new RoomResponseDto(
            room.Id,
            room.Number,
            room.Name,
            room.Has3D,
            room.IsWheelchairAccessible,
            room.Rows.OrderBy(x => x.RowNumber).Select(row => new RowResponseDto(
                row.Id,
                row.RowNumber,
                row.SeatCount
            )).ToList()
        );

        return CreatedAtAction(nameof(GetRoom), new { id = room.Id }, response);
    }

    // PUT /api/rooms/{id}
    [HttpPut("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<IActionResult> UpdateRoom(int id, RoomUpdateDto dto)
    {
        var room = await context.Rooms
            .Include(r => r.Rows)
            .Include(r => r.Seats)
            .FirstOrDefaultAsync(r => r.Id == id);

        if (room is null)
            return NotFound();

        // Check if updating to a number that is already taken by ANOTHER room
        if (room.Number != dto.Number && await context.Rooms.AnyAsync(r => r.Number == dto.Number))
        {
            return BadRequest($"Room number {dto.Number} is already taken by another room.");
        }

        var existingSeatIds = room.Seats.Select(s => s.Id).ToList();
        if (existingSeatIds.Count > 0)
        {
            var hasReservations = await context.ShowtimeSeats
                .AnyAsync(ss => existingSeatIds.Contains(ss.SeatId) && ss.ReservationId.HasValue);

            if (hasReservations)
            {
                return BadRequest("Cannot change room layout because there are already reservations for this room.");
            }

            var showtimeSeatsToDelete = await context.ShowtimeSeats
                .Where(ss => existingSeatIds.Contains(ss.SeatId))
                .ToListAsync();

            context.ShowtimeSeats.RemoveRange(showtimeSeatsToDelete);
            context.Seats.RemoveRange(room.Seats);
            room.Seats.Clear();
        }

        room.Number = dto.Number;
        room.Name = dto.Name;
        room.Has3D = dto.Has3D;
        room.IsWheelchairAccessible = dto.IsWheelchairAccessible;

        // Easiest way to update rows: clear existing and insert new ones
        context.Rows.RemoveRange(room.Rows);
        room.Rows.Clear();

        foreach (var rowDto in dto.Rows)
        {
            room.Rows.Add(new Row
            {
                RoomId = room.Id,
                RowNumber = rowDto.RowNumber,
                SeatCount = rowDto.SeatCount
            });
        }

        AddSeatsForRoomLayout(room);

        await context.SaveChangesAsync();

        return NoContent();
    }

    // DELETE /api/rooms/{id}
    [HttpDelete("{id}")]
    [Authorize(Roles = AuthConstants.EmployeeRole)]
    public async Task<IActionResult> DeleteRoom(int id)
    {
        var room = await context.Rooms.FindAsync(id);
        if (room is null)
            return NotFound();

        context.Rooms.Remove(room);
        await context.SaveChangesAsync();

        return NoContent();
    }

    private static void AddSeatsForRoomLayout(Room room)
    {
        foreach (var row in room.Rows.OrderBy(r => r.RowNumber))
        {
            for (var seatNumber = 1; seatNumber <= row.SeatCount; seatNumber++)
            {
                room.Seats.Add(new Seat
                {
                    Room = room,
                    Row = row.RowNumber,
                    SeatNumber = seatNumber
                });
            }
        }
    }
}
