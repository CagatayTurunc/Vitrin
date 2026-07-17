using FluentAssertions;
using Vitrin.Product.Domain.Entities;
using Xunit;

namespace Vitrin.Product.Tests.Domain;

public sealed class CollectionTests
{
    [Fact]
    public void PrivateCollection_ShouldOnlyBeVisibleToOwner()
    {
        var ownerId = Guid.NewGuid();
        var collection = Collection.Create(
            ownerId,
            "Private picks",
            "private-picks",
            string.Empty,
            CollectionVisibility.Private);

        collection.CanView(ownerId).Should().BeTrue();
        collection.CanView(Guid.NewGuid()).Should().BeFalse();
        collection.CanView(null).Should().BeFalse();
    }

    [Fact]
    public void SharedCollection_ShouldHonorViewerAndEditorRoles()
    {
        var ownerId = Guid.NewGuid();
        var viewerId = Guid.NewGuid();
        var editorId = Guid.NewGuid();
        var collection = Collection.Create(
            ownerId,
            "Team picks",
            "team-picks",
            string.Empty,
            CollectionVisibility.Shared);

        collection.AddOrUpdateCollaborator(viewerId, CollectionCollaboratorRole.Viewer);
        collection.AddOrUpdateCollaborator(editorId, CollectionCollaboratorRole.Editor);

        collection.CanView(viewerId).Should().BeTrue();
        collection.CanEdit(viewerId).Should().BeFalse();
        collection.CanEdit(editorId).Should().BeTrue();
        collection.CanEdit(ownerId).Should().BeTrue();
    }

    [Fact]
    public void UpdatingCollaborator_ShouldNotCreateDuplicateMembership()
    {
        var collection = Collection.Create(
            Guid.NewGuid(),
            "Team picks",
            "team-picks",
            string.Empty,
            CollectionVisibility.Shared);
        var memberId = Guid.NewGuid();

        collection.AddOrUpdateCollaborator(memberId, CollectionCollaboratorRole.Viewer);
        collection.AddOrUpdateCollaborator(memberId, CollectionCollaboratorRole.Editor);

        collection.Collaborators.Should().ContainSingle();
        collection.Collaborators[0].Role.Should().Be(CollectionCollaboratorRole.Editor);
    }
}
