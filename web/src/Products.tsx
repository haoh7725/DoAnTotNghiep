import { useEffect, useMemo, useState } from 'react'
import type { FormEvent } from 'react'
import { api, apiForm } from './api'
import type { Profile } from './api'
import './Products.css'
import type { Classification } from './ProductTaxonomy'

// ── Types ──────────────────────────────────────────────────────────────────────
type Summary = { id: number; researchPlanItemId: number; title: string; articleStatus: string | null; status: string; scoreEquivalent: number | null; updatedAt: string; createdAt: string }
type CoAuthor = { id: number; lecturerId: number; lecturerFullName: string; lecturerCode: string | null; displayOrder: number; authorRole: string }
type Evidence = { id: number; originalFileName: string; fileSizeBytes: number; description: string | null; uploadedAt: string }
type Product = Summary & { workIdentifier: string | null; requiredReviewRole: string | null; canReview: boolean; canEdit: boolean; productTypeId: number; productTypeCode: string; submittedByLecturerId: number; submittedByLecturerFullName: string; description: string | null; publicationInfo: string | null; publishedDate: string | null; journalIndex: string | null; journalClassification: string | null; projectLevel: string | null; researchTargetId: number | null; journalConferenceId: number | null; externalAuthorCount: number; classificationId: number | null; coAuthors: CoAuthor[]; evidences: Evidence[] }
type PlanItem = { id: number; name: string; productTypeId: number; productTypeCode: string; productTypeName: string; academicYearCode: string; academicYearId: number; status: string }
type LecturerOption = { id: number; fullName: string; code: string; departmentName: string }
type AvailableTarget = { id: number; productTypeId: number; productTypeCode: string; productTypeName: string; academicYearId: number; academicYearCode: string; allocatedArticleCount: number; journalConferenceId: number | null; journalConferenceName: string | null; deadline: string | null; status: string }
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
  DA_NHAN_XET: 'Đã nhận xét', DA_NHAN_DANG: 'Đã nhận xét', DA_XUAT_BAN: 'Đã xuất bản',
}

const editable = (p: Product) => ['NHAP', 'TRA_LAI'].includes(p.status)
const canManageProductAttachments = (user: Profile | null, p: Product) =>
  p.canEdit && (user?.roles.some(r => r.role === 'GIANG_VIEN') ?? false)
const canManagerReview = (user: Profile | null, p: Product) => !!user && p.canReview
const needsScore = (user: Profile | null, p: Product) => !!user && p.requiredReviewRole === 'PHONG_QLKH'
type PendingProduct = {
  id: number
  title: string
  status: string
  lecturerName: string
  departmentName: string
}

