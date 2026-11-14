import 'package:flutter/material.dart';
import 'api_client.dart';
import 'product_detail_screen.dart';

class ResearchProgressScreen extends StatefulWidget {
  const ResearchProgressScreen({super.key, required this.client});

  final ApiClient client;

  @override
  State<ResearchProgressScreen> createState() => _ResearchProgressScreenState();
}

class _ResearchProgressScreenState extends State<ResearchProgressScreen> {
  List<dynamic> _years = [];
  int? _yearId;
  bool _loadingYears = true;
  String? _yearsError;
  Future<dynamic>? _progressFuture;
  Future<dynamic>? _targetFuture;
  Future<dynamic>? _departmentFuture;
  Future<dynamic>? _assignmentsFuture;
  Future<List<dynamic>>? _comparisonFuture;

  @override
  void initState() {
    super.initState();
    _loadYears();
  }

  Future<void> _loadYears() async {
    setState(() {
      _loadingYears = true;
      _yearsError = null;
    });

    try {
      final lookups = await widget.client.getLookups();
      final years = lookups['academicYears'] as List<dynamic>? ?? [];

      if (!mounted) return;

      years.sort(
        (a, b) =>
            b['startDate'].toString().compareTo(a['startDate'].toString()),
      );
      final today = DateTime.now().toIso8601String().substring(0, 10);
      final currentYears = years.where(
        (y) =>
            y['startDate'].toString().compareTo(today) <= 0 &&
            y['endDate'].toString().compareTo(today) >= 0,
      );
      final yearId = years.isEmpty
          ? null
          : ((currentYears.isEmpty ? years.first : currentYears.first)['id']
                    as num)
                .toInt();

      setState(() {
        _years = years;
        _yearId = yearId;
        if (yearId != null) _setProgressFutures(yearId);
      });
    } catch (e) {
      if (mounted) {
        setState(() => _yearsError = e.toString());
      }
    } finally {
      if (mounted) {
        setState(() => _loadingYears = false);
      }
    }
  }

