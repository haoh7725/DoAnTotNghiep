import 'package:flutter/material.dart';
import 'api_client.dart';
import 'product_detail_screen.dart' show SectionCard, DetailRow, MessageWidget;

// ---------------------------------------------------------------------------
// Gender mapping
// ---------------------------------------------------------------------------
String _genderLabel(String? g) {
  switch (g) {
    case 'NAM':
      return 'Nam';
    case 'NU':
      return 'Nữ';
    case 'KHAC':
      return 'Khác';
    default:
      return g ?? '';
  }
}

String _formatDate(String? iso) {
  if (iso == null || iso.isEmpty) return '';
  try {
    final dt = DateTime.parse(iso);
    return '${dt.day.toString().padLeft(2, '0')}/${dt.month.toString().padLeft(2, '0')}/${dt.year}';
  } catch (_) {
    return iso;
  }
}

// ---------------------------------------------------------------------------
// ProfileScreen
// ---------------------------------------------------------------------------

class ProfileScreen extends StatefulWidget {
  const ProfileScreen({
    super.key,
    required this.client,
    required this.onLogout,
  });
  final ApiClient client;
  final VoidCallback onLogout;

  @override
  State<ProfileScreen> createState() => _ProfileScreenState();
}

class _ProfileScreenState extends State<ProfileScreen> {
  Map<String, dynamic>? _me;
  Map<String, dynamic>? _lecturer;
  Map<String, dynamic>? _scientificProfile;
  bool _loading = true;
  String? _error;
  bool _noLecturer = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
      _noLecturer = false;
      _me = null;
      _lecturer = null;
      _scientificProfile = null;
    });
    try {
      final me = await widget.client.getMe();
      _me = Map<String, dynamic>.from(me as Map);

      Map<String, dynamic>? lecturer;
      try {
        final lec = await widget.client.getLecturerMe();
        lecturer = Map<String, dynamic>.from(lec as Map);
      } on ApiException catch (e) {
        if (e.status == 404) {
          _noLecturer = true;
        } else {
          rethrow;
        }
      }

      if (lecturer != null) {
        _lecturer = lecturer;
        final lecId = lecturer['id'] as int?;
        if (lecId != null) {
          try {
            final sp = await widget.client.getScientificProfile(lecId);
            _scientificProfile = sp != null
                ? Map<String, dynamic>.from(sp as Map)
                : null;
          } on ApiException catch (e) {
            if (e.status == 404) {
              _scientificProfile = null;
            } else {
              rethrow;
            }
          }
        }
      }

      if (mounted) setState(() {});
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: const Text('Hồ sơ của tôi'),
        backgroundColor: const Color(0xFF173F32),
        foregroundColor: Colors.white,
        actions: [
          IconButton(
            onPressed: widget.onLogout,
            icon: const Icon(Icons.logout),
            tooltip: 'Đăng xuất',
          ),
        ],
      ),
      body: RefreshIndicator(onRefresh: _load, child: _buildBody()),
    );
  }

  Widget _buildBody() {
    if (_loading) {
      return const Center(child: CircularProgressIndicator());
    }
    if (_error != null) {
      return MessageWidget(
        icon: Icons.error_outline,
        title: 'Không tải được hồ sơ',
        text: _error!,
        action: _load,
      );
    }

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // ── Header Card ────────────────────────────────────────────────────
        _buildHeaderCard(),
        const SizedBox(height: 12),

        // ── No lecturer linked ─────────────────────────────────────────────
        if (_noLecturer)
          Card(
            shape: RoundedRectangleBorder(
              borderRadius: BorderRadius.circular(12),
            ),
            color: Colors.amber.shade50,
            child: const Padding(
              padding: EdgeInsets.all(16),
              child: Row(
                children: [
                  Icon(Icons.info_outline, color: Colors.orange),
                  SizedBox(width: 12),
                  Expanded(
                    child: Text(
                      'Tài khoản này chưa được liên kết với hồ sơ giảng viên. '
                      'Vui lòng liên hệ Phòng QLKH để cập nhật.',
                    ),
                  ),
                ],
              ),
            ),
          ),

        // ── Lecturer details ──
        if (_lecturer != null) ...[
          const SizedBox(height: 4),
          SectionCard(
            title: 'Thông tin cá nhân',
            children: [
              if ([
                _lecturer!['birthDate'],
                _lecturer!['gender'],
                _lecturer!['email'],
                _lecturer!['phone'],
                _lecturer!['academicRank'],
                _lecturer!['degree'],
                _lecturer!['position'],
              ].every(
                (value) => value == null || value.toString().trim().isEmpty,
              ))
                const Padding(
                  padding: EdgeInsets.only(bottom: 12),
                  child: Text(
                    'Chưa cập nhật thông tin cá nhân.',
                    style: TextStyle(
                      color: Colors.grey,
                      fontSize: 13,
                      fontStyle: FontStyle.italic,
                    ),
                  ),
                ),
              DetailRow(
                'Ngày sinh',
                _formatDate(_lecturer!['birthDate'] as String?),
              ),
              DetailRow(
                'Giới tính',
                _genderLabel(_lecturer!['gender'] as String?),
              ),
              DetailRow('Email', _lecturer!['email'] as String?),
              DetailRow('Điện thoại', _lecturer!['phone'] as String?),
              DetailRow('Học hàm', _lecturer!['academicRank'] as String?),
              DetailRow('Học vị', _lecturer!['degree'] as String?),
              DetailRow('Chức vụ', _lecturer!['position'] as String?),
            ],
          ),
        ],

        // ── Scientific profile ──
        if (_scientificProfile != null) ...[
          const SizedBox(height: 12),
          _buildScientificProfile(),
        ],

        const SizedBox(height: 24),
      ],
    );
  }

  Widget _buildHeaderCard() {
    final fullName = _lecturer != null
        ? (_lecturer!['fullName'] as String? ?? '')
        : (_me?['fullName'] as String? ?? _me?['username'] as String? ?? '');
    final code = _lecturer != null ? (_lecturer!['code'] as String?) : null;
    final dept = _lecturer != null
        ? (_lecturer!['departmentName'] as String?)
        : null;
    final faculty = _lecturer != null
        ? (_lecturer!['facultyName'] as String?)
        : null;

    return Card(
      color: const Color(0xFF173F32),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.all(20),
        child: Row(
          children: [
            CircleAvatar(
              radius: 32,
              backgroundColor: const Color(0xFF177158),
              child: Text(
                fullName.isNotEmpty ? fullName[0].toUpperCase() : '?',
                style: const TextStyle(
                  color: Colors.white,
                  fontSize: 26,
                  fontWeight: FontWeight.bold,
                ),
              ),
            ),
            const SizedBox(width: 16),
            Expanded(
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    fullName,
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 18,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  if (code != null) ...[
                    const SizedBox(height: 4),
                    Text(
                      code,
                      style: const TextStyle(
                        color: Color(0xFFD5E5DB),
                        fontSize: 13,
                      ),
                    ),
                  ],
                  if (dept != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      dept,
                      style: const TextStyle(
                        color: Color(0xFFD5E5DB),
                        fontSize: 13,
                      ),
                    ),
                  ],
                  if (faculty != null) ...[
                    const SizedBox(height: 2),
                    Text(
                      faculty,
                      style: const TextStyle(
                        color: Color(0xFFD5E5DB),
                        fontSize: 12,
                      ),
                    ),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }

  Widget _buildScientificProfile() {
    final profile = _scientificProfile!;
    final rawDetails = profile['details'];
    final details = rawDetails is Map
        ? Map<String, dynamic>.from(rawDetails)
        : <String, dynamic>{};
    final updatedAt = profile['updatedAt']?.toString();

    return Column(
      children: [
        Card(
          color: const Color(0xFFEAF5F0),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                const Icon(Icons.science_outlined, color: Color(0xFF177158)),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'Lý lịch khoa học đầy đủ',
                        style: TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                      const SizedBox(height: 4),
                      const Text(
                        'Theo Phụ lục V Thông tư 08/2011/TT-BGDĐT. '
                        'Chạm từng mục để xem chi tiết.',
                      ),
                      if (updatedAt != null && updatedAt.isNotEmpty) ...[
                        const SizedBox(height: 6),
                        Text(
                          'Cập nhật: ${_formatDateTime(updatedAt)}',
                          style: const TextStyle(
                            fontSize: 12,
                            color: Colors.black54,
                          ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 8),
        _ProfileExpansion(
          title: 'I. Lý lịch sơ lược',
          icon: Icons.badge_outlined,
          initiallyExpanded: true,
          children: [
            _ProfileRows(
              rows: [
                MapEntry('Nơi sinh', _text(details['birthPlace'])),
                MapEntry('Quê quán', _text(details['hometown'])),
                MapEntry('Dân tộc', _text(details['ethnicity'])),
                MapEntry(
                  'Năm nhận học vị cao nhất',
                  _text(details['highestDegreeYear']),
                ),
                MapEntry(
                  'Nước nhận học vị',
                  _text(details['highestDegreeCountry']),
                ),
                MapEntry(
                  'Năm bổ nhiệm chức danh khoa học',
                  _text(details['academicRankYear']),
                ),
                MapEntry('Đơn vị công tác', _text(details['workUnit'])),
                MapEntry('Địa chỉ liên lạc', _text(details['contactAddress'])),
                MapEntry('Điện thoại cơ quan', _text(details['officePhone'])),
                MapEntry('Điện thoại nhà riêng', _text(details['homePhone'])),
                MapEntry('Fax', _text(details['fax'])),
                MapEntry('Số CMND / CCCD', _text(details['identityNumber'])),
                MapEntry(
                  'Ngày cấp',
                  _formatDate(_text(details['identityIssuedDate'])),
                ),
                MapEntry(
                  'Nơi cấp',
                  _text(details['identityIssuedPlace']),
                ),
              ],
            ),
          ],
        ),
        _ListProfileExpansion(
          title: 'II. Quá trình đào tạo',
          icon: Icons.school_outlined,
          items: _items(details, 'education'),
          rowBuilder: (item) => [
            MapEntry('Bậc đào tạo', _educationLevel(item['level'])),
            MapEntry('Hệ đào tạo', _text(item['trainingMode'])),
            MapEntry('Nơi đào tạo', _text(item['institution'])),
            MapEntry('Ngành / chuyên ngành', _text(item['specialization'])),
            MapEntry('Nước đào tạo', _text(item['country'])),
            MapEntry('Năm tốt nghiệp', _text(item['graduationYear'])),
            MapEntry('Tên luận án', _text(item['thesisTitle'])),
          ],
        ),
        _ListProfileExpansion(
          title: 'Ngoại ngữ',
          icon: Icons.language_outlined,
          items: _items(details, 'languages'),
          rowBuilder: (item) => [
            MapEntry('Ngoại ngữ', _text(item['name'])),
            MapEntry('Mức độ sử dụng', _text(item['proficiency'])),
          ],
        ),
        _ListProfileExpansion(
          title: 'III. Quá trình công tác chuyên môn',
          icon: Icons.work_outline,
          items: _items(details, 'employment'),
          rowBuilder: (item) => [
            MapEntry('Từ ngày', _formatDate(_text(item['fromDate']))),
            MapEntry('Đến ngày', _formatDate(_text(item['toDate']))),
            MapEntry('Nơi công tác', _text(item['organization'])),
            MapEntry('Công việc đảm nhiệm', _text(item['position'])),
          ],
        ),
        _ListProfileExpansion(
          title: 'IV.1. Đề tài nghiên cứu đã và đang tham gia',
          icon: Icons.assignment_outlined,
          items: _items(details, 'researchProjects'),
          rowBuilder: (item) => [
            MapEntry('Tên đề tài', _text(item['title'])),
            MapEntry('Năm bắt đầu', _text(item['startYear'])),
            MapEntry('Năm hoàn thành / dự kiến', _text(item['endYear'])),
            MapEntry('Cấp đề tài', _text(item['level'])),
            MapEntry('Trách nhiệm tham gia', _text(item['responsibility'])),
          ],
        ),
        _ListProfileExpansion(
          title: 'IV.2. Các công trình khoa học đã công bố',
          icon: Icons.menu_book_outlined,
          items: _items(details, 'publications'),
          rowBuilder: (item) => [
            MapEntry('Tên công trình', _text(item['title'])),
            MapEntry('Năm công bố', _text(item['year'])),
            MapEntry('Tạp chí / nơi công bố', _text(item['journal'])),
          ],
        ),
        _ProfileExpansion(
          title: 'Thông tin chuyên môn và hoạt động khoa học',
          icon: Icons.biotech_outlined,
          children: [
            _ScientificField(
              label: 'Chuyên môn',
              text: _text(profile['expertise']),
            ),
            _ScientificField(
              label: 'Lĩnh vực nghiên cứu',
              text: _text(profile['researchFields']),
            ),
            _ScientificField(
              label: 'Hướng nghiên cứu',
              text: _text(profile['researchDirections']),
            ),
            _ScientificField(
              label: 'Tóm tắt hoạt động khoa học',
              text: _text(profile['activitySummary']),
            ),
          ],
        ),
        _ProfileExpansion(
          title: 'Thông tin kê khai',
          icon: Icons.edit_note_outlined,
          children: [
            _ProfileRows(
              rows: [
                MapEntry(
                  'Địa điểm kê khai',
                  _text(details['declarationPlace']),
                ),
                MapEntry(
                  'Ngày kê khai',
                  _formatDate(_text(details['declarationDate'])),
                ),
              ],
            ),
          ],
        ),
      ],
    );
  }

  static String? _text(dynamic value) {
    final text = value?.toString().trim();
    return text == null || text.isEmpty ? null : text;
  }

  static String _formatDateTime(String value) {
    try {
      final date = DateTime.parse(value).toLocal();
      return '${date.hour.toString().padLeft(2, '0')}:'
          '${date.minute.toString().padLeft(2, '0')} '
          '${date.day.toString().padLeft(2, '0')}/'
          '${date.month.toString().padLeft(2, '0')}/${date.year}';
    } catch (_) {
      return value;
    }
  }

  static List<Map<String, dynamic>> _items(
    Map<String, dynamic> details,
    String key,
  ) {
    final value = details[key];
    if (value is! List) return [];
    return value
        .whereType<Map>()
        .map((item) => Map<String, dynamic>.from(item))
        .toList();
  }

  static String? _educationLevel(dynamic value) => switch (value?.toString()) {
    'DAI_HOC' => 'Đại học',
    'DAI_HOC_2' => 'Bằng đại học 2',
    'THAC_SI' => 'Thạc sĩ',
    'TIEN_SI' => 'Tiến sĩ',
    final String value when value.isNotEmpty => value,
    _ => null,
  };
}

class _ScientificField extends StatelessWidget {
  const _ScientificField({required this.label, this.text});
  final String label;
  final String? text;

  @override
  Widget build(BuildContext context) {
    if (text == null || text!.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 14),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Text(
            label,
            style: const TextStyle(
              fontSize: 12,
              color: Colors.grey,
              fontWeight: FontWeight.w600,
            ),
          ),
          const SizedBox(height: 4),
          Text(text!, style: const TextStyle(fontSize: 13)),
          const Divider(height: 20),
        ],
      ),
    );
  }
}

class _ProfileExpansion extends StatelessWidget {
  const _ProfileExpansion({
    required this.title,
    required this.icon,
    required this.children,
    this.initiallyExpanded = false,
  });

  final String title;
  final IconData icon;
  final List<Widget> children;
  final bool initiallyExpanded;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 8),
      clipBehavior: Clip.antiAlias,
      child: ExpansionTile(
        initiallyExpanded: initiallyExpanded,
        leading: Icon(icon, color: const Color(0xFF177158)),
        title: Text(title, style: const TextStyle(fontWeight: FontWeight.bold)),
        childrenPadding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
        children: children,
      ),
    );
  }
}

