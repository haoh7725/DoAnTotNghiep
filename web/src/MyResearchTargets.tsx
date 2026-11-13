import { useEffect, useState } from 'react'
import { api } from './api'

type MyTarget = {
  id: number
  targetId: number
  academicYearCode: string
  allocatedArticleCount: number
  registeredArticles: number
  approvedArticles: number
  completionPercent: number
  isCompleted: boolean
  allocationDate: string
  deadline: string | null
  linkedProducts: { id: number; title: string; status: string; articleStatus: string | null }[]
}

const reviewStatus: Record<string, string> = {
  NHAP: 'Nháp', NOP: 'Đã gửi duyệt', TRUONG_BO_MON_DUYET: 'Trưởng bộ môn đã duyệt',
  TRUONG_KHOA_DUYET: 'Trưởng khoa đã duyệt', PHONG_QLKH_DUYET: 'Phòng QLKH đã duyệt',
  DA_DUYET: 'Đã duyệt', TRA_LAI: 'Yêu cầu bổ sung',
}

const articleStatus: Record<string, string> = {
  DANG_VIET: 'Đang viết', DANG_PHAN_BIEN: 'Đang phản biện',
  DA_NHAN_DANG: 'Đã nhận đăng', DA_NHAN_XET: 'Đã nhận đăng', DA_XUAT_BAN: 'Đã xuất bản',
}

export function MyResearchTargets({ onOpenProducts, onOpenProduct, yearId }: { onOpenProducts: () => void; onOpenProduct: (id: number) => void; yearId?: string }) {
  const [items, setItems] = useState<MyTarget[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    setLoading(true)
    api<MyTarget[]>(yearId ? `/lecturer-target-allocations/mine?academicYearId=${yearId}` : '/lecturer-target-allocations/mine')
      .then(data => { if (active) setItems(data) })
      .catch(e => { if (active) setError((e as Error).message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [yearId])

  const allocated = items.reduce((sum, x) => sum + x.allocatedArticleCount, 0)
  const completed = items.reduce((sum, x) => sum + x.approvedArticles, 0)

  return <>
    <p className="eyebrow">PHÂN CÔNG BÀI BÁO</p>
    <h1>Chỉ tiêu bài báo của tôi</h1>
    <p className="muted">Mỗi bài tính số lượng cho một giảng viên phụ trách. Đồng tác giả được ghi nhận công trình và quy đổi đóng góp riêng.</p>
    {error && <p className="alert" role="alert">{error}</p>}
    {loading ? <p className="loading">Đang tải chỉ tiêu…</p> : <>
      <div className="personal-target-summary">
        <article><span>Được phân công</span><strong>{allocated}</strong><small>bài báo</small></article>
        <article><span>Đã hoàn thành</span><strong>{completed}</strong><small>đã duyệt và xuất bản</small></article>
        <article><span>Còn lại</span><strong>{Math.max(0, allocated - completed)}</strong><small>bài cần hoàn thành</small></article>
      </div>
      {items.length === 0 ? <section className="panel">
        <h2>Chưa có chỉ tiêu được giao</h2>
        <p className="muted">Khi Trưởng bộ môn phân công, chỉ tiêu sẽ xuất hiện tại đây và bạn sẽ nhận được thông báo.</p>
      </section> : items.map(item => {
        const remaining = Math.max(0, item.allocatedArticleCount - item.approvedArticles)
        const percent = Math.max(0, item.completionPercent)
        return <section className="panel personal-target-card" key={item.id}>
          <div className="section-top">
            <div><h2>Năm học {item.academicYearCode}</h2><p className="muted">Giao ngày {item.allocationDate} · Hạn hoàn thành: {item.deadline ?? 'Chưa quy định'}</p></div>
            <span className={`personal-target-status ${item.isCompleted ? 'done' : ''}`}>{item.isCompleted ? 'Đã hoàn thành' : 'Đang thực hiện'}</span>
          </div>
          <div className="personal-target-numbers">
            <span>Được giao <strong>{item.allocatedArticleCount}</strong></span>
            <span>Hoàn thành <strong>{item.approvedArticles}</strong></span>
            <span>Đã liên kết <strong>{item.registeredArticles ?? item.linkedProducts.length}</strong></span>
            <span>Còn lại <strong>{remaining}</strong></span>
          </div>
          <div className="personal-target-progress" aria-label={`Hoàn thành ${percent}%`}><span style={{ width: `${Math.min(100, percent)}%` }} /></div>
          <p className="muted">Tiến độ: <strong>{percent}%</strong>. Chỉ bài báo đã liên kết chỉ tiêu, được duyệt và xuất bản mới được tính.</p>
          {item.linkedProducts.length > 0 ? <div className="linked-target-products">
            <strong>Sản phẩm đã liên kết</strong>
            {item.linkedProducts.map(product => <button key={product.id} onClick={() => onOpenProduct(product.id)}>
              <span>{product.title}</span>
              <small>{product.articleStatus ? articleStatus[product.articleStatus] ?? product.articleStatus : 'Chưa cập nhật tiến độ'} · {reviewStatus[product.status] ?? product.status}</small>
            </button>)}
          </div> : !item.isCompleted && <button className="primary" onClick={onOpenProducts}>Mở sản phẩm để liên kết</button>}
        </section>
      })}
    </>}
  </>
}
