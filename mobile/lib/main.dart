import 'dart:convert';
import 'dart:io';
import 'package:flutter/material.dart';

const apiUrl = String.fromEnvironment('API_URL', defaultValue: 'http://10.0.2.2:8081/api');
void main() => runApp(const ResearchManagementApp());

class ResearchManagementApp extends StatefulWidget {
  const ResearchManagementApp({super.key});
  @override State<ResearchManagementApp> createState() => _AppState();
}
class _AppState extends State<ResearchManagementApp> {
  String? token;
  @override Widget build(BuildContext context) => MaterialApp(
    title: 'ResearchHub', debugShowCheckedModeBanner: false,
    theme: ThemeData(colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF177158)), useMaterial3: true),
    home: token == null ? LoginScreen(onLogin: (v) => setState(() => token = v)) : EvaluationScreen(client: ApiClient(token!), onLogout: () => setState(() => token = null)),
  );
}

class ApiClient {
  ApiClient(this.token); final String token;
  Future<dynamic> get(String path) => _request('GET', path);
  Future<dynamic> _request(String method, String path, [Object? body]) async {
    final client = HttpClient();
    try {
      final request = await client.openUrl(method, Uri.parse('$apiUrl$path'));
      request.headers.set(HttpHeaders.acceptHeader, 'application/json'); request.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token'); request.headers.set('X-Requested-With', 'ResearchHub');
      if (body != null) { request.headers.contentType = ContentType.json; request.write(jsonEncode(body)); }
      final response = await request.close(); final text = await response.transform(utf8.decoder).join(); final data = text.isEmpty ? null : jsonDecode(text);
      if (response.statusCode < 200 || response.statusCode >= 300) throw ApiException(response.statusCode, data is Map ? (data['detail'] ?? data['title'] ?? 'Không thể xử lý yêu cầu.') : 'Không thể xử lý yêu cầu.');
      return data;
    } finally { client.close(); }
  }
  static Future<String> login(String username, String password) async {
    final client = HttpClient();
    try {
      final request = await client.postUrl(Uri.parse('$apiUrl/auth/login')); request.headers.contentType = ContentType.json; request.headers.set('X-Requested-With', 'ResearchHub'); request.write(jsonEncode({'username': username, 'password': password}));
      final response = await request.close(); final text = await response.transform(utf8.decoder).join(); final data = text.isEmpty ? null : jsonDecode(text);
      if (response.statusCode != 200) throw ApiException(response.statusCode, data is Map ? (data['detail'] ?? data['title'] ?? 'Đăng nhập thất bại.') : 'Đăng nhập thất bại.');
      return data['accessToken'] as String;
    } finally { client.close(); }
  }
}
class ApiException implements Exception { ApiException(this.status, this.message); final int status; final String message; @override String toString() => message; }

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.onLogin}); final ValueChanged<String> onLogin;
  @override State<LoginScreen> createState() => _LoginState();
}
class _LoginState extends State<LoginScreen> {
  final form = GlobalKey<FormState>(); final username = TextEditingController(); final password = TextEditingController(); bool busy = false; String? error;
  Future<void> submit() async { if (!form.currentState!.validate()) return; setState(() { busy = true; error = null; }); try { widget.onLogin(await ApiClient.login(username.text.trim(), password.text)); } catch (e) { setState(() => error = e.toString()); } finally { if (mounted) setState(() => busy = false); } }
  @override Widget build(BuildContext context) => Scaffold(body: SafeArea(child: Center(child: SingleChildScrollView(padding: const EdgeInsets.all(24), child: ConstrainedBox(constraints: const BoxConstraints(maxWidth: 420), child: Form(key: form, child: Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
    const Icon(Icons.science_outlined, size: 64, color: Color(0xFF177158)), const SizedBox(height: 18), Text('ResearchHub', textAlign: TextAlign.center, style: Theme.of(context).textTheme.headlineMedium?.copyWith(fontWeight: FontWeight.bold)), const SizedBox(height: 8), const Text('Đăng nhập để xem kết quả nghiên cứu', textAlign: TextAlign.center), const SizedBox(height: 28),
    if (error != null) Card(color: Theme.of(context).colorScheme.errorContainer, child: Padding(padding: const EdgeInsets.all(14), child: Text(error!))),
    TextFormField(controller: username, decoration: const InputDecoration(labelText: 'Tên đăng nhập', border: OutlineInputBorder()), validator: (v) => v == null || v.trim().isEmpty ? 'Nhập tên đăng nhập' : null), const SizedBox(height: 14),
    TextFormField(controller: password, obscureText: true, decoration: const InputDecoration(labelText: 'Mật khẩu', border: OutlineInputBorder()), validator: (v) => v == null || v.isEmpty ? 'Nhập mật khẩu' : null, onFieldSubmitted: (_) => submit()), const SizedBox(height: 20),
    FilledButton(onPressed: busy ? null : submit, child: Padding(padding: const EdgeInsets.all(12), child: Text(busy ? 'Đang đăng nhập…' : 'Đăng nhập'))),
  ]))))));
}

