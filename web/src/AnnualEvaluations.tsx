import { useEffect, useState } from 'react'
import { api, preferredAcademicYear } from './api'
import type { Lookups, Profile } from './api'

type Product = { productId: number; typeName: string; title: string; targetId: number | null; ruleCode: string; hours: number; basis: string }
type Target = { id: number; typeName: string; assigned: number; registered: number; approved: number; percent: number; completed: boolean; hours: number }
type Result = { lecturerId: number; lecturerName: string; lecturerCode: string; totalHours: number; assignedTargets: number; completedTargets: number; classification: string; products: Product[]; targets: Target[]; issues: string[] }
type Nominations = { population: number; eligible: number; advancedLaborQuota: number; topThirtyQuota: number; emulationFighterQuota: number; policy: string; candidates: { lecturerId: number; lecturerName: string; lecturerCode: string; totalHours: number; rank: number; proposedTitle: string | null; tiedAtBoundary: boolean }[] }
const fmt = (n: number) => n.toLocaleString('vi-VN', { maximumFractionDigits: 4 })

export function AnnualEvaluations({ user, lookups }: { user: Profile; lookups: Lookups | null }) {
  const manager = user.roles.some(r => ['TRUONG_KHOA', 'TRUONG_BO_MON', 'QUAN_TRI'].includes(r.role))
  const canFinalize = user.roles.some(r => ['TRUONG_KHOA', 'QUAN_TRI'].includes(r.role))
  const [year, setYear] = useState('')
  const [rows, setRows] = useState<Result[]>([])
  const [detail, setDetail] = useState<Result | null>(null)
  const [error, setError] = useState(''); const [message, setMessage] = useState(''); const [busy, setBusy] = useState(false)
  const [search, setSearch] = useState(''); const [faculty, setFaculty] = useState(''); const [nominations, setNominations] = useState<Nominations | null>(null)
  useEffect(() => { if (!year && lookups?.academicYears.length) setYear(String(preferredAcademicYear(lookups.academicYears).id)) }, [lookups, year])
  useEffect(() => {
    if (!year) return
    let active = true; setBusy(true); setError(''); setDetail(null); setNominations(null)
    api<Result[] | Result>(`/annual-research${manager ? '' : '/me'}?academicYearId=${year}`)
      .then(x => { if (active) { setRows(Array.isArray(x) ? x : [x]); if (!manager) setDetail(x as Result) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setBusy(false) })
    return () => { active = false }
  }, [year, manager])
  async function finalize() {
    if (!detail || busy) return
    setBusy(true); setError(''); setMessage('')
    try { await api(`/annual-research/${detail.lecturerId}/finalize`, 'POST', { academicYearId: Number(year) }); setMessage('Đã lưu và chốt kết quả đánh giá năm.') }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <>
    <h1>Kết quả NCKH năm học</h1>
    <p>Tổng hợp tự động từ sản phẩm đã công nhận theo năm công bố/nghiệm thu. Từ 586 giờ là hoàn thành yêu cầu giờ; tiến độ từng chỉ tiêu được xét riêng.</p>
    <section className="panel"><label>Năm học<select value={year} onChange={e => setYear(e.target.value)}>{lookups?.academicYears.map(y => <option value={y.id} key={y.id}>{y.code}</option>)}</select></label></section>
    {error && <p className="alert" role="alert">{error}</p>}{message && <p role="status">{message}</p>}
    {busy && <p>Đang xử lý…</p>}
    {manager && !detail && <section className="panel">
      <label>Tìm giảng viên<input value={search} onChange={e => setSearch(e.target.value)} placeholder="Mã hoặc họ tên" /></label>
      <div className="table-wrap"><table><thead><tr><th>Giảng viên</th><th>Chỉ tiêu hoàn thành / được giao</th><th>Tổng giờ</th><th>Yêu cầu 586 giờ</th><th /></tr></thead><tbody>
        {rows.filter(r => `${r.lecturerName} ${r.lecturerCode}`.toLocaleLowerCase('vi').includes(search.toLocaleLowerCase('vi'))).map(r => <tr key={r.lecturerId}><td>{r.lecturerCode} — {r.lecturerName}</td><td>{r.completedTargets}/{r.assignedTargets}</td><td>{fmt(r.totalHours)}</td><td>{r.issues.length ? 'Cần kiểm tra quy đổi' : r.classification}</td><td><button onClick={() => { setDetail(r); setMessage('') }}>Chi tiết</button></td></tr>)}
      </tbody></table></div>
    </section>}
    {detail && <>
      {manager && <button onClick={() => setDetail(null)}>← Danh sách</button>}
      <section className="panel"><h2>{detail.lecturerName}</h2><p>Tổng giờ: <strong>{fmt(detail.totalHours)}</strong> / 586 — {detail.issues.length ? 'Chưa đủ căn cứ kết luận' : detail.classification}</p>
        {detail.issues.map(x => <p className="alert" key={x}>{x}</p>)}
        <h3>Từng chỉ tiêu được giao</h3>
        {!detail.targets.length && <p>Chưa được giao chỉ tiêu trong năm này.</p>}
        {detail.targets.map(t => <article key={t.id}><h4>#{t.id} — {t.typeName}</h4><p>{t.approved}/{t.assigned} sản phẩm hoàn thành ({fmt(t.percent)}%) · {t.registered} đã liên kết · {fmt(t.hours)} giờ ghi nhận trong năm · {t.completed ? 'Hoàn thành chỉ tiêu' : 'Chưa hoàn thành chỉ tiêu'}</p><progress max={100} value={Math.min(100, t.percent)} /></article>)}
        <h3>Sản phẩm theo số giờ giảm dần</h3><div className="table-wrap"><table><thead><tr><th>Sản phẩm</th><th>Loại</th><th>Chỉ tiêu</th><th>Giờ</th><th>Căn cứ</th></tr></thead><tbody>{detail.products.map(p => <tr key={p.productId}><td>{p.title}</td><td>{p.typeName}</td><td>{p.targetId ? `#${p.targetId}` : 'Ngoài chỉ tiêu'}</td><td>{fmt(p.hours)}</td><td>{p.basis}</td></tr>)}</tbody></table></div>
        {canFinalize && <button className="primary" disabled={busy || detail.issues.length > 0} onClick={() => void finalize()}>Chốt kết quả năm</button>}
      </section>
    </>}
    {canFinalize && !detail && <section className="panel"><h2>Đề xuất danh hiệu</h2><p>Danh sách chỉ được tính và hiển thị, không lưu: top 10% tổng giờ là Lao động tiên tiến; nhóm tiếp theo đến hết top 30% là Chiến sĩ thi đua.</p>
      <label>Khoa<select value={faculty} onChange={e => setFaculty(e.target.value)}><option value="">Chọn khoa</option>{lookups?.faculties.filter(f => user.roles.some(r => r.role === 'QUAN_TRI' || r.role === 'TRUONG_KHOA' && r.facultyId === f.id)).map(f => <option key={f.id} value={f.id}>{f.name}</option>)}</select></label>
      <button disabled={busy || !faculty} onClick={async () => { setBusy(true); setError(''); try { setNominations(await api<Nominations>(`/annual-research/nominations?academicYearId=${year}&facultyId=${faculty}`)) } catch (e) { setError((e as Error).message) } finally { setBusy(false) } }}>Xem danh sách đề xuất</button>
      {nominations && <><p>{nominations.eligible}/{nominations.population} giảng viên đạt 586 giờ. Suất dự kiến: {nominations.advancedLaborQuota} Lao động tiên tiến và {nominations.emulationFighterQuota} Chiến sĩ thi đua. {nominations.policy}</p>
        <div className="table-wrap"><table><thead><tr><th>Hạng</th><th>Giảng viên</th><th>Tổng giờ</th><th>Nhóm hiển thị</th><th>Lưu ý</th></tr></thead><tbody>{nominations.candidates.map(c => <tr key={c.lecturerId}><td>{c.rank}</td><td>{c.lecturerCode} — {c.lecturerName}</td><td>{fmt(c.totalHours)}</td><td>{c.proposedTitle ?? 'Ngoài top 30%'}</td><td>{c.tiedAtBoundary ? 'Đồng giờ tại ranh giới' : '—'}</td></tr>)}</tbody></table></div>
      </>}
    </section>}
  </>
}
