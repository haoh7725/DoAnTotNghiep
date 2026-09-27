import 'package:flutter_test/flutter_test.dart';
import 'package:research_management_mobile/main.dart';

void main() {
  testWidgets('shows the mobile project identity', (tester) async {
    await tester.pumpWidget(const ResearchManagementApp());

    expect(find.text('ResearchHub'), findsOneWidget);
    expect(find.text('Theo dõi tiến độ nghiên cứu của bạn'), findsOneWidget);
  });
}
