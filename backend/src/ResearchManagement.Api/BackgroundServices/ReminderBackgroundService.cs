
using ResearchManagement.Application.Notifications;

namespace ResearchManagement.Api.BackgroundServices;

public sealed class ReminderBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<ReminderBackgroundService> logger)
    : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        // Chạy lần đầu khi API khởi động.
        await CheckRemindersAsync(stoppingToken);

        using var timer = new PeriodicTimer(
            TimeSpan.FromHours(1));

        try
        {
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                await CheckRemindersAsync(stoppingToken);
            }
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            // API đang dừng bình thường.
        }
    }

    private async Task CheckRemindersAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            await using var scope =
                scopeFactory.CreateAsyncScope();

            var service = scope.ServiceProvider
                .GetRequiredService<ReminderService>();

            var vietnamTimeZone =
                TimeZoneInfo.FindSystemTimeZoneById(
                    "Asia/Ho_Chi_Minh");

            var vietnamNow = TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                vietnamTimeZone);

            var today = DateOnly.FromDateTime(
                vietnamNow.DateTime);

            var createdCount = await service.GenerateAsync(
                today,
                cancellationToken);

            logger.LogInformation(
                "Reminder check completed. Date={Date}, Created={Count}",
                today,
                createdCount);
        }
        catch (OperationCanceledException)
            when (cancellationToken.IsCancellationRequested)
        {
            // Không ghi lỗi khi ứng dụng đang dừng.
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Reminder background check failed.");
        }
    }
}
