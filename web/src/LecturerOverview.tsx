import { useEffect, useState } from 'react'
import { api } from './api'
import type { AcademicYear } from './api'

type Product = { id: number; title: string; status: string }
type Target = { allocatedArticleCount: number; approvedArticles: number; deadline: string | null }
export function LecturerOverview({ fullName, years, yearId, onYearChange, onOpen }: {
  fullName: string; years: AcademicYear[]; yearId: string; onYearChange: (value: string) => void; onOpen: (section: string) => void
}) {
  const [products, setProducts] = useState<Product[]>([])
  const [targets, setTargets] = useState<Target[]>([])
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true)
  useEffect(() => {
    if (!yearId) return
    let active = true; setLoading(true); setError('')
    Promise.all([api<Product[]>(`/products/mine?academicYearId=${yearId}`), api<Target[]>(`/lecturer-target-allocations/mine?academicYearId=${yearId}`)])
      .then(([p, t]) => { if (active) { setProducts(p); setTargets(t) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [yearId])
  const returned = products.filter(x => x.status === 'TRA_LAI')
  const drafts = products.filter(x => x.status === 'NHAP')
  const allocated = targets.reduce((s, t) => s + t.allocatedArticleCount, 0)
  const completed = targets.reduce((s, t) => s + t.approvedArticles, 0)
  const nextDeadline = targets.filter(x => x.approvedArticles < x.allocatedArticleCount && x.deadline).map(x => x.deadline!).sort()[0]
  return <>
    <div className="section-top"><div><p className="eyebrow">CÔNG VIỆC CỦA BẠN</p><h1>Xin chào, {fullName}</h1></div>
      <label>Năm học<select value={yearId} onChange={e => onYearChange(e.target.value)}>{years.map(y => <option key={y.id} value={y.id}>{y.code}</option>)}</select></label></div>
    {error && <p className="alert" role="alert">{error}</p>}
    {loading ? <p role="status">Đang tải công việc…</p> : <>
      <div className="lecturer-work-cards">
        <button onClick={() => onOpen('products')}><span>Cần bổ sung</span><strong>{returned.length}</strong><small>sản phẩm bị trả lại</small></button>
        <button onClick={() => onOpen('products')}><span>Chưa gửi duyệt</span><strong>{drafts.length}</strong><small>bản nháp</small></button>
        <button onClick={() => onOpen('my-targets')}><span>Chỉ tiêu hoàn thành</span><strong>{completed}/{allocated}</strong><small>bài do bạn phụ trách</small></button>
        <button onClick={() => onOpen('products')}><span>Đã công nhận</span><strong>{products.filter(x => x.status === 'DA_DUYET').length}</strong><small>sản phẩm tham gia</small></button>
      </div>
      <section className="panel"><h2>Việc cần làm</h2>
        {returned.length > 0 && <p>{returned.length} sản phẩm cần bổ sung theo phản hồi của người duyệt.</p>}
        {drafts.length > 0 && <p>{drafts.length} bản nháp đang chờ hoàn thiện tác giả, minh chứng và gửi duyệt.</p>}
        {nextDeadline && <p>Hạn chỉ tiêu chưa hoàn thành gần nhất: <strong>{new Date(`${nextDeadline}T00:00:00`).toLocaleDateString('vi-VN')}</strong>.</p>}
        {!returned.length && !drafts.length && !nextDeadline && <p>Chưa có hồ sơ cần bổ sung. Bạn có thể kiểm tra kế hoạch và chỉ tiêu trong năm đang chọn.</p>}
        <div className="actions"><button className="primary" onClick={() => onOpen('research-plans')}>Mở kế hoạch</button><button onClick={() => onOpen('products')}>Mở sản phẩm</button><button onClick={() => onOpen('notifications')}>Xem thông báo</button></div>
      </section>
    </>}
  </>
}
