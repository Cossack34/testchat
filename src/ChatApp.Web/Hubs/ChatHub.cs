using System.Security.Claims;
using System.Text.RegularExpressions;
using ChatApp.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace ChatApp.Web.Hubs
{/// <summary>
 /// SignalR Hub for real-time chat messaging.
 /// Handles client connections, chat room management, and message broadcasting.
 /// </summary>
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly IMessageService _msgService;
        private readonly IChatService _chatService;
        private readonly ILogger<ChatHub> _logger;

        public ChatHub(IMessageService msgService, IChatService chatService, ILogger<ChatHub> logger)
        {
            _msgService = msgService ?? throw new ArgumentNullException(nameof(msgService));
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Called when a client connects to the hub.
        /// Logs the connection and handles any connection errors.
        /// </summary>
        /// <returns>Task representing the connection operation</returns>
        public override async Task OnConnectedAsync()
        {
            try
            {
                var userId = GetCurrentUserId();
                _logger.LogInformation("User {UserId} connected to chat hub with connection {ConnectionId}",
                    userId, Context.ConnectionId);

                await base.OnConnectedAsync();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized connection attempt from {ConnectionId}", Context.ConnectionId);
                Context.Abort();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hub connection for {ConnectionId}", Context.ConnectionId);
                throw;
            }
        }

        /// <summary>
        /// Called when a client disconnects from the hub.
        /// Logs the disconnection and cleans up any resources.
        /// </summary>
        /// <param name="exception">Exception that caused disconnection (if any)</param>
        /// <returns>Task representing the disconnection operation</returns>
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            try
            {
                var userId = GetCurrentUserId();
                if (exception != null)
                {
                    _logger.LogWarning(exception, "User {UserId} disconnected from chat hub with error", userId);
                }
                else
                {
                    _logger.LogInformation("User {UserId} disconnected from chat hub", userId);
                }

                await base.OnDisconnectedAsync(exception);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during hub disconnection for user");
            }
        }

        /// <summary>
        /// Called by client to join a chat group (room).
        /// Verifies user access and adds them to the SignalR group.
        /// </summary>
        /// <param name="chatId">ID of the chat to join</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the join operation</returns>
        public async Task JoinChat(Guid chatId)
        {
            try
            {
                var cancellationToken = Context.ConnectionAborted;
                var userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} attempting to join chat {ChatId}", userId, chatId);

                // Verify user has access to this chat
                var chat = await _chatService.GetByIdAsync(chatId, cancellationToken);
                if (chat == null)
                {
                    _logger.LogWarning("User {UserId} attempted to join non-existent chat {ChatId}", userId, chatId);
                    await Clients.Caller.SendAsync("Error", "Chat not found", cancellationToken);
                    return;
                }

                if (!chat.ParticipantIds.Contains(userId))
                {
                    _logger.LogWarning("User {UserId} attempted to join chat {ChatId} without permission", userId, chatId);
                    await Clients.Caller.SendAsync("Error", "Access denied to this chat", cancellationToken);
                    return;
                }

                // Add user to SignalR group
                await Groups.AddToGroupAsync(Context.ConnectionId, chatId.ToString(), cancellationToken);
                _logger.LogInformation("User {UserId} successfully joined chat {ChatId}", userId, chatId);

                // Notify other users in the chat about new participant
                await Clients.Group(chatId.ToString()).SendAsync("UserJoined",
                    new { UserId = userId, ChatId = chatId }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Join chat operation was cancelled for chat {ChatId}", chatId);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to join chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Access denied", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Failed to join chat", CancellationToken.None);
            }
        }

        /// <summary>
        /// Called by client to leave a chat group.
        /// Removes user from the SignalR group and notifies other participants.
        /// </summary>
        /// <param name="chatId">ID of the chat to leave</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the leave operation</returns>
        public async Task LeaveChat(Guid chatId)
        {
            try
            {
                var cancellationToken = Context.ConnectionAborted;
                var userId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} leaving chat {ChatId}", userId, chatId);

                // Remove user from SignalR group
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId.ToString(), cancellationToken);
                _logger.LogInformation("User {UserId} left chat {ChatId}", userId, chatId);

                // Notify other users in the chat about participant leaving
                await Clients.Group(chatId.ToString()).SendAsync("UserLeft",
                    new { UserId = userId, ChatId = chatId }, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Leave chat operation was cancelled for chat {ChatId}", chatId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error leaving chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Failed to leave chat", CancellationToken.None);
            }
        }

        /// <summary>
        /// Send a new message to chat. All group members will receive it in real-time.
        /// Validates message content and user permissions before broadcasting.
        /// </summary>
        /// <param name="chatId">ID of the chat</param>
        /// <param name="content">Message content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the send operation</returns>
        public async Task SendMessage(Guid chatId, string content)
        {
            try
            {
                var cancellationToken = Context.ConnectionAborted;
                // Validate message content
                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("Error", "Message content cannot be empty", cancellationToken);
                    return;
                }

                if (content.Length > 4000) // Reasonable message length limit
                {
                    await Clients.Caller.SendAsync("Error", "Message content is too long", cancellationToken);
                    return;
                }

                var senderId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} sending message to chat {ChatId}", senderId, chatId);

                // Send message through service layer
                var msg = await _msgService.SendMessageAsync(chatId, senderId, content, cancellationToken);

                _logger.LogInformation("Message {MessageId} sent to chat {ChatId} by user {UserId} via SignalR",
                    msg.Id, chatId, senderId);

                // Broadcast message to all users in this chat group
                await Clients.Group(chatId.ToString()).SendAsync("ReceiveMessage", msg, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Send message operation was cancelled for chat {ChatId}", chatId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Attempt to send message to non-existent chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Chat not found", CancellationToken.None);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to send message to chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Access denied to this chat", CancellationToken.None);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid message content for chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", ex.Message, CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending message to chat {ChatId}", chatId);
                await Clients.Caller.SendAsync("Error", "Failed to send message", CancellationToken.None);
            }
        }

        /// <summary>
        /// Edit an existing message and notify all group members.
        /// Only the original sender can edit their message.
        /// </summary>
        /// <param name="chatId">ID of the chat containing the message</param>
        /// <param name="messageId">ID of the message to edit</param>
        /// <param name="content">New message content</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the edit operation</returns>
        public async Task EditMessage(Guid chatId, Guid messageId, string content)
        {
            try
            {
                var cancellationToken = Context.ConnectionAborted;
                // Validate new content
                if (string.IsNullOrWhiteSpace(content))
                {
                    await Clients.Caller.SendAsync("Error", "Message content cannot be empty", cancellationToken);
                    return;
                }

                if (content.Length > 4000)
                {
                    await Clients.Caller.SendAsync("Error", "Message content is too long", cancellationToken);
                    return;
                }

                var editorId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} editing message {MessageId} in chat {ChatId}",
                    editorId, messageId, chatId);

                // Edit message through service layer
                var msg = await _msgService.EditMessageAsync(messageId, content, editorId, cancellationToken);

                _logger.LogInformation("Message {MessageId} edited by user {UserId} via SignalR", messageId, editorId);

                // Broadcast edited message to all users in the chat
                await Clients.Group(chatId.ToString()).SendAsync("MessageEdited", msg, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Edit message operation was cancelled for message {MessageId}", messageId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Attempt to edit non-existent message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Message not found", CancellationToken.None);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to edit message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Only the sender can edit this message", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error editing message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Failed to edit message", CancellationToken.None);
            }
        }

        /// <summary>
        /// Delete a message and notify all group members.
        /// Only the original sender can delete their message.
        /// </summary>
        /// <param name="chatId">ID of the chat containing the message</param>
        /// <param name="messageId">ID of the message to delete</param>
        /// <param name="cancellationToken">Cancellation token</param>
        /// <returns>Task representing the delete operation</returns>
        public async Task DeleteMessage(Guid chatId, Guid messageId)
        {
            try
            {
                var cancellationToken = Context.ConnectionAborted;
                var deleterId = GetCurrentUserId();
                _logger.LogDebug("User {UserId} deleting message {MessageId} in chat {ChatId}",
                    deleterId, messageId, chatId);

                // Delete message through service layer
                await _msgService.DeleteMessageAsync(messageId, deleterId, cancellationToken);

                _logger.LogInformation("Message {MessageId} deleted by user {UserId} via SignalR", messageId, deleterId);

                // Broadcast deletion to all users in the chat
                await Clients.Group(chatId.ToString()).SendAsync("MessageDeleted", messageId, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("Delete message operation was cancelled for message {MessageId}", messageId);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning(ex, "Attempt to delete non-existent message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Message not found", CancellationToken.None);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning(ex, "Unauthorized attempt to delete message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Only the sender can delete this message", CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting message {MessageId}", messageId);
                await Clients.Caller.SendAsync("Error", "Failed to delete message", CancellationToken.None);
            }
        }

        /// <summary>
        /// Extracts the current user's ID from the JWT token claims.
        /// </summary>
        /// <returns>User ID as Guid</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user is not authenticated</exception>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("User is not authenticated or user ID is invalid");
            }
            return userId;
        }
    }
}