export function ProductReviewWorkspace({ user, initialProductId }: { user: Profile; initialProductId?: number | null }) {
  const [items, setItems] = useState<PendingProduct[]>([])
  const [selected, setSelected] = useState<number | null>(initialProductId ?? null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [revision, setRevision] = useState(0)
  const [query, setQuery] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [departmentFilter, setDepartmentFilter] = useState('')
  const [page, setPage] = useState(1)
  const pageSize = 10

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

  const departments = useMemo(
    () => [...new Set(items.map(item => item.departmentName))].sort((a, b) => a.localeCompare(b, 'vi')),
    [items]
  )
  const statuses = useMemo(
    () => [...new Set(items.map(item => item.status))],
    [items]
  )
  const filteredItems = useMemo(() => {
    const keyword = query.trim().toLocaleLowerCase('vi')
    return items.filter(item => {
      const matchesKeyword = !keyword || [item.title, item.lecturerName, item.departmentName]
        .some(value => value.toLocaleLowerCase('vi').includes(keyword))
      return matchesKeyword
        && (!statusFilter || item.status === statusFilter)
        && (!departmentFilter || item.departmentName === departmentFilter)
    })
  }, [departmentFilter, items, query, statusFilter])
  const pageCount = Math.max(1, Math.ceil(filteredItems.length / pageSize))
  const visibleItems = filteredItems.slice((page - 1) * pageSize, page * pageSize)

  useEffect(() => { setPage(1) }, [query, statusFilter, departmentFilter])
  useEffect(() => { if (page > pageCount) setPage(pageCount) }, [page, pageCount])

  if (selected !== null) {
    return <ProductDetail
      key={selected}
      id={selected}
      user={user}
      allowProgressUpdate={false}
      onBack={() => {
        setSelected(null)
        setRevision(value => value + 1)
      }}
    />
  }

  return <>
    <p className="eyebrow">XÉT DUYỆT SẢN PHẨM</p>
    <h1>Sản phẩm chờ duyệt</h1>
    <p className="muted">
      Tìm và xử lý hồ sơ trong phạm vi được phân quyền. Danh sách được chia trang để dễ theo dõi khi dữ liệu lớn.
    </p>

    <section className="panel">
      <div className="section-top review-list-heading">
        <div>
          <h2>Danh sách cần xử lý</h2>
          <p className="muted">{loading ? 'Đang tải dữ liệu…' : `${filteredItems.length}/${items.length} hồ sơ phù hợp`}</p>
        </div>
        <button disabled={loading} onClick={() => setRevision(value => value + 1)}>Làm mới</button>
      </div>

      <div className="table-toolbar" aria-label="Bộ lọc sản phẩm chờ duyệt">
        <label className="table-search">Tìm kiếm
          <input
            type="search"
            value={query}
            onChange={event => setQuery(event.target.value)}
            placeholder="Tên sản phẩm hoặc giảng viên"
          />
        </label>
        <label>Trạng thái
          <select value={statusFilter} onChange={event => setStatusFilter(event.target.value)}>
            <option value="">Tất cả trạng thái</option>
            {statuses.map(status => <option value={status} key={status}>{workflow[status] ?? status}</option>)}
          </select>
        </label>
        <label>Bộ môn
          <select value={departmentFilter} onChange={event => setDepartmentFilter(event.target.value)}>
            <option value="">Tất cả bộ môn</option>
            {departments.map(department => <option value={department} key={department}>{department}</option>)}
          </select>
        </label>
      </div>

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
              {visibleItems.map(item => <tr key={item.id}>
                <td>{item.title}</td>
                <td>{item.lecturerName}</td>
                <td>{item.departmentName}</td>
                <td><span className={`status-chip status-chip--${item.status.toLowerCase()}`}>{workflow[item.status] ?? item.status}</span></td>
                <td>
                  <button className="review-action" onClick={() => setSelected(item.id)}>
                    Xem và xét duyệt
                  </button>
                </td>
              </tr>)}
            </tbody>
          </table>
        </div>

        {filteredItems.length > pageSize && <nav className="table-pagination" aria-label="Phân trang sản phẩm chờ duyệt">
          <button disabled={page === 1} onClick={() => setPage(value => value - 1)}>← Trang trước</button>
          <span>Trang <strong>{page}</strong> / {pageCount}</span>
          <button disabled={page === pageCount} onClick={() => setPage(value => value + 1)}>Trang sau →</button>
        </nav>}

        {items.length === 0
          ? <p className="empty">Không có sản phẩm đang chờ bạn xét duyệt.</p>
          : filteredItems.length === 0 && <p className="empty">Không tìm thấy hồ sơ phù hợp với bộ lọc.</p>}
      </>}
    </section>
  </>
}

