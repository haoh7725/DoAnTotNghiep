import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api, apiForm } from './api'
import type { Profile } from './api'
import './Products.css'

// ── Types ──────────────────────────────────────────────────────────────────────
type Summary = { id: number; researchPlanItemId: number; title: string; articleStatus: string | null; status: string; scoreEquivalent: number | null; updatedAt: string; createdAt: string }
type CoAuthor = { id: number; lecturerId: number; lecturerFullName: string; lecturerCode: string | null; displayOrder: number; authorRole: string }
type Evidence = { id: number; originalFileName: string; fileSizeBytes: number; description: string | null; uploadedAt: string }
type Product = Summary & { submittedByLecturerId: number; submittedByLecturerFullName: string; description: string | null; publicationInfo: string | null; publishedDate: string | null; journalIndex: string | null; journalClassification: string | null; projectLevel: string | null; coAuthors: CoAuthor[]; evidences: Evidence[] }
type PlanItem = { id: number; name: string; productTypeName: string; academicYearCode: string; status: string }
type LecturerOption = { id: number; fullName: string; code: string; departmentName: string }
type AvailableTarget = { id: number; academicYearId: number; academicYearCode: string; allocatedArticleCount: number; journalConferenceId: number | null; journalConferenceName: string | null; deadline: string | null; status: string }
type ReviewHistory = { id: number; productId: number; actorAccountId: number; actorUsername: string; fromStatus: string; toStatus: string; comment: string | null; occurredAt: string }
type ProductSnapshot = { productId: number; version: number; snapshotByAccountId: number; snapshotAt: string; title: string; description: string | null; publicationInfo: string | null; publishedDate: string | null; articleStatus: string | null; journalIndex: string | null; journalClassification: string | null; projectLevel: string | null; status: string }
type PlagiarismCheck = { id: number; productId: number; evidenceId: number | null; provider: string; status: string; similarityScore: number | null; matchedSourceCount: number | null; totalWords: number | null; creditsUsed: number | null; reportUrl: string | null; errorMessage: string | null; requestedByAccountId: number; createdAt: string; startedAt: string | null; completedAt: string | null }

// ── Lookup maps ────────────────────────────────────────────────────────────────
const workflow: Record<string, string> = {
  NHAP: 'Nháp', NOP: 'Đã gửi',
  TRUONG_BO_MON_DUYET: 'Trưởng bộ môn đã duyệt',
  TRUONG_KHOA_DUYET: 'Trưởng khoa đã duyệt',
  PHONG_QLKH_DUYET: 'Phòng QLKH đã duyệt',
  DA_DUYET: 'Đã duyệt', TRA_LAI: 'Yêu cầu bổ sung',
}
const article: Record<string, string> = {
  DANG_VIET: 'Đang viết', DANG_PHAN_BIEN: 'Đang phản biện',
  DA_NHAN_DANG: 'Đã nhận đăng', DA_XUAT_BAN: 'Đã xuất bản',
}
// Map role → which status it can approve FROM
const roleCanApproveFrom: Record<string, string> = {
  TRUONG_BO_MON: 'NOP',
  TRUONG_KHOA: 'TRUONG_BO_MON_DUYET',
  PHONG_QLKH: 'TRUONG_KHOA_DUYET',
  QUAN_TRI: '', // admin can approve any reviewable status
}
const reviewableStatuses = ['NOP', 'TRUONG_BO_MON_DUYET', 'TRUONG_KHOA_DUYET', 'PHONG_QLKH_DUYET']

const editable = (p: Product) => ['NHAP', 'TRA_LAI'].includes(p.status)
const canManageProductAttachments = (user: Profile | null, p: Product) =>
  editable(p) && (user?.roles.some(r => r.role === 'GIANG_VIEN') ?? false)
const canManagerReview = (user: Profile | null, p: Product) => {
  if (!user) return false
  if (!reviewableStatuses.includes(p.status)) return false
  return user.roles.some(r => {
    if (r.role === 'QUAN_TRI') return true
    if (r.role === 'PHONG_QLKH' && p.status === 'PHONG_QLKH_DUYET') return true
    const fromStatus = roleCanApproveFrom[r.role]
    return fromStatus === p.status
  })
}
// PHONG_QLKH duyệt bước cuối (PHONG_QLKH_DUYET → DA_DUYET) cần nhập điểm
const needsScore = (user: Profile | null, p: Product) =>
  p.status === 'PHONG_QLKH_DUYET' &&
  user?.roles.some(r => r.role === 'PHONG_QLKH' || r.role === 'QUAN_TRI')


type PendingProduct = {
  id: number
  title: string
  status: string
  lecturerName: string
  departmentName: string
}

