using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    /// <summary>
    /// User entity represents a registered user.
    /// </summary>
    public class User
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string PasswordHash { get; set; } = null!;
        public string? DisplayName { get; set; }
        public DateTime RegisteredAt { get; set; } = DateTime.UtcNow;
    }
}