class EvaluationScreen extends StatefulWidget {
  const EvaluationScreen({super.key, required this.client, required this.onLogout}); final ApiClient client; final VoidCallback onLogout;
  @override State<EvaluationScreen> createState() => _EvaluationState();
}
class _EvaluationState extends State<EvaluationScreen> {
  List<dynamic> years = []; int? yearId; Map<String, dynamic>? evaluation; bool loading = true; String? error; bool empty = false;
  @override void initState() { super.initState(); loadYears(); }
  Future<void> loadYears() async { setState(() { loading = true; error = null; }); try { final data = await widget.client.get('/lookups'); years = data['academicYears'] as List<dynamic>; if (years.isNotEmpty) { yearId = years.first['id'] as int; await loadEvaluation(); } } catch (e) { error = e.toString(); } finally { if (mounted) setState(() => loading = false); } }
  Future<void> loadEvaluation() async { if (yearId == null) return; setState(() { loading = true; error = null; empty = false; evaluation = null; }); try { evaluation = Map<String, dynamic>.from(await widget.client.get('/evaluations/me?academicYearId=$yearId')); } on ApiException catch (e) { if (e.status == 404) { empty = true; } else { error = e.message; } } finally { if (mounted) setState(() => loading = false); } }
  @override Widget build(BuildContext context) => Scaffold(appBar: AppBar(title: const Text('Kết quả nghiên cứu'), actions: [IconButton(onPressed: widget.onLogout, tooltip: 'Đăng xuất', icon: const Icon(Icons.logout))]), body: RefreshIndicator(onRefresh: loadEvaluation, child: ListView(padding: const EdgeInsets.all(20), children: [
    DropdownButtonFormField<int>(initialValue: yearId, decoration: const InputDecoration(labelText: 'Năm học', border: OutlineInputBorder()), items: years.map((x) => DropdownMenuItem<int>(value: x['id'] as int, child: Text(x['code'] as String))).toList(), onChanged: (v) { yearId = v; loadEvaluation(); }), const SizedBox(height: 20),
    if (loading) const Center(child: Padding(padding: EdgeInsets.all(32), child: CircularProgressIndicator())) else if (error != null) _Message(icon: Icons.error_outline, title: 'Không tải được kết quả', text: error!, action: loadEvaluation) else if (empty) const _Message(icon: Icons.assignment_outlined, title: 'Chưa có đánh giá', text: 'Năm học này chưa có kết quả đánh giá đã chốt.') else if (evaluation != null) EvaluationView(data: evaluation!),
  ])));
}

class EvaluationView extends StatelessWidget {
  const EvaluationView({super.key, required this.data}); final Map<String, dynamic> data;
  @override Widget build(BuildContext context) { final details = data['details'] as List<dynamic>? ?? []; return Column(crossAxisAlignment: CrossAxisAlignment.stretch, children: [
    Card(color: const Color(0xFF173F32), child: Padding(padding: const EdgeInsets.all(20), child: Column(crossAxisAlignment: CrossAxisAlignment.start, children: [Text(data['classification'] ?? 'Đã chốt', style: const TextStyle(color: Colors.white, fontSize: 24, fontWeight: FontWeight.bold)), const SizedBox(height: 8), Text(data['classificationBasis'] ?? '', style: const TextStyle(color: Color(0xFFD5E5DB)))]))), const SizedBox(height: 12),
    Row(children: [Expanded(child: _Metric(label: 'Giờ quy đổi', value: '${data['totalHours']}')), const SizedBox(width: 12), Expanded(child: _Metric(label: 'Điểm quy đổi', value: '${data['totalPoints']}'))]), const SizedBox(height: 22),
    Text('Chi tiết quy đổi', style: Theme.of(context).textTheme.titleLarge?.copyWith(fontWeight: FontWeight.bold)), const SizedBox(height: 8),
    if (details.isEmpty) const Text('Không có sản phẩm đủ điều kiện quy đổi.') else ...details.map((raw) { final x = raw as Map<String, dynamic>; return Card(child: ListTile(title: Text(x['productTitle'] ?? ''), subtitle: Text('${x['ruleCode']} phiên bản ${x['ruleVersion']}\n${x['calculationBasis']}'), isThreeLine: true, trailing: Text('${x['convertedValue']}\n${x['unit']}', textAlign: TextAlign.right, style: const TextStyle(fontWeight: FontWeight.bold)))); }),
  ]); }
}
class _Metric extends StatelessWidget { const _Metric({required this.label, required this.value}); final String label; final String value; @override Widget build(BuildContext context) => Card(child: Padding(padding: const EdgeInsets.all(18), child: Column(children: [Text(label), const SizedBox(height: 8), Text(value, style: Theme.of(context).textTheme.headlineSmall?.copyWith(fontWeight: FontWeight.bold))]))); }
class _Message extends StatelessWidget { const _Message({required this.icon, required this.title, required this.text, this.action}); final IconData icon; final String title; final String text; final Future<void> Function()? action; @override Widget build(BuildContext context) => Center(child: Padding(padding: const EdgeInsets.symmetric(vertical: 48), child: Column(children: [Icon(icon, size: 54, color: Colors.grey), const SizedBox(height: 14), Text(title, style: Theme.of(context).textTheme.titleLarge), const SizedBox(height: 8), Text(text, textAlign: TextAlign.center), if (action != null) ...[const SizedBox(height: 16), OutlinedButton(onPressed: action, child: const Text('Thử lại'))]]))); }
