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
    /// EF Core repository for chats.
    /// </summary>
    public class ChatRepository : IChatRepository
    {
        private readonly AppDbContext _db;

        public ChatRepository(AppDbContext db) => _db = db;

        public async Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _db.Chats.Include(c => c.Messages).FirstOrDefaultAsync(c => c.Id == id, ct);

        public async Task<List<Chat>> GetByUserIdAsync(Guid userId, CancellationToken ct)
            => await _db.Chats.Where(c => c.ParticipantIds.Contains(userId)).ToListAsync(ct);

        public async Task AddAsync(Chat chat, CancellationToken ct)
        {
            _db.Chats.Add(chat);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Chat chat, CancellationToken ct)
        {
            _db.Chats.Update(chat);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid chatId, CancellationToken ct)
        {
            var chat = await _db.Chats.FindAsync(new object[] { chatId }, ct);
            if (chat != null)
            {
                _db.Chats.Remove(chat);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<List<Chat>> SearchChatsAsync(Guid userId, string query, CancellationToken ct)
            => await _db.Chats
                .Where(c => c.ParticipantIds.Contains(userId) && EF.Functions.Like(c.Name, $"%{query}%"))
                .ToListAsync(ct);
    }
}
