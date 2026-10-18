import 'package:flutter/material.dart';
import 'api_client.dart';

// ---------------------------------------------------------------------------
// Status helpers
// ---------------------------------------------------------------------------

String productStatusLabel(String? s) {
  switch (s) {
    case 'NHAP': return 'Nháp';
    case 'NOP': return 'Đã gửi';
    case 'TRUONG_BO_MON_DUYET': return 'TBM đã duyệt';
    case 'TRUONG_KHOA_DUYET': return 'Trưởng khoa đã duyệt';
    case 'PHONG_QLKH_DUYET': return 'Phòng QLKH đã duyệt';
    case 'DA_DUYET': return 'Đã duyệt';
    case 'TRA_LAI': return 'Yêu cầu bổ sung';
    default: return s ?? '';
  }
}

Color productStatusColor(String? s) {
  switch (s) {
    case 'DA_DUYET': return const Color(0xFF2E7D32);
    case 'TRA_LAI': return Colors.orange.shade800;
    case 'NOP':
    case 'TRUONG_BO_MON_DUYET':
    case 'TRUONG_KHOA_DUYET':
    case 'PHONG_QLKH_DUYET':
      return Colors.blue.shade700;
    case 'NHAP':
    default:
      return Colors.grey.shade700;
  }
}

String articleStatusLabel(String? s) {
  switch (s) {
    case 'DANG_VIET': return 'Đang viết';
    case 'DANG_PHAN_BIEN': return 'Đang phản biện';
    case 'DA_NHAN_DANG': return 'Đã nhận xét';
    case 'DA_XUAT_BAN': return 'Đã xuất bản';
    default: return s ?? '';
  }
}

Color articleStatusColor(String? s) {
  switch (s) {
    case 'DANG_VIET': return Colors.blueGrey;
    case 'DANG_PHAN_BIEN': return Colors.indigo;
    case 'DA_NHAN_DANG': return Colors.teal;
    case 'DA_XUAT_BAN': return const Color(0xFF2E7D32);
    default: return Colors.grey;
  }
}

// ---------------------------------------------------------------------------
// Shared small widgets
// ---------------------------------------------------------------------------

class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.label, required this.color});
  final String label;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 4),
      decoration: BoxDecoration(
        color: color.withOpacity(0.13),
        borderRadius: BorderRadius.circular(20),
        border: Border.all(color: color.withOpacity(0.5)),
      ),
      child: Text(
        label,
        style: TextStyle(color: color, fontSize: 12, fontWeight: FontWeight.w600),
      ),
    );
  }
}

class SectionCard extends StatelessWidget {
  const SectionCard({super.key, required this.title, required this.children});
  final String title;
  final List<Widget> children;

  @override
  Widget build(BuildContext context) {
    return Card(
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: Padding(
        padding: const EdgeInsets.fromLTRB(16, 14, 16, 8),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              title,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                    color: const Color(0xFF173F32),
                  ),
            ),
            const Divider(height: 16),
            ...children,
          ],
        ),
      ),
    );
  }
}

class DetailRow extends StatelessWidget {
  const DetailRow(this.label, this.value, {super.key});
  final String label;
  final String? value;

  @override
  Widget build(BuildContext context) {
    if (value == null || value!.isEmpty) return const SizedBox.shrink();
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          SizedBox(
            width: 140,
            child: Text(label, style: const TextStyle(color: Colors.grey, fontSize: 13)),
          ),
          Expanded(child: Text(value!, style: const TextStyle(fontSize: 13))),
        ],
      ),
    );
  }
}

class MessageWidget extends StatelessWidget {
  const MessageWidget({
    super.key,
    required this.icon,
    required this.title,
    required this.text,
    this.action,
  });
  final IconData icon;
  final String title;
  final String text;
  final Future<void> Function()? action;

  @override
  Widget build(BuildContext context) {
    return Center(
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 48, horizontal: 24),
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            Icon(icon, size: 54, color: Colors.grey),
            const SizedBox(height: 14),
            Text(title,
                style: Theme.of(context).textTheme.titleLarge,
                textAlign: TextAlign.center),
            const SizedBox(height: 8),
            Text(text, textAlign: TextAlign.center),
            if (action != null) ...[
              const SizedBox(height: 16),
              OutlinedButton(onPressed: action, child: const Text('Thử lại')),
            ],
          ],
        ),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// ProductDetailScreen
