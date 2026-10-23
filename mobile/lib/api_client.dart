import 'dart:convert';
import 'dart:io';

const apiUrl = String.fromEnvironment(
  'API_URL',
  defaultValue: 'http://10.0.2.2:8081/api',
);

class ApiClient {
  ApiClient(this.token);
  final String token;

  Future<dynamic> get(String path) => _request('GET', path);
  Future<dynamic> post(String path, [Object? body]) =>
      _request('POST', path, body);
  Future<dynamic> put(String path, [Object? body]) =>
      _request('PUT', path, body);

  Future<dynamic> _request(String method, String path, [Object? body]) async {
    final client = HttpClient();
    try {
      final request = await client.openUrl(method, Uri.parse('$apiUrl$path'));
      request.headers.set(HttpHeaders.acceptHeader, 'application/json');
      request.headers.set(HttpHeaders.authorizationHeader, 'Bearer $token');
      request.headers.set('X-Requested-With', 'ResearchHub');
      if (body != null) {
        request.headers.contentType = ContentType.json;
        request.write(jsonEncode(body));
      }
      final response = await request.close();
      final text = await response.transform(utf8.decoder).join();
      final data = text.isEmpty ? null : jsonDecode(text);
      if (response.statusCode < 200 || response.statusCode >= 300) {
        throw ApiException(
          response.statusCode,
          data is Map
              ? (data['detail'] ?? data['title'] ?? 'Không thể xử lý yêu cầu.')
              : 'Không thể xử lý yêu cầu.',
        );
      }
      return data;
    } finally {
      client.close();
    }
  }

  static Future<String> login(String username, String password) async {
    final client = HttpClient();
    try {
      final request = await client.postUrl(Uri.parse('$apiUrl/auth/login'));
      request.headers.contentType = ContentType.json;
      request.headers.set('X-Requested-With', 'ResearchHub');
      request.write(jsonEncode({'username': username, 'password': password}));
      final response = await request.close();
      final text = await response.transform(utf8.decoder).join();
      final data = text.isEmpty ? null : jsonDecode(text);
      if (response.statusCode != 200) {
        throw ApiException(
          response.statusCode,
          data is Map
              ? (data['detail'] ?? data['title'] ?? 'Đăng nhập thất bại.')
              : 'Đăng nhập thất bại.',
        );
      }
      return data['accessToken'] as String;
    } finally {
      client.close();
    }
  }

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

  // Research Plans / Dashboard
  Future<dynamic> getResearchPlan(int lecturerId, int academicYearId) => get(
    '/research-plans?lecturerId=$lecturerId&academicYearId=$academicYearId',
  );

  Future<dynamic> getResearchPlanItems(int researchPlanId) =>
      get('/research-plans/$researchPlanId/items');

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
