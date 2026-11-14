import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:research_management_mobile/main.dart';
import 'package:research_management_mobile/api_client.dart';
import 'package:research_management_mobile/research_progress_screen.dart';

void main() {
  test('mobile accepts only lecturer-linked working roles', () {
    expect(
      canUseMobileProfile({
        'roles': [
          {'role': 'GIANG_VIEN'},
        ],
      }),
      isTrue,
    );
    expect(
      canUseMobileProfile({
        'roles': [
          {'role': 'PHONG_QLKH'},
        ],
      }),
      isFalse,
    );
    expect(canUseMobileProfile({'roles': []}), isFalse);
  });

  testWidgets('shows the mobile project identity', (tester) async {
    await tester.pumpWidget(
      MaterialApp(home: LoginScreen(onLogin: (_) async {})),
    );

    expect(find.text('ResearchHub'), findsOneWidget);
    expect(find.text('Đăng nhập để xem kết quả nghiên cứu'), findsOneWidget);
  });

  testWidgets('shows assigned targets separately from plan commitments', (
    tester,
  ) async {
    await tester.pumpWidget(
      MaterialApp(home: ResearchProgressScreen(client: _ProgressClient())),
    );
    await tester.pumpAndSettle();
    await tester.scrollUntilVisible(
      find.text('Chỉ tiêu được giao cho tôi'),
      300,
    );
    await tester.pumpAndSettle();
    expect(find.text('Chỉ tiêu được giao cho tôi'), findsOneWidget);
    expect(find.text('Đề tài · Chỉ tiêu #12'), findsOneWidget);
    expect(find.text('Được giao: 2 sản phẩm'), findsOneWidget);
    expect(find.text('Đã liên kết: 1 · Đã hoàn thành: 0'), findsOneWidget);
    expect(find.textContaining('Chỉ tiêu bài báo:'), findsNothing);
  });
}

class _ProgressClient extends ApiClient {
  _ProgressClient() : super('test');

  @override
  Future<dynamic> get(String path) async {
    if (path.contains('lookups')) {
      return {
        'academicYears': [
          {
            'id': 1,
            'code': '2026-2027',
            'startDate': '2026-08-01',
            'endDate': '2027-07-31',
          },
        ],
      };
    }
    if (path.contains('lecturer-target-allocations/mine')) {
      return [
        {
          'targetId': 12,
          'productTypeName': 'Đề tài',
          'allocatedArticleCount': 2,
          'registeredArticles': 1,
          'approvedArticles': 0,
          'completionPercent': 0,
          'linkedProducts': <dynamic>[],
        },
      ];
    }
    if (path.contains('my-department')) {
      return {
        'hasAllocation': true,
        'allocatedArticles': 8,
        'approvedArticles': 3,
        'completionPercent': 37.5,
      };
    }
    if (path.contains('targets/me')) {
      return {
        'hasPlan': true,
        'hasRegisteredPlan': true,
        'committedProducts': 4,
        'approvedProducts': 2,
        'completionPercent': 50,
      };
    }
    return {
      'hasFinalizedEvaluation': false,
      'lecturerName': 'Giảng viên',
      'requiredHours': 200,
    };
  }
}