// ---------------------------------------------------------------------------

class ProductDetailScreen extends StatefulWidget {
  const ProductDetailScreen({
    super.key,
    required this.client,
    required this.productId,
  });
  final ApiClient client;
  final int productId;

  @override
  State<ProductDetailScreen> createState() => _ProductDetailScreenState();
}

class _ProductDetailScreenState extends State<ProductDetailScreen> {
  Map<String, dynamic>? _product;
  List<dynamic>? _history;
  List<dynamic>? _snapshots;
  int? _currentLecturerId;
  List<String> _currentUserRoles = [];
  bool _loading = true;
  String? _error;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final results = await Future.wait([
        widget.client.getProduct(widget.productId),
        widget.client.getReviewHistory(widget.productId),
        widget.client.getProductSnapshots(widget.productId).catchError((_) => <dynamic>[]),
      ]);

      // Lấy thông tin giảng viên và quyền tài khoản hiện tại để kiểm tra phân quyền sửa
      try {
        final lec = await widget.client.getLecturerMe();
        if (lec is Map) _currentLecturerId = lec['id'] as int?;
      } catch (_) {}

      try {
        final me = await widget.client.getMe();
        if (me is Map && me['roles'] is List) {
          _currentUserRoles = (me['roles'] as List)
              .map((r) => (r is Map ? r['role'] : r).toString())
              .toList();
        }
      } catch (_) {}

