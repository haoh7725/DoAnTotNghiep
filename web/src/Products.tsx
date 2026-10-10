import { useEffect, useMemo, useState, useRef } from 'react'
import type { FormEvent, ChangeEvent } from 'react'
import { api, apiUpload } from './api'
import type { Lookups, Profile } from './api'
import { isOffice } from './Lecturers'
import './Products.css'

/* ===== Kiểu dữ liệu khớp với ProductModels.cs ===== */
type Paged<T> = { items: T[]; page: number; pageSize: number; totalCount: number; totalPages: number }
type ProductType = { id: number; code: string; name: string }
type AuthorBrief = { lecturerId: number; lecturerName: string; role: string; order: number }
type ProductSummary = {
  id: number; code: string; title: string; productTypeId: number; productTypeCode: string; productTypeName: string
  academicYearId: number; academicYearCode: string; publicationYear: number | null; journalName: string | null
  articleStatus: string | null; reviewStatus: string; version: number; authors: AuthorBrief[]
}
type Author = {
  lecturerId: number; accountId: number; lecturerCode: string; lecturerName: string; role: string; order: number
  departmentId: number; departmentName: string; facultyId: number
}
type Permissions = { canEdit: boolean; canManageAuthors: boolean; canChangeArticleStatus: boolean; nextArticleStatuses: string[] }
type ProductDetail = ProductSummary & Record<string, unknown> & {
  createdByAccountId: number; createdByName: string; authors: Author[]; permissions: Permissions
}
type Candidate = { id: number; code: string; fullName: string; departmentName: string; facultyName: string }

const reviewNames: Record<string, string> = {
  NHAP: 'Nháp', CHO_DUYET: 'Chờ duyệt', CAN_BO_SUNG: 'Cần bổ sung', DA_DUYET: 'Đã duyệt', TU_CHOI: 'Từ chối',
}
const articleNames: Record<string, string> = {
  DANG_VIET: 'Đang viết', DANG_PHAN_BIEN: 'Đang phản biện', DA_NHAN_XET: 'Đã nhận xét', DA_XUAT_BAN: 'Đã xuất bản',
}
const articleSteps = Object.keys(articleNames)
const roleNames: Record<string, string> = {
  TAC_GIA_CHINH: 'Tác giả chính', TAC_GIA_LIEN_HE: 'Tác giả liên hệ', DONG_TAC_GIA: 'Đồng tác giả',
}

/* ===== Biểu mẫu theo loại sản phẩm (mã loại là định danh ổn định, xem LAM_TUAN_1.md) ===== */
type FieldDef = { key: string; label: string; kind?: 'text' | 'textarea' | 'date' | 'number' | 'year'; max?: number }
const commonFields: FieldDef[] = [
  { key: 'publicationYear', label: 'Năm công bố', kind: 'year' },
  { key: 'doi', label: 'DOI', max: 255 },
  { key: 'publicationInfo', label: 'Thông tin công bố', kind: 'textarea', max: 4000 },
]
const typeFields: Record<string, FieldDef[]> = {
  BAI_BAO: [
    { key: 'journalName', label: 'Tên tạp chí / hội nghị', max: 300 }, { key: 'journalIndex', label: 'Chỉ số tạp chí (ISI, Scopus…)', max: 100 },
    { key: 'journalCategory', label: 'Phân loại tạp chí (Q1, Q2…)', max: 100 }, { key: 'issn', label: 'ISSN', max: 20 },
    { key: 'researchField', label: 'Lĩnh vực nghiên cứu', max: 300 }, { key: 'workScore', label: 'Điểm công trình', kind: 'number' },
    { key: 'submittedDate', label: 'Ngày nộp bài', kind: 'date' }, { key: 'publishedDate', label: 'Ngày xuất bản', kind: 'date' },
  ],
  DE_TAI: [
    { key: 'projectLevel', label: 'Cấp đề tài', max: 100 }, { key: 'hostUnit', label: 'Đơn vị chủ trì', max: 300 },
    { key: 'researchField', label: 'Lĩnh vực nghiên cứu', max: 300 }, { key: 'startDate', label: 'Ngày bắt đầu', kind: 'date' },
    { key: 'endDate', label: 'Ngày kết thúc', kind: 'date' }, { key: 'projectObjective', label: 'Mục tiêu đề tài', kind: 'textarea', max: 4000 },
    { key: 'projectContent', label: 'Nội dung đề tài', kind: 'textarea', max: 4000 }, { key: 'expectedResult', label: 'Kết quả dự kiến', kind: 'textarea', max: 4000 },
  ],
  SACH: [
    { key: 'isbn', label: 'ISBN', max: 30 }, { key: 'publisher', label: 'Nhà xuất bản', max: 300 },
    { key: 'researchField', label: 'Lĩnh vực', max: 300 }, { key: 'publishedDate', label: 'Ngày xuất bản', kind: 'date' },
  ],
  CHUNG_NHAN: [
    { key: 'certificateNumber', label: 'Số chứng nhận', max: 100 }, { key: 'issuingAuthority', label: 'Cơ quan cấp', max: 300 },
    { key: 'publishedDate', label: 'Ngày cấp', kind: 'date' },
  ],
}
const fieldsFor = (typeCode: string) => [...commonFields, ...(typeFields[typeCode] ?? [])]

