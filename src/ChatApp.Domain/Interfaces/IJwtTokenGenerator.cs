using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatApp.Domain.Entities;

namespace ChatApp.Domain.Interfaces
{
    /// <summary>
    /// Provides method to generate JWT token for user authentication.
    /// </summary>
    public interface IJwtTokenGenerator
    {
        /// <summary>
        /// Generates a JWT access token for a user.
        /// </summary>
        /// <param name="user">User entity</param>
        /// <returns>JWT token as string</returns>
        string GenerateToken(User user);
    }
}