      if (mounted) {
        setState(() {
          _product = Map<String, dynamic>.from(results[0] as Map);
          _history = results[1] as List<dynamic>;
          _snapshots = results[2] as List<dynamic>;
        });
      }
    } catch (e) {
      if (mounted) setState(() => _error = e.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
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

  // ──── Kiểm tra quyền sửa & quy tắc phiên bản ─────────────────────────────
  bool _canEdit() {
    if (_product == null) return false;
    final status = _product!['status'] as String? ?? '';
    // Chỉ có thể sửa khi sản phẩm ở trạng thái Nháp hoặc Yêu cầu bổ sung
    final isStatusEditable = status == 'NHAP' || status == 'TRA_LAI';
    if (!isStatusEditable) return false;

    // Chỉ tác giả chính hoặc quản trị viên mới có quyền sửa
    final submittedByLecturerId = _product!['submittedByLecturerId'];
    final isOwner = _currentLecturerId != null && _currentLecturerId == submittedByLecturerId;
    final isAdmin = _currentUserRoles.contains('QUAN_TRI');
    return isOwner || isAdmin;
  }

  String _getEditDisabledReason() {
    if (_product == null) return '';
    final status = _product!['status'] as String? ?? '';
    final isStatusEditable = status == 'NHAP' || status == 'TRA_LAI';
    if (!isStatusEditable) {
      return 'Sản phẩm đang ở trạng thái "${productStatusLabel(status)}". '
          'Theo quy tắc phiên bản và xét duyệt, sản phẩm đã gửi chỉ có thể chỉnh sửa khi ở trạng thái "Nháp" hoặc "Yêu cầu bổ sung".';
    }
    final submittedByLecturerId = _product!['submittedByLecturerId'];
    final isOwner = _currentLecturerId != null && _currentLecturerId == submittedByLecturerId;
    final isAdmin = _currentUserRoles.contains('QUAN_TRI');
    if (!isOwner && !isAdmin) {
      return 'Chỉ tác giả chính (${_product!['submittedByLecturerFullName'] ?? ''}) mới có quyền chỉnh sửa sản phẩm này.';
    }
    return '';
  }

  // ──── Cập nhật trạng thái bài viết & thông tin ────────────────────────────
  Future<void> _showUpdateArticleStatusDialog() async {
    if (!_canEdit() || _product == null) return;

    final p = _product!;
    String? currentArticleStatus = p['articleStatus'] as String?;
    DateTime? selectedDate = p['publishedDate'] != null ? DateTime.tryParse(p['publishedDate']) : null;
    final pubInfoController = TextEditingController(text: p['publicationInfo'] as String? ?? '');
    String? selectedStatus = currentArticleStatus;
    bool busy = false;
    String? dialogError;

    await showModalBottomSheet<void>(
      context: context,
      isScrollControlled: true,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(20)),
      ),
      builder: (ctx) => StatefulBuilder(
        builder: (ctx, setDialogState) {
          return Padding(
            padding: EdgeInsets.only(
              top: 20,
              left: 20,
              right: 20,
              bottom: MediaQuery.of(ctx).viewInsets.bottom + 20,
            ),
            child: SingleChildScrollView(
              child: Column(
                mainAxisSize: MainAxisSize.min,
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  Row(
                    children: [
                      const Icon(Icons.edit_note, color: Color(0xFF177158), size: 28),
                      const SizedBox(width: 10),
                      Text(
                        'Cập nhật trạng thái bài viết',
                        style: Theme.of(ctx).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.bold,
                              color: const Color(0xFF173F32),
                            ),
                      ),
                    ],
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Chọn trạng thái bài viết theo tiến độ nghiên cứu thực tế. '
                    'Thay đổi sẽ được lưu vào phiên bản sản phẩm hiện tại.',
                    style: TextStyle(color: Colors.black54, fontSize: 13),
                  ),
                  const SizedBox(height: 16),
                  if (dialogError != null)
                    Container(
                      padding: const EdgeInsets.all(12),
                      margin: const EdgeInsets.only(bottom: 12),
                      decoration: BoxDecoration(
                        color: Colors.red.shade50,
                        borderRadius: BorderRadius.circular(8),
                        border: Border.all(color: Colors.red.shade200),
                      ),
                      child: Text(dialogError!, style: TextStyle(color: Colors.red.shade800, fontSize: 13)),
                    ),

                  // 4 trạng thái bài viết
                  _ArticleStatusOption(
                    value: 'DANG_VIET',
                    label: 'Đang viết',
                    description: 'Tác giả đang biên soạn và hoàn thiện nội dung bản thảo',
                    groupValue: selectedStatus,
                    color: Colors.blueGrey,
                    onChanged: (v) => setDialogState(() => selectedStatus = v),
                  ),
                  _ArticleStatusOption(
                    value: 'DANG_PHAN_BIEN',
                    label: 'Đang phản biện',
                    description: 'Đã nộp tòa soạn/hội thảo và đang trong quá trình phản biện',
                    groupValue: selectedStatus,
                    color: Colors.indigo,
                    onChanged: (v) => setDialogState(() => selectedStatus = v),
                  ),
                  _ArticleStatusOption(
                    value: 'DA_NHAN_DANG',
                    label: 'Đã nhận xét',
                    description: 'Đã nhận xét phản biện / được hội đồng chấp nhận đăng',
                    groupValue: selectedStatus,
                    color: Colors.teal,
                    onChanged: (v) => setDialogState(() => selectedStatus = v),
                  ),
                  _ArticleStatusOption(
                    value: 'DA_XUAT_BAN',
                    label: 'Đã xuất bản',
                    description: 'Đã phát hành chính thức trên tạp chí hoặc kỷ yếu',
                    groupValue: selectedStatus,
                    color: const Color(0xFF2E7D32),
                    onChanged: (v) => setDialogState(() => selectedStatus = v),
                  ),

                  const SizedBox(height: 12),
                  const Divider(),
                  const SizedBox(height: 8),

                  // Ngày công bố (DatePicker)
                  ListTile(
                    dense: true,
                    contentPadding: EdgeInsets.zero,
                    title: const Text('Ngày công bố', style: TextStyle(fontSize: 13, fontWeight: FontWeight.w600)),
                    subtitle: Text(
                      selectedDate != null
                          ? '${selectedDate!.day.toString().padLeft(2, '0')}/${selectedDate!.month.toString().padLeft(2, '0')}/${selectedDate!.year}'
                          : 'Chưa chọn ngày',
                      style: const TextStyle(fontSize: 13),
                    ),
                    trailing: Row(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        if (selectedDate != null)
                          IconButton(
                            icon: const Icon(Icons.clear, size: 18),
                            onPressed: () => setDialogState(() => selectedDate = null),
                          ),
                        IconButton(
                          icon: const Icon(Icons.calendar_month, color: Color(0xFF177158)),
                          onPressed: () async {
                            final picked = await showDatePicker(
                              context: ctx,
                              initialDate: selectedDate ?? DateTime.now(),
                              firstDate: DateTime(2000),
                              lastDate: DateTime(2035),
                            );
                            if (picked != null) setDialogState(() => selectedDate = picked);
                          },
                        ),
                      ],
                    ),
                  ),

                  // Nơi công bố
                  TextField(
                    controller: pubInfoController,
                    decoration: const InputDecoration(
                      labelText: 'Nơi công bố / Tạp chí / Hội thảo',
                      hintText: 'Nhập tên tạp chí, nhà xuất bản, hội thảo...',
                      border: OutlineInputBorder(),
                      contentPadding: EdgeInsets.symmetric(horizontal: 12, vertical: 10),
                    ),
                    style: const TextStyle(fontSize: 13),
                  ),

                  const SizedBox(height: 20),

                  // Hành động
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton(
                          onPressed: busy ? null : () => Navigator.pop(ctx),
                          child: const Text('Hủy'),
                        ),
                      ),
                      const SizedBox(width: 12),
                      Expanded(
                        child: FilledButton(
                          style: FilledButton.styleFrom(
                            backgroundColor: const Color(0xFF177158),
                          ),
                          onPressed: busy
                              ? null
                              : () async {
                                  setDialogState(() {
                                    busy = true;
                                    dialogError = null;
                                  });
                                  try {
                                    final dateStr = selectedDate != null
                                        ? '${selectedDate!.year}-${selectedDate!.month.toString().padLeft(2, '0')}-${selectedDate!.day.toString().padLeft(2, '0')}'
                                        : null;

                                    final payload = <String, dynamic>{
                                      'title': p['title'],
                                      'description': p['description'],
                                      'publicationInfo': pubInfoController.text.trim().isEmpty
                                          ? null
                                          : pubInfoController.text.trim(),
                                      'publishedDate': dateStr,
                                      'articleStatus': selectedStatus,
                                      'journalIndex': p['journalIndex'],
                                      'journalClassification': p['journalClassification'],
                                      'projectLevel': p['projectLevel'],
                                    };

                                    await widget.client.updateProduct(widget.productId, payload);
                                    if (ctx.mounted) Navigator.pop(ctx);
                                    if (mounted) {
                                      ScaffoldMessenger.of(context).showSnackBar(
                                        const SnackBar(
                                          content: Text('Cập nhật trạng thái bài viết thành công'),
                                          backgroundColor: Color(0xFF2E7D32),
                                        ),
                                      );
                                      await _load();
                                    }
                                  } on ApiException catch (e) {
                                    setDialogState(() {
                                      busy = false;
                                      dialogError = e.message;
                                    });
                                  } catch (e) {
                                    setDialogState(() {
                                      busy = false;
                                      dialogError = e.toString();
                                    });
                                  }
                                },
                          child: Text(busy ? 'Đang lưu…' : 'Lưu cập nhật'),
                        ),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          );
        },
      ),
    );
  }

  // ──── Gửi duyệt sản phẩm (tạo snapshot phiên bản mới) ────────────────────
  Future<void> _submitProduct() async {
    if (!_canEdit() || _product == null) return;

    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: const Text('Gửi duyệt sản phẩm'),
        content: const Text(
          'Bạn có chắc chắn muốn gửi duyệt sản phẩm này không?\n\n'
          'Quy tắc phiên bản: Hệ thống sẽ tự động lưu lại một bản chụp phiên bản mới '
          'và sản phẩm sẽ chuyển sang trạng thái "Đã gửi". '
          'Bạn sẽ không thể chỉnh sửa cho đến khi có kết quả xét duyệt hoặc yêu cầu bổ sung.',
        ),
        actions: [
          TextButton(onPressed: () => Navigator.pop(ctx, false), child: const Text('Hủy')),
          FilledButton(
            style: FilledButton.styleFrom(backgroundColor: const Color(0xFF177158)),
            onPressed: () => Navigator.pop(ctx, true),
            child: const Text('Xác nhận gửi duyệt'),
          ),
        ],
      ),
    );

    if (confirmed != true) return;

    setState(() => _loading = true);
    try {
      await widget.client.submitProduct(widget.productId);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(
            content: Text('Đã gửi duyệt sản phẩm thành công. Hệ thống đã lưu phiên bản mới.'),
            backgroundColor: Color(0xFF2E7D32),
          ),
        );
        await _load();
      }
    } on ApiException catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.message), backgroundColor: Colors.red),
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text(e.toString()), backgroundColor: Colors.red),
        );
      }
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final title = _product != null ? (_product!['title'] as String? ?? 'Chi tiết') : 'Chi tiết sản phẩm';
    return DefaultTabController(
      length: 3,
      child: Scaffold(
        appBar: AppBar(
          title: Text(title, style: const TextStyle(fontSize: 17, fontWeight: FontWeight.bold)),
          backgroundColor: const Color(0xFF173F32),
          foregroundColor: Colors.white,
          bottom: const TabBar(
            labelColor: Colors.white,
            unselectedLabelColor: Color(0xFFD5E5DB),
            indicatorColor: Colors.white,
            tabs: [
              Tab(text: 'Thông tin', icon: Icon(Icons.info_outline, size: 20)),
              Tab(text: 'Lịch sử duyệt', icon: Icon(Icons.history, size: 20)),
              Tab(text: 'Phiên bản', icon: Icon(Icons.layers_outlined, size: 20)),
            ],
          ),
        ),
        body: _loading
            ? const Center(child: CircularProgressIndicator())
            : _error != null
                ? MessageWidget(
                    icon: Icons.error_outline,
                    title: 'Không tải được chi tiết',
                    text: _error!,
                    action: _load,
                  )
                : TabBarView(
                    children: [
                      RefreshIndicator(onRefresh: _load, child: _buildInfoTab()),
                      RefreshIndicator(onRefresh: _load, child: _buildHistoryTab()),
                      RefreshIndicator(onRefresh: _load, child: _buildSnapshotsTab()),
                    ],
                  ),
      ),
    );
  }

  // ──── Tab 1: Thông tin ────────────────────────────────────────────────────
  Widget _buildInfoTab() {
    if (_product == null) return const SizedBox.shrink();
    final p = _product!;
    final coAuthors = p['coAuthors'] as List<dynamic>? ?? [];
    final evidences = p['evidences'] as List<dynamic>? ?? [];
    final canEdit = _canEdit();
    final disabledReason = _getEditDisabledReason();
    final articleStatus = p['articleStatus'] as String?;

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // ── Card trạng thái tổng quan ──
        Card(
          color: const Color(0xFF173F32),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(14)),
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Trạng thái xét duyệt',
                          style: TextStyle(color: Color(0xFFD5E5DB), fontSize: 12)),
                      const SizedBox(height: 6),
                      StatusChip(
                        label: productStatusLabel(p['status'] as String?),
                        color: productStatusColor(p['status'] as String?),
                      ),
                    ],
                  ),
                ),
                if (p['scoreEquivalent'] != null)
                  Column(
                    crossAxisAlignment: CrossAxisAlignment.end,
                    children: [
                      const Text('Điểm quy đổi',
                          style: TextStyle(color: Color(0xFFD5E5DB), fontSize: 12)),
                      const SizedBox(height: 4),
                      Text(
                        '${p['scoreEquivalent']}',
                        style: const TextStyle(
                          color: Colors.white,
                          fontSize: 22,
                          fontWeight: FontWeight.bold,
                        ),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),

        // ── Card trạng thái bài viết & quyền sửa ──
        Card(
          elevation: 1,
          shape: RoundedRectangleBorder(
            borderRadius: BorderRadius.circular(12),
            side: BorderSide(color: Colors.grey.shade200),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  mainAxisAlignment: MainAxisAlignment.spaceBetween,
                  children: [
                    const Text(
                      'Trạng thái bài viết',
                      style: TextStyle(fontWeight: FontWeight.bold, fontSize: 15),
                    ),
                    if (articleStatus != null && articleStatus.isNotEmpty)
                      StatusChip(
                        label: articleStatusLabel(articleStatus),
                        color: articleStatusColor(articleStatus),
                      )
                    else
                      const Text('Chưa cập nhật', style: TextStyle(color: Colors.grey, fontSize: 13)),
                  ],
                ),
                const SizedBox(height: 12),

                // Hành động chỉnh sửa hoặc thông báo khóa quyền
                if (canEdit) ...[
                  Row(
                    children: [
                      Expanded(
                        child: OutlinedButton.icon(
                          onPressed: _showUpdateArticleStatusDialog,
                          icon: const Icon(Icons.edit_outlined, size: 18),
                          label: const Text('Cập nhật trạng thái'),
                        ),
                      ),
                      const SizedBox(width: 8),
                      FilledButton.icon(
                        style: FilledButton.styleFrom(backgroundColor: const Color(0xFF177158)),
                        onPressed: _submitProduct,
                        icon: const Icon(Icons.send_outlined, size: 18),
                        label: const Text('Gửi duyệt'),
                      ),
                    ],
                  ),
                ] else ...[
                  Container(
                    padding: const EdgeInsets.all(12),
                    decoration: BoxDecoration(
                      color: Colors.amber.shade50,
                      borderRadius: BorderRadius.circular(8),
                      border: Border.all(color: Colors.amber.shade200),
                    ),
                    child: Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Icon(Icons.lock_outline, size: 18, color: Colors.amber.shade900),
                        const SizedBox(width: 10),
                        Expanded(
                          child: Text(
                            disabledReason.isNotEmpty
                                ? disabledReason
                                : 'Sản phẩm không thể chỉnh sửa ở bước này.',
                            style: TextStyle(fontSize: 12, color: Colors.amber.shade900, height: 1.35),
                          ),
                        ),
                      ],
                    ),
                  ),
                ],
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),

        // ── Thông tin chi tiết sản phẩm ──
        SectionCard(
          title: 'Thông tin sản phẩm',
          children: [
            DetailRow('Tên sản phẩm', p['title'] as String?),
            DetailRow('Trạng thái bài viết', articleStatusLabel(p['articleStatus'] as String?)),
            DetailRow('Ngày công bố', _formatDate(p['publishedDate'] as String?)),
            DetailRow('Nơi công bố', p['publicationInfo'] as String?),
            DetailRow('Chỉ số tạp chí', p['journalIndex'] as String?),
            DetailRow('Phân loại tạp chí', p['journalClassification'] as String?),
            DetailRow('Cấp đề tài', p['projectLevel'] as String?),
            DetailRow('Người nộp', p['submittedByLecturerFullName'] as String?),
            if (p['description'] != null && (p['description'] as String).isNotEmpty)
              Padding(
                padding: const EdgeInsets.only(bottom: 10),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    const Text('Mô tả', style: TextStyle(color: Colors.grey, fontSize: 13)),
                    const SizedBox(height: 4),
                    Text(p['description'] as String, style: const TextStyle(fontSize: 13)),
                    const Divider(),
                  ],
                ),
              ),
          ],
        ),
        const SizedBox(height: 12),

        // ── Đồng tác giả ──
        if (coAuthors.isNotEmpty) ...[
          SectionCard(
            title: 'Đồng tác giả (${coAuthors.length})',
            children: coAuthors.map((a) {
              final author = a as Map<String, dynamic>;
              return ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: CircleAvatar(
                  backgroundColor: const Color(0xFF177158).withOpacity(0.12),
                  child: Text(
                    '${author['displayOrder'] ?? ''}',
                    style: const TextStyle(
                      color: Color(0xFF177158),
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                ),
                title: Text(author['lecturerFullName'] as String? ?? ''),
                subtitle: Text(author['authorRole'] as String? ?? ''),
                trailing: Text(
                  author['lecturerCode'] as String? ?? '',
                  style: const TextStyle(color: Colors.grey, fontSize: 12),
                ),
              );
            }).toList(),
          ),
          const SizedBox(height: 12),
        ],

        // ── Minh chứng ──
        if (evidences.isNotEmpty) ...[
          SectionCard(
            title: 'Minh chứng (${evidences.length})',
            children: evidences.map((e) {
              final ev = e as Map<String, dynamic>;
              final sizeKb = ev['fileSizeBytes'] != null
                  ? ((ev['fileSizeBytes'] as num) / 1024).toStringAsFixed(1)
                  : null;
              return ListTile(
                dense: true,
                contentPadding: EdgeInsets.zero,
                leading: const Icon(Icons.attach_file, color: Color(0xFF177158)),
                title: Text(ev['originalFileName'] as String? ?? ''),
                subtitle: ev['description'] != null && (ev['description'] as String).isNotEmpty
                    ? Text(ev['description'] as String)
                    : null,
                trailing: sizeKb != null
                    ? Text('$sizeKb KB', style: const TextStyle(color: Colors.grey, fontSize: 12))
                    : null,
              );
            }).toList(),
          ),
          const SizedBox(height: 12),
        ],
        const SizedBox(height: 24),
      ],
    );
  }

  // ──── Tab 2: Lịch sử xét duyệt ───────────────────────────────────────────
  Widget _buildHistoryTab() {
    final history = _history ?? [];
    if (history.isEmpty) {
      return ListView(
        padding: const EdgeInsets.all(24),
        children: const [
          MessageWidget(
            icon: Icons.history_toggle_off,
            title: 'Chưa có lịch sử',
            text: 'Sản phẩm này chưa phát sinh lần chuyển trạng thái xét duyệt nào.',
          ),
        ],
      );
    }
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        SectionCard(
          title: 'Lịch sử xét duyệt (${history.length})',
          children: history.asMap().entries.map((entry) {
            return _HistoryItem(
              item: entry.value as Map<String, dynamic>,
              isLast: entry.key == history.length - 1,
            );
          }).toList(),
        ),
      ],
    );
  }

  // ──── Tab 3: Phiên bản (Snapshots) ────────────────────────────────────────
  Widget _buildSnapshotsTab() {
    final snapshots = _snapshots ?? [];
    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Giải thích quy tắc phiên bản
        Container(
          padding: const EdgeInsets.all(14),
          decoration: BoxDecoration(
            color: const Color(0xFF177158).withOpacity(0.08),
            borderRadius: BorderRadius.circular(12),
            border: Border.all(color: const Color(0xFF177158).withOpacity(0.2)),
          ),
          child: const Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Icon(Icons.info_outline, color: Color(0xFF177158), size: 20),
              SizedBox(width: 10),
              Expanded(
                child: Text(
                  'Quy tắc phiên bản: Mỗi lần gửi duyệt hoặc xét duyệt, hệ thống tự động lưu bản chụp phiên bản bất biến (v1, v2...). '
                  'Khi ở trạng thái Nháp hoặc Yêu cầu bổ sung, các thay đổi trạng thái bài viết sẽ hoàn thiện nội dung trước khi gửi duyệt phiên bản tiếp theo.',
                  style: TextStyle(fontSize: 12, color: Color(0xFF173F32), height: 1.35),
                ),
              ),
            ],
          ),
        ),
        const SizedBox(height: 14),

        if (snapshots.isEmpty)
          Card(
            shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
            child: const Padding(
              padding: EdgeInsets.all(24),
              child: Center(
                child: Text(
                  'Chưa có phiên bản nào được lưu.\nPhiên bản sẽ được tạo tự động khi gửi duyệt sản phẩm.',
                  textAlign: TextAlign.center,
                  style: TextStyle(color: Colors.grey),
                ),
              ),
            ),
          )
        else
          // Hiển thị phiên bản mới nhất ở trên
          ...snapshots.reversed.map((raw) {
            final s = raw as Map<String, dynamic>;
            final version = s['version'] ?? 1;
            final snapArticleStatus = s['articleStatus'] as String?;
            final snapWorkflowStatus = s['status'] as String?;

            return Card(
              elevation: 0.5,
              margin: const EdgeInsets.only(bottom: 12),
              shape: RoundedRectangleBorder(
                borderRadius: BorderRadius.circular(12),
                side: BorderSide(color: Colors.grey.shade200),
              ),
              child: ExpansionTile(
                initiallyExpanded: version == snapshots.length,
                leading: Container(
                  padding: const EdgeInsets.symmetric(horizontal: 10, vertical: 5),
                  decoration: BoxDecoration(
                    color: const Color(0xFF177158),
                    borderRadius: BorderRadius.circular(8),
                  ),
                  child: Text(
                    'v$version',
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.bold,
                      fontSize: 13,
                    ),
                  ),
                ),
                title: Text(
                  s['title'] as String? ?? 'Phiên bản $version',
                  style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14),
                ),
                subtitle: Padding(
                  padding: const EdgeInsets.only(top: 4),
                  child: Wrap(
                    spacing: 6,
                    runSpacing: 4,
                    children: [
                      StatusChip(
                        label: productStatusLabel(snapWorkflowStatus),
                        color: productStatusColor(snapWorkflowStatus),
                      ),
                      if (snapArticleStatus != null && snapArticleStatus.isNotEmpty)
                        StatusChip(
                          label: articleStatusLabel(snapArticleStatus),
                          color: articleStatusColor(snapArticleStatus),
                        ),
                    ],
                  ),
                ),
                children: [
                  Padding(
                    padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        const Divider(),
                        DetailRow('Trạng thái bài viết', articleStatusLabel(snapArticleStatus)),
                        DetailRow('Ngày công bố', _formatDate(s['publishedDate'] as String?)),
                        DetailRow('Nơi công bố', s['publicationInfo'] as String?),
                        DetailRow('Chỉ số tạp chí', s['journalIndex'] as String?),
                        DetailRow('Phân loại tạp chí', s['journalClassification'] as String?),
                        DetailRow('Cấp đề tài', s['projectLevel'] as String?),
                        DetailRow('Thời gian lưu', _formatDateTime(s['snapshotAt'] as String?)),
                      ],
                    ),
                  ),
                ],
              ),
            );
          }),
        const SizedBox(height: 24),
      ],
    );
  }
}

