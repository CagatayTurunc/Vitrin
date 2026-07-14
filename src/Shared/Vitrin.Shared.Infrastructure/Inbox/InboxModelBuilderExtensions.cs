using Microsoft.EntityFrameworkCore;

namespace Vitrin.Shared.Infrastructure.Inbox;

public static class InboxModelBuilderExtensions
{
    public static ModelBuilder ConfigureVitrinInbox(this ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<InboxMessage>(builder =>
        {
            builder.HasKey(message => message.Id);
            builder.Property(message => message.EventType).IsRequired().HasMaxLength(200);
            builder.HasIndex(message => message.ProcessedAtUtc);
        });

        return modelBuilder;
    }
}