class _ProfileRows extends StatelessWidget {
  const _ProfileRows({required this.rows});

  final List<MapEntry<String, String?>> rows;

  @override
  Widget build(BuildContext context) {
    final visible = rows.where((row) => row.value?.isNotEmpty == true).toList();
    if (visible.isEmpty) {
      return const Padding(
        padding: EdgeInsets.symmetric(vertical: 12),
        child: Align(
          alignment: Alignment.centerLeft,
          child: Text(
            'Chưa kê khai.',
            style: TextStyle(color: Colors.grey, fontStyle: FontStyle.italic),
          ),
        ),
      );
    }
    return Column(
      children: visible
          .map((row) => DetailRow(row.key, row.value))
          .toList(),
    );
  }
}

class _ListProfileExpansion extends StatelessWidget {
  const _ListProfileExpansion({
    required this.title,
    required this.icon,
    required this.items,
    required this.rowBuilder,
  });

  final String title;
  final IconData icon;
  final List<Map<String, dynamic>> items;
  final List<MapEntry<String, String?>> Function(Map<String, dynamic>)
  rowBuilder;

  @override
  Widget build(BuildContext context) {
    return _ProfileExpansion(
      title: '$title (${items.length})',
      icon: icon,
      children: items.isEmpty
          ? const [_ProfileRows(rows: [])]
          : [
              for (var index = 0; index < items.length; index++) ...[
                Align(
                  alignment: Alignment.centerLeft,
                  child: Padding(
                    padding: const EdgeInsets.only(top: 10, bottom: 6),
                    child: Text(
                      'Mục ${index + 1}',
                      style: const TextStyle(
                        fontWeight: FontWeight.bold,
                        color: Color(0xFF177158),
                      ),
                    ),
                  ),
                ),
                _ProfileRows(rows: rowBuilder(items[index])),
                if (index < items.length - 1) const Divider(height: 20),
              ],
            ],
    );
  }
}