// ---------------------------------------------------------------------------
// _ArticleStatusOption – Radio widget lựa chọn trạng thái bài viết
// ---------------------------------------------------------------------------

class _ArticleStatusOption extends StatelessWidget {
  const _ArticleStatusOption({
    required this.value,
    required this.label,
    required this.description,
    required this.groupValue,
    required this.color,
    required this.onChanged,
  });

  final String value;
  final String label;
  final String description;
  final String? groupValue;
  final Color color;
  final ValueChanged<String> onChanged;

  @override
  Widget build(BuildContext context) {
    final selected = value == groupValue;
    return Container(
      margin: const EdgeInsets.only(bottom: 8),
      decoration: BoxDecoration(
        color: selected ? color.withOpacity(0.08) : Colors.transparent,
        borderRadius: BorderRadius.circular(10),
        border: Border.all(
          color: selected ? color : Colors.grey.shade300,
          width: selected ? 1.5 : 1.0,
        ),
      ),
      child: RadioListTile<String>(
        value: value,
        groupValue: groupValue,
        activeColor: color,
        onChanged: (v) {
          if (v != null) onChanged(v);
        },
        title: Row(
          children: [
            Text(label, style: const TextStyle(fontWeight: FontWeight.bold, fontSize: 14)),
            const SizedBox(width: 8),
            Container(
              width: 8,
              height: 8,
              decoration: BoxDecoration(color: color, shape: BoxShape.circle),
            ),
          ],
        ),
        subtitle: Text(description, style: const TextStyle(fontSize: 12, color: Colors.black54)),
      ),
    );
  }
}

