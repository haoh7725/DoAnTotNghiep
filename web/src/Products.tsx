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
type ReviewHistory = { id: number; productId: number; actorAccountId: number; actorUsername: string; fromStatus: string; toStatus: string; comment: string | null; occurredAt: string }
type ProductSnapshot = { productId: number; version: number; snapshotByAccountId: number; snapshotAt: string; title: string; description: string | null; publicationInfo: string | null; publishedDate: string | null; articleStatus: string | null; journalIndex: string | null; journalClassification: string | null; projectLevel: string | null; status: string }

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

// ── ProductWorkspace (entry) ───────────────────────────────────────────────────
export function ProductWorkspace({ user }: { user?: Profile | null }) {
  const [items, setItems] = useState<Summary[]>([])
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

  if (selected !== null)
    return <ProductDetail id={selected} user={user ?? null} onBack={() => { setSelected(null); setRevision(x => x + 1) }} />

  return <>
    <p className="eyebrow">SẢN PHẨM KHOA HỌC</p>
    <h1>Sản phẩm của tôi</h1>
    <p className="muted">Quản lý bài viết, đồng tác giả và minh chứng gắn với nội dung kế hoạch.</p>
    {error && <p className="alert">{error}</p>}
    <CreateProduct onCreated={id => { setRevision(x => x + 1); setSelected(id) }} />
    <section className="panel">
      <h2>Danh sách sản phẩm</h2>
      {loading ? <p>Đang tải…</p> : <>
        <div className="table-wrap"><table><thead><tr>
          <th>Tên sản phẩm</th><th>Trạng thái bài viết</th><th>Xét duyệt</th><th>Điểm quy đổi</th><th>Cập nhật</th><th></th>
        </tr></thead><tbody>
          {items.map(p => <tr key={p.id}>
            <td><strong>{p.title}</strong><br /><small>Kế hoạch #{p.researchPlanItemId}</small></td>
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

  useEffect(() => {
    if (open && !plans.length)
      api<PlanItem[]>('/products/available-plan-items').then(setPlans).catch(e => setError((e as Error).message))
  }, [open, plans.length])

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const f = new FormData(e.currentTarget)
    const itemId = Number(f.get('researchPlanItemId'))
    setBusy(true); setError('')
    try { const p = await api<Product>(`/research-plan-items/${itemId}/products`, 'POST', formBody(f, itemId)); onCreated(p.id) }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  return <section className="panel">
    <div className="section-top">
      <div><h2>Thêm sản phẩm</h2><p className="muted product-sub">Chọn nội dung trong kế hoạch nghiên cứu của bạn.</p></div>
      <button className="primary" onClick={() => setOpen(x => !x)}>{open ? 'Đóng' : '+ Thêm'}</button>
    </div>
    {open && <ProductForm onSubmit={submit} busy={busy} error={error} planItems={plans} />}
  </section>
}

// ── ProductDetail ──────────────────────────────────────────────────────────────
function ProductDetail({ id, user, onBack }: { id: number; user: Profile | null; onBack: () => void }) {
  const [p, setP] = useState<Product | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [revision, setRevision] = useState(0)
  const [tab, setTab] = useState<'info' | 'history' | 'snapshots'>('info')

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
        <button className={tab === 'history' ? 'active' : ''} onClick={() => setTab('history')}>Lịch sử xét duyệt</button>
        <button className={tab === 'snapshots' ? 'active' : ''} onClick={() => setTab('snapshots')}>Phiên bản</button>
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

        <CoAuthors product={p} onChanged={() => setRevision(x => x + 1)} />
        <Evidences product={p} onChanged={() => setRevision(x => x + 1)} />
      </>}

      {tab === 'history' && <ReviewHistoryPanel productId={id} key={`hist-${revision}`} />}
      {tab === 'snapshots' && <SnapshotPanel productId={id} key={`snap-${revision}`} />}
    </>}
  </>
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
function ProductForm({ product, onSubmit, busy, error, planItems }: {
  product?: Product; onSubmit: (e: FormEvent<HTMLFormElement>) => void
  busy: boolean; error: string; planItems?: PlanItem[]
}) {
  return <form onSubmit={onSubmit}>
    {error && <p className="alert">{error}</p>}
    <div className="form-grid">
      {planItems && <label className="wide">Nội dung kế hoạch
        <select name="researchPlanItemId" required defaultValue="">
          <option value="">Chọn nội dung kế hoạch</option>
          {planItems.map(x => <option value={x.id} key={x.id}>{x.academicYearCode} · {x.productTypeName} · {x.name}</option>)}
        </select>
      </label>}
      <label className="wide">Tên sản phẩm<input name="title" required minLength={3} maxLength={500} defaultValue={product?.title} /></label>
      <label>Trạng thái bài viết
        <select name="articleStatus" defaultValue={product?.articleStatus ?? ''}>
          <option value="">Không áp dụng</option>
          {Object.entries(article).map(([v, n]) => <option value={v} key={v}>{n}</option>)}
        </select>
      </label>
      <label>Ngày công bố<input type="date" name="publishedDate" defaultValue={product?.publishedDate ?? ''} /></label>
      <label>Nơi công bố<input name="publicationInfo" maxLength={500} defaultValue={product?.publicationInfo ?? ''} /></label>
      <label>Chỉ số tạp chí<input name="journalIndex" maxLength={100} placeholder="Scopus, WoS…" defaultValue={product?.journalIndex ?? ''} /></label>
      <label>Phân loại tạp chí<input name="journalClassification" maxLength={100} placeholder="Q1, Q2…" defaultValue={product?.journalClassification ?? ''} /></label>
      <label>Cấp đề tài<input name="projectLevel" maxLength={100} placeholder="Trường, Bộ, Nhà nước…" defaultValue={product?.projectLevel ?? ''} /></label>
      <label className="wide">Mô tả<textarea name="description" rows={4} defaultValue={product?.description ?? ''} /></label>
    </div>
    <button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu sản phẩm'}</button>
  </form>
}

// ── CoAuthors ──────────────────────────────────────────────────────────────────
function CoAuthors({ product, onChanged }: { product: Product; onChanged: () => void }) {
  const [error, setError] = useState('')
  async function add(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const f = new FormData(e.currentTarget)
    try {
      await api(`/products/${product.id}/co-authors`, 'POST', { lecturerId: Number(f.get('lecturerId')), displayOrder: Number(f.get('displayOrder')), authorRole: f.get('authorRole') })
      e.currentTarget.reset(); onChanged()
    } catch (x) { setError((x as Error).message) }
  }
  async function remove(id: number) {
    try { await api(`/products/${product.id}/co-authors/${id}`, 'DELETE'); onChanged() }
    catch (x) { setError((x as Error).message) }
  }
  return <section className="panel">
    <h2>Đồng tác giả</h2>
    {error && <p className="alert">{error}</p>}
    <ul className="product-list">
      {product.coAuthors.map(a => <li key={a.id}>
        <span>{a.displayOrder}. {a.lecturerFullName}{a.lecturerCode ? ` (${a.lecturerCode})` : ''} — {a.authorRole}</span>
        {editable(product) && <button className="danger" onClick={() => remove(a.id)}>Bỏ</button>}
      </li>)}
    </ul>
    {editable(product) && (
      <form className="inline-editor product-author" onSubmit={add}>
        <input name="lecturerId" type="number" min="1" required placeholder="ID giảng viên" />
        <input name="displayOrder" type="number" min="1" required defaultValue="1" />
        <input name="authorRole" required maxLength={100} defaultValue="DONG_TAC_GIA" />
        <button>Thêm</button>
      </form>
    )}
  </section>
}

// ── Evidences ──────────────────────────────────────────────────────────────────
function Evidences({ product, onChanged }: { product: Product; onChanged: () => void }) {
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  async function upload(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const data = new FormData(e.currentTarget); setBusy(true)
    try { await apiForm(`/products/${product.id}/evidences`, data); e.currentTarget.reset(); onChanged() }
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
        {editable(product) && <button className="danger" onClick={() => remove(e.id)}>Xóa</button>}
      </li>)}
    </ul>
    {product.status !== 'DA_DUYET' && (
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
  return {
    ...(researchPlanItemId ? { researchPlanItemId } : {}),
    title: text('title'), description: text('description'),
    publicationInfo: text('publicationInfo'), publishedDate: text('publishedDate'),
    articleStatus: text('articleStatus'), journalIndex: text('journalIndex'),
    journalClassification: text('journalClassification'), projectLevel: text('projectLevel'),
  }
}