const fmtDate = (iso: unknown) => typeof iso === 'string' && iso ? new Date(iso + 'T00:00:00').toLocaleDateString('vi-VN') : '—'
const Badge = ({ kind, children }: { kind: string; children: string }) => <span className={`prd-badge prd-${kind}`}>{children}</span>
const ReviewBadge = ({ status }: { status: string }) => <Badge kind={`review-${status}`}>{reviewNames[status] ?? status}</Badge>
const ArticleBadge = ({ status }: { status: string | null }) =>
  status ? <Badge kind={`article-${status}`}>{articleNames[status] ?? status}</Badge> : <span className="muted">—</span>
const authorsText = (authors: AuthorBrief[]) => [...authors].sort((a, b) => a.order - b.order).map(a => a.lecturerName).join(', ')

type View = { kind: 'list' } | { kind: 'detail'; id: number } | { kind: 'form'; id?: number }

/* ===== Điều hướng giữa danh sách, chi tiết và biểu mẫu ===== */
export function ProductsModule({ user, lookups }: { user: Profile; lookups: Lookups | null }) {
  const [view, setView] = useState<View>({ kind: 'list' })
  if (view.kind === 'form') return <ProductForm user={user} lookups={lookups} productId={view.id}
    onCancel={() => setView(view.id ? { kind: 'detail', id: view.id } : { kind: 'list' })} onSaved={id => setView({ kind: 'detail', id })} />
  if (view.kind === 'detail') return <ProductDetailView productId={view.id} onBack={() => setView({ kind: 'list' })}
    onEdit={() => setView({ kind: 'form', id: view.id })} />
  return <ProductList lookups={lookups} onOpen={id => setView({ kind: 'detail', id })} onCreate={() => setView({ kind: 'form' })} />
}