// ---------------------------------------------------------------------------
// _HistoryItem – Dòng hiển thị lịch sử xét duyệt
// ---------------------------------------------------------------------------

class _HistoryItem extends StatelessWidget {
  const _HistoryItem({required this.item, required this.isLast});
  final Map<String, dynamic> item;
  final bool isLast;

  String _formatOccurredAt(String? iso) {
    if (iso == null || iso.isEmpty) return '';
    try {
      final dt = DateTime.parse(iso).toLocal();
      return '${dt.day.toString().padLeft(2, '0')}/${dt.month.toString().padLeft(2, '0')}/${dt.year} '
          '${dt.hour.toString().padLeft(2, '0')}:${dt.minute.toString().padLeft(2, '0')}';
    } catch (_) {
      return iso;
    }
  }

  @override
  Widget build(BuildContext context) {
    final from = productStatusLabel(item['fromStatus'] as String?);
    final to = productStatusLabel(item['toStatus'] as String?);
    final comment = item['comment'] as String?;
    final actor = item['actorUsername'] as String? ?? '';
    final date = _formatOccurredAt(item['occurredAt'] as String?);

    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Column(
            children: [
              Container(
                width: 12,
                height: 12,
                decoration: const BoxDecoration(
                  color: Color(0xFF177158),
                  shape: BoxShape.circle,
                ),
              ),
              if (!isLast)
                Expanded(
                  child: Container(
                    width: 2,
                    color: const Color(0xFF177158).withOpacity(0.25),
                  ),
                ),
            ],
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('$from → $to',
                      style: const TextStyle(
                          fontWeight: FontWeight.w600, fontSize: 13)),
                  Text(actor,
                      style: const TextStyle(color: Colors.grey, fontSize: 12)),
                  if (comment != null && comment.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(comment, style: const TextStyle(fontSize: 13)),
                  ],
                  const SizedBox(height: 2),
                  Text(date, style: const TextStyle(color: Colors.grey, fontSize: 11)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
