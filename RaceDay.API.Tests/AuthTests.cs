using System.Net;

namespace RaceDay.API.Tests;

public class AuthTests : ApiTestBase, IClassFixture<ApiFactory>
{
    public AuthTests(ApiFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_NewUser_Returns201WithUserId()
    {
        var client = Factory.CreateClient();

        var res = await client.PostAsync("/api/auth/register",
            Json(new { fullName = "Alice Organiser", email = "alice-auth@test.co.za", password = "Test123!", role = "Organiser" }));

        Assert.Equal(HttpStatusCode.Created, res.StatusCode);
        Assert.True(IdOf(await res.Content.ReadAsStringAsync(), "userId") > 0);
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400()
    {
        var client = Factory.CreateClient();
        var body = Json(new { fullName = "Bob", email = "bob-auth@test.co.za", password = "Test123!", role = "Participant" });
        Assert.Equal(HttpStatusCode.Created, (await client.PostAsync("/api/auth/register", body)).StatusCode);

        var res = await client.PostAsync("/api/auth/register", body);

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Register_RoleOtherThanOrganiserOrParticipant_Returns400()
    {
        var client = Factory.CreateClient();

        var res = await client.PostAsync("/api/auth/register",
            Json(new { fullName = "Eve", email = "eve-auth@test.co.za", password = "Test123!", role = "Admin" }));

        Assert.Equal(HttpStatusCode.BadRequest, res.StatusCode);
    }

    [Fact]
    public async Task Login_WithCorrectPassword_ReturnsProfileWithOwnEmail()
    {
        var client = await RegisterAndLoginAsync("Frank", "frank-auth@test.co.za", "Participant");

        var res = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        Assert.Contains("frank-auth@test.co.za", await res.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        var client = Factory.CreateClient();
        await client.PostAsync("/api/auth/register",
            Json(new { fullName = "Gail", email = "gail-auth@test.co.za", password = "Test123!", role = "Participant" }));

        var res = await client.PostAsync("/api/auth/login",
            Json(new { email = "gail-auth@test.co.za", password = "WrongPassword1!" }));

        Assert.Equal(HttpStatusCode.Unauthorized, res.StatusCode);
    }

    [Fact]
    public async Task Logout_ClearsSession_NextRequestIs401()
    {
        var client = await RegisterAndLoginAsync("Hank", "hank-auth@test.co.za", "Participant");

        var afterLogout = await client.PostAsync("/api/auth/logout", null);
        var profile = await client.GetAsync("/api/users/profile");

        Assert.Equal(HttpStatusCode.OK, afterLogout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, profile.StatusCode);
    }
}
