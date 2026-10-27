import 'package:flutter/material.dart';
import 'api_client.dart';
import 'products_screen.dart';
import 'profile_screen.dart';
import 'notifications_screen.dart';
import 'product_detail_screen.dart' show MessageWidget;
import 'research_progress_screen.dart';

// ---------------------------------------------------------------------------
// HomeScreen – BottomNavigationBar with 3 tabs
// ---------------------------------------------------------------------------

class HomeScreen extends StatefulWidget {
  const HomeScreen({super.key, required this.client, required this.onLogout});
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
          NotificationsScreen(api: widget.client),
          ResearchProgressScreen(client: widget.client),
          ProfileScreen(client: widget.client, onLogout: widget.onLogout),
        ],
      ),

      bottomNavigationBar: BottomNavigationBar(
        type: BottomNavigationBarType.fixed,
        currentIndex: _currentIndex,
        selectedItemColor: const Color(0xFF177158),
        unselectedItemColor: Colors.grey,
        onTap: (i) => setState(() => _currentIndex = i),
        items: const [
          BottomNavigationBarItem(
            icon: Icon(Icons.emoji_events_outlined),
            activeIcon: Icon(Icons.emoji_events),
            label: 'Thi đua',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.article_outlined),
            activeIcon: Icon(Icons.article),
            label: 'Sản phẩm',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.notifications_none),
            activeIcon: Icon(Icons.notifications),
            label: 'Thông báo',
          ),
          BottomNavigationBarItem(
            icon: Icon(Icons.bar_chart_outlined),
            activeIcon: Icon(Icons.bar_chart),
            label: 'Tiến độ',
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
// _EvaluationTab – Xem kết quả & trạng thái thi đua cá nhân
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
  String? emptyMessage;
  bool notLinkedToLecturer = false;
  int _evaluationRequestVersion = 0;

  @override
  void initState() {
    super.initState();
    loadYears();
  }

  Future<void> loadYears() async {
    setState(() {
      loading = true;
      error = null;
      empty = false;
      emptyMessage = null;
      notLinkedToLecturer = false;
    });
    try {
      final data = await widget.client.getLookups();
      years = (data['academicYears'] as List<dynamic>?) ?? [];
      if (years.isNotEmpty) {
        yearId ??= years.first['id'] as int;
        await loadEvaluation();
      } else {
        empty = true;
        emptyMessage = 'Chưa có năm học nào được cấu hình trong hệ thống.';
        loading = false;
      }
    } catch (e) {
      error = e.toString();
      loading = false;
    } finally {
      if (mounted) setState(() {});
    }
  }

  Future<void> loadEvaluation() async {
    if (!mounted) return;

    final requestVersion = ++_evaluationRequestVersion;
    final requestedYearId = yearId;

    setState(() {
      loading = true;
      error = null;
      empty = false;
      emptyMessage = null;
      notLinkedToLecturer = false;
      evaluation = null;
    });

    if (requestedYearId == null) {
      setState(() {
        loading = false;
        empty = true;
        emptyMessage = 'Vui lòng chọn năm học.';
      });
      return;
    }

    try {
      final data = await widget.client.getMyEvaluation(requestedYearId);

      // Bỏ qua phản hồi của lần chọn năm học trước.
      if (!mounted || requestVersion != _evaluationRequestVersion) {
        return;
      }

      if (data is! Map) {
        throw const FormatException('Dữ liệu kết quả đánh giá không hợp lệ.');
      }

      evaluation = Map<String, dynamic>.from(data);
    } on ApiException catch (e) {
      if (!mounted || requestVersion != _evaluationRequestVersion) {
        return;
      }

      if (e.status == 404) {
        empty = true;

        if (e.message.toLowerCase().contains('chưa liên kết')) {
          notLinkedToLecturer = true;
          emptyMessage = e.message;
        } else {
          emptyMessage = e.message.isNotEmpty
              ? e.message
              : 'Năm học này chưa có kết quả đánh giá '
                    'thi đua đã chốt.';
        }
      } else {
        error = e.message;
      }
    } catch (e) {
      if (!mounted || requestVersion != _evaluationRequestVersion) {
        return;
      }

      error = e.toString();
    } finally {
      if (mounted && requestVersion == _evaluationRequestVersion) {
        setState(() => loading = false);
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Kết quả thi đua'),
        backgroundColor: const Color(0xFF173F32),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: years.isEmpty ? loadYears : loadEvaluation,
            tooltip: 'Làm mới',
            icon: const Icon(Icons.refresh),
          ),
          IconButton(
            onPressed: widget.onLogout,
            tooltip: 'Đăng xuất',
            icon: const Icon(Icons.logout),
          ),
        ],
      ),
      body: RefreshIndicator(
        onRefresh: years.isEmpty ? loadYears : loadEvaluation,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (years.isNotEmpty)
              DropdownButtonFormField<int>(
                value: yearId,
                isExpanded: true,
                decoration: InputDecoration(
                  labelText: 'Năm học',
                  prefixIcon: const Icon(
                    Icons.calendar_today_outlined,
                    size: 20,
                  ),
                  border: OutlineInputBorder(
                    borderRadius: BorderRadius.circular(12),
                  ),
                  contentPadding: const EdgeInsets.symmetric(
                    horizontal: 16,
                    vertical: 12,
                  ),
                ),
                items: years
                    .map(
                      (x) => DropdownMenuItem<int>(
                        value: x['id'] as int,
                        child: Text(
                          'Năm học ${x['code']}',
                          style: const TextStyle(fontWeight: FontWeight.w500),
                        ),
                      ),
                    )
                    .toList(),
                onChanged: (v) {
                  if (v != null && v != yearId) {
                    setState(() => yearId = v);
                    loadEvaluation();
                  }
                },
              ),
            const SizedBox(height: 16),
            if (loading)
              const Center(
                child: Padding(
                  padding: EdgeInsets.all(40),
                  child: CircularProgressIndicator(),
                ),
              )
            else if (error != null)
              MessageWidget(
                icon: Icons.error_outline,
                title: 'Không tải được kết quả',
                text: error!,
                action: years.isEmpty ? loadYears : loadEvaluation,
              )
            else if (empty)
              _EmptyEvaluationWidget(
                notLinkedToLecturer: notLinkedToLecturer,
                message: emptyMessage,
                onRefresh: loadEvaluation,
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
// _EmptyEvaluationWidget – Trạng thái chưa có đánh giá
// ---------------------------------------------------------------------------

class _EmptyEvaluationWidget extends StatelessWidget {
  const _EmptyEvaluationWidget({
    required this.notLinkedToLecturer,
    this.message,
    required this.onRefresh,
  });

  final bool notLinkedToLecturer;
  final String? message;
  final VoidCallback onRefresh;

  @override
  Widget build(BuildContext context) {
    if (notLinkedToLecturer) {
      return Card(
        color: Colors.amber.shade50,
        shape: RoundedRectangleBorder(
          borderRadius: BorderRadius.circular(14),
          side: BorderSide(color: Colors.amber.shade300),
        ),
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            children: [
              Icon(
                Icons.person_off_outlined,
                size: 48,
                color: Colors.amber.shade800,
              ),
              const SizedBox(height: 12),
              Text(
                'Tài khoản chưa liên kết giảng viên',
                style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.bold,
                  color: Colors.amber.shade900,
                ),
                textAlign: TextAlign.center,
              ),
              const SizedBox(height: 8),
              Text(
                message ??
                    'Tài khoản này chưa được liên kết với hồ sơ giảng viên để thực hiện đánh giá thi đua. Vui lòng liên hệ Phòng QLKH để được hỗ trợ.',
                textAlign: TextAlign.center,
                style: const TextStyle(color: Colors.black87, fontSize: 13),
              ),
            ],
          ),
        ),
      );
    }

    return Card(
      elevation: 0,
      color: Colors.grey.shade50,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(16),
        side: BorderSide(color: Colors.grey.shade300),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 20, vertical: 32),
        child: Column(
          children: [
            Container(
              padding: const EdgeInsets.all(16),
              decoration: BoxDecoration(
                color: const Color(0xFF177158).withOpacity(0.08),
                shape: BoxShape.circle,
              ),
              child: const Icon(
                Icons.assignment_outlined,
                size: 48,
                color: Color(0xFF177158),
              ),
            ),
            const SizedBox(height: 16),
            Text(
              'Chưa có đánh giá',
              style: Theme.of(context).textTheme.titleLarge?.copyWith(
                fontWeight: FontWeight.bold,
                color: const Color(0xFF173F32),
              ),
              textAlign: TextAlign.center,
            ),
            const SizedBox(height: 8),
            Text(
              message ??
                  'Năm học này chưa có kết quả đánh giá thi đua đã chốt.',
              textAlign: TextAlign.center,
              style: const TextStyle(fontSize: 14, color: Colors.black87),
            ),
            const SizedBox(height: 16),
            Container(
              padding: const EdgeInsets.all(12),
              decoration: BoxDecoration(
                color: Colors.white,
                borderRadius: BorderRadius.circular(10),
                border: Border.all(color: Colors.grey.shade200),
              ),
              child: const Row(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Icon(Icons.info_outline, size: 18, color: Color(0xFF177158)),
                  SizedBox(width: 10),
                  Expanded(
                    child: Text(
                      'Kết quả thi đua sẽ được hiển thị sau khi Phòng Quản lý khoa học tiến hành đánh giá và chốt kết quả xếp loại.',
                      style: TextStyle(
                        fontSize: 12,
                        color: Colors.black87,
                        height: 1.35,
                      ),
                    ),
                  ),
                ],
              ),
            ),
            const SizedBox(height: 20),
            OutlinedButton.icon(
              onPressed: onRefresh,
              icon: const Icon(Icons.refresh, size: 18),
              label: const Text('Thử lại'),
            ),
          ],
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// _EvaluationView – Chi tiết kết quả & trạng thái thi đua
// ---------------------------------------------------------------------------

class _EvaluationView extends StatelessWidget {
  const _EvaluationView({required this.data});
  final Map<String, dynamic> data;

  @override
  Widget build(BuildContext context) {
    final status = data['status'] as String?;
    final isFinalized = status == 'CHOT';
    final statusLabel = switch (status) {
      'CHOT' => 'Đã chốt',
      'NHAP' => 'Bản nháp',
      _ => 'Chưa xác định',
    };
    final classification = data['classification'] as String?;
    final classificationText =
        (classification != null && classification.isNotEmpty)
        ? classification
        : 'Chưa xếp loại';
    final classificationBasis = data['classificationBasis'] as String? ?? '';
    final lecturerName =
        data['lecturerNameSnapshot'] as String? ?? 'Giảng viên';
    final lecturerCode = data['lecturerCodeSnapshot'] as String? ?? '';
    final academicRank = data['academicRankSnapshot'] as String?;
    final degree = data['degreeSnapshot'] as String?;
    final academicYearCode = data['academicYearCode'] as String? ?? '';
    final attempt = data['attempt'] ?? 1;
    final evaluatedAtText = _formatDateTime(data['evaluatedAt'] as String?);

    final lecturerSub = [
      if (lecturerCode.isNotEmpty) lecturerCode,
      if (academicRank != null && academicRank.isNotEmpty) academicRank,
      if (degree != null && degree.isNotEmpty) degree,
    ].join(' · ');

    final details = data['details'] as List<dynamic>? ?? [];

    final productCount = details
        .map((raw) => (raw as Map)['productId'])
        .where((id) => id != null)
        .toSet()
        .length;

    return Column(
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        // ── Card tổng quan kết quả & trạng thái thi đua ──
        Card(
          color: const Color(0xFF173F32),
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(16),
          ),
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Expanded(
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Text(
                            lecturerName,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 18,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                          if (lecturerSub.isNotEmpty) ...[
                            const SizedBox(height: 4),
                            Text(
                              lecturerSub,
                              style: const TextStyle(
                                color: Color(0xFFD5E5DB),
                                fontSize: 13,
                              ),
                            ),
                          ],
                        ],
                      ),
                    ),
                    const SizedBox(width: 8),
                    // Badge trạng thái thi đua (Đã chốt / Bản nháp)
                    Container(
                      padding: const EdgeInsets.symmetric(
                        horizontal: 10,
                        vertical: 5,
                      ),
                      decoration: BoxDecoration(
                        color: isFinalized
                            ? const Color(0xFF2E7D32)
                            : Colors.amber.shade800,
                        borderRadius: BorderRadius.circular(20),
                      ),
                      child: Row(
                        mainAxisSize: MainAxisSize.min,
                        children: [
                          Icon(
                            isFinalized
                                ? Icons.check_circle_outline
                                : Icons.edit_note,
                            size: 14,
                            color: Colors.white,
                          ),
                          const SizedBox(width: 4),
                          Text(
                            statusLabel,
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 12,
                              fontWeight: FontWeight.bold,
                            ),
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
                const Divider(height: 24, color: Color(0xFF265646)),
                const Text(
                  'XẾP LOẠI THI ĐUA',
                  style: TextStyle(
                    color: Color(0xFFD5E5DB),
                    fontSize: 11,
                    fontWeight: FontWeight.w600,
                    letterSpacing: 0.5,
                  ),
                ),
                const SizedBox(height: 4),
                Text(
                  classificationText,
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.bold,
                  ),
                ),
                if (classificationBasis.isNotEmpty) ...[
                  const SizedBox(height: 10),
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.black.withOpacity(0.18),
                      borderRadius: BorderRadius.circular(8),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Icon(
                          Icons.info_outline,
                          size: 16,
                          color: Color(0xFFD5E5DB),
                        ),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            classificationBasis,
                            style: const TextStyle(
                              color: Color(0xFFE2EEE6),
                              fontSize: 13,
                              height: 1.35,
                            ),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
                const SizedBox(height: 12),
                Row(
                  children: [
                    const Icon(
                      Icons.history_outlined,
                      size: 14,
                      color: Color(0xFFD5E5DB),
                    ),
                    const SizedBox(width: 4),
                    Expanded(
                      child: Text(
                        'Lần đánh giá: $attempt · Năm học $academicYearCode',
                        style: const TextStyle(
                          color: Color(0xFFD5E5DB),
                          fontSize: 12,
                        ),
                        softWrap: true,
                      ),
                    ),
                    if (evaluatedAtText.isNotEmpty) ...[
                      const Spacer(),
                      Text(
                        evaluatedAtText,
                        style: const TextStyle(
                          color: Color(0xFFD5E5DB),
                          fontSize: 12,
                        ),
                      ),
                    ],
                  ],
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),

        // ── Thống kê tổng giờ & điểm quy đổi ──
        Row(
          children: [
            Expanded(
              child: _MetricCard(
                label: 'Giờ quy đổi',
                value: _formatNumber(data['totalHours']),
                unit: 'giờ',
                icon: Icons.access_time_filled,
                iconColor: const Color(0xFF177158),
              ),
            ),
            const SizedBox(width: 12),
            Expanded(
              child: _MetricCard(
                label: 'Quy đổi',
                value: _formatNumber(data['totalPoints']),
                unit: 'điểm',
                icon: Icons.military_tech_rounded,
                iconColor: Colors.amber.shade800,
              ),
            ),
          ],
        ),
        const SizedBox(height: 20),

        // ── Chi tiết sản phẩm quy đổi ──
        Row(
          mainAxisAlignment: MainAxisAlignment.spaceBetween,
          children: [
            Text(
              'Sản phẩm được quy đổi',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                fontWeight: FontWeight.bold,
                color: const Color(0xFF173F32),
              ),
            ),
            Container(
              padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
              decoration: BoxDecoration(
                color: const Color(0xFF177158).withOpacity(0.1),
                borderRadius: BorderRadius.circular(12),
              ),
              child: Text(
                '$productCount sản phẩm',
                style: const TextStyle(
                  fontSize: 12,
                  fontWeight: FontWeight.w600,
                  color: Color(0xFF177158),
                ),
              ),
            ),
          ],
        ),
        const SizedBox(height: 10),
        if (details.isEmpty)
          Card(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            child: const Padding(
              padding: EdgeInsets.all(24),
              child: Center(
                child: Text(
                  'Không có sản phẩm nào đủ điều kiện quy đổi trong năm học này.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Colors.grey),
                ),
              ),
            ),
          )
        else
          ...details.map((raw) {
            final x = raw as Map<String, dynamic>;
            final unitLabel = _formatUnit(x['unit'] as String?);
            return Card(
              elevation: 0.5,
              margin: const EdgeInsets.only(bottom: 10),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: Colors.grey.shade200),
              ),
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (x['productTypeName'] != null)
                          Container(
                            padding: const EdgeInsets.symmetric(
                              horizontal: 8,
                              vertical: 3,
                            ),
                            margin: const EdgeInsets.only(right: 8),
                            decoration: BoxDecoration(
                              color: const Color(0xFF177158).withOpacity(0.08),
                              borderRadius: BorderRadius.circular(6),
                            ),
                            child: Text(
                              '${x['productTypeName']}',
                              style: const TextStyle(
                                fontSize: 11,
                                color: Color(0xFF177158),
                                fontWeight: FontWeight.w600,
                              ),
                            ),
                          ),
                        const Spacer(),
                        Text(
                          '+${_formatNumber(x['convertedValue'])} $unitLabel',
                          style: const TextStyle(
                            fontWeight: FontWeight.bold,
                            fontSize: 15,
                            color: Color(0xFF2E7D32),
                          ),
                        ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Text(
                      x['productTitle'] ?? '',
                      style: const TextStyle(
                        fontWeight: FontWeight.w600,
                        fontSize: 14,
                        color: Color(0xFF173F32),
                      ),
                    ),
                    const SizedBox(height: 8),
                    Container(
                      padding: const EdgeInsets.all(10),
                      decoration: BoxDecoration(
                        color: Colors.grey.shade50,
                        borderRadius: BorderRadius.circular(8),
                      ),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          Row(
                            children: [
                              Expanded(
                                child: Text(
                                  'Quy định: ${x['ruleCode']} (v${x['ruleVersion']})',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: Colors.black87,
                                  ),
                                ),
                              ),
                              Text(
                                'Gốc: ${_formatNumber(x['baseValue'])} $unitLabel',
                                style: const TextStyle(
                                  fontSize: 12,
                                  color: Colors.black54,
                                ),
                              ),
                            ],
                          ),
                          const SizedBox(height: 4),
                          Row(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Expanded(
                                child: Text(
                                  'Căn cứ: ${x['calculationBasis'] ?? ''}',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    color: Colors.black54,
                                  ),
                                ),
                              ),
                              if (x['authorCoefficient'] != null) ...[
                                const SizedBox(width: 8),
                                Text(
                                  'Hệ số: ${_formatNumber(x['authorCoefficient'])}',
                                  style: const TextStyle(
                                    fontSize: 12,
                                    fontWeight: FontWeight.w500,
                                  ),
                                ),
                              ],
                            ],
                          ),
                        ],
                      ),
                    ),
                  ],
                ),
              ),
            );
          }),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// _MetricCard & Helpers