  Future<void> _refreshProgress() async {
    final yearId = _yearId;
    if (yearId == null) return;

    late List<Future<dynamic>> requests;
    setState(() {
      requests = _setProgressFutures(yearId);
    });
    try {
      await Future.wait(requests);
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Không làm mới được tiến độ: $e')),
        );
      }
    }
  }

  List<Future<dynamic>> _setProgressFutures(int yearId) {
    final progress = widget.client.getMyResearchProgress(yearId);
    final target = widget.client.getMyProductTargetProgress(yearId);
    final department = widget.client.getMyDepartmentTargetProgress(yearId);
    final assignments = widget.client.getMyAssignedTargets(yearId);
    _assignmentsFuture = assignments;
    _progressFuture = progress;
    _targetFuture = target;
    _departmentFuture = department;
    _comparisonFuture = Future.wait([target, department]);
    return [progress, target, department, assignments];
  }

  Widget _buildComparisonChart() {
    return FutureBuilder<List<dynamic>>(
      future: _comparisonFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Card(
            child: Padding(
              padding: EdgeInsets.all(24),
              child: Center(child: CircularProgressIndicator()),
            ),
          );
        }
        if (snapshot.hasError || snapshot.data == null) {
          return const SizedBox.shrink();
        }

        final personal = Map<String, dynamic>.from(snapshot.data![0] as Map);
        final department = Map<String, dynamic>.from(snapshot.data![1] as Map);
        final personalPercent = (personal['completionPercent'] as num?)
            ?.toDouble();
        final departmentPercent = (department['completionPercent'] as num?)
            ?.toDouble();

        return Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Biểu đồ tổng hợp tiến độ',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 4),
                const Text(
                  'So sánh tỷ lệ hoàn thành kế hoạch cá nhân và chỉ tiêu bộ môn.',
                ),
                const SizedBox(height: 20),
                SizedBox(
                  height: 210,
                  child: Row(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    mainAxisAlignment: MainAxisAlignment.spaceEvenly,
                    children: [
                      _ProgressBarColumn(
                        label: 'Cá nhân',
                        percent: personalPercent,
                        color: const Color(0xFF168C8C),
                      ),
                      _ProgressBarColumn(
                        label: 'Bộ môn',
                        percent: departmentPercent,
                        color: const Color(0xFF17324D),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Cột tối đa 100%; tỷ lệ lớn hơn 100% vẫn hiển thị số thực tế.',
                  style: TextStyle(fontSize: 12, color: Colors.black54),
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildDepartmentProgress() {
    return FutureBuilder<dynamic>(
      future: _departmentFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Padding(
            padding: EdgeInsets.all(20),
            child: Center(child: CircularProgressIndicator()),
          );
        }

        if (snapshot.hasError || snapshot.data is! Map) {
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Không tải được tiến độ bộ môn.',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  if (snapshot.hasError) Text(snapshot.error.toString()),
                  TextButton(
                    onPressed: _refreshProgress,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            ),
          );
        }

        final data = Map<String, dynamic>.from(snapshot.data as Map);
        final hasAllocation = data['hasAllocation'] == true;
        final allocated = (data['allocatedArticles'] as num?)?.toInt();
        final approved = (data['approvedArticles'] as num?)?.toInt();
        final percent = (data['completionPercent'] as num?)?.toDouble();

        return Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Tiến độ bộ môn',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                Text(data['departmentName']?.toString() ?? 'Chưa xác định'),
                const SizedBox(height: 12),
                if (hasAllocation)
                  Text('Chỉ tiêu sản phẩm NCKH: ${_number(allocated)}')
                else
                  const Text(
                    'Bộ môn chưa được phân bổ chỉ tiêu '
                    'trong năm học này.',
                  ),
                Text('Sản phẩm đã công nhận: ${_number(approved)}'),
                const SizedBox(height: 12),
                if (percent != null) ...[
                  Text(
                    'Hoàn thành: ${percent.toStringAsFixed(1)}%',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  const SizedBox(height: 8),
                  LinearProgressIndicator(
                    value: (percent / 100).clamp(0.0, 1.0).toDouble(),
                    minHeight: 12,
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Tỷ lệ = sản phẩm đã công nhận / '
                    'chỉ tiêu sản phẩm được phân bổ × 100%.',
                  ),
                ] else
                  Text(
                    !hasAllocation
                        ? 'Chưa xác định tỷ lệ hoàn thành.'
                        : allocated == 0
                        ? 'Chỉ tiêu bằng 0; không tính phần trăm.'
                        : 'Chưa xác định tỷ lệ hoàn thành.',
                  ),
                const SizedBox(height: 12),
                const Text(
                  'Tổng hợp cả 4 loại NCKH theo phân công chỉ tiêu. '
                  'Bài báo cần được công nhận và đã xuất bản; '
                  'các loại khác cần được công nhận. Mỗi sản phẩm tính một lần.',
                ),
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildTargetProgress() {
    return FutureBuilder<dynamic>(
      future: _targetFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Padding(
            padding: EdgeInsets.all(20),
            child: Center(child: CircularProgressIndicator()),
          );
        }

        if (snapshot.hasError || snapshot.data is! Map) {
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Không tải được tiến độ sản phẩm.',
                    style: TextStyle(fontWeight: FontWeight.bold),
                  ),
                  if (snapshot.hasError) Text(snapshot.error.toString()),
                  TextButton(
                    onPressed: _refreshProgress,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            ),
          );
        }

        final data = Map<String, dynamic>.from(snapshot.data as Map);
        final hasPlan = data['hasPlan'] == true;
        final registered = data['hasRegisteredPlan'] == true;
        final committed = (data['committedProducts'] as num?)?.toInt();
        final approved = (data['approvedProducts'] as num?)?.toInt();
        final percent = (data['completionPercent'] as num?)?.toDouble();

        return Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Tiến độ sản phẩm theo kế hoạch',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 12),
                if (!hasPlan)
                  const Text('Chưa có kế hoạch nghiên cứu trong năm học này.')
                else ...[
                  Text(
                    'Trạng thái kế hoạch: '
                    '${registered
                        ? 'Đã đăng ký'
                        : data['planStatus'] == 'NHAP'
                        ? 'Bản nháp'
                        : data['planStatus'] ?? 'Chưa xác định'}',
                  ),
                  const SizedBox(height: 8),
                  Text('Số sản phẩm cam kết: ${_number(committed)}'),
                  Text('Số sản phẩm đã duyệt: ${_number(approved)}'),
                  const SizedBox(height: 12),
                  if (!registered)
                    const Text(
                      'Kế hoạch chưa đăng ký; '
                      'chưa xác định tỷ lệ hoàn thành.',
                    )
                  else if (committed == null || committed <= 0)
                    const Text(
                      'Chưa có số sản phẩm cam kết hợp lệ '
                      'để tính tỷ lệ hoàn thành.',
                    )
                  else if (percent != null) ...[
                    Text(
                      'Hoàn thành: ${percent.toStringAsFixed(1)}%',
                      style: const TextStyle(fontWeight: FontWeight.bold),
                    ),
                    const SizedBox(height: 8),
                    LinearProgressIndicator(
                      value: (percent / 100).clamp(0.0, 1.0).toDouble(),
                      minHeight: 12,
                    ),
                    const SizedBox(height: 8),
                    const Text(
                      'Tỷ lệ = sản phẩm đã duyệt / '
                      'số sản phẩm cam kết × 100%.',
                    ),
                  ] else
                    const Text('Chưa xác định tỷ lệ hoàn thành.'),
                  const SizedBox(height: 12),
                  const Text(
                    'Chỉ tính sản phẩm bạn đứng tên gửi, '
                    'gắn với kế hoạch này và đã được duyệt hoàn tất.',
                  ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }

  Widget _buildAssignedTargets() {
    return FutureBuilder<dynamic>(
      future: _assignmentsFuture,
      builder: (context, snapshot) {
        if (snapshot.connectionState != ConnectionState.done) {
          return const Card(
            child: Padding(
              padding: EdgeInsets.all(20),
              child: Center(child: CircularProgressIndicator()),
            ),
          );
        }
        if (snapshot.hasError || snapshot.data is! List) {
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(20),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text('Không tải được chỉ tiêu cá nhân.'),
                  TextButton(
                    onPressed: _refreshProgress,
                    child: const Text('Thử lại'),
                  ),
                ],
              ),
            ),
          );
        }
        final assignments = snapshot.data as List;
        return Card(
          child: Padding(
            padding: const EdgeInsets.all(20),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Text(
                  'Chỉ tiêu được giao cho tôi',
                  style: TextStyle(fontSize: 20, fontWeight: FontWeight.bold),
                ),
                const SizedBox(height: 8),
                const Text(
                  'Theo phân công của Trưởng bộ môn; theo dõi riêng với cam kết kế hoạch ở trên.',
                ),
                if (assignments.isEmpty)
                  const Padding(
                    padding: EdgeInsets.only(top: 12),
                    child: Text('Chưa được giao chỉ tiêu trong năm học này.'),
                  ),
                for (final assignment in assignments) ...[
                  const Divider(height: 28),
                  Text(
                    '${assignment['productTypeName']} · Chỉ tiêu #${assignment['targetId']}',
                    style: const TextStyle(fontWeight: FontWeight.bold),
                  ),
                  Text(
                    'Được giao: ${assignment['allocatedArticleCount']} sản phẩm',
                  ),
                  Text(
                    'Đã liên kết: ${assignment['registeredArticles']} · Đã hoàn thành: ${assignment['approvedArticles']}',
                  ),
                  if (assignment['deadline'] != null)
                    Text('Hạn hoàn thành: ${assignment['deadline']}'),
                  const SizedBox(height: 8),
                  Text(
                    'Hoàn thành: ${_number(assignment['completionPercent'])}%',
                  ),
                  LinearProgressIndicator(
                    value:
                        (((assignment['completionPercent'] as num?)
                                        ?.toDouble() ??
                                    0) /
                                100)
                            .clamp(0.0, 1.0)
                            .toDouble(),
                  ),
                  for (final product
                      in (assignment['linkedProducts'] as List? ?? []))
                    ListTile(
                      contentPadding: EdgeInsets.zero,
                      title: Text('${product['title']}'),
                      subtitle: Text(
                        productStatusLabel(product['status'] as String?),
                      ),
                      trailing: const Icon(Icons.chevron_right),
                      onTap: () async {
                        await Navigator.of(context).push(
                          MaterialPageRoute<void>(
                            builder: (_) => ProductDetailScreen(
                              client: widget.client,
                              productId: (product['id'] as num).toInt(),
                            ),
                          ),
                        );
                        if (mounted) await _refreshProgress();
                      },
                    ),
                ],
              ],
            ),
          ),
        );
      },
    );
  }

  String _number(dynamic value) {
    if (value == null) return 'Chưa có';
    final number = value as num;
    return number == number.roundToDouble()
        ? number.toInt().toString()
        : number.toStringAsFixed(2);
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Tiến độ NCKH'),
        actions: [
          IconButton(
            tooltip: 'Làm mới',
            onPressed: _loadingYears
                ? null
                : _yearsError != null || _years.isEmpty
                ? _loadYears
                : _refreshProgress,
            icon: const Icon(Icons.refresh),
          ),
        ],
      ),
      body: _loadingYears
          ? const Center(child: CircularProgressIndicator())
          : _yearsError != null
          ? Center(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Text('Không tải được năm học: $_yearsError'),
              ),
            )
          : _years.isEmpty
          ? const Center(child: Text('Chưa có năm học.'))
          : RefreshIndicator(
              onRefresh: _refreshProgress,
              child: ListView(
                physics: const AlwaysScrollableScrollPhysics(),
                padding: const EdgeInsets.all(16),
                children: [
                  DropdownButton<int>(
                    value: _yearId,
                    isExpanded: true,
                    items: _years.map((year) {
                      return DropdownMenuItem<int>(
                        value: (year['id'] as num).toInt(),
                        child: Text(year['code'].toString()),
                      );
                    }).toList(),
                    onChanged: (id) {
                      if (id == null || id == _yearId) return;

                      setState(() {
                        _yearId = id;
                        _setProgressFutures(id);
                      });
                    },
                  ),
                  const SizedBox(height: 16),
                  _buildComparisonChart(),
                  const SizedBox(height: 16),
                  _buildTargetProgress(),
                  const SizedBox(height: 16),
                  _buildAssignedTargets(),
                  const SizedBox(height: 16),
                  _buildDepartmentProgress(),
                  const SizedBox(height: 16),
                  FutureBuilder<dynamic>(
                    future: _progressFuture,
                    builder: (context, snapshot) {
                      if (snapshot.connectionState != ConnectionState.done) {
                        return const Center(child: CircularProgressIndicator());
                      }

                      if (snapshot.hasError) {
                        return Column(
                          children: [
                            Text(
                              'Không tải được tiến độ: '
                              '${snapshot.error}',
                            ),
                            TextButton(
                              onPressed: _refreshProgress,
                              child: const Text('Thử lại'),
                            ),
                          ],
                        );
                      }

                      final data = Map<String, dynamic>.from(
                        snapshot.data as Map,
                      );

                      final hasEvaluation =
                          data['hasFinalizedEvaluation'] == true ||
                          data['isLive'] == true;
                      final isPoints = data['evaluationUnit'] == 'DIEM';
                      final isLegacy = data['evaluationUnit'] == 'LEGACY';
                      final requiredValue =
                          (data[isPoints ? 'requiredPoints' : 'requiredHours']
                                  as num?)
                              ?.toDouble();
                      final unitLabel = isPoints ? 'điểm' : 'giờ';
                      final percent = (data['completionPercent'] as num?)
                          ?.toDouble();

                      return Card(
                        child: Padding(
                          padding: const EdgeInsets.all(20),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Text(
                                data['lecturerName'].toString(),
                                style: const TextStyle(
                                  fontSize: 20,
                                  fontWeight: FontWeight.bold,
                                ),
                              ),
                              const SizedBox(height: 8),
                              Text(
                                isLegacy
                                    ? 'Kết quả theo quy chế trước đây'
                                    : 'Tổng giờ NCKH tự cập nhật từ sản phẩm đã công nhận',
                              ),
                              const SizedBox(height: 16),
                              Text(
                                'Ngưỡng hoàn thành: '
                                '${_number(requiredValue)} $unitLabel',
                              ),
                              if (!hasEvaluation)
                                const Padding(
                                  padding: EdgeInsets.only(top: 12),
                                  child: Text(
                                    'Chưa có đánh giá đã chốt '
                                    'trong năm học này. '
                                    'Chưa xác định tỷ lệ hoàn thành.',
                                  ),
                                )
                              else ...[
                                Text(
                                  'Giờ đã ghi nhận: '
                                  '${_number(data['confirmedHours'])}',
                                ),
                                Text(
                                  'Điểm đã ghi nhận: '
                                  '${_number(data['confirmedPoints'])}',
                                ),
                                Text(
                                  'Sản phẩm được quy đổi: '
                                  '${_number(data['confirmedProductCount'])}',
                                ),
                                const SizedBox(height: 16),
                                if (percent != null) ...[
                                  Text(
                                    'Hoàn thành: '
                                    '${percent.toStringAsFixed(1)}%',
                                    style: const TextStyle(
                                      fontWeight: FontWeight.bold,
                                    ),
                                  ),
                                  const SizedBox(height: 8),
                                  LinearProgressIndicator(
                                    value: (percent / 100)
                                        .clamp(0.0, 1.0)
                                        .toDouble(),
                                    minHeight: 12,
                                  ),
                                  const SizedBox(height: 8),
                                  Text(
                                    'Tỷ lệ = $unitLabel đã ghi nhận / định mức $unitLabel × 100%.',
                                  ),
                                ] else
                                  Text(
                                    isLegacy
                                        ? 'Kết quả cũ được giữ nguyên; xem căn cứ xếp loại trong kết quả thi đua.'
                                        : requiredValue == null
                                        ? 'Chưa được giao định mức $unitLabel.'
                                        : 'Định mức bằng 0; '
                                              'không tính phần trăm.',
                                  ),
                              ],
                            ],
                          ),
                        ),
                      );
                    },
                  ),
                ],
              ),
            ),
    );
  }
}

