using Microsoft.EntityFrameworkCore;
using VOICE.Data.Context;
using VOICE.Data.Models;

namespace VOICE.Data
{
    public class ConversationRepository
    {
        private readonly DataContext _context;

        public ConversationRepository(DataContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<Conversation>> GetAllConversationsAsync()
        {
            return await _context.Conversations.Include(c => c.Messages).ToListAsync();
        }

        public async Task<Conversation?> GetConversationByIdAsync(int id)
        {
            return await _context.Conversations.Include(c => c.Messages)
                                               .FirstOrDefaultAsync(c => c.id == id);
        }

        public async Task AddConversationAsync(Conversation conversation)
        {
            await _context.Conversations.AddAsync(conversation);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateConversationAsync(Conversation conversation)
        {
            _context.Conversations.Update(conversation);
            await _context.SaveChangesAsync();
        }
    }
}
