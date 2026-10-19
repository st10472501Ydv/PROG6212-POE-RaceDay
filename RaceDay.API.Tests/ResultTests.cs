using System.Net;
using System.Text.Json;

namespace RaceDay.API.Tests;

public class ResultTests : ApiTestBase, IClassFixture<ApiFactory>
{
    public ResultTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Organiser_CapturesResult_ThenPublicResultsShowParticipant()
    {
        var (organiser, eventId, categoryId) = await EventWithCategoryAsync("res1");
        var participant = await RegisterAndLoginAsync("Ria", "ria-result@test.co.za", "Participant");
        var enrolRes = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        int enrolmentId = IdOf(await enrolRes.Content.ReadAsStringAsync(), "enrolmentId");

        var res = await organiser.PostAsync($"/api/events/{eventId}/results",
            Json(new { enrolmentId, finishTime = "00:42:10", position = 1, status = "Finished" }));
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var list = await Factory.CreateClient().GetAsync($"/api/events/{eventId}/results");
        var row = JsonDocument.Parse(await list.Content.ReadAsStringAsync()).RootElement[0];
        Assert.Equal("Ria", row.GetProperty("participantName").GetString());
        Assert.Equal(1, row.GetProperty("position").GetInt32());
    }

    [Fact]
    public async Task CaptureSameEnrolmentTwice_Returns409()
    {
        var (organiser, eventId, categoryId) = await EventWithCategoryAsync("res2");
        var participant = await RegisterAndLoginAsync("Rin", "rin-result@test.co.za", "Participant");
        var enrolRes = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        int enrolmentId = IdOf(await enrolRes.Content.ReadAsStringAsync(), "enrolmentId");
        var body = Json(new { enrolmentId, finishTime = "00:45:00", position = 2, status = "Finished" });

        var first = await organiser.PostAsync($"/api/events/{eventId}/results", body);
        var second = await organiser.PostAsync($"/api/events/{eventId}/results", body);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Capture_UnknownEnrolment_Returns400()
    {
        var (organiser, eventId, _) = await EventWithCategoryAsync("res3");

        var res = await organiser.PostAsync($"/api/events/{eventId}/results",
            Json(new { enrolmentId = 999999, finishTime = "00:30:00", position = 1, status = "Finished" }));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Participant_CapturesResult_Returns403()
    {
        var (_, eventId, _) = await EventWithCategoryAsync("res4");
        var participant = await RegisterAndLoginAsync("Rob", "rob-result@test.co.za", "Participant");

        var res = await participant.PostAsync($"/api/events/{eventId}/results",
            Json(new { enrolmentId = 1, finishTime = "00:30:00", position = 1, status = "Finished" }));

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }

    [Fact]
    public async Task Participant_ReadsOwnResult_FromMyResults()
    {
        var (organiser, eventId, categoryId) = await EventWithCategoryAsync("res5");
        var participant = await RegisterAndLoginAsync("Ruth", "ruth-result@test.co.za", "Participant");
        var enrolRes = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        int enrolmentId = IdOf(await enrolRes.Content.ReadAsStringAsync(), "enrolmentId");
        await organiser.PostAsync($"/api/events/{eventId}/results",
            Json(new { enrolmentId, finishTime = "01:02:03", position = 3, status = "Finished" }));

        var res = await participant.GetAsync("/api/users/results");
        var rows = JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement;

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, rows.GetArrayLength());
        Assert.Equal("Finished", rows[0].GetProperty("status").GetString());
    }
}
