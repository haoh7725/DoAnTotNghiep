import 'dart:math' as math;
import 'package:flutter/material.dart';
import 'api_client.dart';

class DashboardScreen extends StatefulWidget {
  const DashboardScreen({super.key, required this.client});

  final ApiClient client;

  @override
  State<DashboardScreen> createState() => _DashboardScreenState();
}

class _ProgressChartPainter extends CustomPainter {
  const _ProgressChartPainter({
    required this.completed,
    required this.inProgress,
    required this.notStarted,
  });

  final int completed;
  final int inProgress;
  final int notStarted;

  @override
  void paint(Canvas canvas, Size size) {
    final total = completed + inProgress + notStarted;
    final center = Offset(size.width / 2, size.height / 2);
    final radius = math.min(size.width, size.height) / 2 - 12;

    final backgroundPaint = Paint()
      ..color = const Color(0xFFE0E7E3)
      ..style = PaintingStyle.stroke
      ..strokeWidth = 20;

    canvas.drawCircle(center, radius, backgroundPaint);

    if (total == 0) return;

    final rect = Rect.fromCircle(center: center, radius: radius);

    var startAngle = -math.pi / 2;

    final segments = [
      (completed, const Color(0xFF177158)),
      (inProgress, Colors.orange),
      (notStarted, Colors.grey),
    ];

    for (final segment in segments) {
      if (segment.$1 == 0) continue;

      final sweepAngle = 2 * math.pi * segment.$1 / total;

      final paint = Paint()
        ..color = segment.$2
        ..style = PaintingStyle.stroke
        ..strokeWidth = 20;

      canvas.drawArc(rect, startAngle, sweepAngle, false, paint);

      startAngle += sweepAngle;
    }
  }

  @override
  bool shouldRepaint(covariant _ProgressChartPainter oldDelegate) {
    return completed != oldDelegate.completed ||
        inProgress != oldDelegate.inProgress ||
        notStarted != oldDelegate.notStarted;
  }
}