/* ===== Danh sách ===== */
function ProductList({ lookups, onOpen, onCreate }: { lookups: Lookups | null; onOpen: (id: number) => void; onCreate: () => void }) {
  const [types, setTypes] = useState<ProductType[]>([])
  const [filters, setFilters] = useState({ keyword: '', productTypeId: '', academicYearId: '', reviewStatus: '', articleStatus: '', mine: false })
  const [applied, setApplied] = useState(filters)
  const [page, setPage] = useState(1)
  const [data, setData] = useState<Paged<ProductSummary> | null>(null)
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [retry, setRetry] = useState(0)

  useEffect(() => { api<ProductType[]>('/product-types').then(setTypes).catch(() => setTypes([])) }, [])
  useEffect(() => {
    let active = true; setLoading(true); setError('')
    const q = new URLSearchParams({ page: String(page), pageSize: '15' })
    if (applied.keyword) q.set('keyword', applied.keyword)
    if (applied.productTypeId) q.set('productTypeId', applied.productTypeId)
    if (applied.academicYearId) q.set('academicYearId', applied.academicYearId)
    if (applied.reviewStatus) q.set('reviewStatus', applied.reviewStatus)
    if (applied.articleStatus) q.set('articleStatus', applied.articleStatus)
    if (applied.mine) q.set('mine', 'true')
    api<Paged<ProductSummary>>(`/products?${q}`).then(d => { if (active) setData(d) })
      .catch(e => { if (active) setError((e as Error).message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [page, applied, retry])

  const set = (patch: Partial<typeof filters>) => setFilters(f => ({ ...f, ...patch }))
  const submit = (e: FormEvent) => { e.preventDefault(); setPage(1); setApplied({ ...filters, keyword: filters.keyword.trim() }) }
  return <>
    <div className="section-top"><div><p className="eyebrow">SẢN PHẨM KHOA HỌC</p><h1>Sản phẩm khoa học</h1></div>
      <button className="primary" onClick={onCreate}>+ Thêm sản phẩm</button></div>
    <p className="muted">Bài báo, đề tài, sách và chứng nhận trong phạm vi bạn được xem. Bản nháp chỉ tác giả thấy.</p>
    <form className="panel prd-filter" onSubmit={submit}>
      <label>Tìm kiếm<input value={filters.keyword} maxLength={100} placeholder="Tên, mã, tạp chí hoặc DOI" onChange={e => set({ keyword: e.target.value })} /></label>
      <label>Loại<select value={filters.productTypeId} onChange={e => set({ productTypeId: e.target.value })}><option value="">Tất cả</option>
        {types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
      <label>Năm học<select value={filters.academicYearId} onChange={e => set({ academicYearId: e.target.value })}><option value="">Tất cả</option>
        {lookups?.academicYears.map(y => <option key={y.id} value={y.id}>{y.code}</option>)}</select></label>
      <label>Xét duyệt<select value={filters.reviewStatus} onChange={e => set({ reviewStatus: e.target.value })}><option value="">Tất cả</option>
        {Object.entries(reviewNames).map(([v, n]) => <option key={v} value={v}>{n}</option>)}</select></label>
      <label>Trạng thái bài báo<select value={filters.articleStatus} onChange={e => set({ articleStatus: e.target.value })}><option value="">Tất cả</option>
        {Object.entries(articleNames).map(([v, n]) => <option key={v} value={v}>{n}</option>)}</select></label>
      <label className="prd-check"><input type="checkbox" checked={filters.mine} onChange={e => set({ mine: e.target.checked })} />Chỉ của tôi</label>
      <button className="primary" disabled={loading}>Lọc</button>
    </form>
    {error && <p className="alert" role="alert">{error} <button onClick={() => setRetry(x => x + 1)}>Thử lại</button></p>}
    <section className="panel">
      {loading && !data ? <p role="status">Đang tải…</p> : <>
        <div className="table-wrap"><table><thead><tr><th>Mã</th><th>Tên sản phẩm</th><th>Loại</th><th>Tác giả</th><th>Năm học</th><th>Bài báo</th><th>Xét duyệt</th><th></th></tr></thead><tbody>
          {data?.items.map(p => <tr key={p.id}>
            <td>{p.code}</td><td><strong>{p.title}</strong>{p.journalName && <><br /><small className="muted">{p.journalName}</small></>}</td>
            <td>{p.productTypeName}</td><td>{authorsText(p.authors) || '—'}</td><td>{p.academicYearCode}</td>
            <td><ArticleBadge status={p.articleStatus} /></td><td><ReviewBadge status={p.reviewStatus} /></td>
            <td><button onClick={() => onOpen(p.id)}>Xem</button></td></tr>)}
        </tbody></table></div>
        {!loading && !data?.items.length && <p className="empty">Không có sản phẩm phù hợp.</p>}
        {data && data.totalCount > 0 && <div className="actions"><button disabled={page <= 1 || loading} onClick={() => setPage(x => x - 1)}>Trước</button>
          <span>Trang {data.page}/{Math.max(data.totalPages, 1)} · {data.totalCount} sản phẩm</span>
          <button disabled={page >= data.totalPages || loading} onClick={() => setPage(x => x + 1)}>Sau</button></div>}
      </>}
    </section>
  </>
}

/* ===== Chi tiết ===== */
function ProductDetailView({ productId, onBack, onEdit }: { productId: number; onBack: () => void; onEdit: () => void }) {
  const [product, setProduct] = useState<ProductDetail | null>(null)
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [retry, setRetry] = useState(0)
  useEffect(() => {
    let active = true; setLoading(true); setError('')
    api<ProductDetail>(`/products/${productId}`).then(p => { if (active) setProduct(p) })
      .catch(e => { if (active) setError((e as Error).message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [productId, retry])

  const back = <button className="lec-back" onClick={onBack}>← Quay lại danh sách</button>
  if (loading && !product) return <p role="status">Đang tải sản phẩm…</p>
  if (!product) return <>{back}<p className="alert" role="alert">{error}</p><button onClick={() => setRetry(x => x + 1)}>Thử lại</button></>

  const rows = fieldsFor(product.productTypeCode)
    .map(f => ({ label: f.label, value: product[f.key], isDate: f.kind === 'date' }))
    .filter(r => r.value !== null && r.value !== undefined && r.value !== '')
  return <>
    {back}
    <div className="section-top"><div><p className="eyebrow">{product.productTypeName.toUpperCase()} · {product.code}</p><h1>{product.title}</h1></div>
      {product.permissions.canEdit && <button className="primary" onClick={onEdit}>Chỉnh sửa</button>}</div>
    <p className="prd-badges"><ReviewBadge status={product.reviewStatus} />{product.articleStatus && <ArticleBadge status={product.articleStatus} />}
      <span className="muted">Năm học {product.academicYearCode} · Người tạo: {product.createdByName}</span></p>
    {!product.permissions.canEdit && <p className="lec-notice view" role="note"><strong>Chỉ xem</strong>
      Chỉ người tạo hoặc tác giả chính được sửa, và chỉ khi sản phẩm ở trạng thái Nháp hoặc Cần bổ sung.</p>}
    {error && <p className="alert" role="alert">{error}</p>}
    <section className="panel"><h2>Thông tin</h2>
      {rows.length ? <dl className="lec-dl">{rows.map(r => <div key={r.label}><dt>{r.label}</dt>
        <dd className="prd-pre">{r.isDate ? fmtDate(r.value) : String(r.value)}</dd></div>)}</dl>
        : <p className="muted">Chưa có thông tin chi tiết.</p>}
    </section>
    {product.productTypeCode === 'BAI_BAO' && <ArticleStatusSection product={product} onChanged={setProduct} />}
    <AuthorsSection product={product} onChanged={() => setRetry(x => x + 1)} />
    <EvidencesSection product={product} />
  </>
}

/* --- Trạng thái bài báo --- */
function ArticleStatusSection({ product, onChanged }: { product: ProductDetail; onChanged: (p: ProductDetail) => void }) {
  const [busy, setBusy] = useState(false); const [error, setError] = useState('')
  const current = product.articleStatus ? articleSteps.indexOf(product.articleStatus) : -1
  async function change(status: string) {
    setBusy(true); setError('')
    try { onChanged(await api<ProductDetail>(`/products/${product.id}/article-status`, 'PUT', { status })) }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <section className="panel"><h2>Trạng thái bài báo</h2>
    <ol className="prd-steps">{articleSteps.map((s, i) => <li key={s} className={i === current ? 'current' : i < current ? 'done' : ''}><span>{i + 1}</span>{articleNames[s]}</li>)}</ol>
    {error && <p className="alert" role="alert">{error}</p>}
    {product.permissions.canChangeArticleStatus ? <div className="actions">
      {product.permissions.nextArticleStatuses.map(s => <button key={s} className={articleSteps.indexOf(s) > current ? 'primary' : ''} disabled={busy} onClick={() => change(s)}>
        {articleSteps.indexOf(s) > current ? `Chuyển sang “${articleNames[s]}”` : `Quay lại “${articleNames[s]}”`}</button>)}</div>
      : <p className="muted">{product.articleStatus === 'DA_XUAT_BAN' ? 'Bài báo đã xuất bản, không đổi trạng thái được nữa.' : 'Bạn không thể đổi trạng thái bài báo ở thời điểm này.'}</p>}
    <p className="muted prd-hint">Chuyển sang “Đã xuất bản” yêu cầu đã có tên tạp chí/hội nghị và năm công bố.</p>
  </section>
}

/* --- Tác giả và đồng tác giả --- */
type DraftAuthor = { lecturerId: number; code: string; name: string; department: string; role: string }
function AuthorsSection({ product, onChanged }: { product: ProductDetail; onChanged: () => void }) {
  const sorted = useMemo(() => [...product.authors].sort((a, b) => a.order - b.order), [product.authors])
  const [editing, setEditing] = useState(false); const [draft, setDraft] = useState<DraftAuthor[]>([])
  const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [saved, setSaved] = useState(false)
  const start = () => { setDraft(sorted.map(a => ({ lecturerId: a.lecturerId, code: a.lecturerCode, name: a.lecturerName, department: a.departmentName, role: a.role }))); setEditing(true); setSaved(false); setError('') }
  const move = (i: number, d: number) => setDraft(list => { const n = [...list]; [n[i], n[i + d]] = [n[i + d], n[i]]; return n })
  // Chỉ có một tác giả chính: chọn người mới thì người cũ thành đồng tác giả.
  const setRole = (i: number, role: string) => setDraft(list => list.map((a, x) => x === i ? { ...a, role } : role === 'TAC_GIA_CHINH' && a.role === 'TAC_GIA_CHINH' ? { ...a, role: 'DONG_TAC_GIA' } : a))
  const add = (c: Candidate) => setDraft(list => list.some(a => a.lecturerId === c.id) ? list
    : [...list, { lecturerId: c.id, code: c.code, name: c.fullName, department: c.departmentName, role: 'DONG_TAC_GIA' }])

  async function save() {
    if (draft.filter(a => a.role === 'TAC_GIA_CHINH').length !== 1) { setError('Phải có đúng một tác giả chính.'); return }
    setBusy(true); setError('')
    try {
      await api(`/products/${product.id}/authors`, 'PUT', { authors: draft.map(a => ({ lecturerId: a.lecturerId, role: a.role })) })
      setEditing(false); setSaved(true); onChanged()
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }

  return <section className="panel">
    <div className="section-top"><h2>Tác giả và đồng tác giả</h2>
      {product.permissions.canManageAuthors && !editing && <button className="primary" onClick={start}>Chỉnh sửa tác giả</button>}</div>
    {saved && <p className="lec-ok" role="status">Đã lưu danh sách tác giả.</p>}
    {!editing ? <div className="table-wrap"><table><thead><tr><th>#</th><th>Mã</th><th>Họ tên</th><th>Vai trò</th><th>Bộ môn ghi nhận</th></tr></thead><tbody>
      {sorted.map(a => <tr key={a.lecturerId}><td>{a.order}</td><td>{a.lecturerCode}</td><td><strong>{a.lecturerName}</strong></td><td>{roleNames[a.role] ?? a.role}</td><td>{a.departmentName}</td></tr>)}
    </tbody></table></div> : <>
      {error && <p className="alert" role="alert">{error}</p>}
      <ul className="prd-authors">{draft.map((a, i) => <li key={a.lecturerId}>
        <span className="prd-order">{i + 1}</span><span className="prd-author-name"><strong>{a.name}</strong><small>{a.code} · {a.department}</small></span>
        <select aria-label={`Vai trò của ${a.name}`} value={a.role} onChange={e => setRole(i, e.target.value)}>
          {Object.entries(roleNames).map(([v, n]) => <option key={v} value={v}>{n}</option>)}</select>
        <button type="button" aria-label="Lên" disabled={i === 0} onClick={() => move(i, -1)}>↑</button>
        <button type="button" aria-label="Xuống" disabled={i === draft.length - 1} onClick={() => move(i, 1)}>↓</button>
        <button type="button" className="danger" disabled={a.role === 'TAC_GIA_CHINH'} title={a.role === 'TAC_GIA_CHINH' ? 'Chuyển vai trò tác giả chính cho người khác trước' : ''}
          onClick={() => setDraft(list => list.filter((_, x) => x !== i))}>Bỏ</button></li>)}</ul>
      <h3>Thêm đồng tác giả</h3><CandidatePicker onPick={add} exclude={draft.map(a => a.lecturerId)} />
      <div className="actions"><button className="primary" disabled={busy} onClick={save}>{busy ? 'Đang lưu…' : 'Lưu danh sách tác giả'}</button>
        <button disabled={busy} onClick={() => { setEditing(false); setError('') }}>Hủy</button></div>
    </>}
  </section>
}

/* --- Minh chứng đính kèm --- */
type Evidence = {
  id: number
  productId: number
  uploadedByAccountId: number
  uploadedByName: string
  fileName: string
  mimeType: string
  fileSizeBytes: number
  sha256: string | null
  uploadedAt: string
}

const ALLOWED_EVIDENCE_EXTS = ['.pdf', '.doc', '.docx', '.xls', '.xlsx', '.ppt', '.pptx', '.jpg', '.jpeg', '.png', '.zip', '.rar', '.7z']
const MAX_EVIDENCE_SIZE_BYTES = 25 * 1024 * 1024 // 25 MB

function formatFileSize(bytes: number): string {
  if (bytes <= 0) return '0 B'
  const k = 1024
  const sizes = ['B', 'KB', 'MB', 'GB']
  const i = Math.floor(Math.log(bytes) / Math.log(k))
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`
}

function getEvidenceBadge(name: string) {
  const ext = (name.split('.').pop() ?? '').toLowerCase()
  if (ext === 'pdf') return { label: 'PDF', cls: 'prd-ev-pdf' }
  if (['doc', 'docx'].includes(ext)) return { label: 'DOC', cls: 'prd-ev-doc' }
  if (['xls', 'xlsx'].includes(ext)) return { label: 'XLS', cls: 'prd-ev-xls' }
  if (['ppt', 'pptx'].includes(ext)) return { label: 'PPT', cls: 'prd-ev-ppt' }
  if (['jpg', 'jpeg', 'png'].includes(ext)) return { label: 'IMG', cls: 'prd-ev-img' }
  if (['zip', 'rar', '7z'].includes(ext)) return { label: 'ZIP', cls: 'prd-ev-zip' }
  return { label: ext.toUpperCase() || 'FILE', cls: 'prd-ev-other' }
}

function EvidencesSection({ product }: { product: ProductDetail }) {
  const [items, setItems] = useState<Evidence[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [success, setSuccess] = useState('')
  const [fileToUpload, setFileToUpload] = useState<File | null>(null)
  const [uploading, setUploading] = useState(false)
  const [actionBusy, setActionBusy] = useState<number | null>(null)
  const fileInputRef = useRef<HTMLInputElement>(null)

  const fetchEvidences = async () => {
    try {
      setLoading(true)
      const data = await api<Evidence[]>(`/products/${product.id}/evidences`)
      setItems(data)
      setError('')
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    fetchEvidences()
  }, [product.id])

  const validateFile = (file: File): string | null => {
    if (file.size === 0) return 'Tệp không được để trống.'
    if (file.size > MAX_EVIDENCE_SIZE_BYTES) {
      return `Dung lượng tệp (${formatFileSize(file.size)}) vượt quá giới hạn tối đa cho phép (25 MB).`
    }
    const ext = '.' + (file.name.split('.').pop() ?? '').toLowerCase()
    if (!ALLOWED_EVIDENCE_EXTS.includes(ext)) {
      return `Định dạng tệp '${ext}' không được phép. Hệ thống chỉ chấp nhận: PDF, Word, Excel, PowerPoint, Ảnh (JPG, PNG) và tệp nén (ZIP, RAR, 7Z).`
    }
    return null
  }

  const handleFileChange = (e: ChangeEvent<HTMLInputElement>) => {
    setError('')
    setSuccess('')
    const file = e.target.files?.[0]
    if (!file) {
      setFileToUpload(null)
      return
    }
    const err = validateFile(file)
    if (err) {
      setError(err)
      setFileToUpload(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
      return
    }
    setFileToUpload(file)
  }

  const handleUpload = async (e: FormEvent) => {
    e.preventDefault()
    if (!fileToUpload) return
    const err = validateFile(fileToUpload)
    if (err) {
      setError(err)
      return
    }

    setUploading(true)
    setError('')
    setSuccess('')
    try {
      const formData = new FormData()
      formData.append('file', fileToUpload)
      const created = await apiUpload<Evidence>(`/products/${product.id}/evidences`, formData)
      setSuccess(`Đã tải lên minh chứng "${created.fileName}" thành công.`)
      setFileToUpload(null)
      if (fileInputRef.current) fileInputRef.current.value = ''
      await fetchEvidences()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setUploading(false)
    }
  }

  const handleDelete = async (item: Evidence) => {
    if (!window.confirm(`Bạn có chắc chắn muốn xóa minh chứng "${item.fileName}" không?`)) return
    setActionBusy(item.id)
    setError('')
    setSuccess('')
    try {
      await api(`/products/${product.id}/evidences/${item.id}`, 'DELETE')
      setSuccess(`Đã xóa minh chứng "${item.fileName}".`)
      await fetchEvidences()
    } catch (e) {
      setError((e as Error).message)
    } finally {
      setActionBusy(null)
    }
  }

  const handleView = (item: Evidence) => {
    window.open(`/api/products/${product.id}/evidences/${item.id}/view`, '_blank')
  }

  const handleDownload = (item: Evidence) => {
    window.open(`/api/products/${product.id}/evidences/${item.id}/download`, '_blank')
  }

  return (
    <section className="panel prd-evidence-section">
      <div className="section-top">
        <h2>Minh chứng đính kèm ({items.length})</h2>
      </div>

      {error && <p className="alert" role="alert">{error}</p>}
      {success && <p className="lec-ok" role="status">{success}</p>}

      {loading && items.length === 0 ? (
        <p role="status">Đang tải danh sách minh chứng…</p>
      ) : items.length === 0 ? (
        <p className="muted">Chưa có minh chứng nào được đính kèm cho sản phẩm này.</p>
      ) : (
        <div className="table-wrap">
          <table className="prd-evidence-table">
            <thead>
              <tr>
                <th>Loại</th>
                <th>Tên tài liệu</th>
                <th>Dung lượng</th>
                <th>Người tải lên</th>
                <th>Thời điểm</th>
                <th>Mã SHA-256</th>
                <th>Thao tác</th>
              </tr>
            </thead>
            <tbody>
              {items.map(item => {
                const tag = getEvidenceBadge(item.fileName)
                return (
                  <tr key={item.id}>
                    <td>
                      <span className={`prd-ev-badge ${tag.cls}`}>{tag.label}</span>
                    </td>
                    <td>
                      <strong>{item.fileName}</strong>
                    </td>
                    <td>{formatFileSize(item.fileSizeBytes)}</td>
                    <td>{item.uploadedByName || 'Hệ thống'}</td>
                    <td>{new Date(item.uploadedAt).toLocaleString('vi-VN')}</td>
                    <td>
                      {item.sha256 ? (
                        <code className="prd-ev-hash" title={item.sha256}>
                          {item.sha256.slice(0, 10)}…
                        </code>
                      ) : '—'}
                    </td>
                    <td>
                      <div className="prd-ev-actions">
                        <button type="button" onClick={() => handleView(item)} title="Xem trực tiếp trên trình duyệt">
                          Xem
                        </button>
                        <button type="button" onClick={() => handleDownload(item)} title="Tải về máy tính">
                          Tải về
                        </button>
                        {product.permissions.canEdit && (
                          <button
                            type="button"
                            className="danger"
                            disabled={actionBusy === item.id}
                            onClick={() => handleDelete(item)}
                            title="Xóa minh chứng này"
                          >
                            {actionBusy === item.id ? 'Đang xóa…' : 'Xóa'}
                          </button>
                        )}
                      </div>
                    </td>
                  </tr>
                )
              })}
            </tbody>
          </table>
        </div>
      )}

      {product.permissions.canEdit && (
        <div className="prd-ev-upload-box">
          <h3>Tải lên minh chứng mới</h3>
          <p className="muted prd-hint">
            Hỗ trợ: PDF, Word (.doc, .docx), Excel (.xls, .xlsx), PowerPoint (.ppt, .pptx), Ảnh (.jpg, .png) và tệp nén (.zip, .rar, .7z). Dung lượng tối đa: 25 MB.
          </p>
          <form onSubmit={handleUpload} className="prd-ev-upload-form">
            <input
              ref={fileInputRef}
              type="file"
              accept=".pdf,.doc,.docx,.xls,.xlsx,.ppt,.pptx,.jpg,.jpeg,.png,.zip,.rar,.7z"
              onChange={handleFileChange}
              disabled={uploading}
            />
            {fileToUpload && (
              <span className="prd-ev-file-info">
                Đã chọn: <strong>{fileToUpload.name}</strong> ({formatFileSize(fileToUpload.size)})
              </span>
            )}
            <button
              type="submit"
              className="primary"
              disabled={!fileToUpload || uploading}
            >
              {uploading ? 'Đang tải lên…' : 'Tải lên minh chứng'}
            </button>
          </form>
        </div>
      )}
    </section>
  )
}

/* --- Tìm giảng viên toàn trường để chọn tác giả --- */
function CandidatePicker({ onPick, exclude = [] }: { onPick: (c: Candidate) => void; exclude?: number[] }) {
  const [keyword, setKeyword] = useState(''); const [results, setResults] = useState<Candidate[]>([]); const [error, setError] = useState('')
  useEffect(() => {
    const term = keyword.trim()
    if (term.length < 2) { setResults([]); setError(''); return }
    let active = true
    const timer = setTimeout(() => api<Candidate[]>(`/products/author-candidates?keyword=${encodeURIComponent(term)}`)
      .then(r => { if (active) { setResults(r); setError('') } }).catch(e => { if (active) setError((e as Error).message) }), 300)
    return () => { active = false; clearTimeout(timer) }
  }, [keyword])
  const shown = results.filter(r => !exclude.includes(r.id))
  return <div className="prd-picker">
    <input value={keyword} maxLength={100} placeholder="Gõ ít nhất 2 ký tự của họ tên hoặc mã giảng viên" aria-label="Tìm giảng viên" onChange={e => setKeyword(e.target.value)} />
    {error && <p className="alert" role="alert">{error}</p>}
    {shown.length > 0 && <ul>{shown.map(c => <li key={c.id}><span><strong>{c.fullName}</strong><small>{c.code} · {c.departmentName} · {c.facultyName}</small></span>
      <button type="button" onClick={() => { onPick(c); setKeyword(''); setResults([]) }}>Chọn</button></li>)}</ul>}
    {keyword.trim().length >= 2 && !error && !shown.length && <p className="muted">Không tìm thấy giảng viên phù hợp.</p>}
  </div>
}

/* ===== Thêm / sửa ===== */
function ProductForm({ user, lookups, productId, onCancel, onSaved }: {
  user: Profile; lookups: Lookups | null; productId?: number; onCancel: () => void; onSaved: (id: number) => void
}) {
  const editing = productId !== undefined
  const [types, setTypes] = useState<ProductType[]>([])
  const [existing, setExisting] = useState<ProductDetail | null>(null)
  const [typeId, setTypeId] = useState(''); const [yearId, setYearId] = useState(''); const [title, setTitle] = useState('')
  const [articleStatus, setArticleStatus] = useState('DANG_VIET')
  const [values, setValues] = useState<Record<string, string>>({})
  const [mainAuthor, setMainAuthor] = useState<Candidate | null>(null)
  const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    ;(async () => {
      const t = await api<ProductType[]>('/product-types')
      const p = editing ? await api<ProductDetail>(`/products/${productId}`) : null
      if (!active) return
      setTypes(t)
      if (p) {
        setExisting(p); setTypeId(String(p.productTypeId)); setYearId(String(p.academicYearId)); setTitle(p.title)
        setValues(Object.fromEntries(fieldsFor(p.productTypeCode).map(f => [f.key, p[f.key] == null ? '' : String(p[f.key])])))
      } else if (lookups?.academicYears[0]) setYearId(String(lookups.academicYears[0].id))
    })().catch(e => { if (active) setError((e as Error).message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [productId, editing, lookups])

  const type = types.find(t => String(t.id) === typeId)
  const fields = type ? fieldsFor(type.code) : []
  const setValue = (key: string, v: string) => setValues(x => ({ ...x, [key]: v }))

  async function submit(e: FormEvent) {
    e.preventDefault(); setError('')
    if (!type) { setError('Hãy chọn loại sản phẩm.'); return }
    const body: Record<string, unknown> = { academicYearId: Number(yearId), title: title.trim() }
    for (const f of fields) {
      const raw = (values[f.key] ?? '').trim()
      body[f.key] = raw === '' ? null : f.kind === 'number' || f.kind === 'year' ? Number(raw) : raw
    }
    setBusy(true)
    try {
      if (editing) { await api(`/products/${productId}`, 'PUT', body); onSaved(productId) }
      else {
        const created = await api<ProductDetail>('/products', 'POST', {
          ...body, productTypeId: Number(typeId), articleStatus: type.code === 'BAI_BAO' ? articleStatus : null,
          mainAuthorLecturerId: mainAuthor?.id ?? null,
        })
        onSaved(created.id)
      }
    } catch (err) { setError((err as Error).message) } finally { setBusy(false) }
  }

  if (loading) return <p role="status">Đang tải…</p>
  return <>
    <button className="lec-back" onClick={onCancel}>← Quay lại</button>
    <p className="eyebrow">SẢN PHẨM KHOA HỌC</p><h1>{editing ? 'Chỉnh sửa sản phẩm' : 'Thêm sản phẩm'}</h1>
    <p className="muted">{editing ? `${existing?.code} · loại sản phẩm không đổi được sau khi tạo.` : 'Bạn sẽ là tác giả chính. Thêm đồng tác giả ở trang chi tiết sau khi lưu.'}</p>
    <form className="panel" onSubmit={submit}>
      {error && <p className="alert" role="alert">{error}</p>}
      <div className="form-grid">
        <label>Loại sản phẩm *<select required value={typeId} disabled={editing} onChange={e => setTypeId(e.target.value)}>
          <option value="">— Chọn loại —</option>{types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
        <label>Năm học ghi nhận *<select required value={yearId} onChange={e => setYearId(e.target.value)}>
          <option value="">— Chọn năm học —</option>{lookups?.academicYears.map(y => <option key={y.id} value={y.id}>{y.code}</option>)}</select></label>
        <label className="wide">Tên sản phẩm *<input required minLength={3} maxLength={500} value={title} onChange={e => setTitle(e.target.value)} /></label>
        {!editing && type?.code === 'BAI_BAO' && <label>Trạng thái bài báo<select value={articleStatus} onChange={e => setArticleStatus(e.target.value)}>
          {articleSteps.map(s => <option key={s} value={s}>{articleNames[s]}</option>)}</select></label>}
        {fields.map(f => <label key={f.key} className={f.kind === 'textarea' ? 'wide' : ''}>{f.label}
          {f.kind === 'textarea' ? <textarea rows={3} maxLength={f.max} value={values[f.key] ?? ''} onChange={e => setValue(f.key, e.target.value)} />
            : <input type={f.kind === 'date' ? 'date' : f.kind === 'number' || f.kind === 'year' ? 'number' : 'text'} maxLength={f.max}
              {...(f.kind === 'year' ? { min: 1900, max: 2200, step: 1 } : f.kind === 'number' ? { min: 0, step: '0.0001' } : {})}
              value={values[f.key] ?? ''} onChange={e => setValue(f.key, e.target.value)} />}</label>)}
      </div>
      {!editing && isOffice(user) && <div className="prd-onbehalf"><h3>Nhập hộ giảng viên (Phòng QLKH / Quản trị)</h3>
        <p className="muted">Để trống nếu bạn là tác giả chính. Chọn giảng viên để đặt họ làm tác giả chính.</p>
        {mainAuthor ? <p><strong>{mainAuthor.fullName}</strong> ({mainAuthor.code}) <button type="button" onClick={() => setMainAuthor(null)}>Bỏ chọn</button></p>
          : <CandidatePicker onPick={setMainAuthor} />}</div>}
      <div className="actions form-actions"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : editing ? 'Lưu thay đổi' : 'Tạo sản phẩm'}</button>
        <button type="button" disabled={busy} onClick={onCancel}>Hủy</button></div>
    </form>
  </>
}
