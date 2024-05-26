using Microsoft.EntityFrameworkCore;
using VOICE.Data.Models;

namespace VOICE.Data.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Conversation>(entity =>
        {
            entity.Property(e => e.name).HasColumnType("varchar(255)"); // Adjust the length as necessary
            entity.Property(e => e.created).HasColumnType("datetime");
        });
    }
    }
}