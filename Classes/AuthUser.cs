using Microsoft.AspNetCore.Identity;

namespace TryToDo_Api.Classes;

public class AuthUser : IdentityUser
{
    public Guid UserId { get; set; }
}