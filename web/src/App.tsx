import './App.css'

const features = [
  ['01', 'Hồ sơ khoa học', 'Quản lý lý lịch, đề tài, bài báo, sách và chứng nhận.'],
  ['02', 'Kế hoạch và tiến độ', 'Đăng ký nội dung, cập nhật tiến độ và nhận cảnh báo hạn.'],
  ['03', 'Xét duyệt minh chứng', 'Gửi duyệt, phản hồi, bổ sung và lưu lịch sử phiên bản.'],
  ['04', 'Báo cáo đánh giá', 'Quy đổi giờ nghiên cứu, thống kê và xuất Excel hoặc PDF.'],
]

function App() {
  return (
    <main className="app-shell">
      <header className="topbar">
        <div><span className="eyebrow">HUIT</span><strong>ResearchHub</strong></div>
        <span className="status">Đang xây dựng</span>
      </header>
      <section className="hero">
        <p className="eyebrow">Quản lý tập trung · Theo dõi minh bạch</p>
        <h1>Hồ sơ nghiên cứu khoa học của giảng viên</h1>
        <p className="lead">Lập kế hoạch, nộp minh chứng, xét duyệt sản phẩm và theo dõi chỉ tiêu trên cùng một hệ thống.</p>
      </section>
      <section className="feature-grid" aria-label="Các phân hệ chính">
        {features.map(([number, title, description]) => (
          <article className="feature-card" key={number}>
            <span>{number}</span><h2>{title}</h2><p>{description}</p>
          </article>
        ))}
      </section>
    </main>
  )
}

export default App
