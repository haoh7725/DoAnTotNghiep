import 'package:flutter/material.dart';
import 'api_client.dart';

class ResearchProgressScreen extends StatefulWidget {
  const ResearchProgressScreen({
    super.key,
    required this.client,
  });

  final ApiClient client;

  @override
  State<ResearchProgressScreen> createState() =>
      _ResearchProgressScreenState();
}

class _ResearchProgressScreenState
    extends State<ResearchProgressScreen> {
  List<dynamic> _years = [];
  int? _yearId;
  bool _loadingYears = true;
  String? _yearsError;
  Future<dynamic>? _progressFuture;

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

      final yearId = years.isEmpty
          ? null
          : (years.first['id'] as num).toInt();

      setState(() {
        _years = years;
        _yearId = yearId;
        _progressFuture = yearId == null
            ? null
            : widget.client.getMyResearchProgress(yearId);
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
      _progressFuture =
          widget.client.getMyResearchProgress(yearId);
    });
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
        title: const Text('Tiến độ cá nhân'),
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
                              _progressFuture = widget.client
                                  .getMyResearchProgress(id);
                            });
                          },
                        ),
                        const SizedBox(height: 16),
                        FutureBuilder<dynamic>(
                          future: _progressFuture,
                          builder: (context, snapshot) {
                            if (snapshot.connectionState !=
                                ConnectionState.done) {
                              return const Center(
                                child: CircularProgressIndicator(),
                              );
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
                            final requiredHours =
                                (data['requiredHours'] as num?)?.toDouble();
                            final percent =
                                (data['completionPercent'] as num?)
                                    ?.toDouble();

                            return Card(
                              child: Padding(
                                padding: const EdgeInsets.all(20),
                                child: Column(
                                  crossAxisAlignment:
                                      CrossAxisAlignment.start,
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