export function ProductReviewWorkspace({ user }: { user: Profile }) {
  const [items, setItems] = useState<PendingProduct[]>([])
  const [selected, setSelected] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)

  useEffect(() => {
    let active = true
    setLoading(true)
    setError('')

    api<PendingProduct[]>('/products/pending-review')
      .then(data => { if (active) setItems(data) })
      .catch(err => {
        if (active) setError((err as Error).message)
      })
      .finally(() => { if (active) setLoading(false) })

    return () => { active = false }
  }, [revision])

  if (selected !== null) {
    return <ProductDetail
      key={selected}
      id={selected}
      user={user}
      onBack={() => {
        setSelected(null)
        setRevision(value => value + 1)
      }}
    />
  }

  return <>
    <h1>Sản phẩm chờ duyệt</h1>
    <p className="muted">
      Hồ sơ chờ xét duyệt trong phạm vi được phân quyền.
    </p>

    <section className="panel">
      <button
        disabled={loading}
        onClick={() => setRevision(value => value + 1)}
      >
        Làm mới
      </button>

      {error && <p className="alert" role="alert">{error}</p>}

      {loading ? <p>Đang tải…</p> : !error && <>
        <div className="table-wrap">
          <table>
            <thead>
              <tr>
                <th>Sản phẩm</th>
                <th>Giảng viên</th>
                <th>Bộ môn</th>
                <th>Trạng thái</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => <tr key={item.id}>
                <td>{item.title}</td>
                <td>{item.lecturerName}</td>
                <td>{item.departmentName}</td>
                <td>{workflow[item.status] ?? item.status}</td>
                <td>
                  <button onClick={() => setSelected(item.id)}>
                    Xem và xét duyệt
                  </button>
                </td>
              </tr>)}
            </tbody>
          </table>
        </div>

        {items.length === 0 &&
          <p>Không có sản phẩm đang chờ bạn xét duyệt.</p>}
      </>}
    </section>
  </>
}

