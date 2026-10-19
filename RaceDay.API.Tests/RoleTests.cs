using System.Net;

namespace RaceDay.API.Tests;

public class RoleTests : ApiTestBase, IClassFixture<ApiFactory>
{
    public RoleTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Profile_WithoutSession_Returns401()
    {
        var client = Factory.CreateClient();

        var res = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_WithoutSession_Returns401()
    {
        var client = Factory.CreateClient();

        var res = await client.PostAsync("/api/events", Json(new { eventName = "x" }));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task CreateEvent_AsParticipant_Returns403()
    {
        var participant = await RegisterAndLoginAsync("Pat", "pat-role@test.co.za", "Participant");

        var res = await participant.PostAsync("/api/events", Json(new
        {
            eventName = "Sneaky Event",
            eventDate = "2026-12-05T09:00:00",
            location = "Park",
            distance = "5km",
            eventType = "Run"
        }));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Enrol_AsOrganiser_Returns403()
    {
        var (organiser, _, categoryId) = await EventWithCategoryAsync("organiserenrol");

        var res = await organiser.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task OrganiserEnrolmentList_AsParticipant_Returns403()
    {
        var (_, eventId, _) = await EventWithCategoryAsync("participantlist");
        var participant = await RegisterAndLoginAsync("Pia", "pia-role@test.co.za", "Participant");

        var res = await participant.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
