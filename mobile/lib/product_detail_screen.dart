import 'package:flutter/material.dart';
import 'api_client.dart';

// ---------------------------------------------------------------------------
// Status helpers (also used in products_screen.dart — duplicated for isolation)
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
    case 'DA_DUYET': return Colors.green;
    case 'TRA_LAI': return Colors.orange;
    default: return Colors.grey;
  }
}

String articleStatusLabel(String? s) {
  switch (s) {
    case 'DANG_VIET': return 'Đang viết';
    case 'DANG_PHAN_BIEN': return 'Đang phản biện';
    case 'DA_NHAN_DANG': return 'Đã nhận đăng';
    case 'DA_XUAT_BAN': return 'Đã xuất bản';
    default: return s ?? '';
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
      ]);
      if (mounted) {
        setState(() {
          _product = Map<String, dynamic>.from(results[0] as Map);
          _history = results[1] as List<dynamic>;
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

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(
        title: Text(
          _product != null
              ? (_product!['title'] as String? ?? 'Chi tiết sản phẩm')
              : 'Chi tiết sản phẩm',
        ),
        backgroundColor: const Color(0xFF173F32),
        foregroundColor: Colors.white,
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
        title: 'Không tải được chi tiết',
        text: _error!,
        action: _load,
      );
    }
    if (_product == null) {
      return ListView(children: const [SizedBox(height: 80)]);
    }

    final p = _product!;
    final coAuthors = p['coAuthors'] as List<dynamic>? ?? [];
    final evidences = p['evidences'] as List<dynamic>? ?? [];
    final history = _history ?? [];

    return ListView(
      padding: const EdgeInsets.all(16),
      children: [
        // Status + score header card
        Card(
          color: const Color(0xFF173F32),
          shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
          child: Padding(
            padding: const EdgeInsets.all(18),
            child: Row(
              children: [
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text('Trạng thái',
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
                            fontWeight: FontWeight.bold),
                      ),
                    ],
                  ),
              ],
            ),
          ),
        ),
        const SizedBox(height: 12),

        // Detail fields
        SectionCard(
          title: 'Thông tin sản phẩm',
          children: [
            DetailRow('Tên sản phẩm', p['title'] as String?),
            DetailRow('Trạng thái xét duyệt', productStatusLabel(p['status'] as String?)),
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
                    const Text('Mô tả',
                        style: TextStyle(color: Colors.grey, fontSize: 13)),
                    const SizedBox(height: 4),
                    Text(p['description'] as String,
                        style: const TextStyle(fontSize: 13)),
                    const Divider(),
                  ],
                ),
              ),
          ],
        ),
        const SizedBox(height: 12),

        // Co-authors
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
                        color: Color(0xFF177158), fontWeight: FontWeight.bold),
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

        // Evidences
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
                subtitle: ev['description'] != null &&
                        (ev['description'] as String).isNotEmpty
                    ? Text(ev['description'] as String)
                    : null,
                trailing: sizeKb != null
                    ? Text('$sizeKb KB',
                        style: const TextStyle(color: Colors.grey, fontSize: 12))
                    : null,
              );
            }).toList(),
          ),
          const SizedBox(height: 12),
        ],

        // Review history
        if (history.isNotEmpty) ...[
          SectionCard(
            title: 'Lịch sử xét duyệt',
            children: history.asMap().entries.map((entry) {
              return _HistoryItem(
                  item: entry.value as Map<String, dynamic>,
                  isLast: entry.key == history.length - 1);
            }).toList(),
          ),
        ],

        const SizedBox(height: 24),
      ],
    );
  }
}

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
                      color: const Color(0xFF177158).withOpacity(0.25)),
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
                      style:
                          const TextStyle(color: Colors.grey, fontSize: 12)),
                  if (comment != null && comment.isNotEmpty) ...[
                    const SizedBox(height: 4),
                    Text(comment, style: const TextStyle(fontSize: 13)),
                  ],
                  const SizedBox(height: 2),
                  Text(date,
                      style: const TextStyle(
                          color: Colors.grey, fontSize: 11)),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
