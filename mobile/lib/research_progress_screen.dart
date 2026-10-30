import 'package:flutter/material.dart';
import 'api_client.dart';

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

      final yearId = years.isEmpty ? null : (years.first['id'] as num).toInt();

      setState(() {
        _years = years;
        _yearId = yearId;
        _progressFuture = yearId == null
            ? null
            : widget.client.getMyResearchProgress(yearId);
        _targetFuture = yearId == null
            ? null
            : widget.client.getMyProductTargetProgress(yearId);

        _departmentFuture = yearId == null
            ? null
            : widget.client.getMyDepartmentTargetProgress(yearId);
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

  void _refreshProgress() {
    final yearId = _yearId;
    if (yearId == null) return;

    setState(() {
      _progressFuture = widget.client.getMyResearchProgress(yearId);
      _targetFuture = widget.client.getMyProductTargetProgress(yearId);
      _departmentFuture = widget.client.getMyDepartmentTargetProgress(yearId);
    });
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
                  Text('Chỉ tiêu bài báo: ${_number(allocated)}')
                else
                  const Text(
                    'Bộ môn chưa được phân bổ chỉ tiêu '
                    'trong năm học này.',
                  ),
                Text('Bài báo đã duyệt: ${_number(approved)}'),
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
                    'Tỷ lệ = bài báo đã duyệt / '
                    'chỉ tiêu bài báo được phân bổ × 100%.',
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
                  'Mỗi bài báo đã duyệt hoàn tất được tính một lần, '
                  'theo bộ môn hiện tại của người đứng tên gửi '
                  'và năm học của kế hoạch.',
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
          : ListView(
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
                      _progressFuture = widget.client.getMyResearchProgress(id);
                      _targetFuture = widget.client.getMyProductTargetProgress(
                        id,
                      );
                      _departmentFuture = widget.client
                          .getMyDepartmentTargetProgress(id);
                    });
                  },
                ),
                const SizedBox(height: 16),
                _buildTargetProgress(),
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
                        data['hasFinalizedEvaluation'] == true;
                    final requiredHours = (data['requiredHours'] as num?)
                        ?.toDouble();
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
                            const Text(
                              'Mức hoàn thành định mức giờ NCKH '
                              'theo kết quả đã chốt',
                            ),
                            const SizedBox(height: 16),
                            Text(
                              'Định mức: '
                              '${_number(requiredHours)} giờ',
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
                                const Text(
                                  'Tỷ lệ = giờ đã ghi nhận / '
                                  'định mức giờ × 100%.',
                                ),
                              ] else
                                Text(
                                  requiredHours == null
                                      ? 'Chưa được giao định mức giờ.'
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
    );
  }
}
