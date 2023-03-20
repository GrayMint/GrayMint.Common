﻿namespace GrayMint.Common.AspNetCore.SimpleUserManagement.Models;

public class UserRole
{
    public Guid UserId { get; set; } 
    public Guid RoleId { get; set; } 
    public string AppId { get; set; } = default!;
    public virtual Role? Role { get; set; }
    public virtual User? User { get; set; }
}