// ── ProductWorkspace (entry) ───────────────────────────────────────────────────
export function ProductWorkspace({ user, initialProductId, yearId }: { user?: Profile | null; initialProductId?: number | null; yearId?: string }) {
  const [query, setQuery] = useState('')
  const [typeFilter, setTypeFilter] = useState('')
  const [statusFilter, setStatusFilter] = useState('')
  const [items, setItems] = useState<Summary[]>([])
  const [planItems, setPlanItems] = useState<PlanItem[]>([])
  const [selected, setSelected] = useState<number | null>(initialProductId ?? null)
  const [revision, setRevision] = useState(0)
  const [error, setError] = useState('')
  const [loading, setLoading] = useState(true)

  useEffect(() => {
    let active = true; setLoading(true); setError('')
    api<Summary[]>(yearId ? `/products/mine?academicYearId=${yearId}` : '/products/mine')
      .then(x => { if (active) setItems(x) })
      .catch(e => { if (active) setError((e as Error).message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [revision, yearId])

  useEffect(() => {
    api<PlanItem[]>('/products/available-plan-items').then(setPlanItems).catch(() => {
      // Danh sách sản phẩm vẫn dùng được nếu chưa tải được nhãn kế hoạch.
    })
  }, [revision, yearId])

  if (selected !== null)
    return <ProductDetail id={selected} user={user ?? null} allowProgressUpdate onBack={() => { setSelected(null); setRevision(x => x + 1) }} />

  return <>
    <p className="eyebrow">SẢN PHẨM KHOA HỌC</p>
    <h1>Sản phẩm của tôi</h1>
    <p className="muted">Khai báo sản phẩm thực tế và minh chứng để gửi xét duyệt. Kế hoạch chỉ là phần cam kết đầu năm.</p>
    {error && <p className="alert">{error}</p>}
    <CreateProduct yearId={yearId} onCreated={id => { setRevision(x => x + 1); setSelected(id) }} />
    <section className="panel">
      <div className="section-top"><h2>Danh sách sản phẩm</h2><span>{items.length} sản phẩm trong năm đang chọn</span></div>
      <div className="product-filters"><label>Tìm sản phẩm<input value={query} onChange={e => setQuery(e.target.value)} placeholder="Nhập tên công trình" /></label>
        <label>Loại<select value={typeFilter} onChange={e => setTypeFilter(e.target.value)}><option value="">Tất cả</option>{Array.from(new Map(planItems.map(x => [x.productTypeCode, x.productTypeName]))).map(([code, name]) => <option value={code} key={code}>{name}</option>)}</select></label>
        <label>Xét duyệt<select value={statusFilter} onChange={e => setStatusFilter(e.target.value)}><option value="">Tất cả</option>{Object.entries(workflow).map(([code, name]) => <option value={code} key={code}>{name}</option>)}</select></label>
      </div>
      {loading ? <p>Đang tải…</p> : <>
        <div className="table-wrap"><table><thead><tr>
          <th>Tên sản phẩm</th><th>Trạng thái bài viết</th><th>Xét duyệt</th><th>Cập nhật</th><th></th>
        </tr></thead><tbody>
            {items.filter(p => p.title.toLocaleLowerCase('vi-VN').includes(query.toLocaleLowerCase('vi-VN')) && (!statusFilter || p.status === statusFilter) && (!typeFilter || planItems.find(x => x.id === p.researchPlanItemId)?.productTypeCode === typeFilter)).map(p => <tr key={p.id}>
              <td><strong>{p.title}</strong><br /><small>{(() => {
                const plan = planItems.find(item => item.id === p.researchPlanItemId)
                return plan ? `${plan.productTypeName} · ${plan.name}` : 'Nội dung kế hoạch đã liên kết'
              })()}</small></td>
              <td>{p.articleStatus ? article[p.articleStatus] ?? p.articleStatus : '—'}</td>
              <td><span className={`status-chip status-chip--${p.status.toLowerCase()}`}>{workflow[p.status] ?? p.status}</span></td>

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
function CreateProduct({ onCreated, yearId }: { onCreated: (id: number) => void; yearId?: string }) {
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
      const body = { ...formBody(f, itemId), journalConferenceId: target?.journalConferenceId ?? (Number(f.get('journalConferenceId')) || null) }
      const p = await api<Product>(`/research-plan-items/${itemId}/products`, 'POST', body)
      onCreated(p.id)
    }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

  return <section className="panel">
    <div className="section-top">
      <div><h2>Khai báo sản phẩm thực tế</h2><p className="muted product-sub">Chọn nội dung kế hoạch, lưu thông tin cơ bản rồi bổ sung tác giả và minh chứng.</p></div>
      <button className="primary" onClick={() => setOpen(x => !x)}>{open ? 'Đóng' : '+ Thêm'}</button>
    </div>
    {open && <ProductForm onSubmit={submit} busy={busy} error={error} planItems={plans} targets={targets} yearId={yearId} />}
  </section>
}

// ── ProductDetail ──────────────────────────────────────────────────────────────
function ProductDetail({ id, user, allowProgressUpdate = false, onBack }: { id: number; user: Profile | null; allowProgressUpdate?: boolean; onBack: () => void }) {
  const [p, setP] = useState<Product | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [editing, setEditing] = useState(false)
  const [editingProgress, setEditingProgress] = useState(false)
  const [revision, setRevision] = useState(0)
  const [linkTargets, setLinkTargets] = useState<AvailableTarget[]>([])
  const [planItems, setPlanItems] = useState<PlanItem[]>([])
  const [tab, setTab] = useState<'info' | 'plagiarism' | 'history' | 'snapshots' | 'feedback'>('info')

  useEffect(() => {
    let active = true; setError('')
    api<Product>(`/products/${id}`)
      .then(x => { if (active) setP(x) })
      .catch(e => { if (active) setError((e as Error).message) })
    return () => { active = false }
  }, [id, revision])

  useEffect(() => {
    if (!user?.roles.some(r => r.role === 'GIANG_VIEN')) return
    api<AvailableTarget[]>('/products/available-research-targets').then(setLinkTargets).catch(() => setLinkTargets([]))
    api<PlanItem[]>('/products/available-plan-items').then(setPlanItems).catch(() => setPlanItems([]))
  }, [user])

  async function linkTarget(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const targetId = Number(new FormData(e.currentTarget).get('researchTargetId'))
    setBusy(true); setError('')
    try { setP(await api<Product>(`/products/${id}/link-target`, 'POST', { researchTargetId: targetId })) }
    catch (x) { setError((x as Error).message) }
    finally { setBusy(false) }
  }

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

  async function saveProgress(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    const form = new FormData(e.currentTarget)
    const articleStatus = String(form.get('articleStatus') ?? '')
    const publishedDate = String(form.get('publishedDate') ?? '') || null
    if (articleStatus === 'DA_XUAT_BAN' && !publishedDate) {
      setError('Bài viết đã xuất bản phải có ngày công bố.')
      return
    }
    setBusy(true); setError('')
    try {
      setP(await api<Product>(`/products/${id}/article-progress`, 'PATCH', {
        articleStatus,
        publishedDate,
        publicationInfo: String(form.get('publicationInfo') ?? '').trim() || null,
      }))
      setEditingProgress(false)
    } catch (x) { setError((x as Error).message) }
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
        {p.scoreEquivalent !== null && <> · <strong>Điểm thẩm định: {p.scoreEquivalent}</strong></>}
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
        {p.researchTargetId === null && planItems.some(x => x.id === p.researchPlanItemId) &&
          <section className="panel">
            <h2>Liên kết với chỉ tiêu được giao</h2>
            <p className="muted">Giảng viên chọn chỉ tiêu đúng loại sản phẩm của mình. Bài báo chỉ được tính sau khi xuất bản; các loại khác được tính khi được công nhận.</p>
            <form onSubmit={linkTarget}>
              <label>Chỉ tiêu
                <select name="researchTargetId" required defaultValue="">
                  <option value="">Chọn chỉ tiêu phù hợp</option>
                  {linkTargets.filter(x => x.status === 'DA_KHOA' && x.productTypeId === p.productTypeId && (!x.journalConferenceId || !p.journalConferenceId || x.journalConferenceId === p.journalConferenceId)).map(x =>
                    <option key={x.id} value={x.id}>{x.academicYearCode} · {x.productTypeName} · {x.journalConferenceName ?? 'Chỉ tiêu chung'}</option>)}
                </select>
              </label>
              <button className="primary" disabled={busy}>Liên kết với chỉ tiêu</button>
            </form>
          </section>}
        {p.status === 'DA_DUYET' && !p.publishedDate && user?.roles.some(r => ['PHONG_QLKH', 'QUAN_TRI'].includes(r.role)) &&
          <section className="panel"><h2>Bổ sung ngày công nhận còn thiếu</h2><p className="muted">Chỉ bổ sung ngày còn thiếu và lưu lịch sử. Kết quả đã chốt được giữ nguyên; tạo đánh giá mới để áp dụng kỳ ghi nhận theo ngày này.</p>
            <form onSubmit={async e => {
              e.preventDefault(); const f = new FormData(e.currentTarget); setBusy(true); setError('')
              try { setP(await api<Product>(`/products/${p.id}/recognition-date`, 'POST', { publishedDate: f.get('publishedDate'), reason: f.get('reason') })); setRevision(x => x + 1) }
              catch (x) { setError((x as Error).message) } finally { setBusy(false) }
            }}><label>Ngày công bố / nghiệm thu<input type="date" name="publishedDate" required /></label>
              <label>Lý do bổ sung<textarea name="reason" required maxLength={1000} /></label><button className="primary" disabled={busy}>Bổ sung ngày và lưu lịch sử</button></form>
          </section>}
        {p.researchTargetId !== null && p.canEdit && <section className="panel"><h2>Chỉ tiêu đã liên kết</h2><p className="muted">Bài này đang giữ một suất đăng ký. Có thể bỏ liên kết khi còn nháp hoặc bị trả lại để thay bài khác.</p>
          <button disabled={busy} onClick={async () => { setBusy(true); setError(''); try { setP(await api<Product>(`/products/${p.id}/link-target`, 'DELETE')) } catch (x) { setError((x as Error).message) } finally { setBusy(false) } }}>Bỏ liên kết chỉ tiêu</button></section>}
        {/* Manager review panel */}
        {canManagerReview(user, p) && (
          <ReviewPanel product={p} user={user!} onReviewed={updated => { setP(updated); setRevision(x => x + 1) }} />
        )}

        {/* Product info */}
        {editing
          ? <section className="panel">
            <ProductForm product={p} onSubmit={save} busy={busy} error={error} targets={linkTargets} />
            <button disabled={busy} onClick={() => setEditing(false)}>Hủy</button>
          </section>
          : <section className="panel">
            <div className="section-top">
              <h2>Thông tin</h2>
              <div className="actions">
                {allowProgressUpdate && p.status !== 'DA_DUYET' && !user?.roles.some(r => r.role === 'GIANG_VIEN') &&
                  <button onClick={() => setEditingProgress(x => !x)}>Cập nhật tiến độ bài viết</button>}
                {p.canEdit && <>
                  <button onClick={() => setEditing(true)}>Chỉnh sửa nội dung</button>
                  <button className="primary" disabled={busy} onClick={submitProduct}>Gửi duyệt</button>
                </>}
              </div>
            </div>
            {editingProgress && <form className="product-progress-form" onSubmit={saveProgress}>
              <h3>Tiến độ xuất bản</h3>
              <label>Trạng thái bài viết<select name="articleStatus" required defaultValue={p.articleStatus ?? 'DANG_VIET'}>
                <option value="DANG_VIET">Đang viết</option><option value="DANG_PHAN_BIEN">Đang phản biện</option>
                <option value="DA_NHAN_XET">Đã nhận xét</option><option value="DA_XUAT_BAN">Đã xuất bản</option>
              </select></label>
              <label>Ngày công bố<input name="publishedDate" type="date" max={new Date().toISOString().slice(0, 10)} defaultValue={p.publishedDate ?? ''} /></label>
              <label>Nơi công bố<input name="publicationInfo" maxLength={500} defaultValue={p.publicationInfo ?? ''} /></label>
              <div className="actions"><button className="primary" disabled={busy}>Lưu tiến độ</button><button type="button" onClick={() => setEditingProgress(false)}>Hủy</button></div>
            </form>}
            {p.status === 'TRA_LAI' && (
              <p className="alert review-alert" role="alert">
                ⚠️ Sản phẩm được yêu cầu bổ sung. Vui lòng chỉnh sửa và gửi duyệt lại.
              </p>
            )}
            <dl className="product-grid">
              <div><dt>DOI / mã công trình</dt><dd>{p.workIdentifier ?? '—'}</dd></div>
              <div><dt>Trạng thái bài viết</dt><dd>{p.articleStatus ? article[p.articleStatus] : '—'}</dd></div>
              <div><dt>Ngày công bố / công nhận / nghiệm thu</dt><dd>{p.publishedDate ?? '—'}</dd></div>
              <div><dt>Nơi công bố</dt><dd>{p.publicationInfo ?? '—'}</dd></div>
              <div><dt>Chỉ mục công bố</dt><dd>{p.journalIndex ?? '—'}</dd></div>
              <div><dt>Phân hạng công bố</dt><dd>{p.journalClassification ?? '—'}</dd></div>
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
  const [articleStatus, setArticleStatus] = useState(product.articleStatus ?? 'DANG_VIET')
  const [publishedDate, setPublishedDate] = useState(product.publishedDate ?? '')
  const [publicationInfo, setPublicationInfo] = useState(product.publicationInfo ?? '')
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const showScore = needsScore(user, product) && action === 'APPROVE'

  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); setBusy(true); setError('')
    try {
      const body: Record<string, unknown> = { action, comment: comment.trim() || null }
      if (showScore && score) body.scoreEquivalent = parseFloat(score)
      if (action === 'APPROVE' && product.requiredReviewRole === 'PHONG_QLKH' && product.productTypeCode === 'BAI_BAO') {
        body.articleStatus = articleStatus
        body.publishedDate = publishedDate || null
        body.publicationInfo = publicationInfo.trim() || null
      }
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
              Điểm thẩm định <small className="muted">(nếu quy định yêu cầu)</small>
              <input type="number" min="0" max="999.99" step="0.01" value={score}
                onChange={e => setScore(e.target.value)}  placeholder="Ví dụ: 1.5" />
            </label>
          )}
          {action === 'APPROVE' && product.requiredReviewRole === 'PHONG_QLKH' && product.productTypeCode === 'BAI_BAO' && <>
            <label>Trạng thái bài viết khi công nhận cuối<select value={articleStatus} onChange={e => setArticleStatus(e.target.value)}>
              <option value="DANG_NHAN_XET">Đã nhận xét</option><option value="DA_XUAT_BAN">Đã xuất bản</option>
            </select></label>
            {articleStatus === 'DA_XUAT_BAN' && <><label>Ngày công bố<input type="date" required value={publishedDate} onChange={e => setPublishedDate(e.target.value)} /></label>
              <label>Nơi công bố<input value={publicationInfo} onChange={e => setPublicationInfo(e.target.value)} /></label></>}
          </>}
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
                    <div><dt>Chỉ mục công bố</dt><dd>{s.journalIndex ?? '—'}</dd></div>
                    <div><dt>Phân hạng công bố</dt><dd>{s.journalClassification ?? '—'}</dd></div>
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
export function ProductForm({ product, onSubmit, busy, error, planItems, targets, yearId }: {
  product?: Product; onSubmit: (e: FormEvent<HTMLFormElement>) => void
  busy: boolean; error: string; planItems?: PlanItem[]; targets?: AvailableTarget[]; yearId?: string
}) {
  const [selectedPlanItemId, setSelectedPlanItemId] = useState(String(product?.researchPlanItemId ?? ''))
  const [classifications, setClassifications] = useState<Classification[]>([])
  const [publications, setPublications] = useState<{ id: number; name: string; type: string; issn: string | null; index: string | null; classification: string | null }[]>([])
  const [catalogError, setCatalogError] = useState('')
  const [classification, setClassification] = useState(String(product?.classificationId ?? ''))
  const [publicationId, setPublicationId] = useState(String(product?.journalConferenceId ?? ''))
  const [targetId, setTargetId] = useState(String(product?.researchTargetId ?? ''))
  useEffect(() => {
    Promise.all([api<Classification[]>('/product-classifications'), api<{ id: number; name: string; type: string; issn: string | null; index: string | null; classification: string | null }[]>('/products/publication-options')])
      .then(([c, p]) => { setClassifications(c); setPublications(p) }).catch(e => setCatalogError(e.message))
  }, [])
  const selectedPlan = planItems?.find(x => x.id === Number(selectedPlanItemId))
  const typeCode = selectedPlan?.productTypeCode ?? product?.productTypeCode
  const typeId = selectedPlan?.productTypeId ?? product?.productTypeId
  const isArticle = typeCode === 'BAI_BAO'
  const isConference = typeCode === 'HOI_NGHI_HOI_THAO'
  const isProject = typeCode === 'DE_TAI'
  const isBook = typeCode === 'SACH'
  const isCertificate = typeCode === 'CHUNG_NHAN'
  const target = targets?.find(x => x.id === Number(targetId))
  const lockedPublication = target?.journalConferenceId
  const effectivePublicationId = lockedPublication ? String(lockedPublication) : publicationId
  const selectedPublication = publications.find(x => x.id === Number(effectivePublicationId))
  const classes = classifications.filter(c => (c.isActive || c.id === product?.classificationId) && c.productTypeId === typeId)
  const applicableTargets = (targets ?? []).filter(x => x.academicYearId === (selectedPlan?.academicYearId ?? Number(yearId)) && x.productTypeId === typeId)
  const dateLabel = isProject ? 'Ngày nghiệm thu' : isCertificate ? 'Ngày cấp' : 'Ngày công bố / xuất bản'
  const today = new Date(); const maxDate = `${today.getFullYear()}-${String(today.getMonth() + 1).padStart(2, '0')}-${String(today.getDate()).padStart(2, '0')}`
  return <form className="product-simple-form" onSubmit={onSubmit}>
    {error && <p className="alert" role="alert">{error}</p>}
    {catalogError && <p className="alert" role="alert">Không tải được danh mục: {catalogError}</p>}
    {planItems && <label>Nội dung trong kế hoạch
      <select name="researchPlanItemId" required value={selectedPlanItemId} onChange={e => {
        setSelectedPlanItemId(e.target.value); setClassification(''); setTargetId(''); setPublicationId('')
      }}><option value="">Chọn nội dung đã đăng ký</option>
        {planItems.filter(x => !yearId || x.academicYearId === Number(yearId)).map(x =>
          <option value={x.id} key={x.id}>{x.productTypeName} · {x.name}</option>)}
      </select>
      {!planItems.some(x => !yearId || x.academicYearId === Number(yearId)) && <small>Chưa có nội dung trong năm này. Hãy đăng ký kế hoạch trước.</small>}
    </label>}
    {(product || selectedPlan) && <>
      <p className="product-type-context">{selectedPlan?.productTypeName ?? ({ BAI_BAO: 'Bài báo', DE_TAI: 'Đề tài', SACH: 'Sách', CHUNG_NHAN: 'Chứng nhận', HOI_NGHI_HOI_THAO: 'Hội nghị / hội thảo' } as Record<string, string>)[typeCode ?? '']}</p>
      <div className="form-grid">
        <label className="wide">Tên {isProject ? 'đề tài' : isBook ? 'sách / giáo trình' : isCertificate ? 'chứng nhận' : 'công trình'}
          <input name="title" required minLength={3} maxLength={500} defaultValue={product?.title} /></label>
        {isArticle && product && <p className="product-type-context">Trạng thái bài viết: <strong>{article[product.articleStatus ?? 'DANG_VIET'] ?? 'Đang viết'}</strong>. Cấp duyệt sẽ chuyển bước sau khi kiểm tra minh chứng.</p>}
        {isArticle && !product && <input type="hidden" name="articleStatus" value="DANG_VIET" />}
        <label>{dateLabel}<input type="date" name="publishedDate" max={maxDate} defaultValue={product?.publishedDate ?? ''} />
          <small>Có thể bổ sung trước khi công nhận.</small></label>
        {(isArticle || isConference) && <label>Nơi công bố
          <select name="journalConferenceId" value={effectivePublicationId} disabled={!!lockedPublication} onChange={e => setPublicationId(e.target.value)}>
            <option value="">Khác / chưa có trong danh mục</option>
            {publications.filter(p => p.id === Number(effectivePublicationId) || p.type === (isConference ? 'HOI_NGHI' : 'TAP_CHI')).map(p => <option key={p.id} value={p.id}>{p.name}</option>)}
          </select>{lockedPublication && <input type="hidden" name="journalConferenceId" value={lockedPublication} />}
        </label>}
        {selectedPublication ? <input type="hidden" name="publicationInfo" value={selectedPublication.name} /> :
          <label>{isBook ? 'Nhà xuất bản' : isCertificate ? 'Đơn vị cấp' : isProject ? 'Đơn vị nghiệm thu' : 'Tên nơi công bố'}
            <input name="publicationInfo" maxLength={500} defaultValue={product?.publicationInfo ?? ''} /></label>}
        {isProject && <label>Cấp đề tài<input name="projectLevel" maxLength={100} placeholder="Trường, Bộ…" defaultValue={product?.projectLevel ?? ''} /></label>}
      </div>
      {!product && applicableTargets.length > 0 && <label>Gắn với chỉ tiêu được giao
        <select name="researchTargetId" value={targetId} onChange={e => setTargetId(e.target.value)}><option value="">Chưa gắn chỉ tiêu</option>
                  {applicableTargets.map(x => <option key={x.id} value={x.id}>{x.productTypeName} · {x.journalConferenceName ?? 'Chỉ tiêu chung'} · {x.allocatedArticleCount} sản phẩm</option>)}
        </select><small>Loại: {applicableTargets[0]?.productTypeName}. Một sản phẩm tính cho giảng viên phụ trách; đồng tác giả được quy đổi đóng góp riêng.</small>
      </label>}
      <details className="product-extra-fields"><summary>Thông tin bổ sung (khi cần)</summary>
        <div className="form-grid">
          <label>{isArticle || isConference ? 'DOI / mã công trình' : 'Mã công trình / quyết định'}<input name="workIdentifier" maxLength={255} defaultValue={product?.workIdentifier ?? ''} /></label>
          {classes.length > 0 && <label>Phân loại đề xuất<select name="classificationId" value={classification} onChange={e => setClassification(e.target.value)}>
            <option value="">Để QLKH xác nhận</option>{classes.map(c => <option value={c.id} key={c.id}>{c.name}</option>)}
          </select></label>}
          {(isArticle || isConference) && <>
            <label>ISSN{selectedPublication ? <input name="issn" readOnly value={selectedPublication.issn ?? ''} /> : <input name="issn" defaultValue={product?.journalConferenceId ? '' : ''} placeholder="Có thể bổ sung sau" />}</label>
            <label>Chỉ mục {selectedPublication && <small>tự lấy từ danh mục nơi công bố</small>}{selectedPublication ? <input name="journalIndex" readOnly value={selectedPublication.index ?? ''} /> : <input name="journalIndex" maxLength={100} defaultValue={product?.journalIndex ?? ''} placeholder="Scopus, Web of Science…" />}</label>
            <label>Phân hạng {selectedPublication && <small>tự lấy từ danh mục nơi công bố</small>}{selectedPublication ? <input name="journalClassification" readOnly value={selectedPublication.classification ?? ''} /> : <input name="journalClassification" maxLength={100} defaultValue={product?.journalClassification ?? ''} placeholder="Q1, Q2…" />}</label>
          </>}
          <label className="wide">Mô tả<textarea name="description" rows={3} defaultValue={product?.description ?? ''} /></label>
        </div>
      </details>
      <p className="muted">Sau khi lưu, bổ sung đồng tác giả và minh chứng trong chi tiết sản phẩm rồi gửi duyệt.</p>
      <button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu thông tin'}</button>
    </>}
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
        <select name="authorRole" required defaultValue="DONG_TAC_GIA">
          <option value="DONG_TAC_GIA">Đồng tác giả</option>
          <option value="TAC_GIA_LIEN_HE">Tác giả liên hệ</option>
        </select>
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
    <h2>Minh chứng</h2><p className="muted">Cần có ít nhất một minh chứng trước khi Phòng QLKH công nhận sản phẩm.</p>
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
    title: text('title'), description: text('description'), workIdentifier: String(f.get('workIdentifier') ?? ''),
    classificationId: Number(f.get('classificationId')) || null,
    ...(f.has('externalAuthorCount') ? { externalAuthorCount: Number(f.get('externalAuthorCount')) || 0 } : {}),
    updateJournalConference: true, journalConferenceId: Number(f.get('journalConferenceId')) || null,
    publicationInfo: text('publicationInfo'), publishedDate: text('publishedDate'),
    articleStatus: text('articleStatus'), journalIndex: text('journalIndex'),
    journalClassification: text('journalClassification'), projectLevel: text('projectLevel'),
  }
}