// ── ProductWorkspace (entry) ───────────────────────────────────────────────────
export function ProductWorkspace({ user }: { user?: Profile | null }) {
  const [items, setItems] = useState<Summary[]>([])
  const [planItems, setPlanItems] = useState<PlanItem[]>([])
  const [selected, setSelected] = useState<number | null>(null)
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true; setLoading(true); setError('')
    api<Summary[]>('/products/mine')
      .then(x => { if (active) setItems(x) })
      .catch(e => { if (active) setError((e as Error).message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [revision])

  useEffect(() => {
    api<PlanItem[]>('/products/available-plan-items').then(setPlanItems).catch(() => {
      // Danh sách sản phẩm vẫn dùng được nếu chưa tải được nhãn kế hoạch.
    })
  }, [revision])

  if (selected !== null)
    return <ProductDetail id={selected} user={user ?? null} onBack={() => { setSelected(null); setRevision(x => x + 1) }} />

  return <>
    <p className="eyebrow">SẢN PHẨM KHOA HỌC</p>
    <h1>Sản phẩm của tôi</h1>
    <p className="muted">Khai báo sản phẩm thực tế và minh chứng để gửi xét duyệt. Kế hoạch chỉ là phần cam kết đầu năm.</p>
    {error && <p className="alert">{error}</p>}
    <CreateProduct onCreated={id => { setRevision(x => x + 1); setSelected(id) }} />
    <section className="panel">
      <h2>Danh sách sản phẩm</h2>
      {loading ? <p>Đang tải…</p> : <>
        <div className="table-wrap"><table><thead><tr>
          <th>Tên sản phẩm</th><th>Trạng thái bài viết</th><th>Xét duyệt</th><th>Điểm quy đổi</th><th>Cập nhật</th><th></th>
        </tr></thead><tbody>
            {items.map(p => <tr key={p.id}>
              <td><strong>{p.title}</strong><br /><small>{(() => {
                const plan = planItems.find(item => item.id === p.researchPlanItemId)
                return plan ? `${plan.productTypeName} · ${plan.name}` : 'Nội dung kế hoạch đã liên kết'
              })()}</small></td>
              <td>{p.articleStatus ? article[p.articleStatus] ?? p.articleStatus : '—'}</td>
              <td><span className={`status-chip status-chip--${p.status.toLowerCase()}`}>{workflow[p.status] ?? p.status}</span></td>
              <td>{p.scoreEquivalent !== null ? p.scoreEquivalent : '—'}</td>
              <td>{new Date(p.updatedAt).toLocaleString('vi-VN')}</td>
              <td><button onClick={() => setSelected(p.id)}>Chi tiết</button></td>
            </tr>)}
          </tbody></table></div>
        {!items.length && <p className="empty">Chưa có sản phẩm.</p>}
      </>}
    </section>
  </>
}

// ── CreateProduct ──────────────────────────────────────────────────────────────
function CreateProduct({ onCreated }: { onCreated: (id: number) => void }) {
  const [open, setOpen] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [plans, setPlans] = useState<PlanItem[]>([])
  const [targets, setTargets] = useState<AvailableTarget[]>([])

  useEffect(() => {
    if (open && !plans.length) {
      Promise.all([
        api<PlanItem[]>('/products/available-plan-items'),
        api<AvailableTarget[]>('/products/available-research-targets')
      ]).then(([planData, targetData]) => { setPlans(planData); setTargets(targetData) })
        .catch(e => setError((e as Error).message))
    }
  }, [open, plans.length])

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const f = new FormData(e.currentTarget)
    const itemId = Number(f.get('researchPlanItemId'))
    const targetId = Number(f.get('researchTargetId')) || null
    const target = targets.find(x => x.id === targetId)
    setBusy(true); setError('')
    try {
      const body = { ...formBody(f, itemId), journalConferenceId: target?.journalConferenceId ?? null }
      const p = await api<Product>(`/research-plan-items/${itemId}/products`, 'POST', body)
      onCreated(p.id)
    }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  return <section className="panel">
    <div className="section-top">
      <div><h2>Khai báo sản phẩm thực tế</h2><p className="muted product-sub">Bước 1: chọn nội dung đã cam kết trong kế hoạch. Bước 2: nhập sản phẩm và minh chứng để gửi xét duyệt.</p></div>
      <button className="primary" onClick={() => setOpen(x => !x)}>{open ? 'Đóng' : '+ Thêm'}</button>
    </div>
    {open && <ProductForm onSubmit={submit} busy={busy} error={error} planItems={plans} targets={targets} />}
  </section>
}

// ── ProductDetail ──────────────────────────────────────────────────────────────
function ProductDetail({ id, user, onBack }: { id: number; user: Profile | null; onBack: () => void }) {
  const [p, setP] = useState<Product | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [revision, setRevision] = useState(0)
  const [tab, setTab] = useState<'info' | 'plagiarism' | 'history' | 'snapshots' | 'feedback'>('info')

  useEffect(() => {
    let active = true; setError('')
    api<Product>(`/products/${id}`)
      .then(x => { if (active) setP(x) })
      .catch(e => { if (active) setError((e as Error).message) })
    return () => { active = false }
  }, [id, revision])

  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const f = new FormData(e.currentTarget)
    setBusy(true); setError('')
    try { setP(await api<Product>(`/products/${id}`, 'PUT', formBody(f))); setEditing(false) }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  async function submitProduct() {
    setBusy(true); setError('')
    try { setP(await api<Product>(`/products/${id}/submit`, 'POST')) }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  return <>
    <button onClick={onBack}>← Danh sách sản phẩm</button>
    {error && <p className="alert">{error}</p>}
    {!p ? <p>Đang tải…</p> : <>
      <p className="eyebrow">CHI TIẾT SẢN PHẨM</p>
      <h1>{p.title}</h1>
      <p className="muted">
        <span className={`status-chip status-chip--${p.status.toLowerCase()}`}>{workflow[p.status] ?? p.status}</span>
        {' '}· Tác giả chính: {p.submittedByLecturerFullName}
        {p.scoreEquivalent !== null && <> · <strong>Điểm quy đổi: {p.scoreEquivalent}</strong></>}
      </p>

      {/* Tab bar */}
      <div className="tabs">
        <button className={tab === 'info' ? 'active' : ''} onClick={() => setTab('info')}>Thông tin</button>
        <button className={tab === 'plagiarism' ? 'active' : ''} onClick={() => setTab('plagiarism')}>Kiểm tra tương đồng</button>
        <button className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')}>Lịch sử xét duyệt</button>
        <button className={tab === 'snapshots' ? 'active' : ''} onClick={() => setTab('snapshots')}>Phiên bản</button>
        <button className={tab === 'feedback' ? 'active' : ''} onClick={() => setTab('feedback')}>Phản hồi QLKH</button>
      </div>

      {tab === 'info' && <>
        {/* Manager review panel */}
        {canManagerReview(user, p) && (
          <ReviewPanel product={p} user={user!} onReviewed={updated => { setP(updated); setRevision(x => x + 1) }} />
        )}

        {/* Product info */}
        {editing
          ? <section className="panel">
            <ProductForm product={p} onSubmit={save} busy={busy} error={error} />
            <button disabled={busy} onClick={() => setEditing(false)}>Hủy</button>
          </section>
          : <section className="panel">
            <div className="section-top">
              <h2>Thông tin</h2>
              {editable(p) && <div className="actions">
                <button onClick={() => setEditing(true)}>Chỉnh sửa</button>
                <button className="primary" disabled={busy} onClick={submitProduct}>Gửi duyệt</button>
              </div>}
            </div>
            {p.status === 'TRA_LAI' && (
              <p className="alert review-alert" role="alert">
                ⚠️ Sản phẩm được yêu cầu bổ sung. Vui lòng chỉnh sửa và gửi duyệt lại.
              </p>
            )}
            <dl className="product-grid">
              <div><dt>Trạng thái bài viết</dt><dd>{p.articleStatus ? article[p.articleStatus] : '—'}</dd></div>
              <div><dt>Ngày công bố</dt><dd>{p.publishedDate ?? '—'}</dd></div>
              <div><dt>Nơi công bố</dt><dd>{p.publicationInfo ?? '—'}</dd></div>
              <div><dt>Chỉ số tạp chí</dt><dd>{p.journalIndex ?? '—'}</dd></div>
              <div><dt>Phân loại tạp chí</dt><dd>{p.journalClassification ?? '—'}</dd></div>
              <div><dt>Cấp đề tài</dt><dd>{p.projectLevel ?? '—'}</dd></div>
            </dl>
            {p.description && <p>{p.description}</p>}
          </section>
        }

        <CoAuthors product={p} user={user} onChanged={() => setRevision(x => x + 1)} />
        <Evidences product={p} user={user} onChanged={() => setRevision(x => x + 1)} />
      </>}

      {tab === 'history' && <ReviewHistoryPanel productId={id} key={`hist-${revision}`} />}
      {tab === 'plagiarism' && <PlagiarismPanel product={p} />}
      {tab === 'snapshots' && <SnapshotPanel productId={id} key={`snap-${revision}`} />}
      {tab === 'feedback' && <FeedbackPanel productId={id} user={user} />}
    </>}
  </>
}

const plagiarismStatus: Record<string, string> = {
  CHO_XU_LY: 'Chờ xử lý', DANG_GUI: 'Đang gửi', DANG_KIEM_TRA: 'Đang kiểm tra',
  HOAN_THANH: 'Hoàn thành', THAT_BAI: 'Thất bại', DA_HUY: 'Đã hủy',
}

function PlagiarismPanel({ product }: { product: Product }) {
  const [items, setItems] = useState<PlagiarismCheck[]>([])
  const [evidenceId, setEvidenceId] = useState('')
  const [consent, setConsent] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const supported = product.evidences.filter(x => /\.(pdf|docx?|txt)$/i.test(x.originalFileName))
  const load = () => api<PlagiarismCheck[]>(`/products/${product.id}/plagiarism-checks`)
    .then(setItems).catch(e => setError((e as Error).message))

  useEffect(() => { void load() }, [product.id])
  useEffect(() => {
    if (!items.some(x => ['CHO_XU_LY', 'DANG_GUI', 'DANG_KIEM_TRA'].includes(x.status))) return
    const timer = window.setInterval(() => void load(), 5000)
    return () => window.clearInterval(timer)
  }, [items, product.id])

  async function submit(e: FormEvent) {
    e.preventDefault(); setBusy(true); setError('')
    try {
      await api(`/products/${product.id}/plagiarism-checks`, 'POST', { evidenceId: Number(evidenceId), consent })
      setConsent(false); await load()
    } catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  return <>
    <section className="panel plagiarism-intro">
      <h2>Kiểm tra tương đồng với Copyleaks</h2>
      <p className="muted">Kết quả là tỷ lệ nội dung tương đồng, không phải kết luận tự động về đạo văn. Người xét duyệt cần xem các nguồn và ngữ cảnh trích dẫn.</p>
      {error && <p className="alert" role="alert">{error}</p>}
      {editable(product) ? <form className="plagiarism-form" onSubmit={submit}>
        <label>Tài liệu
          <select required value={evidenceId} onChange={e => setEvidenceId(e.target.value)}>
            <option value="">Chọn PDF, DOC, DOCX hoặc TXT</option>
            {supported.map(x => <option key={x.id} value={x.id}>{x.originalFileName}</option>)}
          </select>
        </label>
        <label className="consent-check">
          <input type="checkbox" checked={consent} onChange={e => setConsent(e.target.checked)} required />
          Tôi đồng ý gửi tài liệu này đến Copyleaks để phân tích tương đồng.
        </label>
        <button className="primary" disabled={busy || !evidenceId || !consent}>{busy ? 'Đang gửi…' : 'Kiểm tra tương đồng'}</button>
        {!supported.length && <p className="muted">Hãy tải lên một tệp PDF, DOC, DOCX hoặc TXT trong phần Minh chứng trước.</p>}
      </form> : <p className="muted">Sản phẩm đã gửi duyệt; bạn có thể xem các kết quả đã có.</p>}
    </section>
    <section className="panel">
      <div className="section-top"><h2>Lịch sử kiểm tra</h2><button onClick={() => void load()}>Làm mới</button></div>
      {!items.length ? <p className="empty">Chưa có lần kiểm tra nào.</p> : <div className="plagiarism-list">
        {items.map(x => {
          const file = product.evidences.find(e => e.id === x.evidenceId)
          const scoreClass = x.similarityScore === null ? '' : x.similarityScore < 20 ? 'low' : x.similarityScore < 40 ? 'medium' : 'high'
          return <article className="plagiarism-item" key={x.id}>
            <div className={`similarity-score ${scoreClass}`}>{x.similarityScore === null ? '—' : `${x.similarityScore}%`}</div>
            <div className="plagiarism-body">
              <strong>{file?.originalFileName ?? `Minh chứng #${x.evidenceId ?? 'đã xóa'}`}</strong>
              <p><span className={`scan-status scan-status--${x.status.toLowerCase()}`}>{plagiarismStatus[x.status] ?? x.status}</span> · Copyleaks · {new Date(x.createdAt).toLocaleString('vi-VN')}</p>
              {x.status === 'HOAN_THANH' && <p>{x.matchedSourceCount ?? 0} nguồn · {x.totalWords ?? 0} từ · {x.creditsUsed ?? 0} credit</p>}
              {x.errorMessage && <p className="scan-error">{x.errorMessage}</p>}
              {x.reportUrl?.startsWith('http') && <a href={x.reportUrl} target="_blank" rel="noreferrer">Xem báo cáo Copyleaks ↗</a>}
            </div>
          </article>
        })}
      </div>}
    </section>
  </>
}

type ProductFeedback = { id: number; senderName: string; content: string; createdAt: string }
function FeedbackPanel({ productId, user }: { productId: number; user: Profile | null }) {
  const [items, setItems] = useState<ProductFeedback[]>([])
  const [content, setContent] = useState('')
  const [error, setError] = useState('')
  const canSend = user?.roles.some(x => x.role === 'PHONG_QLKH' || x.role === 'QUAN_TRI') ?? false
  const load = () => api<ProductFeedback[]>(`/products/${productId}/feedback`).then(setItems).catch(e => setError((e as Error).message))
  useEffect(() => { void load() }, [productId])
  async function send(e: FormEvent) {
    e.preventDefault(); setError('')
    try { await api(`/products/${productId}/feedback`, 'POST', { content }); setContent(''); await load() }
    catch (x) { setError((x as Error).message) }
  }
  return <section className="panel">
    <h2>Phản hồi từ Phòng Quản lý khoa học</h2>
    {error && <p className="alert">{error}</p>}
    {items.length === 0 ? <p className="empty">Chưa có phản hồi.</p> : <ul>{items.map(x => <li key={x.id}><strong>{x.senderName}</strong> · {new Date(x.createdAt).toLocaleString('vi-VN')}<br />{x.content}</li>)}</ul>}
    {canSend && <form onSubmit={send}><label>Nội dung phản hồi<textarea value={content} maxLength={2000} required onChange={e => setContent(e.target.value)} /></label><button className="primary" type="submit">Gửi phản hồi</button></form>}
  </section>
}

// ── ReviewPanel (for managers) ─────────────────────────────────────────────────
function ReviewPanel({ product, user, onReviewed }: { product: Product; user: Profile; onReviewed: (p: Product) => void }) {
  const [open, setOpen] = useState(false)
  const [action, setAction] = useState<'APPROVE' | 'REJECT'>('APPROVE')
  const [comment, setComment] = useState('')
  const [score, setScore] = useState('')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const showScore = needsScore(user, product) && action === 'APPROVE'

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); setBusy(true); setError('')
    try {
      const body: Record<string, unknown> = { action, comment: comment.trim() || null }
      if (showScore && score) body.scoreEquivalent = parseFloat(score)
      const updated = await api<Product>(`/products/${product.id}/review`, 'POST', body)
      onReviewed(updated); setOpen(false); setComment(''); setScore('')
    } catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  const stepLabel = workflow[product.status] ?? product.status
  return (
    <section className="panel review-panel">
      <div className="section-top">
        <div>
          <h2>Xét duyệt</h2>
          <p className="muted">Đang chờ xét duyệt từ bước: <strong>{stepLabel}</strong></p>
        </div>
        <button className="primary" onClick={() => setOpen(x => !x)}>{open ? 'Đóng' : '⚖️ Thực hiện xét duyệt'}</button>
      </div>
      {open && (
        <form className="review-form" onSubmit={submit}>
          {error && <p className="alert" role="alert">{error}</p>}
          <div className="review-actions">
            <label className={`review-radio${action === 'APPROVE' ? ' selected' : ''}`}>
              <input type="radio" name="action" value="APPROVE" checked={action === 'APPROVE'} onChange={() => setAction('APPROVE')} />
              ✅ Phê duyệt (chuyển bước tiếp)
            </label>
            <label className={`review-radio${action === 'REJECT' ? ' selected' : ''}`}>
              <input type="radio" name="action" value="REJECT" checked={action === 'REJECT'} onChange={() => setAction('REJECT')} />
              🔁 Yêu cầu bổ sung (trả lại giảng viên)
            </label>
          </div>
          {showScore && (
            <label>
              Điểm quy đổi <small className="muted">(Bắt buộc khi duyệt cuối)</small>
              <input type="number" min="0" max="999.99" step="0.01" value={score}
                onChange={e => setScore(e.target.value)} required={showScore} placeholder="Ví dụ: 1.5" />
            </label>
          )}
          <label>
            Ghi chú / Lý do {action === 'REJECT' && <strong>(bắt buộc khi trả lại)</strong>}
            <textarea rows={3} maxLength={1000} value={comment} onChange={e => setComment(e.target.value)}
              required={action === 'REJECT'} placeholder={action === 'REJECT' ? 'Nêu rõ lý do yêu cầu bổ sung…' : 'Nhận xét (không bắt buộc)…'} />
          </label>
          <div className="actions">
            <button type="submit" className={action === 'APPROVE' ? 'primary' : 'danger'} disabled={busy}>
              {busy ? 'Đang xử lý…' : action === 'APPROVE' ? 'Xác nhận phê duyệt' : 'Xác nhận trả lại'}
            </button>
            <button type="button" disabled={busy} onClick={() => { setOpen(false); setError('') }}>Hủy</button>
          </div>
        </form>
      )}
    </section>
  )
}

// ── ReviewHistoryPanel ─────────────────────────────────────────────────────────
function ReviewHistoryPanel({ productId }: { productId: number }) {
  const [history, setHistory] = useState<ReviewHistory[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    api<ReviewHistory[]>(`/products/${productId}/review-history`)
      .then(setHistory)
      .catch(e => setError((e as Error).message))
      .finally(() => setLoading(false))
  }, [productId])

  const actionIcon = (from: string, to: string) => {
    if (to === 'TRA_LAI') return '🔁'
    if (to === 'DA_DUYET') return '✅'
    if (to === 'NOP') return '📤'
    if (from === 'NOP' || from.includes('DUYET')) return '✅'
    return '➡️'
  }

  return (
    <section className="panel">
      <h2>Lịch sử xét duyệt</h2>
      {loading ? <p>Đang tải…</p> : error ? <p className="alert">{error}</p> :
        !history.length ? <p className="empty">Chưa có lịch sử xét duyệt.</p> :
          <ol className="review-timeline">
            {history.map(h => (
              <li key={h.id} className={`timeline-item${h.toStatus === 'TRA_LAI' ? ' timeline-item--reject' : h.toStatus === 'DA_DUYET' ? ' timeline-item--approved' : ''}`}>
                <span className="timeline-icon">{actionIcon(h.fromStatus, h.toStatus)}</span>
                <div className="timeline-body">
                  <div className="timeline-title">
                    <strong>{workflow[h.fromStatus] ?? h.fromStatus}</strong>
                    {' → '}
                    <strong>{workflow[h.toStatus] ?? h.toStatus}</strong>
                  </div>
                  <div className="timeline-meta">
                    {h.actorUsername ? `@${h.actorUsername}` : `Tài khoản #${h.actorAccountId}`}
                    {' · '}
                    {new Date(h.occurredAt).toLocaleString('vi-VN')}
                  </div>
                  {h.comment && <p className="timeline-comment">"{h.comment}"</p>}
                </div>
              </li>
            ))}
          </ol>
      }
    </section>
  )
}

// ── SnapshotPanel ──────────────────────────────────────────────────────────────
function SnapshotPanel({ productId }: { productId: number }) {
  const [snapshots, setSnapshots] = useState<ProductSnapshot[]>([])
  const [expanded, setExpanded] = useState<number | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    setLoading(true)
    api<ProductSnapshot[]>(`/products/${productId}/snapshots`)
      .then(setSnapshots)
      .catch(e => setError((e as Error).message))
      .finally(() => setLoading(false))
  }, [productId])

  return (
    <section className="panel">
      <h2>Lịch sử phiên bản</h2>
      <p className="muted">Mỗi lần gửi duyệt hoặc chuyển trạng thái, hệ thống lưu một bản chụp nội dung.</p>
      {loading ? <p>Đang tải…</p> : error ? <p className="alert">{error}</p> :
        !snapshots.length ? <p className="empty">Chưa có phiên bản nào được lưu.</p> :
          <ul className="snapshot-list">
            {[...snapshots].reverse().map(s => (
              <li key={s.version} className="snapshot-item">
                <button className="snapshot-header" onClick={() => setExpanded(expanded === s.version ? null : s.version)}>
                  <span className="snapshot-ver">v{s.version}</span>
                  <span className="snapshot-title">{s.title}</span>
                  <span className={`status-chip status-chip--${s.status.toLowerCase()}`}>{workflow[s.status] ?? s.status}</span>
                  <span className="snapshot-time">{new Date(s.snapshotAt).toLocaleString('vi-VN')}</span>
                  <span className="snapshot-toggle">{expanded === s.version ? '▲' : '▼'}</span>
                </button>
                {expanded === s.version && (
                  <dl className="product-grid snapshot-detail">
                    <div><dt>Tên</dt><dd>{s.title}</dd></div>
                    <div><dt>Trạng thái bài viết</dt><dd>{s.articleStatus ? article[s.articleStatus] : '—'}</dd></div>
                    <div><dt>Ngày công bố</dt><dd>{s.publishedDate ?? '—'}</dd></div>
                    <div><dt>Nơi công bố</dt><dd>{s.publicationInfo ?? '—'}</dd></div>
                    <div><dt>Chỉ số tạp chí</dt><dd>{s.journalIndex ?? '—'}</dd></div>
                    <div><dt>Phân loại tạp chí</dt><dd>{s.journalClassification ?? '—'}</dd></div>
                    <div><dt>Cấp đề tài</dt><dd>{s.projectLevel ?? '—'}</dd></div>
                    {s.description && <div className="wide"><dt>Mô tả</dt><dd>{s.description}</dd></div>}
                  </dl>
                )}
              </li>
            ))}
          </ul>
      }
    </section>
  )
}

