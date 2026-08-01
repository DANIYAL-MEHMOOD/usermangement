using System.ComponentModel.DataAnnotations;
using MyApp.Api.DTOs;
using MyApp.Api.Helpers;

namespace MyApp.Api.Validators;

public static class UserValidators
{
    public static bool ValidateCreateUser(CreateUserRequest request, out string error)
    {
        if (string.IsNullOrWhiteSpace(request.Username))
        {
            error = "Username is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(request.Email) || !new EmailAddressAttribute().IsValid(request.Email))
        {
            error = "Valid Email is required.";
            return false;
        }
        if (!PasswordPolicyHelper.ValidatePassword(request.Password, out error))
        {
            return false;
        }
        error = string.Empty;
        return true;
    }
}
