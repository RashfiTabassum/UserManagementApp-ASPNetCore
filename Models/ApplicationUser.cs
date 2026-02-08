using Microsoft.AspNetCore.Identity;
using System;

public enum UserStatus
{
    Unverified = 0,
    Active = 1,
    Blocked = 2
}

public class ApplicationUser : IdentityUser
{
    //status to handle block/unverified/active
    public UserStatus Status { get; set; } = UserStatus.Unverified;

    //store last login time to sort the table
    public DateTime? LastLoginTime { get; set; }
}
