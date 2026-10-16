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

public class RoutesControllerTests
{
    private static async Task<(RaceDayContext Context, RoutesController Controller)>
        CreateTestSetupAsync()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        var context = new RaceDayContext(options);

        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSessionForRoutes();
        httpContext.Session.SetInt32("UserID", 1);
        httpContext.Session.SetString("Role", "Organiser");

        var controller = new RoutesController(context)
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

    private static RaceRoute CreateRoute(int eventId)
    {
        return new RaceRoute
        {
            EventID = eventId,
            RouteName = "Main Route",
            DistanceKM = 10m,
            StartPoint = "City Hall",
            FinishPoint = "Sports Ground",
            RouteURL = "https://example.com/route"
        };
    }

    [Fact]
    public async Task GetRoutes_ReturnsRoutesOrderedByEvent()
    {
        var (context, controller) = await CreateTestSetupAsync();

        context.Routes.AddRange(
            CreateRoute(2),
            CreateRoute(1));

        await context.SaveChangesAsync();

        var result = await controller.GetRoutes();

        var ok = Assert.IsType<OkObjectResult>(result);
        var routes = Assert.IsAssignableFrom<IEnumerable<RaceRoute>>(ok.Value);

        Assert.Equal(new[] { 1, 2 }, routes.Select(r => r.EventID));

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetRoute_WhenRouteDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var result = await controller.GetRoute(999);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetRoute_WhenRouteExists_ReturnsOk()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var route = CreateRoute(raceEvent.EventID);
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var result = await controller.GetRoute(route.RouteID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedRoute = Assert.IsType<RaceRoute>(ok.Value);

        Assert.Equal(route.RouteID, returnedRoute.RouteID);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetRoutesByEvent_WhenEventDoesNotExist_ReturnsNotFound()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var result = await controller.GetRoutesByEvent(999);

        Assert.IsType<NotFoundObjectResult>(result);

        await context.DisposeAsync();
    }

    [Fact]
    public async Task GetRoutesByEvent_ReturnsOnlyRoutesForRequestedEvent()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var firstEvent = CreateEvent();
        var secondEvent = CreateEvent(2);

        context.Events.AddRange(firstEvent, secondEvent);
        await context.SaveChangesAsync();

        context.Routes.AddRange(
            CreateRoute(firstEvent.EventID),
            CreateRoute(secondEvent.EventID));

        await context.SaveChangesAsync();

        var result = await controller.GetRoutesByEvent(firstEvent.EventID);

        var ok = Assert.IsType<OkObjectResult>(result);
        var routes = Assert.IsAssignableFrom<IEnumerable<RaceRoute>>(ok.Value);

        Assert.Single(routes);
        Assert.All(routes, r => Assert.Equal(firstEvent.EventID, r.EventID));

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateRoute_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent(organiserId: 2);
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.RouteCreateRequest
        {
            EventID = raceEvent.EventID,
            RouteName = "Main Route",
            DistanceKM = 10m,
            StartPoint = "City Hall",
            FinishPoint = "Sports Ground",
            RouteURL = "https://example.com/route"
        };

        var result = await controller.CreateRoute(request);

        var forbidden = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status403Forbidden, forbidden.StatusCode);
        Assert.Empty(await context.Routes.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task CreateRoute_WhenEventIsOpenAndOrganiserOwnsIt_ReturnsCreated()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var request = new RaceDay.Part2.DTOs.RouteCreateRequest
        {
            EventID = raceEvent.EventID,
            RouteName = "  Main Route  ",
            DistanceKM = 10m,
            StartPoint = "  City Hall  ",
            FinishPoint = "  Sports Ground  ",
            RouteURL = "  https://example.com/route  "
        };

        var result = await controller.CreateRoute(request);

        var created = Assert.IsType<CreatedAtActionResult>(result);
        var route = Assert.IsType<RaceRoute>(created.Value);

        Assert.Equal("Main Route", route.RouteName);
        Assert.Equal("City Hall", route.StartPoint);
        Assert.Equal("Sports Ground", route.FinishPoint);
        Assert.Equal("https://example.com/route", route.RouteURL);
        Assert.Single(await context.Routes.ToListAsync());

        await context.DisposeAsync();
    }

    [Fact]
    public async Task DeleteRoute_WhenOrganiserOwnsRoute_ReturnsOkAndRemovesRoute()
    {
        var (context, controller) = await CreateTestSetupAsync();

        var raceEvent = CreateEvent();
        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var route = CreateRoute(raceEvent.EventID);
        context.Routes.Add(route);
        await context.SaveChangesAsync();

        var result = await controller.DeleteRoute(route.RouteID);

        Assert.IsType<OkObjectResult>(result);
        Assert.Empty(await context.Routes.ToListAsync());

        await context.DisposeAsync();
    }

    private sealed class TestSessionForRoutes : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "RoutesTestSession";
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
