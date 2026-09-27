import 'package:flutter/material.dart';

void main() => runApp(const ResearchManagementApp());

class ResearchManagementApp extends StatelessWidget {
  const ResearchManagementApp({super.key});

  @override
  Widget build(BuildContext context) {
    return MaterialApp(
      title: 'ResearchHub',
      debugShowCheckedModeBanner: false,
      theme: ThemeData(
        colorScheme: ColorScheme.fromSeed(seedColor: const Color(0xFF177158)),
        useMaterial3: true,
      ),
      home: const HomeScreen(),
    );
  }
}

class HomeScreen extends StatelessWidget {
  const HomeScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: const Text('ResearchHub')),
      body: const Padding(
        padding: EdgeInsets.all(24),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text('HỒ SƠ NGHIÊN CỨU KHOA HỌC', style: TextStyle(fontWeight: FontWeight.w700)),
            SizedBox(height: 16),
            Text('Theo dõi tiến độ nghiên cứu của bạn', style: TextStyle(fontSize: 30, fontWeight: FontWeight.w700)),
            SizedBox(height: 12),
            Text('Ứng dụng mobile sẽ dùng chung REST API với hệ thống Web.'),
          ],
        ),
      ),
    );
  }
}
