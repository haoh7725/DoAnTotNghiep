import 'package:flutter/material.dart';
import 'api_client.dart';

class NotificationsScreen extends StatefulWidget {
  final ApiClient api;

  const NotificationsScreen({super.key, required this.api});

  @override
  State<NotificationsScreen> createState() => _NotificationsScreenState();
}

class _NotificationsScreenState extends State<NotificationsScreen> {
  List<dynamic> notifications = [];
  bool loading = true;
  bool busy = false;
  String? error;

  @override
  void initState() {
    super.initState();
    loadNotifications();
  }

  Future<void> loadNotifications() async {
    setState(() {
      loading = true;
      error = null;
    });

    try {
      final data = await widget.api.getNotifications();

      if (!mounted) return;

      setState(() {
        notifications = data;
        loading = false;
      });
    } catch (e) {
      if (!mounted) return;

      setState(() {
        error = e.toString();
        loading = false;
      });
    }
  }

  Future<void> markAsRead(Map<String, dynamic> item) async {
    if (busy || item['isRead'] == true) return;

    setState(() => busy = true);

    try {
      final id = (item['id'] as num).toInt();
      await widget.api.markNotificationAsRead(id);

      await loadNotifications();
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(
        context,
      ).showSnackBar(SnackBar(content: Text('Không thể đánh dấu đã đọc: $e')));
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  Future<void> markAllAsRead() async {
    if (busy) return;

    setState(() => busy = true);

    try {
      await widget.api.markAllNotificationsAsRead();
      await loadNotifications();
    } catch (e) {
      if (!mounted) return;

      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('Không thể cập nhật thông báo: $e')),
      );
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final unreadCount = notifications
        .where((item) => item['isRead'] != true)
        .length;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Thông báo'),
        actions: [
          IconButton(
            tooltip: 'Làm mới',
            onPressed: loading || busy ? null : loadNotifications,
            icon: const Icon(Icons.refresh),
          ),
          TextButton(
            onPressed: loading || busy || unreadCount == 0
                ? null
                : markAllAsRead,
            child: const Text('Đọc tất cả'),
          ),
        ],
      ),
      body: loading
          ? const Center(child: CircularProgressIndicator())
          : error != null
          ? Center(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  const Text('Không thể tải thông báo'),
                  const SizedBox(height: 12),
                  ElevatedButton(
                    onPressed: loadNotifications,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            )
          : notifications.isEmpty
          ? const Center(child: Text('Bạn chưa có thông báo nào.'))
          : RefreshIndicator(
              onRefresh: loadNotifications,
              child: ListView.separated(
                padding: const EdgeInsets.all(12),
                itemCount: notifications.length,
                separatorBuilder: (_, _) => const SizedBox(height: 8),
                itemBuilder: (context, index) {
                  final item = Map<String, dynamic>.from(
                    notifications[index] as Map,
                  );

                  final isRead = item['isRead'] == true;
                  final content = item['content']?.toString() ?? '';
                  final rawType = item['type']?.toString() ?? '';
                  final type = switch (rawType.toUpperCase()) {
                    'QUA_HAN' => 'Thông báo quá hạn',
                    _ => rawType.isEmpty ? 'Thông báo' : rawType,
                  };

                  final rawDate = item['sentAt'] ?? item['scheduledAt'];
                  final parsedDate = rawDate == null
                      ? null
                      : DateTime.tryParse(rawDate.toString())?.toLocal();

                  final date = parsedDate == null
                      ? null
                      : '${parsedDate.day.toString().padLeft(2, '0')}/'
                            '${parsedDate.month.toString().padLeft(2, '0')}/'
                            '${parsedDate.year} '
                            '${parsedDate.hour.toString().padLeft(2, '0')}:'
                            '${parsedDate.minute.toString().padLeft(2, '0')}';

                  return Card(
                    child: ListTile(
                      leading: Icon(
                        isRead
                            ? Icons.notifications_none
                            : Icons.notifications_active,
                        color: isRead
                            ? Colors.grey
                            : Theme.of(context).colorScheme.primary,
                      ),
                      title: Text(
                        type,
                        style: TextStyle(
                          fontWeight: isRead
                              ? FontWeight.normal
                              : FontWeight.bold,
                        ),
                      ),
                      subtitle: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          const SizedBox(height: 4),
                          Text(content),
                          if (date != null) ...[
                            const SizedBox(height: 6),
                            Text(
                              date,
                              style: Theme.of(context).textTheme.bodySmall,
                            ),
                          ],
                        ],
                      ),
                      trailing: isRead
                          ? const Icon(Icons.done, color: Colors.grey)
                          : const Icon(
                              Icons.circle,
                              size: 10,
                              color: Colors.blue,
                            ),
                      onTap: busy ? null : () => markAsRead(item),
                    ),
                  );
                },
              ),
            ),
    );
  }
}
