using System.Security.Claims;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers
{
    /// <summary>
    /// Message endpoints with comprehensive error handling and logging.
    /// </summary>
    [ApiController]
    [Route("api/chats/{chatId}/[controller]")]
    [Authorize]
    public class MessagesController : ControllerBase
    {
        private readonly IMessageService _msgService;
        private readonly ILogger<MessagesController> _logger;

        public MessagesController(IMessageService msgService, ILogger<MessagesController> logger)
        {
            _msgService = msgService;
            _logger = logger;
        }

        /// <summary>
        /// Send a new message to the chat.
        /// </summary>
        /// <param name="chatId">Chat ID</param>
        /// <param name="req">Message content</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Created message</returns>
        [HttpPost]
        public async Task<ActionResult<MessageDto>> Send(Guid chatId, [FromBody] SendMessageRequest req, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Content))
                {
                    return BadRequest("Message content cannot be empty");
                }

                var userId = GetCurrentUserId();
                var msg = await _msgService.SendMessageAsync(chatId, userId, req.Content, ct);
                _logger.LogInformation("Message {MessageId} sent to chat {ChatId} by user {UserId}", msg.Id, chatId, userId);
                return Ok(msg);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Chat not found when sending message: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized message send attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid message send request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message send cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message send");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get messages from a chat with pagination.
        /// </summary>
        /// <param name="chatId">Chat ID</param>
        /// <param name="skip">Number of messages to skip</param>
        /// <param name="take">Number of messages to take</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of messages</returns>
        [HttpGet]
        public async Task<ActionResult<List<MessageDto>>> Get(Guid chatId, [FromQuery] int skip = 0, [FromQuery] int take = 50, CancellationToken ct = default)
        {
            try
            {
                if (take > 100)
                {
                    return BadRequest("Maximum 100 messages per request");
                }

                var userId = GetCurrentUserId();
                var msgs = await _msgService.GetMessagesAsync(chatId, skip, take, userId, ct);
                return Ok(msgs);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Chat not found when getting messages: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized message retrieval attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get messages cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message retrieval");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Edit an existing message.
        /// </summary>
        /// <param name="chatId">Chat ID</param>
        /// <param name="messageId">Message ID</param>
        /// <param name="req">New message content</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Updated message</returns>
        [HttpPut("{messageId}")]
        public async Task<ActionResult<MessageDto>> Edit(Guid chatId, Guid messageId, [FromBody] EditMessageRequest req, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Content))
                {
                    return BadRequest("Message content cannot be empty");
                }

                var userId = GetCurrentUserId();
                var msg = await _msgService.EditMessageAsync(messageId, req.Content, userId, ct);
                _logger.LogInformation("Message {MessageId} edited by user {UserId}", messageId, userId);
                return Ok(msg);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Message not found during edit: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized message edit attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message edit cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message edit");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a message (soft delete).
        /// </summary>
        /// <param name="chatId">Chat ID</param>
        /// <param name="messageId">Message ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{messageId}")]
        public async Task<IActionResult> Delete(Guid chatId, Guid messageId, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _msgService.DeleteMessageAsync(messageId, userId, ct);
                _logger.LogInformation("Message {MessageId} deleted by user {UserId}", messageId, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Message not found during deletion: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized message deletion attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message deletion cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message deletion");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search messages within a chat using full-text search.
        /// </summary>
        /// <param name="chatId">Chat ID</param>
        /// <param name="query">Search query</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of matching messages</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<MessageDto>>> Search(Guid chatId, [FromQuery] string query, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var userId = GetCurrentUserId();
                var msgs = await _msgService.SearchMessagesAsync(chatId, query, userId, ct);
                return Ok(msgs);
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Chat not found during message search: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized message search attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Message search cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message search");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Helper method to extract current user ID from JWT claims.
        /// </summary>
        /// <returns>Current user's GUID</returns>
        /// <exception cref="UnauthorizedAccessException">Thrown when user ID cannot be extracted</exception>
        private Guid GetCurrentUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                throw new UnauthorizedAccessException("Invalid user token");
            }
            return userId;
        }
    }

    /// <summary>
    /// Request model for sending a message.
    /// </summary>
    public class SendMessageRequest
    {
        public string Content { get; set; } = null!;
    }

    /// <summary>
    /// Request model for editing a message.
    /// </summary>
    public class EditMessageRequest
    {
        public string Content { get; set; } = null!;
    }
    
}
