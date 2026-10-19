using System.Net;
using System.Text.Json;

namespace RaceDay.API.Tests;

public class EnrolmentTests : ApiTestBase, IClassFixture<ApiFactory>
{
    public EnrolmentTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Participant_Enrols_ThenAppearsInOwnList()
    {
        var (_, _, categoryId) = await EventWithCategoryAsync("join1");
        var participant = await RegisterAndLoginAsync("Paul", "paul-enrol@test.co.za", "Participant");

        var res = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        Assert.Equal(HttpStatusCode.Created, res.StatusCode);

        var list = await participant.GetAsync("/api/users/enrolments");
        Assert.Equal(HttpStatusCode.OK, list.StatusCode);
        Assert.Equal(1, JsonDocument.Parse(await list.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Participant_EnrolsTwice_Returns409()
    {
        var (_, _, categoryId) = await EventWithCategoryAsync("dup1");
        var participant = await RegisterAndLoginAsync("Penny", "penny-enrol@test.co.za", "Participant");

        var first = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var second = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task Enrol_IntoFullCategory_Returns409()
    {
        var (_, _, categoryId) = await EventWithCategoryAsync("full1", maxParticipants: 1);
        var first = await RegisterAndLoginAsync("First", "first-enrol@test.co.za", "Participant");
        var second = await RegisterAndLoginAsync("Second", "second-enrol@test.co.za", "Participant");

        var firstRes = await first.PostAsync($"/api/categories/{categoryId}/enrol", null);
        var secondRes = await second.PostAsync($"/api/categories/{categoryId}/enrol", null);

        Assert.Equal(HttpStatusCode.Created, firstRes.StatusCode);
        Assert.Equal(HttpStatusCode.Conflict, secondRes.StatusCode);
    }

    [Fact]
    public async Task Enrol_UnknownCategory_Returns404()
    {
        var participant = await RegisterAndLoginAsync("Nina", "nina-enrol@test.co.za", "Participant");

        var res = await participant.PostAsync("/api/categories/999999/enrol", null);

        Assert.Equal(HttpStatusCode.NotFound, res.StatusCode);
    }

    [Fact]
    public async Task Organiser_SeesEnrolmentOnOwnEvent()
    {
        var (organiser, eventId, categoryId) = await EventWithCategoryAsync("view1");
        var participant = await RegisterAndLoginAsync("Vusi", "vusi-enrol@test.co.za", "Participant");
        await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);

        var res = await organiser.GetAsync($"/api/events/{eventId}/enrolments");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Equal(1, JsonDocument.Parse(await res.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Participant_CancelsOwnEnrolment_ThenGoneFromList()
    {
        var (_, _, categoryId) = await EventWithCategoryAsync("cancel1");
        var participant = await RegisterAndLoginAsync("Cara", "cara-enrol@test.co.za", "Participant");
        var enrolRes = await participant.PostAsync($"/api/categories/{categoryId}/enrol", null);
        int enrolmentId = IdOf(await enrolRes.Content.ReadAsStringAsync(), "enrolmentId");

        var res = await participant.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.NoContent, res.StatusCode);
        var list = await participant.GetAsync("/api/users/enrolments");
        Assert.Equal(0, JsonDocument.Parse(await list.Content.ReadAsStringAsync()).RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Cancel_SomeoneElsesEnrolment_Returns403()
    {
        var (_, _, categoryId) = await EventWithCategoryAsync("cancel2");
        var owner = await RegisterAndLoginAsync("Own", "own-cancel2@enrol.test", "Participant");
        var other = await RegisterAndLoginAsync("Oth", "oth-cancel2@enrol.test", "Participant");
        var enrolRes = await owner.PostAsync($"/api/categories/{categoryId}/enrol", null);
        int enrolmentId = IdOf(await enrolRes.Content.ReadAsStringAsync(), "enrolmentId");

        var res = await other.DeleteAsync($"/api/enrolments/{enrolmentId}");

        Assert.Equal(HttpStatusCode.Forbidden, res.StatusCode);
    }
}
