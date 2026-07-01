using Microsoft.AspNetCore.Identity;

namespace Shortie.Api.Entities;

public class ApplicationUser : IdentityUser
{
    public string FullName { get; set; } = string.Empty;
}
