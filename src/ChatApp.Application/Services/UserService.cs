using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Application.DTOs;
using ChatApp.Application.Interfaces;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;

namespace ChatApp.Application.Services
{
    /// <summary>
    /// Implements user registration and authentication logic.
    /// </summary>
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepo;
        private readonly IJwtTokenGenerator _jwtTokenGenerator;

        public UserService(IUserRepository userRepo, IJwtTokenGenerator jwtTokenGenerator)
        {
            _userRepo = userRepo;
            _jwtTokenGenerator = jwtTokenGenerator;
        }

        public async Task<UserDto> RegisterAsync(string userName, string password, string? displayName, CancellationToken ct)
        {
            if (await _userRepo.ExistsAsync(userName, ct))
                throw new InvalidOperationException("User already exists.");

            var hash = HashPassword(password);
            var user = new User
            {
                Id = Guid.NewGuid(),
                UserName = userName,
                PasswordHash = hash,
                DisplayName = displayName
            };
            await _userRepo.AddAsync(user, ct);

            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            };
        }

        public async Task<string> AuthenticateAsync(string userName, string password, CancellationToken ct)
        {
            var user = await _userRepo.GetByUserNameAsync(userName, ct)
                       ?? throw new UnauthorizedAccessException("Invalid credentials.");
            if (user.PasswordHash != HashPassword(password))
                throw new UnauthorizedAccessException("Invalid credentials.");
            return _jwtTokenGenerator.GenerateToken(user);
        }

        public async Task<UserDto?> GetByIdAsync(Guid id, CancellationToken ct)
        {
            var user = await _userRepo.GetByIdAsync(id, ct);
            if (user == null) return null;
            return new UserDto
            {
                Id = user.Id,
                UserName = user.UserName,
                DisplayName = user.DisplayName
            };
        }

        private static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(password);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }
    }
}
