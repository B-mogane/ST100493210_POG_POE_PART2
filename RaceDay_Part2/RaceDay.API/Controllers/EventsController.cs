using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.API.Data;
using RaceDay.API.DTOs;
using RaceDay.API.Models;

namespace RaceDay.API.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(RaceDayDbContext db) : ControllerBase
{
    private int CurrentUserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetUpcoming()
    {
        var items = await db.Events.Where(e => e.EventDate >= DateTime.UtcNow && e.Status == "Published")
            .OrderBy(e => e.EventDate).Select(e => new { e.EventID, e.EventName, e.Description,
                e.EventDate, e.Location, e.RouteInfo, e.EntryFee, e.Status })
            .ToListAsync();
        return Ok(items);
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id)
    {
        var item = await db.Events.Where(e => e.EventID == id)
            .Select(e => new { e.EventID, e.EventName, e.Description, e.EventDate, e.Location,
                e.RouteInfo, e.EntryFee, e.Status,
                Categories = e.Categories.Select(c => new { c.CategoryID, c.CategoryName, c.DistanceKm, c.MaxParticipants, c.Fee }) })
            .SingleOrDefaultAsync();
        return item is null ? NotFound() : Ok(item);
    }

    [HttpPost, Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventRequest request)
    {
        if (request.EventDate <= DateTime.UtcNow || request.EntryFee < 0 ||
            request.Status is not ("Draft" or "Published" or "Cancelled" or "Completed"))
            return BadRequest(new { message = "Check event date, fee, and status." });
        var item = new Event { OrganiserID = CurrentUserId, EventName = request.EventName.Trim(),
            Description = request.Description, EventDate = request.EventDate, Location = request.Location.Trim(),
            RouteInfo = request.RouteInfo, EntryFee = request.EntryFee, Status = request.Status, CreatedAt = DateTime.UtcNow };
        db.Events.Add(item);
        await db.SaveChangesAsync();
        return CreatedAtAction(nameof(GetById), new { id = item.EventID }, item);
    }

    [HttpPut("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventRequest request)
    {
        var item = await db.Events.FindAsync(id);
        if (item is null) return NotFound();
        if (item.OrganiserID != CurrentUserId) return Forbid();
        if (request.EntryFee < 0 || request.Status is not ("Draft" or "Published" or "Cancelled" or "Completed"))
            return BadRequest(new { message = "Check event fee and status." });
        item.EventName = request.EventName.Trim(); item.Description = request.Description;
        item.EventDate = request.EventDate; item.Location = request.Location.Trim();
        item.RouteInfo = request.RouteInfo; item.EntryFee = request.EntryFee; item.Status = request.Status;
        await db.SaveChangesAsync();
        return Ok(item);
    }

    [HttpDelete("{id:int}"), Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var item = await db.Events.Include(e => e.Categories).ThenInclude(c => c.Enrolments)
            .ThenInclude(e => e.Result).FirstOrDefaultAsync(e => e.EventID == id);
        if (item is null) return NotFound();
        if (item.OrganiserID != CurrentUserId) return Forbid();
        // Categories with enrolments are restricted by the supplied schema. Do not silently delete participant history.
        if (item.Categories.Any(c => c.Enrolments.Any()))
            return Conflict(new { message = "Cannot delete an event with enrolments. Cancel the event instead or define a history-retention policy." });
        db.Events.Remove(item);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
