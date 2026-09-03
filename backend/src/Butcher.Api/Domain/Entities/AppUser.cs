using Microsoft.AspNetCore.Identity;

namespace Butcher.Api.Domain.Entities;

public class AppUser : IdentityUser<Guid>
{
    public DateTimeOffset CreatedAt { get; set; }
}
