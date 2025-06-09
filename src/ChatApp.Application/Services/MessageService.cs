using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;

namespace ChatApp.Application.Services
{
    /// <summary>
    /// Implements message business logic (send, edit, delete, search).
    /// </summary>
    public class MessageService : IMessageService
    {
        private readonly IMessageRepository _messageRepo;
        private readonly IChatRepository _chatRepo;

        public MessageService(IMessageRepository messageRepo, IChatRepository chatRepo)
        {
            _messageRepo = messageRepo;
            _chatRepo = chatRepo;
        }

        public async Task<MessageDto> SendMessageAsync(Guid chatId, Guid senderId, string content, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(content))
                throw new ArgumentException("Message content cannot be empty.");

            var chat = await _chatRepo.GetByIdAsync(chatId, ct)
                ?? throw new KeyNotFoundException("Chat not found.");

            if (!chat.ParticipantIds.Contains(senderId))
                throw new UnauthorizedAccessException("User not a member of the chat.");

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ChatId = chatId,
                SenderId = senderId,
                Content = content,
                SentAt = DateTime.UtcNow,
                IsDeleted = false
            };
            await _messageRepo.AddAsync(message, ct);

            return ToDto(message);
        }

        public async Task<List<MessageDto>> GetMessagesAsync(Guid chatId, int skip, int take, Guid requesterId, CancellationToken ct)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId, ct)
                ?? throw new KeyNotFoundException("Chat not found.");
            if (!chat.ParticipantIds.Contains(requesterId))
                throw new UnauthorizedAccessException("User not a member of the chat.");

            var messages = await _messageRepo.GetByChatIdAsync(chatId, skip, take, ct);
            return messages.Select(ToDto).ToList();
        }

        public async Task<MessageDto> EditMessageAsync(Guid messageId, string newContent, Guid editorId, CancellationToken ct)
        {
            var message = await _messageRepo.GetByIdAsync(messageId, ct)
                ?? throw new KeyNotFoundException("Message not found.");

            if (message.SenderId != editorId)
                throw new UnauthorizedAccessException("Only the sender can edit the message.");

            message.Content = newContent;
            message.EditedAt = DateTime.UtcNow;
            await _messageRepo.UpdateAsync(message, ct);

            return ToDto(message);
        }

        public async Task DeleteMessageAsync(Guid messageId, Guid requesterId, CancellationToken ct)
        {
            var message = await _messageRepo.GetByIdAsync(messageId, ct)
                ?? throw new KeyNotFoundException("Message not found.");
            if (message.SenderId != requesterId)
                throw new UnauthorizedAccessException("Only the sender can delete the message.");
            message.IsDeleted = true;
            await _messageRepo.UpdateAsync(message, ct);
        }

        public async Task<List<MessageDto>> SearchMessagesAsync(Guid chatId, string query, Guid requesterId, CancellationToken ct)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId, ct)
                ?? throw new KeyNotFoundException("Chat not found.");
            if (!chat.ParticipantIds.Contains(requesterId))
                throw new UnauthorizedAccessException("User not a member of the chat.");

            var messages = await _messageRepo.SearchMessagesAsync(chatId, query, ct);
            return messages.Select(ToDto).ToList();
        }

        private static MessageDto ToDto(Message m)
        {
            return new MessageDto
            {
                Id = m.Id,
                ChatId = m.ChatId,
                SenderId = m.SenderId,
                Content = m.Content,
                SentAt = m.SentAt,
                EditedAt = m.EditedAt,
                IsDeleted = m.IsDeleted
            };
        }
    }
}