// ── ProductForm ────────────────────────────────────────────────────────────────
function ProductForm({ product, onSubmit, busy, error, planItems, targets }: {
  product?: Product; onSubmit: (e: FormEvent<HTMLFormElement>) => void
  busy: boolean; error: string; planItems?: PlanItem[]; targets?: AvailableTarget[]
}) {
  const [selectedPlanItemId, setSelectedPlanItemId] = useState('')
  const selectedPlan = planItems?.find(x => x.id === Number(selectedPlanItemId))
  const isArticle = selectedPlan?.productTypeName.trim().toLocaleLowerCase('vi-VN') === 'bài báo'
  const isProject = selectedPlan?.productTypeName.trim().toLocaleLowerCase('vi-VN') === 'đề tài'
  return <form onSubmit={onSubmit}>
    {error && <p className="alert">{error}</p>}
    <div className="form-grid">
      {planItems && <><label className="wide">1. Nội dung đã cam kết trong kế hoạch
        <select name="researchPlanItemId" required value={selectedPlanItemId} onChange={e => setSelectedPlanItemId(e.target.value)}>
          <option value="">Chọn nội dung kế hoạch</option>
          {planItems.map(x => <option value={x.id} key={x.id}>{x.academicYearCode} · {x.productTypeName} · {x.name}</option>)}
        </select>
        <small className="muted">Dùng để đối chiếu sản phẩm này với cam kết đầu năm; đây không phải nơi gửi duyệt.</small>
      </label>
      {selectedPlan && <div className="wide product-context"><strong>Loại đã chọn: {selectedPlan.productTypeName}</strong><span>{isArticle ? 'Bài báo có thể được gắn với chỉ tiêu được giao.' : 'Loại này không dùng chỉ tiêu bài báo; chỉ cần khai báo thông tin và minh chứng.'}</span></div>}
      {isArticle && <label className="wide">2. Chỉ tiêu bài báo được giao <small className="muted">(nếu áp dụng)</small>
        <select name="researchTargetId" defaultValue="">
          <option value="">Không gắn chỉ tiêu cụ thể</option>
          {(targets ?? []).map(x => <option value={x.id} key={x.id}>
            {x.academicYearCode} · {x.journalConferenceName ?? 'Chỉ tiêu bài báo chung'} · phân bổ {x.allocatedArticleCount} bài
          </option>)}
        </select>
        {targets?.length === 0 && <small className="muted">Bạn chưa được giao chỉ tiêu bài báo đang công bố; vẫn có thể lưu sản phẩm nhưng không tính vào chỉ tiêu.</small>}
      </label>}</>}
      <label className="wide">{planItems ? '3. Tên sản phẩm thực tế' : 'Tên sản phẩm'}<input name="title" required minLength={3} maxLength={500} defaultValue={product?.title} /></label>
      {(isArticle || !planItems) && <label>Trạng thái bài viết
        <select name="articleStatus" defaultValue={product?.articleStatus ?? ''}>
          <option value="">Không áp dụng</option>
          {Object.entries(article).map(([v, n]) => <option value={v} key={v}>{n}</option>)}
        </select>
      </label>}
      {(isArticle || !planItems) && <label>Ngày công bố<input type="date" name="publishedDate" defaultValue={product?.publishedDate ?? ''} /></label>}
      <label>Nơi công bố / đơn vị cấp<input name="publicationInfo" maxLength={500} defaultValue={product?.publicationInfo ?? ''} /></label>
      {(isArticle || !planItems) && <label>Chỉ số tạp chí<input name="journalIndex" maxLength={100} placeholder="Scopus, WoS…" defaultValue={product?.journalIndex ?? ''} /></label>}
      {(isArticle || !planItems) && <label>Phân loại tạp chí<input name="journalClassification" maxLength={100} placeholder="Q1, Q2…" defaultValue={product?.journalClassification ?? ''} /></label>}
      {(isProject || !planItems) && <label>Cấp đề tài<input name="projectLevel" maxLength={100} placeholder="Trường, Bộ, Nhà nước…" defaultValue={product?.projectLevel ?? ''} /></label>}
      <label className="wide">Mô tả<textarea name="description" rows={4} defaultValue={product?.description ?? ''} /></label>
    </div>
    <button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu sản phẩm'}</button>
  </form>
}

