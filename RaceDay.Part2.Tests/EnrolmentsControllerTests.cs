using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Controllers;
using RaceDay.Part2.Data;
using RaceDay.Part2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RaceDay.Part2.Tests;

public class EnrolmentsControllerTests
{
    private static async Task<(RaceDayContext Context, EnrolmentsController Controller)>
        CreateTestSetupAsync()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayContext(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSessionForEnrolments();
        httpContext.Session.SetInt32("UserID", 3);

        var controller = new EnrolmentsController(context)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = httpContext
            }
        };

        return (context, controller);
    }

    [Fact]
    public async Task GetMyEnrolments_ReturnsOnlyCurrentParticipantsEnrolments()
    {
        var (context, controller) = await CreateTestSetupAsync();

        context.Enrolments.AddRange(
            new Enrolment
            {
                ParticipantID = 3,
                CategoryID = 1,
                EnrolmentDate = DateTime.Today,
                Status = "Confirmed"
            },
            new Enrolment
            {
                ParticipantID = 4,
                CategoryID = 2,
                EnrolmentDate = DateTime.Today,
                Status = "Confirmed"
            });

        await context.SaveChangesAsync();

        var result = await controller.GetMyEnrolments();

        var okResult = Assert.IsType<OkObjectResult>(result);
        var enrolments = Assert.IsAssignableFrom<IEnumerable<Enrolment>>(
            okResult.Value);

        Assert.Single(enrolments);
        Assert.All(enrolments, e => Assert.Equal(3, e.ParticipantID));

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetMyEnrolment_WhenEnrolmentBelongsToAnotherParticipant_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var enrolment = new Enrolment
        {
            ParticipantID = 4,
            CategoryID = 2,
            EnrolmentDate = DateTime.Today,
            Status = "Confirmed"
        };

        context.Enrolments.Add(enrolment);
        await context.SaveChangesAsync();

        var result = await controller.GetMyEnrolment(enrolment.EnrolmentID);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateEnrolment_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var request = new RaceDay.Part2.DTOs.EnrolmentCreateRequest
        {
            CategoryID = 999,
            EmergencyContact = "0712345678"
        };

        var result = await controller.CreateEnrolment(request);

        Assert.IsType<NotFoundObjectResult>(result);
        Assert.Empty(await context.Enrolments.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CancelEnrolment_WhenEnrolmentBelongsToParticipant_ReturnsOkAndUpdatesStatus()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var enrolment = new Enrolment
        {
            ParticipantID = 3,
            CategoryID = 1,
            EnrolmentDate = DateTime.Today,
            Status = "Confirmed"
        };

        context.Enrolments.Add(enrolment);
        await context.SaveChangesAsync();

        var result = await controller.CancelEnrolment(enrolment.EnrolmentID);

        Assert.IsType<OkObjectResult>(result);

        var savedEnrolment = await context.Enrolments
            .FirstAsync(e => e.EnrolmentID == enrolment.EnrolmentID);

        Assert.Equal("Cancelled", savedEnrolment.Status);

        await context.DisposeAsync();
    }

    private sealed class TestSessionForEnrolments : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "EnrolmentsTestSession";
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

