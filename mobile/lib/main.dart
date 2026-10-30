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
  ApiClient? _client;
  bool checkingSession = true;
  String? _sessionError;
  String? _loginMessage;
  int _navigationVersion = 0;

  @override
  void initState() {
    super.initState();
    restoreSession();
  }

  ApiClient _createClient(String accessToken) {
    late final ApiClient client;

    client = ApiClient(
      accessToken,
      onUnauthorized: () async {
        // Bỏ qua lỗi từ yêu cầu thuộc phiên cũ.
        if (!mounted || !identical(_client, client)) return;

        await _endSession(
          message: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        );
      },
    );

    return client;
  }

  Future<void> restoreSession() async {
    setState(() {
      checkingSession = true;
      _sessionError = null;
    });

    try {
      final savedToken = await SessionStorage.readToken();

      if (savedToken == null || savedToken.isEmpty) {
        if (!mounted) return;
        setState(() => _client = null);
        return;
      }

      // Chỉ kiểm tra token ở bước khôi phục.
      await ApiClient(savedToken).getMe();

      if (!mounted) return;
      setState(() {
        _client = _createClient(savedToken);
        _loginMessage = null;
      });
    } on ApiException catch (e) {
      if (e.status == 401) {
        await _endSession(
          message: 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
        );
      } else {
        // Lỗi mạng hoặc lỗi máy chủ: giữ token để thử lại.
        if (!mounted) return;
        setState(() => _sessionError = e.message);
      }
    } catch (_) {
      if (!mounted) return;
      setState(() {
        _sessionError =
            'Không kiểm tra được phiên đăng nhập. Vui lòng thử lại.';
      });
    } finally {
      if (mounted) {
        setState(() => checkingSession = false);
      }
    }
  }

  Future<void> handleLogin(String newToken) async {
    await SessionStorage.saveToken(newToken);

    if (!mounted) return;
    setState(() {
      _client = _createClient(newToken);
      _loginMessage = null;
      _sessionError = null;
      _navigationVersion++;
    });
  }

  Future<void> handleLogout() => _endSession();

  Future<void> _endSession({String? message}) async {
    // Đóng cả các trang chi tiết đang mở của phiên hiện tại.
    if (!mounted) return;
    setState(() {
      _client = null;
      checkingSession = true;
      _navigationVersion++;
    });

    String? storageError;

    try {
      await SessionStorage.clearToken();
    } catch (_) {
      storageError =
          'Chưa xóa được phiên đã lưu trên thiết bị. '
          'Vui lòng bấm Đăng xuất và đăng nhập lại để thử lại.';
    }

    if (!mounted) return;
    setState(() {
      _loginMessage = message;
      _sessionError = storageError;
      checkingSession = false;
    });
  }

  @override
  Widget build(BuildContext context) {
    Widget screen;

    if (checkingSession) {
      screen = const Scaffold(body: Center(child: CircularProgressIndicator()));
    } else if (_sessionError != null) {
      screen = Scaffold(
        body: SafeArea(
          child: Center(
            child: Padding(
              padding: const EdgeInsets.all(24),
              child: Column(
                mainAxisSize: MainAxisSize.min,
                children: [
                  Text(_sessionError!, textAlign: TextAlign.center),
                  const SizedBox(height: 16),
                  FilledButton(
                    onPressed: restoreSession,
                    child: const Text('Thử lại'),
                  ),
                  TextButton(
                    onPressed: handleLogout,
                    child: const Text('Đăng xuất và đăng nhập lại'),
                  ),
                ],
              ),
            ),
          ),
        ),
      );
    } else if (_client == null) {
      screen = LoginScreen(onLogin: handleLogin, message: _loginMessage);
    } else {
      screen = HomeScreen(client: _client!, onLogout: handleLogout);
    }

    return MaterialApp(
      key: ValueKey(_navigationVersion),
      title: 'ResearchHub',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF177158)),
        useMaterial3: true,
      ),
      home: screen,
    );
  }
}

// ---------------------------------------------------------------------------
// LoginScreen — preserved EXACTLY as authored by Phi
// ---------------------------------------------------------------------------

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key, required this.onLogin, this.message});

  final Future<void> Function(String) onLogin;
  final String? message;
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
    if (busy) return;
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
      if (mounted) {
        setState(() => error = e.toString());
      }
    } finally {
      if (mounted) setState(() => busy = false);
    }
  }

  @override
  void dispose() {
    username.dispose();
    password.dispose();
    super.dispose();
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
                  if (widget.message != null)
                    Padding(
                      padding: const EdgeInsets.only(bottom: 12),
                      child: Text(widget.message!, textAlign: TextAlign.center),
                    ),
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