// ── CoAuthors ──────────────────────────────────────────────────────────────────
function CoAuthors({ product, user, onChanged }: { product: Product; user: Profile | null; onChanged: () => void }) {
  const [error, setError] = useState('')
  const [lecturers, setLecturers] = useState<LecturerOption[]>([])
  const canManage = canManageProductAttachments(user, product)
  useEffect(() => {
    if (!canManage) return
    api<LecturerOption[]>('/products/co-author-options')
      .then(setLecturers)
      .catch(e => setError((e as Error).message))
  }, [canManage])
  async function add(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const form = e.currentTarget; const f = new FormData(form)
    try {
      await api(`/products/${product.id}/co-authors`, 'POST', { lecturerId: Number(f.get('lecturerId')), displayOrder: Number(f.get('displayOrder')), authorRole: f.get('authorRole') })
      form.reset(); onChanged()
    } catch (x) { setError((x as Error).message) }
  }
  async function remove(id: number) {
    try { await api(`/products/${product.id}/co-authors/${id}`, 'DELETE'); onChanged() }
    catch (x) { setError((x as Error).message) }
  }
  return <section className="panel">
    <h2>Đồng tác giả</h2>
    {canManage && error && <p className="alert">{error}</p>}
    <ul className="product-list">
      {product.coAuthors.map(a => <li key={a.id}>
        <span>{a.displayOrder}. {a.lecturerFullName}{a.lecturerCode ? ` (${a.lecturerCode})` : ''} — {a.authorRole}</span>
        {canManage && <button className="danger" onClick={() => remove(a.id)}>Bỏ</button>}
      </li>)}
    </ul>
    {canManage && (
      <form className="inline-editor product-author" onSubmit={add}>
        <select name="lecturerId" required defaultValue="">
          <option value="" disabled>Chọn giảng viên đồng tác giả</option>
          {lecturers.map(lecturer => <option key={lecturer.id} value={lecturer.id}>
            {lecturer.fullName} ({lecturer.code}) · {lecturer.departmentName}
          </option>)}
        </select>
        <input name="displayOrder" type="number" min="1" required defaultValue="1" />
        <input name="authorRole" required maxLength={100} defaultValue="DONG_TAC_GIA" />
        <button>Thêm</button>
      </form>
    )}
  </section>
}

