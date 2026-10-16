using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using RaceDay.Part2.Models;
using RaceDay.Part2.Services;
using Xunit;

namespace RaceDay.Part2.Tests;

public class PasswordServiceTests
{
    [Fact]
    public void HashPassword_ReturnsHashedPassword()
    {
        //Arrange
        var service = new PasswordService();
        var user = new User();
        string password = "TestPassword123!";

        //Act
        string hashedPassword = service.HashPassword(user, password);

        //Assert
        Assert.NotNull(hashedPassword);
        Assert.NotEmpty(hashedPassword);
        Assert.NotEqual(password, hashedPassword);
    }

    [Fact]
    public void VerifyPassword_ReturnsTrueForCorrectPassword()
    {
        //Arrange
        var service = new PasswordService();
        var user = new User();
        string password = "TestPassword123!";

        user.PasswordHash = service.HashPassword(user, password);

        //Act
        bool result = service.VerifyPassword(user, password);

        //Assert
        Assert.True(result);
    }

    [Fact]
    public void VerifyPassword_ReturnsFalseForIncorrectPassword()
    {
        //Arrange
        var service = new PasswordService();
        var user = new User();
        string password = "TestPassword123!";

        user.PasswordHash = service.HashPassword(user, password);

        //Act
        bool result = service.VerifyPassword(user, "WrongPassword456!");

        //Assert
        Assert.False(result);
    }
}
