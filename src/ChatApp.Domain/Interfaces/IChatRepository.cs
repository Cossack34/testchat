using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces
{
    public interface IChatRepository
    {
        Task<Chat?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<Chat>> GetByUserIdAsync(Guid userId, CancellationToken ct);
        Task AddAsync(Chat chat, CancellationToken ct);
        Task UpdateAsync(Chat chat, CancellationToken ct);
        Task DeleteAsync(Guid chatId, CancellationToken ct);
        Task<List<Chat>> SearchChatsAsync(Guid userId, string query, CancellationToken ct);
    }
}
