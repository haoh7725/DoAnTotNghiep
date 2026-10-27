import 'package:flutter/material.dart';
import 'api_client.dart';

class ResearchPlanScreen extends StatefulWidget {
  const ResearchPlanScreen({
    super.key,
    required this.client,
    this.planId,
    this.itemId,
  }) : assert(planId != null || itemId != null);

  final ApiClient client;
  final int? planId;
  final int? itemId;

  @override
  State<ResearchPlanScreen> createState() =>
      _ResearchPlanScreenState();
}

class _ResearchPlanScreenState extends State<ResearchPlanScreen> {
  late Future<Map<String, dynamic>> _future;

  @override
  void initState() {
    super.initState();
    _future = _load();
  }

  Future<Map<String, dynamic>> _load() async {
    int? planId = widget.planId;

    if (widget.itemId != null) {
      final item = await widget.client
          .getResearchPlanItem(widget.itemId!);
      planId = (item['researchPlanId'] as num).toInt();
    }

    if (planId == null) {
      throw Exception('Không xác định được kế hoạch liên quan.');
    }

    final plan = await widget.client.getResearchPlan(planId);
    final items = await widget.client.getResearchPlanItems(planId);

    return {
      'plan': Map<String, dynamic>.from(plan as Map),
      'items': items,
    };
  }

  String _statusLabel(String? status) => switch (status) {
    'CHUA_THUC_HIEN' => 'Chưa thực hiện',
    'DANG_THUC_HIEN' => 'Đang thực hiện',
    'DA_THUC_HIEN' => 'Đã thực hiện',
    _ => 'Chưa xác định',
  };

  String _dateLabel(dynamic value) {
    final date = DateTime.tryParse(value?.toString() ?? '');
    if (date == null) return 'Chưa có';
    return '${date.day.toString().padLeft(2, '0')}/'
        '${date.month.toString().padLeft(2, '0')}/'
        '${date.year}';
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('Kế hoạch liên quan')),
      body: FutureBuilder<Map<String, dynamic>>(
        future: _future,
        builder: (context, snapshot) {
          if (snapshot.connectionState != ConnectionState.done) {
            return const Center(child: CircularProgressIndicator());
          }

          if (snapshot.hasError) {
            return Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Text(
                      'Không tải được kế hoạch: ${snapshot.error}',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 12),
                    ElevatedButton(
                      onPressed: () {
                        setState(() => _future = _load());
                      },
                      child: const Text('Thử lại'),
                    ),
                  ],
                ),
              ),
            );
          }

          final data = snapshot.data!;
          final plan = data['plan'] as Map<String, dynamic>;
          final items = data['items'] as List<dynamic>;

          return ListView(
            padding: const EdgeInsets.all(16),
            children: [
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Thông tin kế hoạch',
                        style: TextStyle(
                          fontWeight: FontWeight.bold,
                          fontSize: 18,
                        ),
                      ),
                      const SizedBox(height: 8),
                      Text(
                        'Trạng thái: ${plan['status'] == 'DA_DANG_KY'
                            ? 'Đã đăng ký' : 'Bản nháp'}',
                      ),
                      Text(
                        'Số sản phẩm cam kết: '
                        '${plan['committedProductCount']}',
                      ),
                      Text(
                        'Hạn hoàn thành: '
                        '${_dateLabel(plan['deadline'])}',
                      ),
                    ],
                  ),
                ),
              ),
              const SizedBox(height: 12),
              if (items.isEmpty)
                const Text('Kế hoạch chưa có nội dung nghiên cứu.'),
              ...items.map((raw) {
                final item = Map<String, dynamic>.from(raw as Map);
                final related = item['id'] == widget.itemId;

                return Card(
                  shape: RoundedRectangleBorder(
                    borderRadius: BorderRadius.circular(12),
                    side: BorderSide(
                      color: related ? Colors.teal : Colors.grey.shade300,
                      width: related ? 2 : 1,
                    ),
                  ),
                  child: Padding(
                    padding: const EdgeInsets.all(16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (related)
                          const Text(
                            'Nội dung được nhắc trong thông báo',
                            style: TextStyle(color: Colors.teal),
                          ),
                        Text(
                          item['name']?.toString() ?? '',
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 16,
                          ),
                        ),
                        const SizedBox(height: 8),
                        Text(
                          'Tiến độ: '
                          '${_statusLabel(item['status'] as String?)}',
                        ),
                        Text(
                          'Số lượng cam kết: ${item['committedQuantity']}',
                        ),
                        Text(
                          'Hạn hoàn thành: ${_dateLabel(item['deadline'])}',
                        ),
                        if (item['result'] != null)
                          Text('Kết quả: ${item['result']}'),
                        if (item['difficultyProposal'] != null)
                          Text('Khó khăn/đề xuất: ${item['difficultyProposal']}'),
                      ],
                    ),
                  ),
                );
              }),
            ],
          );
        },
      ),
    );
  }
}