// ---------------------------------------------------------------------------

class _MetricCard extends StatelessWidget {
  const _MetricCard({
    required this.label,
    required this.value,
    required this.unit,
    required this.icon,
    required this.iconColor,
  });

  final String label;
  final String value;
  final String unit;
  final IconData icon;
  final Color iconColor;

  @override
  Widget build(BuildContext context) {
    return Card(
      elevation: 0.5,
      shape: RoundedRectangleBorder(
        borderRadius: BorderRadius.circular(14),
        side: BorderSide(color: Colors.grey.shade200),
      ),
      child: Padding(
        padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                Icon(icon, size: 18, color: iconColor),
                const SizedBox(width: 6),
                Expanded(
                  child: Text(
                    label,
                    style: const TextStyle(
                      fontSize: 13,
                      color: Colors.black54,
                      fontWeight: FontWeight.w500,
                    ),
                    maxLines: 2,
                    softWrap: true,
                    overflow: TextOverflow.visible,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 8),
            Row(
              crossAxisAlignment: CrossAxisAlignment.baseline,
              textBaseline: TextBaseline.alphabetic,
              children: [
                Text(
                  value,
                  style: Theme.of(context).textTheme.headlineSmall?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFF173F32),
                  ),
                ),
                const SizedBox(width: 4),
                Text(
                  unit,
                  style: const TextStyle(
                    fontSize: 13,
                    color: Colors.black54,
                    fontWeight: FontWeight.w500,
                  ),
                ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}

String _formatNumber(dynamic val) {
  if (val == null) return '0';
  final num? n = val is num ? val : num.tryParse(val.toString());
  if (n == null) return val.toString();
  if (n == n.truncateToDouble()) {
    return n.toInt().toString();
  }
  var s = n.toStringAsFixed(4);
  while (s.contains('.') && (s.endsWith('0') || s.endsWith('.'))) {
    s = s.substring(0, s.length - 1);
  }
  return s;
}

String _formatUnit(String? unit) {
  if (unit == 'GIO') return 'giờ';
  if (unit == 'DIEM') return 'điểm';
  return unit?.toLowerCase() ?? '';
}

String _formatDateTime(String? iso) {
  if (iso == null || iso.isEmpty) return '';
  try {
    final dt = DateTime.parse(iso).toLocal();
    final d = dt.day.toString().padLeft(2, '0');
    final m = dt.month.toString().padLeft(2, '0');
    final y = dt.year;
    final h = dt.hour.toString().padLeft(2, '0');
    final min = dt.minute.toString().padLeft(2, '0');
    return '$d/$m/$y $h:$min';
  } catch (_) {
    return iso;
  }
}
