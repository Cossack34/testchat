using System.Security.Claims;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers
{
    /// <summary>
    /// Chat CRUD endpoints with comprehensive error handling and logging.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class ChatsController : ControllerBase
    {
        private readonly IChatService _chatService;
        private readonly ILogger<ChatsController> _logger;

        public ChatsController(IChatService chatService, ILogger<ChatsController> logger)
        {
            _chatService = chatService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new chat room.
        /// </summary>
        /// <param name="req">Chat creation request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Created chat information</returns>
        [HttpPost]
        public async Task<ActionResult<ChatDto>> Create([FromBody] CreateChatRequest req, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var chat = await _chatService.CreateChatAsync(req.Name, req.ParticipantIds, userId, ct);
                _logger.LogInformation("Chat {ChatId} created by user {UserId}", chat.Id, userId);
                return Ok(chat);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning("Invalid chat creation request: {Message}", ex.Message);
                return BadRequest(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized chat creation attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat creation cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during chat creation");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get all chats for the current user.
        /// </summary>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of user's chats</returns>
        [HttpGet]
        public async Task<ActionResult<List<ChatDto>>> GetMyChats(CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                var chats = await _chatService.GetChatsByUserAsync(userId, ct);
                return Ok(chats);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get chats request cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting user chats");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Get specific chat by ID.
        /// </summary>
        /// <param name="id">Chat ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>Chat information</returns>
        [HttpGet("{id}")]
        public async Task<ActionResult<ChatDto>> GetById(Guid id, CancellationToken ct = default)
        {
            try
            {
                var chat = await _chatService.GetByIdAsync(id, ct);
                return chat == null ? NotFound("Chat not found") : Ok(chat);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Get chat by ID request cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while getting chat {ChatId}", id);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Rename an existing chat.
        /// </summary>
        /// <param name="id">Chat ID</param>
        /// <param name="req">Rename request</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>No content on success</returns>
        [HttpPut("{id}")]
        public async Task<IActionResult> Rename(Guid id, [FromBody] RenameChatRequest req, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatService.UpdateChatNameAsync(id, req.Name, userId, ct);
                _logger.LogInformation("Chat {ChatId} renamed by user {UserId}", id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Chat not found during rename: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized chat rename attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat rename cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during chat rename");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Delete a chat.
        /// </summary>
        /// <param name="id">Chat ID</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>No content on success</returns>
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct = default)
        {
            try
            {
                var userId = GetCurrentUserId();
                await _chatService.DeleteChatAsync(id, userId, ct);
                _logger.LogInformation("Chat {ChatId} deleted by user {UserId}", id, userId);
                return NoContent();
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogWarning("Chat not found during deletion: {Message}", ex.Message);
                return NotFound(ex.Message);
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Unauthorized chat deletion attempt: {Message}", ex.Message);
                return Unauthorized(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat deletion cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during chat deletion");
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Search chats by name.
        /// </summary>
        /// <param name="query">Search query</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>List of matching chats</returns>
        [HttpGet("search")]
        public async Task<ActionResult<List<ChatDto>>> Search([FromQuery] string query, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest("Search query cannot be empty");
                }

                var userId = GetCurrentUserId();
                var chats = await _chatService.SearchChatsAsync(userId, query, ct);
                return Ok(chats);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Chat search cancelled");
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during chat search");
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
    /// Request model for creating a new chat.
    /// </summary>
    public class CreateChatRequest
    {
        public string Name { get; set; } = null!;
        public List<Guid> ParticipantIds { get; set; } = new();
    }

    /// <summary>
    /// Request model for renaming a chat.
    /// </summary>
    public class RenameChatRequest
    {
        public string Name { get; set; } = null!;
    }
}
