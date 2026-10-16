using System;
using Microsoft.AspNetCore.Identity;
using RaceDay.Part2.Models;

namespace RaceDay.Part2.Services;

public class PasswordService
{
    private readonly PasswordHasher<User> _passwordHasher = new();

    //Convert a plain-text password into a secure password hash
    public string HashPassword(User user, string password)
    {
        return _passwordHasher.HashPassword(user, password);
    }

    //Check a supplied password against the stored hash
    public bool VerifyPassword(User user, string password)
    {
        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            password);

        return result == PasswordVerificationResult.Success
            || result == PasswordVerificationResult.SuccessRehashNeeded;
    }
}
