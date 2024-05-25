using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;


namespace VOICE.Data.Models
{
public class Conversation
	{
		[Key]
		public int id { get; set; }
		[MaxLength(255)]
		public string? name { get; set; }
		public DateTime created { get; set; }
		public List<Message> Messages { get; set; } = new List<Message>();
	}
}