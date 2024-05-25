using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace VOICE.Data.Models
{
    public class Message
    {
        [Key]
        public int id { get; set; }
        [ForeignKey("Conversation")]
        public int conversationId { get; set; }
        public string? role { get; set; }
        public string? content { get; set; }
    }
}
