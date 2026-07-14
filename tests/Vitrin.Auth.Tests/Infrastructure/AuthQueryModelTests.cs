using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitrin.Auth.Infrastructure.Data;
using Xunit;

namespace Vitrin.Auth.Tests.Infrastructure;

public class AuthQueryModelTests
{
    [Fact]
    public void IdentityColumns_ShouldUseCaseInsensitiveIndexedPostgreSqlTypes()
    {
        var options = new DbContextOptionsBuilder<AuthDbContext>()
            .UseNpgsql("Host=localhost;Database=translation_test;Username=test;Password=test")
            .Options;
        using var db = new AuthDbContext(options);
        var user = db.Model.FindEntityType("Vitrin.Auth.Domain.Entities.User")!;

        user.FindProperty("Email")!.GetColumnType().Should().Be("citext");
        user.FindProperty("Username")!.GetColumnType().Should().Be("citext");
        user.GetIndexes().Select(index => index.GetDatabaseName()).Should().Contain([
            "UX_Users_Email",
            "UX_Users_Username"
        ]);

        var sql = db.Users
            .AsNoTracking()
            .Where(item => item.Username == "CaseInsensitiveUser")
            .ToQueryString();

        sql.Should().Contain("Username");
        sql.Should().NotContain("lower(");
    }
}
