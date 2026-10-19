using System.Net;
using System.Text.Json;

namespace RaceDay.API.Tests;

public class EventTests : ApiTestBase, IClassFixture<ApiFactory>
{
    public EventTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task ListEvents_WithoutLogin_Returns200()
    {
        var client = Factory.CreateClient();

        var res = await client.GetAsync("/api/events");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_CreateEvent_ThenAnyoneGetsDetailWithCategory()
    {
        var organiser = await RegisterAndLoginAsync("Ola", "ola-events@test.co.za", "Organiser");
        int eventId = await CreateEventAsync(organiser, "Campus 5K");
        int categoryId = await CreateCategoryAsync(organiser, eventId, "Students");

        var res = await Factory.CreateClient().GetAsync($"/api/events/{eventId}");
        var root = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal("Campus 5K", root.GetProperty("eventName").GetString());
        Assert.Equal(1, root.GetProperty("categories").GetArrayLength());
        Assert.Equal(categoryId, root.GetProperty("categories")[0].GetProperty("categoryId").GetInt32());
    }

    [Fact]
    public async Task GetEvent_UnknownId_Returns404()
    {
        var client = Factory.CreateClient();

        var res = await client.GetAsync("/api/events/999999");

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithBadEventType_Returns400()
    {
        var organiser = await RegisterAndLoginAsync("Evan", "evan-events@test.co.za", "Organiser");

        var res = await organiser.PostAsync("/api/events", Json(new
        {
            eventName = "Swim Sprint",
            eventDate = "2026-12-06T09:00:00",
            location = "Stadium",
            distance = "5km",
            eventType = "Marathon Swim"
        }));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_UpdateEvent_Returns200WithNewName()
    {
        var organiser = await RegisterAndLoginAsync("Otto", "otto-events@test.co.za", "Organiser");
        int eventId = await CreateEventAsync(organiser);

        var res = await organiser.PutAsync($"/api/events/{eventId}", Json(new
        {
            eventName = "Renamed Run",
            eventDate = "2026-12-06T09:00:00",
            location = "Stadium",
            distance = "5km",
            eventType = "Run"
        }));

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("Renamed Run", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task OtherOrganiser_Update_Returns403()
    {
        var owner = await RegisterAndLoginAsync("Owner One", "owner1-events@test.co.za", "Organiser");
        int eventId = await CreateEventAsync(owner);
        var other = await RegisterAndLoginAsync("Owner Two", "owner2-events@test.co.za", "Organiser");

        var res = await other.PutAsync($"/api/events/{eventId}", Json(new
        {
            eventName = "Hijacked",
            eventDate = "2026-12-06T09:00:00",
            location = "Stadium",
            distance = "5km",
            eventType = "Run"
        }));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task OtherOrganiser_Delete_Returns403_EventStillExists()
    {
        var owner = await RegisterAndLoginAsync("Owner A", "ownera-events@test.co.za", "Organiser");
        int eventId = await CreateEventAsync(owner);
        var other = await RegisterAndLoginAsync("Owner B", "ownerb-events@test.co.za", "Organiser");

        var res = await other.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await Factory.CreateClient().GetAsync($"/api/events/{eventId}")).StatusCode);
    }

    [Fact]
    public async Task Organiser_DeleteEvent_ThenGetReturns404()
    {
        var organiser = await RegisterAndLoginAsync("Dana", "dana-events@test.co.za", "Organiser");
        int eventId = await CreateEventAsync(organiser);

        var res = await organiser.DeleteAsync($"/api/events/{eventId}");

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await Factory.CreateClient().GetAsync($"/api/events/{eventId}")).StatusCode);
    }
}
