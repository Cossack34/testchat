using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Domain.Entities
{
    /// <summary>
    /// Chat entity represents a chat room or a dialog between users.
    /// </summary>
    public class Chat
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// List of user IDs participating in the chat.
        /// </summary>
        public ICollection<Guid> ParticipantIds { get; set; } = new List<Guid>();

        public ICollection<Message> Messages { get; set; } = new List<Message>();
    }
}
