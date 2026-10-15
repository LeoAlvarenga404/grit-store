using Grit.Modules.Probe.Domain;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace Grit.Modules.Probe.Infrastructure;

internal sealed class ProbeDbContext(DbContextOptions<ProbeDbContext> options) : DbContext(options)
{
    public const string Schema = "probe";

    public DbSet<Note> Notes => Set<Note>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);
        modelBuilder.Entity<Note>(note =>
        {
            note.ToTable("notes");
            note.HasKey(entity => entity.Id);
            note.Property(entity => entity.Id).HasConversion(id => id.Value, value => new NoteId(value));
            note.Property(entity => entity.Text).HasMaxLength(100);
            note.HasIndex(entity => entity.Text).IsUnique();
        });
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
