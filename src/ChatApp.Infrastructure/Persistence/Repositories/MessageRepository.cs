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
    /// EF Core repository for messages with full-text search support.
    /// </summary>
    public class MessageRepository : IMessageRepository
    {
        private readonly AppDbContext _db;

        public MessageRepository(AppDbContext db) => _db = db;

        public async Task<Message?> GetByIdAsync(Guid id, CancellationToken ct)
            => await _db.Messages.FindAsync(new object[] { id }, ct);

        public async Task<List<Message>> GetByChatIdAsync(Guid chatId, int skip, int take, CancellationToken ct)
            => await _db.Messages
                .Where(m => m.ChatId == chatId)
                .OrderBy(m => m.SentAt)
                .Skip(skip).Take(take)
                .ToListAsync(ct);

        public async Task AddAsync(Message message, CancellationToken ct)
        {
            _db.Messages.Add(message);
            await _db.SaveChangesAsync(ct);
        }

        public async Task UpdateAsync(Message message, CancellationToken ct)
        {
            _db.Messages.Update(message);
            await _db.SaveChangesAsync(ct);
        }

        public async Task DeleteAsync(Guid messageId, CancellationToken ct)
        {
            var message = await _db.Messages.FindAsync(new object[] { messageId }, ct);
            if (message != null)
            {
                _db.Messages.Remove(message);
                await _db.SaveChangesAsync(ct);
            }
        }

        public async Task<List<Message>> SearchMessagesAsync(Guid chatId, string query, CancellationToken ct)
            => await _db.Messages
                .Where(m => m.ChatId == chatId &&
                            EF.Functions.ToTsVector("english", m.Content)
                                .Matches(EF.Functions.PlainToTsQuery("english", query)))
                .OrderByDescending(m => m.SentAt)
                .ToListAsync(ct);
    }
}
