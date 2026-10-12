import 'package:flutter/material.dart';
import 'api_client.dart';
import 'product_detail_screen.dart' show SectionCard, DetailRow, MessageWidget;

// ---------------------------------------------------------------------------
// Gender mapping
// ---------------------------------------------------------------------------
String _genderLabel(String? g) {
  switch (g) {
    case 'NAM': return 'Nam';
    case 'NU': return 'Nữ';
    case 'KHAC': return 'Khác';
    default: return g ?? '';
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
          } catch (_) {
            // scientific profile may not exist — silently ignore
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
      body: RefreshIndicator(
        onRefresh: _load,
        child: _buildBody(),
      ),
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
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
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

        // ── Lecturer details ───────────────────────────────────────────────
        if (_lecturer != null) ...[
          const SizedBox(height: 4),
          SectionCard(
            title: 'Thông tin cá nhân',
            children: [
              DetailRow('Ngày sinh', _formatDate(_lecturer!['birthDate'] as String?)),
              DetailRow('Giới tính', _genderLabel(_lecturer!['gender'] as String?)),
              DetailRow('Email', _lecturer!['email'] as String?),
              DetailRow('Điện thoại', _lecturer!['phone'] as String?),
              DetailRow('Học hàm', _lecturer!['academicRank'] as String?),
              DetailRow('Học vị', _lecturer!['degree'] as String?),
              DetailRow('Chức vụ', _lecturer!['position'] as String?),
            ],
          ),
        ],

        // ── Scientific profile ─────────────────────────────────────────────
        if (_scientificProfile != null) ...[
          const SizedBox(height: 12),
          SectionCard(
            title: 'Lý lịch khoa học',
            children: [
              _ScientificField(
                label: 'Chuyên môn',
                text: _scientificProfile!['expertise'] as String?,
              ),
              _ScientificField(
                label: 'Lĩnh vực nghiên cứu',
                text: _scientificProfile!['researchFields'] as String?,
              ),
              _ScientificField(
                label: 'Hướng nghiên cứu',
                text: _scientificProfile!['researchDirections'] as String?,
              ),
              _ScientificField(
                label: 'Tóm tắt hoạt động',
                text: _scientificProfile!['activitySummary'] as String?,
              ),
            ],
          ),
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
    final dept = _lecturer != null ? (_lecturer!['departmentName'] as String?) : null;
    final faculty = _lecturer != null ? (_lecturer!['facultyName'] as String?) : null;

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
                    fontWeight: FontWeight.bold),
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
                        fontWeight: FontWeight.bold),
                  ),
                  if (code != null) ...[
                    const SizedBox(height: 4),
                    Text(code,
                        style: const TextStyle(
                            color: Color(0xFFD5E5DB), fontSize: 13)),
                  ],
                  if (dept != null) ...[
                    const SizedBox(height: 2),
                    Text(dept,
                        style: const TextStyle(
                            color: Color(0xFFD5E5DB), fontSize: 13)),
                  ],
                  if (faculty != null) ...[
                    const SizedBox(height: 2),
                    Text(faculty,
                        style: const TextStyle(
                            color: Color(0xFFD5E5DB), fontSize: 12)),
                  ],
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
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
          Text(label,
              style: const TextStyle(
                  fontSize: 12,
                  color: Colors.grey,
                  fontWeight: FontWeight.w600)),
          const SizedBox(height: 4),
          Text(text!, style: const TextStyle(fontSize: 13)),
          const Divider(height: 20),
        ],
      ),
    );
  }
}
