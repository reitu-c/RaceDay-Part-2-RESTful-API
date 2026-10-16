using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Part2.Controllers;
using RaceDay.Part2.Data;
using RaceDay.Part2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RaceDay.Part2.Tests;

public class CategoriesControllerTests
{
    [Fact]
    public async Task GetCategories_ReturnsCategoriesOrderedByEventAndName()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        context.Categories.AddRange(
            new Category
            {
                EventID = 2,
                CategoryName = "Walking",
                DistanceKM = 5m,
                EntryFee = 50m
            },
            new Category
            {
                EventID = 1,
                CategoryName = "10 KM",
                DistanceKM = 10m,
                EntryFee = 100m
            },
            new Category
            {
                EventID = 1,
                CategoryName = "5 KM",
                DistanceKM = 5m,
                EntryFee = 75m
            });

        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        //Act
        var result = await controller.GetCategories();

        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(
            okResult.Value);

        var returnedCategories = categories.ToList();

        Assert.Equal(3, returnedCategories.Count);
        Assert.Equal("10 KM", returnedCategories[0].CategoryName);
        Assert.Equal("5 KM", returnedCategories[1].CategoryName);
        Assert.Equal("Walking", returnedCategories[2].CategoryName);
    }

    [Fact]
    public async Task GetCategory_WhenCategoryDoesNotExist_ReturnsNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);
        var controller = new CategoriesController(context);

        //Act
        var result = await controller.GetCategory(999);

        //Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetCategoriesByEvent_WhenEventDoesNotExist_ReturnsNotFound()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);
        var controller = new CategoriesController(context);

        //Act
        var result = await controller.GetCategoriesByEvent(999);

        //Assert
        var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);

        Assert.Equal(404, notFoundResult.StatusCode);
    }

    [Fact]
    public async Task GetCategory_WhenCategoryExists_ReturnsOkWithCategory()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var category = new Category
        {
            EventID = 1,
            CategoryName = "10 KM",
            DistanceKM = 10m,
            EntryFee = 100m
        };

        context.Categories.Add(category);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        //Act
        var result = await controller.GetCategory(category.CategoryID);

        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var returnedCategory = Assert.IsType<Category>(okResult.Value);

        Assert.Equal(category.CategoryID, returnedCategory.CategoryID);
        Assert.Equal("10 KM", returnedCategory.CategoryName);
        Assert.Equal(10m, returnedCategory.DistanceKM);
    }

    [Fact]
    public async Task GetCategoriesByEvent_WhenEventExists_ReturnsOnlyItsCategories()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        context.Events.AddRange(
            new Event
            {
                EventName = "Pretoria Race",
                EventDate = DateTime.Today.AddDays(30),
                Location = "Pretoria",
                Status = "Open",
                OrganiserID = 1
            },
            new Event
            {
                EventName = "Johannesburg Race",
                EventDate = DateTime.Today.AddDays(40),
                Location = "Johannesburg",
                Status = "Open",
                OrganiserID = 1
            });

        await context.SaveChangesAsync();

        var firstEvent = await context.Events
            .SingleAsync(e => e.EventName == "Pretoria Race");

        var secondEvent = await context.Events
            .SingleAsync(e => e.EventName == "Johannesburg Race");

        context.Categories.AddRange(
            new Category
            {
                EventID = firstEvent.EventID,
                CategoryName = "Walking",
                DistanceKM = 5m,
                EntryFee = 50m
            },
            new Category
            {
                EventID = firstEvent.EventID,
                CategoryName = "Running",
                DistanceKM = 10m,
                EntryFee = 100m
            },
            new Category
            {
                EventID = secondEvent.EventID,
                CategoryName = "Cycling",
                DistanceKM = 20m,
                EntryFee = 150m
            });

        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);

        //Act
        var result = await controller.GetCategoriesByEvent(firstEvent.EventID);

        //Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var categories = Assert.IsAssignableFrom<IEnumerable<Category>>(
            okResult.Value);

        var returnedCategories = categories.ToList();

        Assert.Equal(2, returnedCategories.Count);
        Assert.Equal("Running", returnedCategories[0].CategoryName);
        Assert.Equal("Walking", returnedCategories[1].CategoryName);
        Assert.All(returnedCategories,
            c => Assert.Equal(firstEvent.EventID, c.EventID));
    }

    [Fact]
    public async Task CreateCategory_WhenOrganiserDoesNotOwnEvent_ReturnsForbidden()
    {
        //Arrange
        var options = new DbContextOptionsBuilder<RaceDayContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        await using var context = new RaceDayContext(options);

        var raceEvent = new Event
        {
            EventName = "Another Organiser's Race",
            EventDate = DateTime.Today.AddDays(30),
            Location = "Pretoria",
            Status = "Open",
            OrganiserID = 1
        };

        context.Events.Add(raceEvent);
        await context.SaveChangesAsync();

        var controller = new CategoriesController(context);
        controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext()
        };

        controller.HttpContext.Session = new TestSessionForCategories();
        controller.HttpContext.Session.SetInt32("UserID", 2);

        var request = new RaceDay.Part2.DTOs.CategoryCreateRequest
        {
            EventID = raceEvent.EventID,
            CategoryName = "10 KM",
            DistanceKM = 10m,
            EntryFee = 100m,
            MaxParticipants = 100
        };

        //Act
        var result = await controller.CreateCategory(request);

        //Assert
        var forbiddenResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(403, forbiddenResult.StatusCode);

        Assert.Empty(await context.Categories.ToListAsync());
    }
    private sealed class TestSessionForCategories : ISession
    {
        private readonly Dictionary<string, byte[]> _store = new();

        public IEnumerable<string> Keys => _store.Keys;
        public string Id => "CategoriesTestSession";
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

        public Task LoadAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task CommitAsync(
            CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}

