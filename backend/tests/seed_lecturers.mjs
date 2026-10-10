// Seed realistic lecturer accounts with profiles and scientific CVs
const BASE_URL = 'http://localhost:8081/api';
const ADMIN_USER = 'admin';
const ADMIN_PASS = 'change_me';
const DEFAULT_PASS = 'ResearchHub@2026';

async function main() {
  console.log('=== 1. ĐĂNG NHẬP QUẢN TRỊ ===');
  const loginRes = await fetch(`${BASE_URL}/auth/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json', 'X-Requested-With': 'ResearchHub' },
    body: JSON.stringify({ username: ADMIN_USER, password: ADMIN_PASS })
  });
  if (!loginRes.ok) {
    throw new Error(`Đăng nhập thất bại: ${loginRes.status} ${await loginRes.text()}`);
  }
  const loginData = await loginRes.json();
  const token = loginData.accessToken;
  console.log('Đăng nhập thành công! Token:', token.substring(0, 20) + '...');

  const api = async (path, method = 'GET', body = null) => {
    const opts = {
      method,
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`,
        'X-Requested-With': 'ResearchHub'
      }
    };
    if (body) opts.body = JSON.stringify(body);
    const res = await fetch(`${BASE_URL}${path}`, opts);
    if (!res.ok) {
      const err = await res.text();
      throw new Error(`Lỗi ${method} ${path}: ${res.status} ${err}`);
    }
    if (res.status === 204) return null;
    return res.json().catch(() => null);
  };

  console.log('\n=== 2. KHỞI TẠO DANH MỤC KHOA ===');
  const existingFaculties = await api('/admin/faculties') || [];
  const facultiesToSeed = [
    { code: 'CNTT', name: 'Khoa Công nghệ thông tin' },
    { code: 'DDT', name: 'Khoa Điện - Điện tử' },
    { code: 'CNTP', name: 'Khoa Công nghệ thực phẩm' }
  ];
  const facultyMap = {};
  for (const f of facultiesToSeed) {
    let item = existingFaculties.find(x => x.code === f.code);
    if (!item) {
      item = await api('/admin/faculties', 'POST', f);
      console.log(`+ Đã tạo khoa: ${f.name} (ID: ${item.id})`);
    } else {
      console.log(`- Khoa ${f.name} đã có (ID: ${item.id})`);
    }
    facultyMap[f.code] = item.id;
  }

  console.log('\n=== 3. KHỞI TẠO DANH MỤC BỘ MÔN ===');
  const existingDepts = await api('/admin/departments') || [];
  const deptsToSeed = [
    { facultyCode: 'CNTT', code: 'CNPM', name: 'Bộ môn Công nghệ phần mềm' },
    { facultyCode: 'CNTT', code: 'KHMT', name: 'Bộ môn Khoa học máy tính' },
    { facultyCode: 'CNTT', code: 'HTTT', name: 'Bộ môn Hệ thống thông tin' },
    { facultyCode: 'DDT', code: 'TDH', name: 'Bộ môn Tự động hóa' },
    { facultyCode: 'DDT', code: 'KTDT', name: 'Bộ môn Kỹ thuật Điện tử' },
    { facultyCode: 'CNTP', code: 'CNCB', name: 'Bộ môn Công nghệ chế biến' }
  ];
  const deptMap = {};
  for (const d of deptsToSeed) {
    let item = existingDepts.find(x => x.code === d.code);
    if (!item) {
      item = await api('/admin/departments', 'POST', {
        facultyId: facultyMap[d.facultyCode],
        code: d.code,
        name: d.name
      });
      console.log(`+ Đã tạo bộ môn: ${d.name} (ID: ${item.id})`);
    } else {
      console.log(`- Bộ môn ${d.name} đã có (ID: ${item.id})`);
    }
    deptMap[d.code] = item.id;
  }

  console.log('\n=== 4. KHỞI TẠO NĂM HỌC ===');
  const existingYears = await api('/admin/academic-years') || [];
  const yearsToSeed = [
    { code: '2024-2025', startDate: '2024-09-01', endDate: '2025-08-31' },
    { code: '2025-2026', startDate: '2025-09-01', endDate: '2026-08-31' },
    { code: '2026-2027', startDate: '2026-09-01', endDate: '2027-08-31' }
  ];
  for (const y of yearsToSeed) {
    let item = existingYears.find(x => x.code === y.code);
    if (!item) {
      item = await api('/admin/academic-years', 'POST', y);
      console.log(`+ Đã tạo năm học: ${y.code}`);
    } else {
      console.log(`- Năm học ${y.code} đã có`);
    }
  }

  console.log('\n=== 5. KHỞI TẠO TÀI KHOẢN, HỒ SƠ & LÝ LỊCH KHOA HỌC ===');
  const lecturersData = [
    {
      username: 'truongkhoa.cntt',
      fullName: 'Nguyễn Văn Hùng',
      email: 'hungnv@huit.edu.vn',
      phone: '0912345601',
      birthDate: '1978-05-12',
      gender: 'NAM',
      code: 'GV001',
      deptCode: 'CNPM',
      facCode: 'CNTT',
      academicRank: 'Phó giáo sư',
      degree: 'Tiến sĩ',
      position: 'Trưởng khoa',
      role: 'TRUONG_KHOA',
      scope: 'KHOA',
      profile: {
        expertise: 'Kỹ thuật phần mềm và Hệ thống tính toán phân tán',
        researchFields: 'Khoa học máy tính, Công nghệ phần mềm',
        researchDirections: 'Kiến trúc microservices quy mô lớn; Tối ưu hóa hiệu năng điện toán đám mây (Cloud Computing); Điện toán biên (Edge Computing)',
        activitySummary: 'Chủ nhiệm 02 đề tài cấp Bộ, 05 đề tài cấp Cơ sở; Công bố hơn 30 bài báo quốc tế trên hệ thống ISI/Scopus (Q1/Q2); Tác giả 02 giáo trình chuyên ngành Kỹ thuật phần mềm đã xuất bản.'
      }
    },
    {
      username: 'truongbm.cnpm',
      fullName: 'Trần Thị Mai',
      email: 'maitt@huit.edu.vn',
      phone: '0912345602',
      birthDate: '1983-11-20',
      gender: 'NU',
      code: 'GV002',
      deptCode: 'CNPM',
      facCode: 'CNTT',
      academicRank: null,
      degree: 'Tiến sĩ',
      position: 'Trưởng bộ môn',
      role: 'TRUONG_BO_MON',
      scope: 'BO_MON',
      profile: {
        expertise: 'Công nghệ phần mềm và Đảm bảo chất lượng phần mềm',
        researchFields: 'Công nghệ thông tin, Kiểm thử phần mềm',
        researchDirections: 'Kiểm thử phần mềm tự động dựa trên AI; Phân tích và phát hiện lỗ hổng bảo mật mã nguồn; Phương pháp phát triển Agile/DevOps',
        activitySummary: 'Chủ nhiệm 03 đề tài NCKH cấp Trường trọng điểm; Hơn 15 bài báo đăng trên tạp chí quốc tế và kỷ yếu hội thảo uy tín (IEEE, ACM); Hướng dẫn thành công 12 học viên cao học.'
      }
    },
    {
      username: 'gv.tuan',
      fullName: 'Lê Văn Tuấn',
      email: 'tuanlv@huit.edu.vn',
      phone: '0912345603',
      birthDate: '1986-08-15',
      gender: 'NAM',
      code: 'GV003',
      deptCode: 'CNPM',
      facCode: 'CNTT',
      academicRank: null,
      degree: 'Tiến sĩ',
      position: 'Giảng viên chính',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Kỹ thuật dữ liệu và Phát triển ứng dụng Web/Mobile',
        researchFields: 'Khoa học máy tính, Trí tuệ nhân tạo',
        researchDirections: 'Xử lý dữ liệu lớn thời gian thực; Hệ thống khuyến nghị (Recommender Systems); Tích hợp mô hình AI trên thiết bị di động',
        activitySummary: 'Đã xuất bản 08 bài báo Scopus; Tham gia 02 đề tài cấp Bộ và 04 đề tài cấp Cơ sở; Đạt giải thưởng Nhà giáo trẻ tiêu biểu cấp Trường năm 2024.'
      }
    },
    {
      username: 'gv.ha',
      fullName: 'Phạm Thu Hà',
      email: 'hapt@huit.edu.vn',
      phone: '0912345604',
      birthDate: '1991-03-28',
      gender: 'NU',
      code: 'GV004',
      deptCode: 'CNPM',
      facCode: 'CNTT',
      academicRank: null,
      degree: 'Thạc sĩ',
      position: 'Giảng viên',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Tương tác người - máy (HCI) và Thiết kế UI/UX',
        researchFields: 'Công nghệ thông tin, Tương tác người máy',
        researchDirections: 'Trải nghiệm người dùng thông minh trong ứng dụng giáo dục; Khả năng tiếp cận số (Digital Accessibility); Thiết kế hệ thống thông tin hướng người dùng',
        activitySummary: 'Đã công bố 05 bài báo khoa học tại các hội thảo chuyên ngành trong nước và quốc tế; Tham gia thực hiện 03 đề tài NCKH cấp cơ sở.'
      }
    },
    {
      username: 'gv.quan',
      fullName: 'Đỗ Minh Quân',
      email: 'quandm@huit.edu.vn',
      phone: '0912345605',
      birthDate: '1980-01-10',
      gender: 'NAM',
      code: 'GV005',
      deptCode: 'KHMT',
      facCode: 'CNTT',
      academicRank: 'Phó giáo sư',
      degree: 'Tiến sĩ',
      position: 'Giảng viên chính',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Trí tuệ nhân tạo và Thị giác máy tính',
        researchFields: 'Khoa học máy tính, Trí tuệ nhân tạo, Thị giác máy tính',
        researchDirections: 'Học sâu trong chẩn đoán hình ảnh y tế; Nhận diện hành vi con người từ video giám sát; Học tăng cường sâu (Deep Reinforcement Learning)',
        activitySummary: 'Hơn 45 bài báo ISI/Scopus (20 bài nhóm Q1); Chủ nhiệm 03 đề tài Quỹ NAFOSTED và 01 đề tài cấp Nhà nước; Thành viên hội đồng khoa học chuyên ngành Khoa học thông tin.'
      }
    },
    {
      username: 'gv.ngan',
      fullName: 'Hoàng Kim Ngân',
      email: 'nganhk@huit.edu.vn',
      phone: '0912345606',
      birthDate: '1988-09-05',
      gender: 'NU',
      code: 'GV006',
      deptCode: 'HTTT',
      facCode: 'CNTT',
      academicRank: null,
      degree: 'Tiến sĩ',
      position: 'Giảng viên',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Khai phá dữ liệu và Hệ thống thông tin quản lý',
        researchFields: 'Hệ thống thông tin, Khai phá dữ liệu',
        researchDirections: 'Khai phá quy trình nghiệp vụ (Process Mining); Phân tích dữ liệu lớn trong tài chính - ngân hàng; Quản trị tri thức tổ chức',
        activitySummary: 'Đã công bố 12 bài báo trên các tạp chí quốc tế danh mục Scopus Q2/Q3; Tác giả 01 sách chuyên khảo về Khai phá dữ liệu quản trị.'
      }
    },
    {
      username: 'gv.dang',
      fullName: 'Vũ Hải Đăng',
      email: 'dangvh@huit.edu.vn',
      phone: '0912345607',
      birthDate: '1985-12-18',
      gender: 'NAM',
      code: 'GV007',
      deptCode: 'TDH',
      facCode: 'DDT',
      academicRank: null,
      degree: 'Tiến sĩ',
      position: 'Giảng viên',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Điều khiển tự động và Robot công nghiệp',
        researchFields: 'Kỹ thuật điều khiển và Tự động hóa',
        researchDirections: 'Điều khiển thích nghi và bền vững; Cánh tay robot công nghiệp thông minh; Hệ thống SCADA và IoT công nghiệp (IIoT)',
        activitySummary: '10 bài báo quốc tế thuộc danh mục Scopus; Sở hữu 01 Giải pháp hữu ích về thiết bị bay không người lái phục vụ quan trắc; Chủ nhiệm 02 đề tài cấp cơ sở.'
      }
    },
    {
      username: 'gv.bich',
      fullName: 'Nguyễn Thị Bích',
      email: 'bichnt@huit.edu.vn',
      phone: '0912345608',
      birthDate: '1990-07-22',
      gender: 'NU',
      code: 'GV008',
      deptCode: 'CNCB',
      facCode: 'CNTP',
      academicRank: null,
      degree: 'Thạc sĩ',
      position: 'Giảng viên',
      role: 'GIANG_VIEN',
      scope: 'CA_NHAN',
      profile: {
        expertise: 'Công nghệ sau thu hoạch và Chế biến thực phẩm',
        researchFields: 'Công nghệ thực phẩm',
        researchDirections: 'Bảo quản nông sản bằng màng sinh học nano; Chiết xuất các hợp chất có hoạt tính sinh học từ phụ phẩm nông nghiệp nhiệt đới',
        activitySummary: 'Tác giả của 06 bài báo trên các tạp chí chuyên ngành trong nước và 02 bài báo quốc tế; Chủ nhiệm 02 đề tài NCKH cấp Trường.'
      }
    },
    {
      username: 'qlkh.long',
      fullName: 'Lê Hoàng Long',
      email: 'longlh@huit.edu.vn',
      phone: '0912345609',
      birthDate: '1984-04-14',
      gender: 'NAM',
      code: 'GV009',
      deptCode: 'CNPM',
      facCode: 'CNTT',
      academicRank: null,
      degree: 'Thạc sĩ',
      position: 'Chuyên viên QLKH',
      role: 'PHONG_QLKH',
      scope: 'TOAN_TRUONG',
      profile: {
        expertise: 'Quản lý khoa học công nghệ và Chuyển giao công nghệ',
        researchFields: 'Quản lý khoa học và công nghệ',
        researchDirections: 'Đánh giá hiệu quả hoạt động NCKH trong trường đại học; Mô hình liên kết Viện - Trường - Doanh nghiệp trong đổi mới sáng tạo',
        activitySummary: 'Nhiều năm kinh nghiệm điều phối đề tài NCKH các cấp và xác lập quyền sở hữu trí tuệ; Tham gia biên soạn sổ tay hướng dẫn NCKH cho giảng viên.'
      }
    }
  ];

  // Lấy accounts và lecturers hiện có
  const existingAccountsRes = await api('/admin/accounts?page=1&pageSize=100');
  const existingAccounts = existingAccountsRes?.items || [];

  const existingLecturersRes = await api('/lecturers?page=1&pageSize=100');
  const existingLecturers = existingLecturersRes?.items || [];

  for (const lec of lecturersData) {
    console.log(`\n-> Xử lý: ${lec.username} (${lec.fullName})`);

    // 1. Tạo hoặc lấy tài khoản
    let acc = existingAccounts.find(a => a.username === lec.username);
    if (!acc) {
      acc = await api('/admin/accounts', 'POST', {
        username: lec.username,
        fullName: lec.fullName,
        email: lec.email,
        password: DEFAULT_PASS
      });
      console.log(`  + Đã tạo tài khoản: ID ${acc.id}`);
    } else {
      console.log(`  - Tài khoản đã tồn tại: ID ${acc.id}`);
    }

    // 2. Gán quyền
    const deptId = deptMap[lec.deptCode];
    const facId = facultyMap[lec.facCode];
    let permission;
    if (lec.role === 'TRUONG_KHOA') {
      permission = { role: 'TRUONG_KHOA', scope: 'KHOA', facultyId: facId, departmentId: null };
    } else if (lec.role === 'TRUONG_BO_MON') {
      permission = { role: 'TRUONG_BO_MON', scope: 'BO_MON', facultyId: facId, departmentId: deptId };
    } else if (lec.role === 'PHONG_QLKH') {
      permission = { role: 'PHONG_QLKH', scope: 'TOAN_TRUONG', facultyId: null, departmentId: null };
    } else {
      permission = { role: 'GIANG_VIEN', scope: 'CA_NHAN', facultyId: null, departmentId: null };
    }
    await api(`/admin/accounts/${acc.id}/permissions`, 'PUT', [permission]);
    console.log(`  + Đã gán quyền: ${permission.role} (${permission.scope})`);

    // 3. Tạo hoặc lấy hồ sơ giảng viên
    let lecturerRecord = existingLecturers.find(l => l.code === lec.code || l.accountId === acc.id);
    if (!lecturerRecord) {
      lecturerRecord = await api('/lecturers', 'POST', {
        accountId: acc.id,
        departmentId: deptId,
        code: lec.code,
        fullName: lec.fullName,
        birthDate: lec.birthDate,
        gender: lec.gender,
        email: lec.email,
        phone: lec.phone,
        academicRank: lec.academicRank,
        degree: lec.degree,
        position: lec.position
      });
      console.log(`  + Đã tạo hồ sơ giảng viên: ${lec.code} (ID: ${lecturerRecord.id})`);
    } else {
      console.log(`  - Hồ sơ giảng viên đã có: ${lecturerRecord.code} (ID: ${lecturerRecord.id})`);
    }

    // 4. Cập nhật lý lịch khoa học
    await api(`/lecturers/${lecturerRecord.id}/scientific-profile`, 'PUT', lec.profile);
    console.log(`  + Đã cập nhật lý lịch khoa học`);
  }

  console.log('\n=======================================================');
  console.log('HOÀN TẤT TẠO CƠ SỞ DỮ LIỆU TÀI KHOẢN GIẢNG VIÊN VÀ LÝ LỊCH KHOA HỌC!');
  console.log(`Mật khẩu mặc định: ${DEFAULT_PASS}`);
  console.log('=======================================================');
}

main().catch(err => {
  console.error('\nLỖI SEED:', err);
  process.exit(1);
});
