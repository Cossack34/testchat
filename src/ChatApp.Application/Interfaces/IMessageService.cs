using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Application.DTOs;

namespace ChatApp.Application.Interfaces
{
    /// <summary>
    /// Service interface for message-related operations.
    /// </summary>
    public interface IMessageService
    {
        Task<MessageDto> SendMessageAsync(Guid chatId, Guid senderId, string content, CancellationToken ct);
        Task<List<MessageDto>> GetMessagesAsync(Guid chatId, int skip, int take, Guid requesterId, CancellationToken ct);
        Task<MessageDto> EditMessageAsync(Guid messageId, string newContent, Guid editorId, CancellationToken ct);
        Task DeleteMessageAsync(Guid messageId, Guid requesterId, CancellationToken ct);
        Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, Guid requesterId, CancellationToken ct);
    }
}
