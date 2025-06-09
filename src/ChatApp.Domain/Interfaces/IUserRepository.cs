using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<User?> GetByUserNameAsync(string userName, CancellationToken ct);
        Task AddAsync(User user, CancellationToken ct);
        Task<bool> ExistsAsync(string userName, CancellationToken ct);
    }
}
