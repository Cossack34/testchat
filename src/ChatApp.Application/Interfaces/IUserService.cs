using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Application.DTOs;

namespace ChatApp.Application.Interfaces
{
    /// <summary>
    /// Service interface for user-related operations.
    /// </summary>
    public interface IUserService
    {
        Task<UserDto> RegisterAsync(string userName, string password, string? displayName, CancellationToken ct);
        Task<string> AuthenticateAsync(string userName, string password, CancellationToken ct);
        Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct);
    }
}
