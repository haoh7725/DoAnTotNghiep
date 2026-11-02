import 'package:flutter_test/flutter_test.dart';
import 'package:flutter/material.dart';
import 'package:research_management_mobile/main.dart';

void main() {
  testWidgets('shows the mobile project identity', (tester) async {
    await tester.pumpWidget(MaterialApp(home: LoginScreen(onLogin: (_) async {})));

    expect(find.text('ResearchHub'), findsOneWidget);
    expect(find.text('Đăng nhập để xem kết quả nghiên cứu'), findsOneWidget);
  });
}