class _DashboardScreenState extends State<DashboardScreen> {
  List<dynamic> _years = [];
  int? _yearId;
  Map<String, dynamic>? _plan;
  List<Map<String, dynamic>> _items = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _loadInitial();
  }

  Future<void> _loadInitial() async {
    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final lookupData = await widget.client.getLookups();
      final years = (lookupData['academicYears'] as List?) ?? [];

      if (!mounted) return;

      _years = years;
      _yearId = years.isNotEmpty ? (years.first['id'] as num).toInt() : null;

      await _loadDashboard();
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  Future<void> _loadDashboard() async {
    if (_yearId == null) {
      setState(() {
        _plan = null;
        _items = [];
        _loading = false;
      });
      return;
    }

    setState(() {
      _loading = true;
      _error = null;
    });

    try {
      final lecturer = await widget.client.getLecturerMe();
      if (lecturer is! Map || lecturer['id'] == null) {
        throw Exception('Tài khoản chưa liên kết với giảng viên.');
      }

      final lecturerId = (lecturer['id'] as num).toInt();
      final planData = await widget.client.getResearchPlan(
        lecturerId,
        _yearId!,
      );

      Map<String, dynamic>? plan;
      List<Map<String, dynamic>> items = [];

      if (planData is Map) {
        plan = Map<String, dynamic>.from(planData);
        final planId = (plan['id'] as num).toInt();
        final itemData = await widget.client.getResearchPlanItems(planId);

        if (itemData is List) {
          items = itemData
              .whereType<Map>()
              .map((e) => Map<String, dynamic>.from(e))
              .toList();
        }
      }

      if (!mounted) return;
      setState(() {
        _plan = plan;
        _items = items;
        _loading = false;
      });
    } catch (e) {
      if (!mounted) return;
      setState(() {
        _error = e.toString();
        _loading = false;
      });
    }
  }

  @override
  Widget build(BuildContext context) {
    final total = _items.length;
    final completed = _items.where((e) => e['status'] == 'DA_THUC_HIEN').length;
    final inProgress = _items
        .where((e) => e['status'] == 'DANG_THUC_HIEN')
        .length;
    final notStarted = _items
        .where((e) => e['status'] == 'CHUA_THUC_HIEN')
        .length;
    final progress = total == 0 ? 0.0 : completed / total;

    return Scaffold(
      appBar: AppBar(
        title: const Text('Dashboard NCKH'),
        actions: [
          IconButton(
            onPressed: _loading ? null : _loadDashboard,
            icon: const Icon(Icons.refresh),
            tooltip: 'Làm mới',
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: _loadDashboard,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (_years.isNotEmpty)
              DropdownButtonFormField<int>(
                initialValue: _yearId,
                decoration: const InputDecoration(
                  labelText: 'Năm học',
                  border: OutlineInputBorder(),
                ),
                items: _years.map((year) {
                  final id = (year['id'] as num).toInt();
                  return DropdownMenuItem<int>(
                    value: id,
                    child: Text(
                      (year['code'] ?? year['name'] ?? id).toString(),
                    ),
                  );
                }).toList(),
                onChanged: _loading
                    ? null
                    : (value) {
                        if (value == null) return;
                        setState(() => _yearId = value);
                        _loadDashboard();
                      },
              ),
            const SizedBox(height: 20),
            if (_loading)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(40),
                  child: CircularProgressIndicator(),
                ),
              )
            else if (_error != null)
              _messageCard(_error!, Icons.error_outline)
            else if (_plan == null)
              _messageCard(
                'Chưa có kế hoạch nghiên cứu trong năm học này.',
                Icons.event_note,
              )
            else ...[
              Text(
                'Tổng quan kế hoạch',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              _statCard(
                'Sản phẩm cam kết',
                '${_plan!['committedProductCount'] ?? 0}',
                Icons.menu_book,
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _statCard(
                      'Tổng công việc',
                      '$total',
                      Icons.list_alt,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _statCard(
                      'Đã thực hiện',
                      '$completed',
                      Icons.check_circle_outline,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 8),
              Row(
                children: [
                  Expanded(
                    child: _statCard(
                      'Đang thực hiện',
                      '$inProgress',
                      Icons.timelapse,
                    ),
                  ),
                  const SizedBox(width: 8),
                  Expanded(
                    child: _statCard(
                      'Chưa thực hiện',
                      '$notStarted',
                      Icons.pending_actions,
                    ),
                  ),
                ],
              ),
              const SizedBox(height: 24),
              Text(
                'Tiến độ công việc',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              Text(
                '${(progress * 100).toStringAsFixed(1)}% công việc đã thực hiện',
                style: const TextStyle(fontWeight: FontWeight.bold),
              ),
              const SizedBox(height: 8),
              LinearProgressIndicator(
                value: progress,
                minHeight: 12,
                borderRadius: BorderRadius.circular(8),
                color: const Color(0xFF177158),
              ),
              const SizedBox(height: 8),
              Text(
                '$completed/$total công việc đã thực hiện',
                style: const TextStyle(color: Colors.grey),
              ),
              const SizedBox(height: 24),
              _buildProgressChart(
                completed: completed,
                inProgress: inProgress,
                notStarted: notStarted,
              ),
              const SizedBox(height: 24),
              Text(
                'Danh sách công việc',
                style: Theme.of(context).textTheme.titleLarge,
              ),
              const SizedBox(height: 12),
              if (_items.isEmpty)
                _messageCard(
                  'Kế hoạch chưa có công việc nào.',
                  Icons.inbox_outlined,
                )
              else
                ..._items.map(_buildItemCard),
            ],
          ],
        ),
      ),
    );
  }

  Widget _buildProgressChart({
    required int completed,
    required int inProgress,
    required int notStarted,
  }) {
    final total = completed + inProgress + notStarted;

    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text(
              'Phân bố trạng thái công việc',
              style: TextStyle(fontSize: 16, fontWeight: FontWeight.bold),
            ),
            const SizedBox(height: 20),
            Center(
              child: SizedBox(
                width: 180,
                height: 180,
                child: CustomPaint(
                  painter: _ProgressChartPainter(
                    completed: completed,
                    inProgress: inProgress,
                    notStarted: notStarted,
                  ),
                  child: Center(
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Text(
                          '$total',
                          style: const TextStyle(
                            fontSize: 32,
                            fontWeight: FontWeight.bold,
                          ),
                        ),
                        const Text('Công việc'),
                      ],
                    ),
                  ),
                ),
              ),
            ),
            const SizedBox(height: 20),
            _chartLegend('Đã thực hiện', completed, const Color(0xFF177158)),
            _chartLegend('Đang thực hiện', inProgress, Colors.orange),
            _chartLegend('Chưa thực hiện', notStarted, Colors.grey),
          ],
        ),
      ),
    );
  }

  Widget _chartLegend(String label, int count, Color color) {
    return Padding(
      padding: const EdgeInsets.symmetric(vertical: 5),
      child: Row(
        children: [
          Container(
            width: 12,
            height: 12,
            decoration: BoxDecoration(
              color: color,
              borderRadius: BorderRadius.circular(3),
            ),
          ),
          const SizedBox(width: 10),
          Expanded(child: Text(label)),
          Text('$count', style: const TextStyle(fontWeight: FontWeight.bold)),
        ],
      ),
    );
  }

  Widget _statCard(String title, String value, IconData icon) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: const Color(0xFF177158)),
            const SizedBox(height: 8),
            Text(title, style: const TextStyle(fontSize: 12)),
            const SizedBox(height: 6),
            Text(
              value,
              style: const TextStyle(fontSize: 23, fontWeight: FontWeight.bold),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildItemCard(Map<String, dynamic> item) {
    final status = (item['status'] ?? '').toString();

    final statusLabel = switch (status) {
      'DA_THUC_HIEN' => 'Đã thực hiện',
      'DANG_THUC_HIEN' => 'Đang thực hiện',
      'CHUA_THUC_HIEN' => 'Chưa thực hiện',
      _ => status,
    };

    final statusColor = switch (status) {
      'DA_THUC_HIEN' => const Color(0xFF177158),
      'DANG_THUC_HIEN' => Colors.orange,
      'CHUA_THUC_HIEN' => Colors.grey,
      _ => Colors.blueGrey,
    };

    final deadlineText = item['deadline']?.toString();
    DateTime? deadline;

    if (deadlineText != null && deadlineText.isNotEmpty) {
      deadline = DateTime.tryParse(deadlineText);
    }

    final formattedDeadline = deadline == null
        ? (deadlineText == null || deadlineText.isEmpty
              ? 'Chưa có'
              : deadlineText)
        : '${deadline.day.toString().padLeft(2, '0')}/'
              '${deadline.month.toString().padLeft(2, '0')}/'
              '${deadline.year}';

    final now = DateTime.now();
    final today = DateTime(now.year, now.month, now.day);

    final isOverdue =
        deadline != null &&
        status != 'DA_THUC_HIEN' &&
        DateTime(deadline.year, deadline.month, deadline.day).isBefore(today);

    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 4),
        child: ListTile(
          leading: Icon(
            status == 'DA_THUC_HIEN'
                ? Icons.check_circle
                : status == 'DANG_THUC_HIEN'
                ? Icons.timelapse
                : Icons.assignment_outlined,
            color: isOverdue ? Colors.red : statusColor,
          ),
          title: Text(
            (item['name'] ?? 'Không có tên').toString(),
            style: const TextStyle(fontWeight: FontWeight.w600),
          ),
          subtitle: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const SizedBox(height: 6),
              Text('Trạng thái: $statusLabel'),
              Text('Số lượng cam kết: ${item['committedQuantity'] ?? 0}'),
              Text('Hạn hoàn thành: $formattedDeadline'),
              if (isOverdue) ...[
                const SizedBox(height: 8),
                const Row(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Icon(
                      Icons.warning_amber_rounded,
                      color: Colors.red,
                      size: 18,
                    ),
                    SizedBox(width: 5),
                    Text(
                      'Quá hạn',
                      style: TextStyle(
                        color: Colors.red,
                        fontWeight: FontWeight.bold,
                      ),
                    ),
                  ],
                ),
              ],
            ],
          ),
        ),
      ),
    );
  }

  Widget _messageCard(String message, IconData icon) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(24),
        child: Column(
          children: [
            Icon(icon, size: 40, color: Colors.grey),
            const SizedBox(height: 12),
            Text(message, textAlign: TextAlign.center),
          ],
        ),
      ),
    );
  }
}
