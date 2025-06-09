using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Application.DTOs;

namespace ChatApp.Application.Interfaces
{
    /// <summary>
    /// Service interface for chat-related operations.
    /// </summary>
    public interface IChatService
    {
        Task<ChatDto> CreateChatAsync(string name, List<Guid> participantIds, Guid creatorId, CancellationToken ct);
        Task<List<ChatDto>> GetChatsByUserAsync(Guid userId, CancellationToken ct);
        Task<ChatDto?> GetByIdAsync(Guid chatId, CancellationToken ct);
        Task UpdateChatNameAsync(Guid chatId, string name, Guid requesterId, CancellationToken ct);
        Task DeleteChatAsync(Guid chatId, Guid requesterId, CancellationToken ct);
        Task<List<ChatDto>> SearchChatsAsync(Guid userId, string query, CancellationToken ct);
    }
}
