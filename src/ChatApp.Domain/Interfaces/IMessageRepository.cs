using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces
{
    public interface IMessageRepository
    {
        Task<Message?> GetByIdAsync(Guid id, CancellationToken ct);
        Task<List<Message>> GetByChatIdAsync(Guid chatId, int skip, int take, CancellationToken ct);
        Task AddAsync(Message message, CancellationToken ct);
        Task UpdateAsync(Message message, CancellationToken ct);
        Task DeleteAsync(Guid messageId, CancellationToken ct);
        Task<List<Message>> SearchMessagesAsync(Guid chatId, string query, CancellationToken ct);
    }
}
