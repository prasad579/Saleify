using MarketplaceCopilot.Data;
using MarketplaceCopilot.Entities;
using Microsoft.AspNetCore.Mvc;

namespace MarketplaceCopilot.Api.Controllers;

[ApiController]
[Route("api/people")]
public class PeopleController(DataStore store) : ControllerBase
{
    [HttpGet]
    public ActionResult<IEnumerable<Person>> GetAll() => store.People;

    /// <summary>Create or update a person (engagement owner).</summary>
    [HttpPut]
    public ActionResult<Person> Upsert([FromBody] Person request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            return BadRequest(new { message = "Name is required." });

        request.Name = request.Name.Trim();
        request.Email = request.Email?.Trim() ?? "";
        request.Role = request.Role?.Trim() ?? "";
        request.EngagementTypes = (request.EngagementTypes ?? []).Select(t => t.Trim()).Where(t => t.Length > 0).ToList();
        request.Source = string.IsNullOrWhiteSpace(request.Source) ? "manual" : request.Source;

        if (string.IsNullOrWhiteSpace(request.Id))
        {
            request.Id = store.NextPersonId();
            store.People.Add(request);
        }
        else
        {
            var idx = store.People.FindIndex(p => p.Id == request.Id);
            if (idx >= 0) store.People[idx] = request;
            else store.People.Add(request);
        }

        store.SavePeople();
        return Ok(request);
    }

    [HttpPost("{id}/toggle")]
    public ActionResult<Person> ToggleEnabled(string id)
    {
        var person = store.People.FirstOrDefault(p => p.Id == id);
        if (person is null) return NotFound();
        person.Enabled = !person.Enabled;
        store.SavePeople();
        return person;
    }

    [HttpDelete("{id}")]
    public ActionResult Delete(string id)
    {
        var person = store.People.FirstOrDefault(p => p.Id == id);
        if (person is null) return NotFound();
        store.People.Remove(person);
        store.SavePeople();
        return Ok(new { success = true });
    }

    [HttpPost("reset")]
    public ActionResult<IEnumerable<Person>> Reset()
    {
        store.ResetPeople();
        return Ok(store.People);
    }
}
