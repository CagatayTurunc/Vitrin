using FluentAssertions;
using Vitrin.Auth.Domain.Entities;
using Xunit;

namespace Vitrin.Auth.Tests.Domain;

public class UserTests
{
    [Fact]
    public void CreateWithPassword_Should_Create_User_With_Local_Provider()
    {
        // Arrange
        var email = "test@example.com";
        var username = "testuser";
        var fullName = "Test User";
        var passwordHash = "hashedpassword";

        // Act
        var user = User.CreateWithPassword(email, username, fullName, passwordHash);

        // Assert
        user.Email.Should().Be(email);
        user.Username.Should().Be(username);
        user.FullName.Should().Be(fullName);
        user.PasswordHash.Should().Be(passwordHash);
        user.Provider.Should().Be(AuthProvider.Local);
        user.Role.Should().Be(UserRole.Member);
        user.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void CreateWithGoogle_Should_Create_User_With_Google_Provider()
    {
        // Arrange
        var email = "google@example.com";
        var username = "googleuser";
        var fullName = "Google User";
        var avatarUrl = "https://avatar.example.com/photo.jpg";
        var googleId = "google-id-123";

        // Act
        var user = User.CreateWithGoogle(email, username, fullName, avatarUrl, googleId);

        // Assert
        user.Provider.Should().Be(AuthProvider.Google);
        user.GoogleId.Should().Be(googleId);
        user.PasswordHash.Should().BeNull();
        user.Email.Should().Be(email);
        user.AvatarUrl.Should().Be(avatarUrl);
    }

    [Fact]
    public void CreateWithGithub_Should_Create_User_With_Github_Provider()
    {
        // Arrange
        var githubId = "github-id-456";
        var user = User.CreateWithGithub("gh@example.com", "ghuser", "GH User", "https://avatar.example.com/gh.jpg", githubId);

        // Assert
        user.Provider.Should().Be(AuthProvider.Github);
        user.GithubId.Should().Be(githubId);
        user.PasswordHash.Should().BeNull();
    }

    [Fact]
    public void UpdateProfile_Should_Update_User_Fields()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "olduser", "Old Name", "hash");

        // Act
        user.UpdateProfile("New Name", "newuser", "Headline", "About me", null, "https://example.com", null, null);

        // Assert
        user.FullName.Should().Be("New Name");
        user.Username.Should().Be("newuser");
        user.Headline.Should().Be("Headline");
        user.About.Should().Be("About me");
    }

    [Fact]
    public void UpdateRole_Should_Change_User_Role()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "user", "User", "hash");
        user.Role.Should().Be(UserRole.Member);

        // Act
        user.UpdateRole(UserRole.Admin);

        // Assert
        user.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public void RecordVoteActivity_FirstTime_Should_Start_Streak_At_1()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "user", "User", "hash");

        // Act
        user.RecordVoteActivity();

        // Assert
        user.CurrentStreak.Should().Be(1);
        user.LongestStreak.Should().Be(1);
        user.LastVoteDate.Should().NotBeNull();
    }

    [Fact]
    public void RecordVoteActivity_VotedTodayAgain_Should_Not_Change_Streak()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "user", "User", "hash");
        user.RecordVoteActivity(); // First vote today

        // Act
        user.RecordVoteActivity(); // Second vote today

        // Assert
        user.CurrentStreak.Should().Be(1); // Should stay at 1
    }

    [Fact]
    public void AddBadge_Should_Add_Badge_When_Not_Already_Present()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "user", "User", "hash");

        // Act
        user.AddBadge("First Vote", "🗳️");
        user.AddBadge("First Vote", "🗳️"); // duplicate

        // Assert
        user.Badges.Should().HaveCount(1);
        user.Badges.First().Name.Should().Be("First Vote");
    }

    [Fact]
    public void AddBadge_Should_Add_Multiple_Different_Badges()
    {
        // Arrange
        var user = User.CreateWithPassword("test@example.com", "user", "User", "hash");

        // Act
        user.AddBadge("First Vote", "🗳️");
        user.AddBadge("Week Streak", "🔥");

        // Assert
        user.Badges.Should().HaveCount(2);
    }
}
