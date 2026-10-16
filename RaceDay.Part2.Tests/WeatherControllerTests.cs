using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Controllers;
using RaceDay.Part2.Data;
using RaceDay.Part2.Models;
using System.Threading;
using Xunit;

namespace RaceDay.Part2.Tests;

public class WeatherControllerTests
{
    private static async Task<(RaceDayContext Context, WeatherController Controller)>
        CreateTestSetupAsync()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayContext(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSessionForWeather();
        httpContext.Session.SetInt32("UserID", 1);
        httpContext.Session.SetString("Role", "Organiser");

        var controller = new WeatherController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return (context, controller);
    }

    private static Event CreateEvent(int organiserId = 1)
    {
        return new Event
        {
            EventName = "Pretoria Road Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = organiserId
        };
    }

    private static Weather CreateWeather(int eventId, DateTime recordedAt)
    {
        return new Weather
        {
            EventID = eventId,
            RecordedAt = recordedAt,
            TemperatureC = 22m,
            Condition = "Sunny",
            WindSpeedKmh = 10m,
            RainProbability = 20
        };
    }

    [Fact]
    public async Task GetWeatherRecords_ReturnsRecordsNewestFirst()
    {
        var (context, controller) = await CreateTestSetupAsync();

        context.WeatherRecords.AddRange(
            CreateWeather(1, DateTime.Today.AddDays(-2)),
            CreateWeather(1, DateTime.Today));

        await context.SaveChangesAsync();

        var result = await controller.GetWeatherRecords();

        var ok = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsAssignableFrom<IEnumerable<Weather>>(ok.Value);

        Assert.Equal(2, records.Count());
        Assert.True(records.First().RecordedAt >= records.Last().RecordedAt);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetWeatherRecord_WhenRecordDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var result = await controller.GetWeatherRecord(999);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetWeatherByEvent_WhenEventDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var result = await controller.GetWeatherByEvent(999);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetWeatherByEvent_ReturnsOnlyRecordsForRequestedEvent()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var firstEvent = CreateEvent();
        var secondEvent = CreateEvent(2);

        context.Events.AddRange(firstEvent, secondEvent);
        await context.SaveChangesAsync();

        context.WeatherRecords.AddRange(
            CreateWeather(firstEvent.EventID, DateTime.Today),
            CreateWeather(secondEvent.EventID, DateTime.Today));

        await context.SaveChangesAsync();

        var result = await controller.GetWeatherByEvent(firstEvent.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var records = Assert.IsAssignableFrom<IEnumerable<Weather>>(ok.Value);

        Assert.Single(records);
        Assert.All(records, r => Assert.Equal(firstEvent.EventID, r.EventID));

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateWeatherRecord_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent(2);
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.WeatherCreateRequest
        {
            EventID = raceEvent.EventID,
            RecordedAt = DateTime.Now,
            TemperatureC = 22m,
            Condition = "Sunny",
            WindSpeedKmh = 10m,
            RainProbability = 20
        };

        var result = await controller.CreateWeatherRecord(request);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(await context.WeatherRecords.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateWeatherRecord_WithValidRequest_ReturnsCreated()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.WeatherCreateRequest
        {
            EventID = raceEvent.EventID,
            RecordedAt = DateTime.Now,
            TemperatureC = 22m,
            Condition = "  Sunny  ",
            WindSpeedKmh = 10m,
            RainProbability = 20
        };

        var result = await controller.CreateWeatherRecord(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var record = Assert.IsType<Weather>(created.Value);

        Assert.Equal("Sunny", record.Condition);
        Assert.Single(await context.WeatherRecords.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateWeatherRecord_WhenWindSpeedIsNegative_ReturnsBadRequest()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.WeatherCreateRequest
        {
            EventID = raceEvent.EventID,
            RecordedAt = DateTime.Now,
            TemperatureC = 22m,
            Condition = "Sunny",
            WindSpeedKmh = -1m,
            RainProbability = 20
        };

        var result = await controller.CreateWeatherRecord(request);

        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await context.WeatherRecords.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task UpdateWeatherRecord_WhenRecordDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var request = new RaceDay.Part2.DTOs.WeatherUpdateRequest
        {
            RecordedAt = DateTime.Now,
            TemperatureC = 25m,
            Condition = "Cloudy",
            WindSpeedKmh = 12m,
            RainProbability = 30
        };

        var result = await controller.UpdateWeatherRecord(999, request);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task DeleteWeatherRecord_WhenOrganiserOwnsRecord_ReturnsOkAndDeletesRecord()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var record = CreateWeather(raceEvent.EventID, DateTime.Now);
        context.WeatherRecords.Add(record);
        await context.SaveChangesAsync();

        var result = await controller.DeleteWeatherRecord(record.WeatherID);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await context.WeatherRecords.ToListAsync());

        await context.DisposeAsync();
    }

    private sealed class TestSessionForWeather : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "WeatherTestSession";
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value)
            => _store[key] = value;

        public bool TryGetValue(string key, out byte[]? value)
        {
            if (_store.TryGetValue(key, out var storedValue))
            {
                value = storedValue;
                return true;
            }

            value = null;
            return false;
        }

        public Task LoadAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitAsync(CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
