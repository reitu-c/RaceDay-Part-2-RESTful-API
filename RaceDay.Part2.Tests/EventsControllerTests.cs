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
using System.Threading.Tasks;
using Xunit;

namespace RaceDay.Part2.Tests;

public class EventsControllerTests
{
    [Fact]
    public async Task GetEvents_ReturnsEventsOrderedByDate()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        context.Events.AddRange(
            new Event
            {
                EventName = "Later Event",
                EventDate = new DateTime(2027, 6, 15),
                Location = "Pretoria",
                Status = "Open"
            },
            new Event
            {
                EventName = "Earlier Event",
                EventDate = new DateTime(2027, 3, 10),
                Location = "Johannesburg",
                Status = "Open"
            });

        await context.SaveChangesAsync();

        var controller = new EventsController(context);

        //Act
        var result = await controller.GetEvents();

        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var events = Assert.IsAssignableFrom<IEnumerable<Event>>(
            okResult.Value);

        var orderedEvents = events.ToList();

        Assert.Equal(2, orderedEvents.Count);
        Assert.Equal("Earlier Event", orderedEvents[0].EventName);
        Assert.Equal("Later Event", orderedEvents[1].EventName);
    }


    [Fact]
    public async Task GetEvent_WhenEventDoesNotExist_ReturnsNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);
        var controller = new EventsController(context);

        //Act
        var result = await controller.GetEvent(999);

        //Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFoundResult.StatusCode);
    }


    [Fact]
    public async Task CreateEvent_WhenDateIsMissing_ReturnsBadRequest()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);
        var controller = new EventsController(context);

        var request = new EventCreateRequest
        {
            EventName = "Test Event",
            EventDate = default,
            Location = "Pretoria",
            Status = "Open"
        };

        //Act
        var result = await controller.CreateEvent(request);

        //Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await context.Events.ToListAsync());
    }

    [Fact]
    public async Task CreateEvent_WhenStatusIsInvalid_ReturnsBadRequest()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);
        var controller = new EventsController(context);

        var request = new RaceDay.Part2.DTOs.EventCreateRequest
        {
            EventName = "Test Event",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Pending"
        };

        //Act
        var result = await controller.CreateEvent(request);

        //Assert
        Assert.IsType<BadRequestObjectResult>(result);
        Assert.Empty(await context.Events.ToListAsync());
    }

    [Fact]
    public async Task UpdateEvent_WhenOrganiserDoesNotOwnEvent_ReturnsNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        context.Events.Add(new Event
        {
            EventName = "Another Organiser's Event",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 1
        });

        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new Microsoft.AspNetCore.Http.DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        var request = new RaceDay.Part2.DTOs.EventUpdateRequest
        {
            EventName = "Updated Event",
            EventDate = DateTime.Today.AddDays(40),
            Location = "Johannesburg",
            Status = "Open"
        };

        //Act
        var result = await controller.UpdateEvent(1, request);

        //Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    private sealed class TestSessionForEvents : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;

        public string Id => "EventsTestSession";

        public bool IsAvailable => true;

        public void Clear() => _store.Clear();

        public void Remove(string key) => _store.Remove(key);

        public void Set(string key, byte[] value)
        {
            _store[key] = value;
        }

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

        public Task LoadAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    [Fact]
    public async Task DeleteEvent_WhenOrganiserDoesNotOwnEvent_ReturnsNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        context.Events.Add(new Event
        {
            EventName = "Another Organiser's Event",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 1
        });

        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        //Act
        var result = await controller.DeleteEvent(1);

        //Assert
        Assert.IsType<NotFoundObjectResult>(result);

        var eventStillExists = await context.Events
            .AnyAsync(e => e.EventID == 1);

        Assert.True(eventStillExists);
    }

    [Fact]
    public async Task DeleteEvent_WhenEventHasCategories_ReturnsConflict()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var eventItem = new Event
        {
            EventName = "Test Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 2
        };

        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        context.Categories.Add(new Category
        {
            EventID = eventItem.EventID,
            CategoryName = "10 KM",
            DistanceKM = 10m,
            EntryFee = 100m
        });

        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        //Act
        var result = await controller.DeleteEvent(eventItem.EventID);

        //Assert
        Assert.IsType<ConflictObjectResult>(result);

        Assert.True(await context.Events.AnyAsync(
            e => e.EventID == eventItem.EventID));
    }

    [Fact]
    public async Task GetEvent_WhenEventExists_ReturnsOkWithEvent()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var eventItem = new Event
        {
            EventName = "Pretoria Road Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 1
        };

        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        var controller = new EventsController(context);

        //Act
        var result = await controller.GetEvent(eventItem.EventID);

        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedEvent = Assert.IsType<Event>(okResult.Value);

        Assert.Equal(eventItem.EventID, returnedEvent.EventID);
        Assert.Equal("Pretoria Road Race", returnedEvent.EventName);
    }

    [Fact]
    public async Task CreateEvent_WithValidRequest_ReturnsCreated()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        var request = new RaceDay.Part2.DTOs.EventCreateRequest
        {
            EventName = "  Community Road Race  ",
            EventDate = DateTime.Today.AddDays(30),
            Location = "  Pretoria  ",
            Description = "  A community running event  ",
            Status = "Open"
        };

        //Act
        var result = await controller.CreateEvent(request);

        //Assert
        var createdResult = Assert.IsType<CreatedAtActionResult>(result);
        var createdEvent = Assert.IsType<Event>(createdResult.Value);

        Assert.Equal("Community Road Race", createdEvent.EventName);
        Assert.Equal("Pretoria", createdEvent.Location);
        Assert.Equal("A community running event", createdEvent.Description);
        Assert.Equal(2, createdEvent.OrganiserID);
        Assert.Single(await context.Events.ToListAsync());
    }

    [Fact]
    public async Task UpdateEvent_WhenOrganiserOwnsEvent_ReturnsOk()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var eventItem = new Event
        {
            EventName = "Original Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 2
        };

        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        var request = new RaceDay.Part2.DTOs.EventUpdateRequest
        {
            EventName = "Updated Race",
            EventDate = DateTime.Today.AddDays(40),
            Location = "Johannesburg",
            Description = "Updated event description",
            Status = "Closed"
        };

        //Act
        var result = await controller.UpdateEvent(eventItem.EventID, request);

        //Assert
        Assert.IsType<OkObjectResult>(result);

        var savedEvent = await context.Events
            .SingleAsync(e => e.EventID == eventItem.EventID);

        Assert.Equal("Updated Race", savedEvent.EventName);
        Assert.Equal("Johannesburg", savedEvent.Location);
        Assert.Equal("Closed", savedEvent.Status);
    }

    [Fact]
    public async Task DeleteEvent_WhenOrganiserOwnsEventAndNoRelatedRecords_ReturnsNoContent()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var eventItem = new Event
        {
            EventName = "Race to Delete",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 2
        };

        context.Events.Add(eventItem);
        await context.SaveChangesAsync();

        var controller = new EventsController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForEvents();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        //Act
        var result = await controller.DeleteEvent(eventItem.EventID);

        //Assert
        Assert.IsType<NoContentResult>(result);

        Assert.False(await context.Events.AnyAsync(
            e => e.EventID == eventItem.EventID));
    }

}
