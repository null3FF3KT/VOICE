using Microsoft.EntityFrameworkCore;
using VOICE.Data.Models;

namespace VOICE.Data.Context
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options) { }

        public DbSet<Conversation> Conversations { get; set; }
        public DbSet<Message> Messages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("Server=localhost:1433;Database=VoiceDb;User Id=sa;Password=mark99Paris!;Trusted_Connection=False;");
            }
        }

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