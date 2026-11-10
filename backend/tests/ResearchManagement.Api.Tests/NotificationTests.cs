using ResearchManagement.Domain.Entities;

namespace ResearchManagement.Api.Tests;

public sealed class NotificationTests
{
    [Fact]
    public void Notification_PreservesActionPathAndProductLink()
    {
        var notification = new Notification(
            12,
            "HE_THONG",
            "Sản phẩm đang chờ duyệt.",
            "PRODUCT_WAIT_DEAN:45:12",
            scientificProductId: 45,
            actionPath: "product-reviews:45");

        Assert.Equal(45, notification.ScientificProductId);
        Assert.Equal("product-reviews:45", notification.ActionPath);
    }

    [Fact]
    public void Notification_RejectsOversizedActionPath()
    {
        Assert.Throws<ArgumentException>(() => new Notification(
            1,
            "HE_THONG",
            "Thông báo",
            "key",
            actionPath: new string('x', 201)));
    }

    [Fact]
    public void Notification_TracksDeliveryAndReadState()
    {
        var notification = new Notification(1, "HE_THONG", "Thông báo", "key");

        notification.MarkAsSent();
        notification.MarkAsRead();

        Assert.Equal("DA_GUI", notification.DeliveryStatus);
        Assert.NotNull(notification.SentAt);
        Assert.True(notification.IsRead);
    }
}