class _ProgressBarColumn extends StatelessWidget {
  const _ProgressBarColumn({
    required this.label,
    required this.percent,
    required this.color,
  });

  final String label;
  final double? percent;
  final Color color;

  @override
  Widget build(BuildContext context) {
    final normalized = ((percent ?? 0) / 100).clamp(0.0, 1.0).toDouble();
    return SizedBox(
      width: 104,
      child: Column(
        mainAxisAlignment: MainAxisAlignment.end,
        children: [
          Text(
            percent == null ? 'Chưa có' : '${percent!.toStringAsFixed(1)}%',
            style: TextStyle(fontWeight: FontWeight.bold, color: color),
          ),
          const SizedBox(height: 8),
          Expanded(
            child: Container(
              width: 58,
              decoration: BoxDecoration(
                color: Colors.grey.shade200,
                borderRadius: BorderRadius.circular(8),
              ),
              alignment: Alignment.bottomCenter,
              child: FractionallySizedBox(
                heightFactor: normalized,
                widthFactor: 1,
                child: Container(
                  decoration: BoxDecoration(
                    color: color,
                    borderRadius: BorderRadius.circular(8),
                  ),
                ),
              ),
            ),
          ),
          const SizedBox(height: 8),
          Text(label, style: const TextStyle(fontWeight: FontWeight.w600)),
        ],
      ),
    );
  }
}
