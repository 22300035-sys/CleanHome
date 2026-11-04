using CleanHome.Domain.Entities;

namespace CleanHome.UnitTests;

public class ServiceCategoryTests
{
    [Theory]
    [InlineData("", "valid")]
    [InlineData("Valid", "")]
    [InlineData("Valid", "invalid_slug")]
    [InlineData("Valid", "-invalid")]
    public void InvalidCategoryIsRejected(string name, string slug)
    {
        Assert.Throws<ArgumentException>(() => new ServiceCategory(name, slug, ""));
    }

    [Fact]
    public void CategoryNormalizesInputAndCanBeDeactivated()
    {
        var category = new ServiceCategory(" General ", " GENERAL ", " Description ");
        Assert.Equal("General", category.Name);
        Assert.Equal("general", category.Slug);
        Assert.Equal("Description", category.Description);
        Assert.NotEqual(Guid.Empty, category.Id);
        Assert.True(category.IsActive);
        Assert.Equal(DateTimeKind.Utc, category.CreatedAt.Kind);
        category.Deactivate();
        Assert.False(category.IsActive);
        Assert.True(category.UpdatedAt >= category.CreatedAt);
    }

    [Theory]
    [InlineData(121, 1, 0)]
    [InlineData(1, 121, 0)]
    [InlineData(1, 1, 1001)]
    public void OversizedFieldsAreRejected(int nameLength, int slugLength, int descriptionLength)
    {
        Assert.Throws<ArgumentException>(() => new ServiceCategory(
            new string('a', nameLength), new string('a', slugLength),
            new string('a', descriptionLength)));
    }
}
