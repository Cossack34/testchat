using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace ChatApp.Infrastructure.Persistence.Repositories
{
    /// <summary>
    /// EF Core repository for users.
    /// </summary>
    public class UserRepository : IUserRepository
    {
        private readonly AppDbContext _db;

        public UserRepository(AppDbContext db) => _db = db;

        public async Task<User?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _db.Users.FindAsync(new object[] { id }, ct);

        public async Task<User?> GetByUserNameAsync(string userName, CancellationToken ct)
            => await _db.Users.FirstOrDefaultAsync(x => x.UserName == userName, ct);

        public async Task AddAsync(User user, CancellationToken ct)
        {
            _db.Users.Add(user);
            await _db.SaveChangesAsync(ct);
        }

        public async Task<bool> ExistsAsync(string userName, CancellationToken ct)
            => await _db.Users.AnyAsync(x => x.UserName == userName, ct);
    }
}
