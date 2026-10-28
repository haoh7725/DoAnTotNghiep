import 'dart:async';
import 'dart:convert';
import 'dart:io';

const apiUrl = String.fromEnvironment(
  'API_URL',
  defaultValue: 'http://10.0.2.2:8081/api',
);

class ApiClient {
  ApiClient(this.token, {this.onUnauthorized});

  final String token;
  final Future<void> Function()? onUnauthorized;

  bool _unauthorizedHandled = false;

  Future<dynamic> getMyDepartmentTargetProgress(int academicYearId) => get(
    '/research-progress/targets/my-department'
    '?academicYearId=$academicYearId',
  );

  Future<dynamic> getMyResearchProgress(int academicYearId) =>
      get('/research-progress/me?academicYearId=$academicYearId');

  Future<dynamic> getMyProductTargetProgress(int academicYearId) =>
      get('/research-progress/targets/me?academicYearId=$academicYearId');

  Future<dynamic> get(String path) => _request('GET', path);
  Future<dynamic> post(String path, [Object? body]) =>
      _request('POST', path, body);
  Future<dynamic> put(String path, [Object? body]) =>
      _request('PUT', path, body);

  static Future<T> _withClient<T>(Future<T> Function(HttpClient) action) async {
    final client = HttpClient();
    client.connectionTimeout = const Duration(seconds: 15);
    try {
      return await action(client).timeout(const Duration(seconds: 30));
    } on TimeoutException {
      throw ApiException(408, 'Yêu cầu quá thời gian chờ. Vui lòng thử lại.');
    } on SocketException {
      throw ApiException(
        503,
        'Không kết nối được máy chủ. '
        'Vui lòng kiểm tra kết nối và thử lại.',
      );
    } on HttpException {
      throw ApiException(
        503,
        'Kết nối với máy chủ bị gián đoạn. Vui lòng thử lại.',
      );
    } finally {
      client.close(force: true);
    }
  }

  static Future<dynamic> _readResponse(HttpClientResponse response) async {
    final text = await response.transform(utf8.decoder).join();
    final success = response.statusCode >= 200 && response.statusCode < 300;

    dynamic data;

    if (text.trim().isNotEmpty) {
      try {
        data = jsonDecode(text);
      } on FormatException {
        if (success) {
          throw ApiException(
            502,
            'Máy chủ trả về dữ liệu không hợp lệ. Vui lòng thử lại.',
          );
        }
      }
    }

    if (!success) {
      String message;

      switch (response.statusCode) {
        case 401:
          message = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.';
          break;
        case 403:
          message = 'Bạn không có quyền thực hiện thao tác này.';
          break;
        case 404:
          message = 'Không tìm thấy dữ liệu hoặc chức năng yêu cầu.';
          break;
        default:
          message = 'Không thể xử lý yêu cầu (HTTP ${response.statusCode}).';
      }

      if (data is Map) {
        final detail = data['detail'] ?? data['title'];
        if (detail is String && detail.trim().isNotEmpty) {
          message = detail;
        }
      }

      throw ApiException(response.statusCode, message);
    }

    return data;
  }

  Future<dynamic> _request(String method, String path, [Object? body]) =>
      _withClient((client) async {
        final request = await client.openUrl(method, Uri.parse('$apiUrl$path'));

        request.headers.set(HttpHeaders.acceptHeader, 'application/json');
        request.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token');
        request.headers.set('X-Requested-With', 'ResearchHub');

        if (body != null) {
          request.headers.contentType = ContentType.json;
          request.write(jsonEncode(body));
        }

        final response = await request.close();

        if (response.statusCode == 401) {
          if (!_unauthorizedHandled) {
            _unauthorizedHandled = true;
            await onUnauthorized?.call();
          }

          throw ApiException(
            401,
            'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.',
          );
        }

        return await _readResponse(response);
      });

  static Future<String> login(String username, String password) => _withClient((
    client,
  ) async {
    final request = await client.postUrl(Uri.parse('$apiUrl/auth/login'));

    request.headers.contentType = ContentType.json;
    request.headers.set(HttpHeaders.acceptHeader, 'application/json');
    request.headers.set('X-Requested-With', 'ResearchHub');

    request.write(jsonEncode({'username': username, 'password': password}));

    final response = await request.close();

    // 401 ở màn hình đăng nhập là thông tin đăng nhập
    // không hợp lệ, chưa phải phiên đang dùng bị hết hạn.
    if (response.statusCode == 401) {
      throw ApiException(401, 'Tên đăng nhập hoặc mật khẩu không đúng.');
    }

    final data = await _readResponse(response);
    final accessToken = data is Map ? data['accessToken'] : null;

    if (accessToken is! String || accessToken.trim().isEmpty) {
      throw ApiException(502, 'Máy chủ chưa trả về phiên đăng nhập hợp lệ.');
    }

    return accessToken;
  });

  // Auth / Profile
  Future<dynamic> getMe() => get('/auth/me');

  // Lecturer profile
  Future<dynamic> getLecturerMe() => get('/lecturers/me');

  Future<dynamic> getScientificProfile(int lecturerId) =>
      get('/lecturers/$lecturerId/scientific-profile');

  // Lookups
  Future<dynamic> getLookups() => get('/lookups');

  // Products
  Future<List<dynamic>> getMyProducts() async {
    final data = await get('/products/mine');
    return data as List<dynamic>;
  }

  Future<dynamic> getProduct(int id) => get('/products/$id');

  Future<dynamic> updateProduct(int id, Map<String, dynamic> data) =>
      put('/products/$id', data);

  Future<dynamic> submitProduct(int id) => post('/products/$id/submit');

  Future<List<dynamic>> getReviewHistory(int id) async {
    final data = await get('/products/$id/review-history');
    return data as List<dynamic>;
  }

  Future<List<dynamic>> getProductSnapshots(int id) async {
    final data = await get('/products/$id/snapshots');
    return data as List<dynamic>;
  }

  // Evaluations / Emulation
  Future<dynamic> getMyEvaluation(int academicYearId) =>
      get('/evaluations/me?academicYearId=$academicYearId');

  // Research plans
  Future<dynamic> getResearchPlan(int id) => get('/research-plans/$id');

  Future<dynamic> getResearchPlanItem(int id) =>
      get('/research-plan-items/$id');

  Future<List<dynamic>> getResearchPlanItems(int planId) async {
    final data = await get('/research-plans/$planId/items');
    return data as List<dynamic>;
  }

  // Notifications
  Future<List<dynamic>> getNotifications() async {
    final data = await get('/notifications');
    return data as List<dynamic>;
  }

  Future<int> getUnreadNotificationCount() async {
    final data = await get('/notifications/unread-count');
    return (data['unreadCount'] as num).toInt();
  }

  Future<dynamic> markNotificationAsRead(int id) =>
      put('/notifications/$id/read');

  Future<dynamic> markAllNotificationsAsRead() =>
      put('/notifications/read-all');
}

class ApiException implements Exception {
  ApiException(this.status, this.message);
  final int status;
  final String message;
  @override
  String toString() => message;
}
