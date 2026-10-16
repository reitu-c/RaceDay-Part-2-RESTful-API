using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using RaceDay.Part2.Authorization;
using RaceDay.Part2.Data;
using RaceDay.Part2.Models;
using Xunit;

namespace RaceDay.Part2.Tests;

public class SessionAuthorizationTests
{
    [Fact]
    public async Task MissingSession_Returns401Unauthorized()
    {
        //Arrange
        var context = CreateContext();
        var httpContext = new DefaultHttpContext();
        httpContext.Session = new TestSession();

        var actionContext = CreateActionContext(httpContext);
        var filterContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var attribute = new SessionAuthorizationAttribute();
        var nextCalled = false;

        // Act
        await attribute.OnActionExecutionAsync(
            filterContext,
            () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(
                    new ActionExecutedContext(
                        actionContext,
                        new List<IFilterMetadata>(),
                        new object()));
            });

        // Assert
        Assert.IsType<UnauthorizedObjectResult>(filterContext.Result);
        Assert.False(nextCalled);

        await context.DisposeAsync();
    }


    [Fact]
    public async Task ParticipantAccessingOrganiserResource_Returns403Forbidden()
    {
        // Arrange
        await using var context = CreateContext();

        var user = new User
        {
            FullName = "Test Participant",
            Email = "participant@test.com",
            PasswordHash = "TestHash",
            Role = "Participant"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(context)
            .BuildServiceProvider();

        var session = new TestSession();
        session.SetInt32("UserID", user.UserID);
        session.SetString("Role", "Participant");
        httpContext.Session = session;

        var actionContext = CreateActionContext(httpContext);
        var filterContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var attribute = new SessionAuthorizationAttribute("Organiser");
        var nextCalled = false;

        // Act
        await attribute.OnActionExecutionAsync(
            filterContext,
            () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(
                    new ActionExecutedContext(
                        actionContext,
                        new List<IFilterMetadata>(),
                        new object()));
            });

        //Assert
        var result = Assert.IsType<ObjectResult>(filterContext.Result);
        Assert.Equal(StatusCodes.Status403Forbidden, result.StatusCode);
        Assert.False(nextCalled);
    }

    [Fact]
    public async Task ChangedDatabaseRole_Returns401AndClearsSession()
    {
        //Arrange
        await using var context = CreateContext();

        var user = new User
        {
            FullName = "Test Organiser",
            Email = "organiser@test.com",
            PasswordHash = "TestHash",
            Role = "Participant"
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        var httpContext = new DefaultHttpContext();
        httpContext.RequestServices = new ServiceCollection()
            .AddSingleton(context)
            .BuildServiceProvider();

        var session = new TestSession();
        session.SetInt32("UserID", user.UserID);
        session.SetString("Role", "Organiser");
        httpContext.Session = session;

        var actionContext = CreateActionContext(httpContext);
        var filterContext = new ActionExecutingContext(
            actionContext,
            new List<IFilterMetadata>(),
            new Dictionary<string, object?>(),
            new object());

        var attribute = new SessionAuthorizationAttribute();
        var nextCalled = false;

        //Act
        await attribute.OnActionExecutionAsync(
            filterContext,
            () =>
            {
                nextCalled = true;
                return Task.FromResult<ActionExecutedContext>(
                    new ActionExecutedContext(
                        actionContext,
                        new List<IFilterMetadata>(),
                        new object()));
            });

        //Assert
        Assert.IsType<UnauthorizedObjectResult>(filterContext.Result);
        Assert.Empty(session.Keys);
        Assert.False(nextCalled);
    }

    private static RaceDayContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new RaceDayContext(options);
    }

    private static ActionContext CreateActionContext(
        HttpContext httpContext)
    {
        return new ActionContext(
            httpContext,
            new Microsoft.AspNetCore.Routing.RouteData(),
            new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor());
    }

    private sealed class TestSession : ISession
    {
        private readonly Dictionary<string, byte[]> _values = new();

        public bool IsAvailable => true;
        public string Id => "TestSession";
        public IEnumerable<string> Keys => _values.Keys;

        public void Clear() => _values.Clear();

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task LoadAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public void Remove(string key) => _values.Remove(key);

        public void Set(string key, byte[] value)
            => _values[key] = value;

        public bool TryGetValue(string key, out byte[]? value)
            => _values.TryGetValue(key, out value);

        public void SetInt32(string key, int value)
        {
            byte[] bytes = new byte[4];

            System.Buffers.Binary.BinaryPrimitives
                .WriteInt32BigEndian(bytes, value);

            Set(key, bytes);
        }

        public void SetString(string key, string value)
        {
            Set(key, System.Text.Encoding.UTF8.GetBytes(value));
        }

    }
}

