using Microsoft.EntityFrameworkCore;

namespace Vitrin.Shared.Infrastructure.Outbox;

public static class OutboxModelBuilderExtensions
{
    public static ModelBuilder ConfigureVitrinOutbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<OutboxMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property(message => message.EventType).IsRequired().HasMaxLength(200);
            builder.Property(message => message.EventVersion).IsRequired().HasMaxLength(20);
            builder.Property(message => message.Topic).IsRequired().HasMaxLength(100);
            builder.Property(message => message.Payload).IsRequired();
            builder.Property(message => message.LastError).HasMaxLength(2_000);
            builder.HasIndex(message => new
            {
                message.ProcessedAtUtc,
                message.DeadLetteredAtUtc,
                message.NextAttemptAtUtc
            });
        });

        return modelBuilder;
    }
}
