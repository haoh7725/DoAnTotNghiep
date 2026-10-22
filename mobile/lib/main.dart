import 'package:flutter/material.dart';
import 'api_client.dart';
import 'home_screen.dart';
import 'session_storage.dart';

void main() => runApp(const ResearchManagementApp());

class ResearchManagementApp extends StatefulWidget {
  const ResearchManagementApp({super.key});
  @override
  State<ResearchManagementApp> createState() => _AppState();
}

class _AppState extends State<ResearchManagementApp> {
  String? token;
  bool checkingSession = true;

  @override
  void initState() {
    super.initState();
    //checkingSession = false;
    restoreSession();
  }

  Future<void> restoreSession() async {
    try {
      final savedToken = await SessionStorage.readToken();

      if (savedToken != null && savedToken.isNotEmpty) {
        await ApiClient(savedToken).getMe();

        if (!mounted) return;
        setState(() => token = savedToken);
      }
    } catch (_) {
      await SessionStorage.clearToken();
    } finally {
      if (mounted) {
        setState(() => checkingSession = false);
      }
    }
  }

  Future<void> handleLogin(String newToken) async {
    await SessionStorage.saveToken(newToken);

    if (!mounted) return;
    setState(() => token = newToken);
  }

  Future<void> handleLogout() async {
    await SessionStorage.clearToken();

    if (!mounted) return;
    setState(() => token = null);
  }

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ResearchHub',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF177158)),
        useMaterial3: true,
      ),
      home: checkingSession
          ? const Scaffold(body: Center(child: CircularProgressIndicator()))
          : token == null
          ? LoginScreen(onLogin: handleLogin)
          : HomeScreen(client: ApiClient(token!), onLogout: handleLogout),
    );
  }
}

// ---------------------------------------------------------------------------
// LoginScreen — preserved EXACTLY as authored by Phi
// ---------------------------------------------------------------------------

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.onLogin});
  final Future<void> Function(String) onLogin;
  @override
  State<LoginScreen> createState() => _LoginState();
}

class _LoginState extends State<LoginScreen> {
  final form = GlobalKey<FormState>();
  final username = TextEditingController();
  final password = TextEditingController();
  bool busy = false;
  String? error;

  Future<void> submit() async {
    if (!form.currentState!.validate()) return;
    setState(() {
      busy = true;
      error = null;
    });
    try {
      final accessToken = await ApiClient.login(
        username.text.trim(),
        password.text,
      );
      await widget.onLogin(accessToken);
    } catch (e) {
      setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    body: SafeArea(
      child: Center(
        child: SingleChildScrollView(
          padding: const EdgeInsets.all(24),
          child: ConstrainedBox(
            constraints: const BoxConstraints(maxWidth: 420),
            child: Form(
              key: form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.stretch,
                children: [
                  const Icon(
                    Icons.science_outlined,
                    size: 64,
                    color: Color(0xFF177158),
                  ),
                  const SizedBox(height: 18),
                  Text(
                    'ResearchHub',
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 8),
                  const Text(
                    'Đăng nhập để xem kết quả nghiên cứu',
                    textAlign: TextAlign.center,
                  ),
                  const SizedBox(height: 28),
                  if (error != null)
                    Card(
                      color: Theme.of(context).colorScheme.errorContainer,
                      child: Padding(
                        padding: const EdgeInsets.all(14),
                        child: Text(error!),
                      ),
                    ),
                  TextFormField(
                    controller: username,
                    decoration: const InputDecoration(
                      labelText: 'Tên đăng nhập',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) => v == null || v.trim().isEmpty
                        ? 'Nhập tên đăng nhập'
                        : null,
                  ),
                  const SizedBox(height: 14),
                  TextFormField(
                    controller: password,
                    obscureText: true,
                    decoration: const InputDecoration(
                      labelText: 'Mật khẩu',
                      border: OutlineInputBorder(),
                    ),
                    validator: (v) =>
                        v == null || v.isEmpty ? 'Nhập mật khẩu' : null,
                    onFieldSubmitted: (_) => submit(),
                  ),
                  const SizedBox(height: 20),
                  FilledButton(
                    onPressed: busy ? null : submit,
                    child: Padding(
                      padding: const EdgeInsets.all(12),
                      child: Text(busy ? 'Đang đăng nhập…' : 'Đăng nhập'),
                    ),
                  ),
                ],
              ),
            ),
          ),
        ),
      ),
    ),
  );
}
