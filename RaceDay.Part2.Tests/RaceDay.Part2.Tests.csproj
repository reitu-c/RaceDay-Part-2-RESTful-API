using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Controllers;
using RaceDay.Part2.Data;
using RaceDay.Part2.DTOs;
using RaceDay.Part2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RaceDay.Part2.Tests;

public class ResultsControllerTests
{
    private static async Task<(RaceDayContext Context, ResultsController Controller)>
        CreateTestSetupAsync()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayContext(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSessionForResults();
        httpContext.Session.SetInt32("UserID", 1);
        httpContext.Session.SetString("Role", "Organiser");

        var controller = new ResultsController(context)
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

    private static async Task<(Event Event, Category Category, Enrolment Enrolment)>
        AddEnrolmentSetupAsync(
            RaceDayContext context,
            int organiserId = 1)
    {
        var raceEvent = CreateEvent(organiserId);
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var category = new Category
        {
            EventID = raceEvent.EventID,
            CategoryName = "10 KM",
            DistanceKM = 10m,
            EntryFee = 100m,
            MaxParticipants = 100
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var enrolment = new Enrolment
        {
            ParticipantID = 3,
            CategoryID = category.CategoryID,
            EnrolmentDate = DateTime.Today,
            Status = "Confirmed"
        };

        context.Enrolments.Add(enrolment);
        await context.SaveChangesAsync();

        return (raceEvent, category, enrolment);
    }

    private static ResultCreateRequest CreateValidRequest(int enrolmentId)
    {
        return new ResultCreateRequest
        {
            EnrolmentID = enrolmentId,
            FinishTime = TimeSpan.FromMinutes(60),
            OverallPosition = 1,
            CategoryPosition = 1,
            ResultStatus = "Finished"
        };
    }

    [Fact]
    public async Task GetResults_ReturnsResultsOrderedByOverallPosition()
    {
        var (context, controller) = await CreateTestSetupAsync();

        context.Results.AddRange(
            new Result
            {
                EnrolmentID = 1,
                FinishTime = TimeSpan.FromMinutes(70),
                OverallPosition = 2,
                CategoryPosition = 2,
                ResultStatus = "Finished"
            },
            new Result
            {
                EnrolmentID = 2,
                FinishTime = TimeSpan.FromMinutes(60),
                OverallPosition = 1,
                CategoryPosition = 1,
                ResultStatus = "Finished"
            });

        await context.SaveChangesAsync();

        var response = await controller.GetResults();

        var ok = Assert.IsType<OkObjectResult>(response);
        var results = Assert.IsAssignableFrom<IEnumerable<Result>>(ok.Value);

        Assert.Equal(new int?[] { 1, 2 }, results.Select(r => r.OverallPosition).ToArray());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetResult_WhenResultDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var response = await controller.GetResult(999);

        Assert.IsType<NotFoundObjectResult>(response);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetResultByEnrolment_WhenResultDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var response = await controller.GetResultByEnrolment(999);

        Assert.IsType<NotFoundObjectResult>(response);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateResult_WhenEnrolmentDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var response = await controller.CreateResult(CreateValidRequest(999));

        Assert.IsType<NotFoundObjectResult>(response);
        Assert.Empty(await context.Results.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateResult_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
    {
        var (context, controller) = await CreateTestSetupAsync();
        var setup = await AddEnrolmentSetupAsync(context, organiserId: 2);

        var response = await controller.CreateResult(
            CreateValidRequest(setup.Enrolment.EnrolmentID));

        var forbidden = Assert.IsType<ObjectResult>(response);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(await context.Results.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateResult_WhenFinishedHasNoFinishTime_ReturnsBadRequest()
    {
        var (context, controller) = await CreateTestSetupAsync();
        var setup = await AddEnrolmentSetupAsync(context);

        var request = CreateValidRequest(setup.Enrolment.EnrolmentID);
        request.FinishTime = null;

        var response = await controller.CreateResult(request);

        Assert.IsType<BadRequestObjectResult>(response);
        Assert.Empty(await context.Results.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateResult_WhenValidRequestIsProvided_ReturnsCreated()
    {
        var (context, controller) = await CreateTestSetupAsync();
        var setup = await AddEnrolmentSetupAsync(context);

        var response = await controller.CreateResult(
            CreateValidRequest(setup.Enrolment.EnrolmentID));

        var created = Assert.IsType<CreatedAtActionResult>(response);
        var result = Assert.IsType<Result>(created.Value);

        Assert.Equal(setup.Enrolment.EnrolmentID, result.EnrolmentID);
        Assert.Equal("Finished", result.ResultStatus);
        Assert.Single(await context.Results.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task UpdateResult_WhenResultDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var request = new RaceDay.Part2.DTOs.ResultUpdateRequest
        {
            FinishTime = TimeSpan.FromMinutes(55),
            OverallPosition = 1,
            CategoryPosition = 1,
            ResultStatus = "Finished"
        };

        var response = await controller.UpdateResult(999, request);

        Assert.IsType<NotFoundObjectResult>(response);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task UpdateResult_WhenValidRequestIsProvided_UpdatesResult()
    {
        var (context, controller) = await CreateTestSetupAsync();
        var setup = await AddEnrolmentSetupAsync(context);

        var result = new Result
        {
            EnrolmentID = setup.Enrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(70),
            OverallPosition = 2,
            CategoryPosition = 2,
            ResultStatus = "Finished"
        };

        context.Results.Add(result);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.ResultUpdateRequest
        {
            FinishTime = TimeSpan.FromMinutes(55),
            OverallPosition = 1,
            CategoryPosition = 1,
            ResultStatus = "Finished"
        };

        var response = await controller.UpdateResult(result.ResultID, request);

        Assert.IsType<OkObjectResult>(response);

        var saved = await context.Results.FirstAsync(
            r => r.ResultID == result.ResultID);

        Assert.Equal(1, saved.OverallPosition);
        Assert.Equal(TimeSpan.FromMinutes(55), saved.FinishTime);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task DeleteResult_WhenResultDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var response = await controller.DeleteResult(999);

        Assert.IsType<NotFoundObjectResult>(response);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task DeleteResult_WhenOrganiserOwnsEvent_DeletesResult()
    {
        var (context, controller) = await CreateTestSetupAsync();
        var setup = await AddEnrolmentSetupAsync(context);

        var result = new Result
        {
            EnrolmentID = setup.Enrolment.EnrolmentID,
            FinishTime = TimeSpan.FromMinutes(60),
            OverallPosition = 1,
            CategoryPosition = 1,
            ResultStatus = "Finished"
        };

        context.Results.Add(result);
        await context.SaveChangesAsync();

        var response = await controller.DeleteResult(result.ResultID);

        Assert.IsType<OkObjectResult>(response);
        Assert.Empty(await context.Results.ToListAsync());

        await context.DisposeAsync();
    }

    private sealed class TestSessionForResults : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "ResultsTestSession";
        public bool IsAvailable => true;

        public void Clear() => _store.Clear();

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value) => _store[key] = value;

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
