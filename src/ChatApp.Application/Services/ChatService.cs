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
    /// Implements chat-related business logic.
    /// </summary>
    public class ChatService : IChatService
    {
        private readonly IChatRepository _chatRepo;
        private readonly IUserRepository _userRepo;

        public ChatService(IChatRepository chatRepo, IUserRepository userRepo)
        {
            _chatRepo = chatRepo;
            _userRepo = userRepo;
        }

        public async Task<ChatDto> CreateChatAsync(string name, List<Guid> participantIds, Guid creatorId, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Chat name cannot be empty.");

            var allParticipants = new HashSet<Guid>(participantIds) { creatorId };
            foreach (var id in allParticipants)
            {
                if (await _userRepo.GetByIdAsync(id, ct) == null)
                    throw new ArgumentException($"User {id} does not exist.");
            }

            var chat = new Chat
            {
                Id = Guid.NewGuid(),
                Name = name,
                ParticipantIds = allParticipants.ToList(),
                CreatedAt = DateTime.UtcNow
            };
            await _chatRepo.AddAsync(chat, ct);

            return new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                CreatedAt = chat.CreatedAt,
                ParticipantIds = chat.ParticipantIds.ToList()
            };
        }

        public async Task<List<ChatDto>> GetChatsByUserAsync(Guid userId, CancellationToken ct)
        {
            var chats = await _chatRepo.GetByUserIdAsync(userId, ct);
            return chats.Select(chat => new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                CreatedAt = chat.CreatedAt,
                ParticipantIds = chat.ParticipantIds.ToList()
            }).ToList();
        }

        public async Task<ChatDto?> GetByIdAsync(Guid chatId, CancellationToken ct)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId, ct);
            if (chat == null) return null;
            return new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                CreatedAt = chat.CreatedAt,
                ParticipantIds = chat.ParticipantIds.ToList()
            };
        }

        public async Task UpdateChatNameAsync(Guid chatId, string name, Guid requesterId, CancellationToken ct)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId, ct)
                ?? throw new KeyNotFoundException("Chat not found.");
            if (!chat.ParticipantIds.Contains(requesterId))
                throw new UnauthorizedAccessException("Only chat participants can rename the chat.");
            chat.Name = name;
            await _chatRepo.UpdateAsync(chat, ct);
        }

        public async Task DeleteChatAsync(Guid chatId, Guid requesterId, CancellationToken ct)
        {
            var chat = await _chatRepo.GetByIdAsync(chatId, ct)
                ?? throw new KeyNotFoundException("Chat not found.");
            if (!chat.ParticipantIds.Contains(requesterId))
                throw new UnauthorizedAccessException("Only chat participants can delete the chat.");
            await _chatRepo.DeleteAsync(chatId, ct);
        }

        public async Task<List<ChatDto>> SearchChatsAsync(Guid userId, string query, CancellationToken ct)
        {
            var chats = await _chatRepo.SearchChatsAsync(userId, query, ct);
            return chats.Select(chat => new ChatDto
            {
                Id = chat.Id,
                Name = chat.Name,
                CreatedAt = chat.CreatedAt,
                ParticipantIds = chat.ParticipantIds.ToList()
            }).ToList();
        }
    }
}
