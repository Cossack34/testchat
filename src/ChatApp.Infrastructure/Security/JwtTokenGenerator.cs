using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;
using ChatApp.Domain.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace ChatApp.Infrastructure.Security
{
    /// <summary>
    /// Service to generate JWT tokens.
    /// </summary>
    public class JwtTokenGenerator : IJwtTokenGenerator
    {
        private readonly string _secret;
        private readonly string _issuer;
        private readonly string _audience;
        private readonly TimeSpan _lifetime = TimeSpan.FromDays(7);

        public JwtTokenGenerator(IConfiguration config)
        {
            _secret = config["Jwt:Secret"] ?? throw new InvalidOperationException("Jwt:Secret not found in config.");
            _issuer = config["Jwt:Issuer"] ?? "ChatApp";
            _audience = config["Jwt:Audience"] ?? "ChatApp";
        }

        public string GenerateToken(User user)
        {
            var claims = new[]
            {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName),
            new Claim("displayName", user.DisplayName ?? user.UserName)
        };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                issuer: _issuer,          
                audience: _audience,
                expires: DateTime.UtcNow.Add(_lifetime),
                signingCredentials: creds
            );
            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
