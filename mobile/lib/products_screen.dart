import 'package:flutter/material.dart';
import 'api_client.dart';
import 'product_detail_screen.dart';

class ProductsScreen extends StatefulWidget {
  const ProductsScreen({super.key, required this.client});
  final ApiClient client;

  @override
  State<ProductsScreen> createState() => _ProductsScreenState();
}

class _ProductsScreenState extends State<ProductsScreen> {
  List<dynamic>? _products;
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
      final data = await widget.client.getMyProducts();
      if (mounted) setState(() => _products = data);
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
        title: const Text('Sản phẩm của tôi'),
        backgroundColor: const Color(0xFF173F32),
        foregroundColor: Colors.white,
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
        title: 'Không tải được sản phẩm',
        text: _error!,
        action: _load,
      );
    }
    final products = _products ?? [];
    if (products.isEmpty) {
      return ListView(
        physics: const AlwaysScrollableScrollPhysics(),
        children: const [
          MessageWidget(
            icon: Icons.article_outlined,
            title: 'Chưa có sản phẩm',
            text: 'Bạn chưa có sản phẩm nào được ghi nhận.',
          ),
        ],
      );
    }
    return ListView.builder(
      physics: const AlwaysScrollableScrollPhysics(),
      padding: const EdgeInsets.symmetric(horizontal: 16, vertical: 12),
      itemCount: products.length,
      itemBuilder: (ctx, i) {
        final p = products[i] as Map<String, dynamic>;
        return _ProductCard(
          product: p,
          onTap: () async {
            await Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) => ProductDetailScreen(
                  client: widget.client,
                  productId: p['id'] as int,
                ),
              ),
            );

            if (!mounted) return;
            await _load();
          },
        );
      },
    );
  }
}

class _ProductCard extends StatelessWidget {
  const _ProductCard({required this.product, required this.onTap});
  final Map<String, dynamic> product;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    final status = product['status'] as String?;
    final articleStatus = product['articleStatus'] as String?;
    final score = product['scoreEquivalent'];

    return Card(
      margin: const EdgeInsets.only(bottom: 12),
      shape: RoundedRectangleBorder(borderRadius: BorderRadius.circular(12)),
      child: InkWell(
        borderRadius: BorderRadius.circular(12),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(16),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text(
                product['title'] as String? ?? '(Không có tiêu đề)',
                style: const TextStyle(
                  fontWeight: FontWeight.bold,
                  fontSize: 15,
                ),
              ),
              const SizedBox(height: 10),
              Wrap(
                spacing: 8,
                runSpacing: 6,
                children: [
                  StatusChip(
                    label: productStatusLabel(status),
                    color: productStatusColor(status),
                  ),
                  if (articleStatus != null && articleStatus.isNotEmpty)
                    StatusChip(
                      label: articleStatusLabel(articleStatus),
                      color: articleStatusColor(articleStatus),
                    ),
                  if (score != null)
                    StatusChip(
                      label: 'Điểm: $score',
                      color: const Color(0xFF177158),
                    ),
                ],
              ),
            ],
          ),
        ),
      ),
    );
  }
}
