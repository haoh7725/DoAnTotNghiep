import 'package:flutter/material.dart';
import 'api_client.dart';
import 'products_screen.dart';
import 'profile_screen.dart';
import 'product_detail_screen.dart' show MessageWidget;

// ---------------------------------------------------------------------------
// HomeScreen – BottomNavigationBar with 3 tabs
// ---------------------------------------------------------------------------

class HomeScreen extends StatefulWidget {
  const HomeScreen({
    super.key,
    required this.client,
    required this.onLogout,
  });
  final ApiClient client;
  final VoidCallback onLogout;

  @override
  State<HomeScreen> createState() => _HomeScreenState();
}

class _HomeScreenState extends State<HomeScreen> {
  int _currentIndex = 0;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: IndexedStack(
        index: _currentIndex,
        children: [
          _EvaluationTab(client: widget.client, onLogout: widget.onLogout),
          ProductsScreen(client: widget.client),
          ProfileScreen(client: widget.client, onLogout: widget.onLogout),
        ],
      ),
      bottomNavigationBar: BottomNavigationBar(
        currentIndex: _currentIndex,
        selectedItemColor: const Color(0xFF177158),
        unselectedItemColor: Colors.grey,
        onTap: (i) => setState(() => _currentIndex = i),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.bar_chart_outlined),
            activeIcon: Icon(Icons.bar_chart),
            label: 'Kết quả',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.article_outlined),
            activeIcon: Icon(Icons.article),
            label: 'Sản phẩm',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.person_outlined),
            activeIcon: Icon(Icons.person),
            label: 'Hồ sơ',
          ),
        ],
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// _EvaluationTab – ported from original EvaluationScreen
// ---------------------------------------------------------------------------

class _EvaluationTab extends StatefulWidget {
  const _EvaluationTab({required this.client, required this.onLogout});
  final ApiClient client;
  final VoidCallback onLogout;

  @override
  State<_EvaluationTab> createState() => _EvaluationTabState();
}

class _EvaluationTabState extends State<_EvaluationTab> {
  List<dynamic> years = [];
  int? yearId;
  Map<String, dynamic>? evaluation;
  bool loading = true;
  String? error;
  bool empty = false;

  @override
  void initState() {
    super.initState();
    loadYears();
  }

  Future<void> loadYears() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final data = await widget.client.get('/lookups');
      years = data['academicYears'] as List<dynamic>;
      if (years.isNotEmpty) {
        yearId = years.first['id'] as int;
        await loadEvaluation();
      }
    } catch (e) {
      error = e.toString();
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> loadEvaluation() async {
    if (yearId == null) return;
    setState(() {
      loading = true;
      error = null;
      empty = false;
      evaluation = null;
    });
    try {
      evaluation = Map<String, dynamic>.from(
          await widget.client.get('/evaluations/me?academicYearId=$yearId'));
    } on ApiException catch (e) {
      if (e.status == 404) {
        empty = true;
      } else {
        error = e.message;
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kết quả nghiên cứu'),
        backgroundColor: const Color(0xFF173F32),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: widget.onLogout,
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: loadEvaluation,
        child: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            if (years.isNotEmpty)
              DropdownButtonFormField<int>(
                value: yearId,
                decoration: const InputDecoration(
                    labelText: 'Năm học', border: OutlineInputBorder()),
                items: years
                    .map((x) => DropdownMenuItem<int>(
                          value: x['id'] as int,
                          child: Text(x['code'] as String),
                        ))
                    .toList(),
                onChanged: (v) {
                  yearId = v;
                  loadEvaluation();
                },
              ),
            const SizedBox(height: 20),
            if (loading)
              const Center(
                  child: Padding(
                      padding: EdgeInsets.all(32),
                      child: CircularProgressIndicator()))
            else if (error != null)
              MessageWidget(
                icon: Icons.error_outline,
                title: 'Không tải được kết quả',
                text: error!,
                action: loadEvaluation,
              )
            else if (empty)
              const MessageWidget(
                icon: Icons.assignment_outlined,
                title: 'Chưa có đánh giá',
                text: 'Năm học này chưa có kết quả đánh giá đã chốt.',
              )
            else if (evaluation != null)
              _EvaluationView(data: evaluation!),
          ],
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// _EvaluationView, _Metric – ported from original
// ---------------------------------------------------------------------------

class _EvaluationView extends StatelessWidget {
  const _EvaluationView({required this.data});
  final Map<String, dynamic> data;

  @override
  Widget build(BuildContext context) {
    final details = data['details'] as List<dynamic>? ?? [];
    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        Card(
          color: const Color(0xFF173F32),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  data['classification'] ?? 'Đã chốt',
                  style: const TextStyle(
                      color: Colors.white,
                      fontSize: 24,
                      fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                Text(
                  data['classificationBasis'] ?? '',
                  style: const TextStyle(color: Color(0xFFD5E5DB)),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),
        Row(
          children: [
            Expanded(
                child: _Metric(
                    label: 'Giờ quy đổi', value: '${data['totalHours']}')),
            const SizedBox(width: 12),
            Expanded(
                child: _Metric(
                    label: 'Điểm quy đổi', value: '${data['totalPoints']}')),
          ],
        ),
        const SizedBox(height: 22),
        Text(
          'Chi tiết quy đổi',
          style: Theme.of(context)
              .textTheme
              .titleLarge
              ?.copyWith(fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 8),
        if (details.isEmpty)
          const Text('Không có sản phẩm đủ điều kiện quy đổi.')
        else
          ...details.map((raw) {
            final x = raw as Map<String, dynamic>;
            return Card(
              shape: RoundedRectangleBorder(
                  borderRadius: BorderRadius.circular(12)),
              child: ListTile(
                title: Text(x['productTitle'] ?? ''),
                subtitle: Text(
                  '${x['ruleCode']} phiên bản ${x['ruleVersion']}\n${x['calculationBasis']}',
                ),
                isThreeLine: true,
                trailing: Text(
                  '${x['convertedValue']}\n${x['unit']}',
                  textAlign: TextAlign.right,
                  style: const TextStyle(fontWeight: FontWeight.bold),
                ),
              ),
            );
          }),
      ],
    );
  }
}

class _Metric extends StatelessWidget {
  const _Metric({required this.label, required this.value});
  final String label;
  final String value;

  @override
  Widget build(BuildContext context) {
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(18),
        child: Column(
          children: [
            Text(label),
            const SizedBox(height: 8),
            Text(
              value,
              style: Theme.of(context)
                  .textTheme
                  .headlineSmall
                  ?.copyWith(fontWeight: FontWeight.bold),
            ),
          ],
        ),
      ),
    );
  }
}
