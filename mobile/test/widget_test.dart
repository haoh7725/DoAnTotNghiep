import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:research_management_mobile/main.dart';

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
}
