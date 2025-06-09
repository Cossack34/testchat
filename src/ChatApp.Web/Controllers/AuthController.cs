using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace ChatApp.Web.Controllers
{
    /// <summary>
    /// Authentication endpoints: register and login with comprehensive error handling.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IUserService userService, ILogger<AuthController> logger)
        {
            _userService = userService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user account.
        /// </summary>
        /// <param name="request">Registration details</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>User information</returns>
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register([FromBody] RegisterRequest request, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest("Username and password are required.");
                }

                var user = await _userService.RegisterAsync(request.UserName, request.Password, request.DisplayName, ct);
                _logger.LogInformation("User {UserName} registered successfully", request.UserName);
                return Ok(user);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogWarning("Registration failed for user {UserName}: {Message}", request.UserName, ex.Message);
                return BadRequest(ex.Message);
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Registration cancelled for user {UserName}", request.UserName);
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during registration for user {UserName}", request.UserName);
                return StatusCode(500, "Internal server error");
            }
        }

        /// <summary>
        /// Authenticate user and return JWT token.
        /// </summary>
        /// <param name="request">Login credentials</param>
        /// <param name="ct">Cancellation token</param>
        /// <returns>JWT token</returns>
        [HttpPost("login")]
        public async Task<ActionResult<object>> Login([FromBody] LoginRequest request, CancellationToken ct = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.UserName) || string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest("Username and password are required.");
                }

                var token = await _userService.AuthenticateAsync(request.UserName, request.Password, ct);
                _logger.LogInformation("User {UserName} logged in successfully", request.UserName);
                return Ok(new { token });
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogWarning("Authentication failed for user {UserName}: {Message}", request.UserName, ex.Message);
                return Unauthorized("Invalid credentials");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("Login cancelled for user {UserName}", request.UserName);
                return StatusCode(499, "Request cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during login for user {UserName}", request.UserName);
                return StatusCode(500, "Internal server error");
            }
        }
    }

    /// <summary>
    /// Request model for user registration.
    /// </summary>
    public class RegisterRequest
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
        public string? DisplayName { get; set; }
    }

    /// <summary>
    /// Request model for user login.
    /// </summary>
    public class LoginRequest
    {
        public string UserName { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}