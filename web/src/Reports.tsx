import { useEffect, useMemo, useState } from 'react'
import {
  ResponsiveContainer,
  BarChart,
  Bar,
  XAxis,
  YAxis,
  CartesianGrid,
  Tooltip,
  Legend,
  PieChart,
  Pie,
  Cell,
} from 'recharts'
import { api } from './api'
import type { Lookups } from './api'
import './Reports.css'

type Totals = { lecturers: number; products: number; hours: number; points: number }
type Group = Totals & { name: string }
type Row = { lecturerId: number; lecturerCode: string; lecturerName: string; departmentId: number; departmentName: string; academicRank: string | null; degree: string | null; classification: string | null; products: number; hours: number; points: number; evaluatedAt: string }
type Report = { academicYearId: number; academicYearCode: string; totals: Totals; byAcademicRank: Group[]; byDegree: Group[]; byDepartment: Group[]; rows: Row[] }
const CHART_COLORS = ['#168c8c', '#17324d', '#e6a45c', '#8b7fc7', '#65a886', '#d47d91']

export function ResearchReports({ lookups }: { lookups: Lookups | null }) {
  const [year, setYear] = useState(''); const [faculty, setFaculty] = useState(''); const [department, setDepartment] = useState('')
  const [rank, setRank] = useState(''); const [degree, setDegree] = useState(''); const [dimension, setDimension] = useState<'byDepartment' | 'byAcademicRank' | 'byDegree'>('byDepartment')
  const [report, setReport] = useState<Report | null>(null); const [error, setError] = useState(''); const [loading, setLoading] = useState(false)
  useEffect(() => { if (!year && lookups?.academicYears.length) setYear(String(lookups.academicYears[0].id)) }, [lookups, year])
  const departments = useMemo(() => (lookups?.departments ?? []).filter(x => !faculty || x.facultyId === Number(faculty)), [lookups, faculty])
  const query = () => { const q = new URLSearchParams({ academicYearId: year }); if (faculty) q.set('facultyId', faculty); if (department) q.set('departmentId', department); if (rank.trim()) q.set('academicRank', rank.trim()); if (degree.trim()) q.set('degree', degree.trim()); return q }
  async function load() { if (!year) return; setLoading(true); setError(''); try { setReport(await api<Report>(`/reports/research-results?${query()}`)) } catch (x) { setError((x as Error).message) } finally { setLoading(false) } }
  useEffect(() => { if (year) void load() }, [year]) // eslint-disable-line react-hooks/exhaustive-deps
  const groups = report?.[dimension] ?? []
  return <><p className="eyebrow">THỐNG KÊ VÀ BÁO CÁO</p><h1>Kết quả nghiên cứu khoa học</h1><p className="muted">Số liệu lấy từ lần đánh giá đã chốt mới nhất của từng giảng viên và giới hạn theo phạm vi tài khoản.</p>
    <section className="panel report-filters"><label>Năm học<select value={year} onChange={e => setYear(e.target.value)} required><option value="">Chọn năm học</option>{lookups?.academicYears.map(x => <option key={x.id} value={x.id}>{x.code}</option>)}</select></label><label>Khoa<select value={faculty} onChange={e => { setFaculty(e.target.value); setDepartment('') }}><option value="">Tất cả được phép</option>{lookups?.faculties.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label><label>Bộ môn<select value={department} onChange={e => setDepartment(e.target.value)}><option value="">Tất cả</option>{departments.map(x => <option key={x.id} value={x.id}>{x.name}</option>)}</select></label><label>Học hàm<input value={rank} onChange={e => setRank(e.target.value)} placeholder="Ví dụ: Phó giáo sư" /></label><label>Học vị<input value={degree} onChange={e => setDegree(e.target.value)} placeholder="Ví dụ: Tiến sĩ" /></label><button className="primary" disabled={loading || !year} onClick={load}>{loading ? 'Đang tải…' : 'Xem báo cáo'}</button></section>
    {error && <p className="alert">{error}</p>}{report && <><div className="report-cards"><article><span>Giảng viên</span><strong>{report.totals.lecturers}</strong></article><article><span>Sản phẩm</span><strong>{report.totals.products}</strong></article><article><span>Giờ quy đổi</span><strong>{report.totals.hours.toLocaleString('vi-VN')}</strong></article><article><span>Điểm quy đổi</span><strong>{report.totals.points.toLocaleString('vi-VN')}</strong></article></div>
      <div className="actions report-actions"><a className="report-download" href={`/api/reports/research-results/export.xlsx?${query()}`}>Tải Excel</a><a className="report-download" href={`/api/reports/research-results/export.pdf?${query()}`}>Tải PDF</a></div>
      <ResearchDashboard report={report} />
      <section className="panel"><div className="section-top"><h2>Tổng hợp</h2><select className="report-dimension" value={dimension} onChange={e => setDimension(e.target.value as typeof dimension)}><option value="byDepartment">Theo bộ môn</option><option value="byAcademicRank">Theo học hàm</option><option value="byDegree">Theo học vị</option></select></div><ReportTable groups={groups} /></section>
      <section className="panel"><h2>Chi tiết giảng viên</h2><div className="table-wrap"><table><thead><tr><th>Mã</th><th>Họ tên</th><th>Bộ môn</th><th>Học hàm / học vị</th><th>Xếp loại</th><th>Sản phẩm</th><th>Giờ</th><th>Điểm</th></tr></thead><tbody>{report.rows.map(x => <tr key={x.lecturerId}><td>{x.lecturerCode}</td><td>{x.lecturerName}</td><td>{x.departmentName}</td><td>{[x.academicRank, x.degree].filter(Boolean).join(' · ') || '—'}</td><td>{x.classification ?? '—'}</td><td>{x.products}</td><td>{x.hours.toLocaleString('vi-VN')}</td><td>{x.points.toLocaleString('vi-VN')}</td></tr>)}</tbody></table></div>{!report.rows.length && <p className="empty">Chưa có kết quả đánh giá đã chốt phù hợp.</p>}</section></>}</>
}

function ReportTable({ groups }: { groups: Group[] }) { return <div className="table-wrap"><table><thead><tr><th>Nhóm</th><th>Giảng viên</th><th>Sản phẩm</th><th>Giờ</th><th>Điểm</th></tr></thead><tbody>{groups.map(x => <tr key={x.name}><td>{x.name}</td><td>{x.lecturers}</td><td>{x.products}</td><td>{x.hours.toLocaleString('vi-VN')}</td><td>{x.points.toLocaleString('vi-VN')}</td></tr>)}</tbody></table></div> }
function ResearchDashboard({ report }: { report: Report }) {
  const departments = report.byDepartment
  const degrees = report.byDegree
  const ranks = report.byAcademicRank

  const hasData = report.totals.lecturers > 0

  if (!hasData) {
    return (
      <section className="panel">
        <h2>Dashboard thống kê NCKH</h2>
        <p className="empty">
          Chưa có kết quả đánh giá đã chốt để hiển thị biểu đồ.
        </p>
      </section>
    )
  }

  return (
    <section className="panel">
      <h2>Dashboard thống kê NCKH</h2>
      <p className="muted">
        Biểu đồ tổng hợp từ kết quả đánh giá đã chốt trong năm học {report.academicYearCode}.
      </p>

      <div className="dashboard-grid">
        <article className="dashboard-chart">
          <h3>Số sản phẩm theo bộ môn</h3>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={departments}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" tick={false} />
              <YAxis allowDecimals={false} />
              <Tooltip />
              <Bar dataKey="products" name="Sản phẩm" fill="#168c8c" />
            </BarChart>
          </ResponsiveContainer>
          <p className="muted">Di chuột vào cột để xem tên bộ môn và số sản phẩm.</p>
        </article>

        <article className="dashboard-chart">
          <h3>Tỷ lệ giảng viên theo học vị</h3>
          <ResponsiveContainer width="100%" height={280}>
            <PieChart>
              <Pie
                data={degrees.filter(x => x.lecturers > 0)}
                dataKey="lecturers"
                nameKey="name"
                cx="50%"
                cy="50%"
                outerRadius={90}
                label={({ percent }) => `${((percent ?? 0) * 100).toFixed(0)}%`}
              >
                {degrees.filter(x => x.lecturers > 0).map((item, index) => (
                  <Cell
                    key={item.name}
                    fill={CHART_COLORS[index % CHART_COLORS.length]}
                  />
                ))}
              </Pie>
              <Tooltip />
              <Legend />
            </PieChart>
          </ResponsiveContainer>
        </article>

        <article className="dashboard-chart">
          <h3>Tổng giờ quy đổi theo bộ môn</h3>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={departments} layout="vertical" margin={{ left: 12 }}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis type="number" />
              <YAxis type="category" dataKey="name" width={110} />
              <Tooltip />
              <Bar dataKey="hours" name="Giờ quy đổi" fill="#65a886" />
            </BarChart>
          </ResponsiveContainer>
        </article>

        <article className="dashboard-chart">
          <h3>Tổng điểm quy đổi theo học hàm</h3>
          <ResponsiveContainer width="100%" height={280}>
            <BarChart data={ranks}>
              <CartesianGrid strokeDasharray="3 3" />
              <XAxis dataKey="name" tick={false} />
              <YAxis />
              <Tooltip />
              <Bar dataKey="points" name="Điểm quy đổi" fill="#8b7fc7" />
            </BarChart>
          </ResponsiveContainer>
          <p className="muted">Di chuột vào cột để xem học hàm và điểm quy đổi.</p>
        </article>
      </div>
    </section>
  )
}