// ── Evidences ──────────────────────────────────────────────────────────────────
function Evidences({ product, user, onChanged }: { product: Product; user: Profile | null; onChanged: () => void }) {
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  const canManage = canManageProductAttachments(user, product)
  async function upload(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const form = e.currentTarget; const data = new FormData(form); setBusy(true); setError('')
    try { await apiForm(`/products/${product.id}/evidences`, data); form.reset(); onChanged() }
    catch (x) { setError((x as Error).message) } finally { setBusy(false) }
  }
  async function remove(id: number) {
    try { await api(`/products/${product.id}/evidences/${id}`, 'DELETE'); onChanged() }
    catch (x) { setError((x as Error).message) }
  }
  return <section className="panel">
    <h2>Minh chứng</h2>
    {error && <p className="alert">{error}</p>}
    <ul className="product-list">
      {product.evidences.map(e => <li key={e.id}>
        <a href={`/api/products/${product.id}/evidences/${e.id}/download`}>{e.originalFileName}</a>
        <span>{(e.fileSizeBytes / 1024).toFixed(1)} KB</span>
        {canManage && <button className="danger" onClick={() => remove(e.id)}>Xóa</button>}
      </li>)}
    </ul>
    {canManage && (
      <form className="product-upload" onSubmit={upload}>
        <label>Tệp minh chứng<input name="file" type="file" required accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.jpg,.jpeg,.png,.zip" /></label>
        <label>Mô tả<input name="description" maxLength={300} /></label>
        <button disabled={busy}>{busy ? 'Đang tải…' : 'Tải lên'}</button>
      </form>
    )}
  </section>
}

// ── Helpers ────────────────────────────────────────────────────────────────────
function formBody(f: FormData, researchPlanItemId?: number) {
  const text = (k: string) => String(f.get(k) ?? '').trim() || null
  const researchTargetId = Number(f.get('researchTargetId')) || null
  return {
    ...(researchPlanItemId ? { researchPlanItemId } : {}),
    ...(researchPlanItemId ? { researchTargetId } : {}),
    title: text('title'), description: text('description'),
    publicationInfo: text('publicationInfo'), publishedDate: text('publishedDate'),
    articleStatus: text('articleStatus'), journalIndex: text('journalIndex'),
    journalClassification: text('journalClassification'), projectLevel: text('projectLevel'),
  }
}
