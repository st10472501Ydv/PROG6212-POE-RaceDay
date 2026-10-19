using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Testing;

namespace RaceDay.API.Tests;

// Shared helpers for every test class: sign users in over HTTP, build JSON bodies,
// and read ids out of responses. Each test uses its own emails so users never clash.
public abstract class ApiTestBase
{
    protected ApiTestBase(ApiFactory factory) => Factory = factory;

    protected ApiFactory Factory { get; }

    protected static StringContent Json(object data) =>
        new(JsonSerializer.Serialize(data), Encoding.UTF8, "application/json");

    protected static int IdOf(string json, string property) =>
        JsonDocument.Parse(json).RootElement.GetProperty(property).GetInt32();

    protected async Task<HttpClient> RegisterAndLoginAsync(string fullName, string email, string role)
    {
        var client = Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true });
        var res = await client.PostAsync("/api/auth/register",
            Json(new { fullName, email, password = "Test123!", role }));
        res.EnsureSuccessStatusCode();
        res = await client.PostAsync("/api/auth/login", Json(new { email, password = "Test123!" }));
        res.EnsureSuccessStatusCode();
        return client;
    }

    protected async Task<int> CreateEventAsync(HttpClient organiser, string name = "Test 10K")
    {
        var res = await organiser.PostAsync("/api/events", Json(new
        {
            eventName = name,
            description = "created by a test",
            eventDate = "2026-12-05T09:00:00",
            location = "Campus Park",
            distance = "10km",
            eventType = "Run"
        }));
        res.EnsureSuccessStatusCode();
        return IdOf(await res.Content.ReadAsStringAsync(), "eventId");
    }

    protected async Task<int> CreateCategoryAsync(HttpClient organiser, int eventId, string name = "Open", int maxParticipants = 0)
    {
        var res = await organiser.PostAsync($"/api/events/{eventId}/categories",
            Json(new { categoryName = name, maxParticipants, entryFee = 25 }));
        res.EnsureSuccessStatusCode();
        return IdOf(await res.Content.ReadAsStringAsync(), "categoryId");
    }

    // One organiser + one event + one category, ready for enrolment or result tests.
    protected async Task<(HttpClient Organiser, int EventId, int CategoryId)> EventWithCategoryAsync(string tag, int maxParticipants = 0)
    {
        var organiser = await RegisterAndLoginAsync($"Organiser {tag}", $"o-{tag}@enrol.test", "Organiser");
        int eventId = await CreateEventAsync(organiser, $"Event {tag}");
        int categoryId = await CreateCategoryAsync(organiser, eventId, "Open", maxParticipants);
        return (organiser, eventId, categoryId);
    }
}
