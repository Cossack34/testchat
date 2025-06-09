using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChatApp.Application.DTOs
{
    /// <summary>
    /// DTO for Chat data transfer.
    /// </summary>
    public class ChatDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public List<Guid> ParticipantIds { get; set; } = new();
    }
}
