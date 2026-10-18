using Microsoft.EntityFrameworkCore;
using ResearchManagement.Domain.Entities;
using ResearchManagement.Infrastructure.Persistence;

namespace ResearchManagement.Api.Tests;

public sealed class ProductSnapshotTests
{
    [Fact]
    public void Snapshot_PreservesContentAfterProductChanges()
    {
        var product = new Product(1, 2, "Original", "Description", "Publication", null,
            null, "Index", "Classification", "Level");
        product.Submit();
        var snapshot = new ProductSnapshot(product, 3, 1);
        product.Return();
        product.Update("Changed", null, null, null, null, null, null, null);

        Assert.Equal("Original", snapshot.Title);
        Assert.Equal("Description", snapshot.Description);
        Assert.Equal("NOP", snapshot.Status);
        Assert.Equal(3, snapshot.SnapshotByAccountId);
        Assert.Equal(1, snapshot.Version);
    }

    [Fact]
    public void Snapshot_UsesApplicationTableAndCompositeKey()
    {
        using var db = new ApplicationDbContext(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql("Host=localhost;Database=unused;Username=unused;Password=unused").Options);
        var entity = db.Model.FindEntityType(typeof(ProductSnapshot))!;
        Assert.Equal("san_pham_phien_ban_ung_dung", entity.GetTableName());
        Assert.Equal(new[] { "ProductId", "Version" }, entity.FindPrimaryKey()!.Properties.Select(p => p.Name));
    }
}
