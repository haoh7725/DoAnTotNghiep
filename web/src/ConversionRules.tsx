import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import { useAutoReveal } from './useAutoReveal'
import { ProductTaxonomy } from './ProductTaxonomy'
import type { ProductGroup, Classification } from './ProductTaxonomy'
import './ConversionRules.css'

type Criterion = { field: string; operator: string; stringValue: string | null; numericValue: number | null }
type Coefficient = { authorRole: string; coefficient: number }
type Rule = { id?: number; productTypeId: number; classificationId: number | null; code: string; version: number; name: string; conditionDescription: string; conversionValue: number; unit: string; priority: number; authorSharingMode: string; authorRuleDescription: string; effectiveFrom: string; effectiveTo: string | null; legalBasis: string; criteria: Criterion[]; authorCoefficients: Coefficient[]; isUsed?: boolean }
const blank = (): Rule => ({ productTypeId: 0, classificationId: null, code: '', version: 1, name: '', conditionDescription: '', conversionValue: 0, unit: 'GIO', priority: 100, authorSharingMode: 'CHIA_136', authorRuleDescription: 'Tác giả chính nhận 1/3; 2/3 còn lại chia đều cho tất cả thành viên, kể cả tác giả chính và người ngoài trường.', effectiveFrom: '', effectiveTo: null, legalBasis: '', criteria: [], authorCoefficients: [] })
const fields: Record<string, string> = { HINH_THUC_CONG_BO: 'Hình thức công bố', CHI_SO_TAP_CHI: 'Chỉ mục (SCI, Scopus…)', PHAN_LOAI_TAP_CHI: 'Phân hạng (Q1–Q4…)', CAP_DE_TAI: 'Cấp đề tài', DIEM_CONG_TRINH: 'Điểm công trình đã xác nhận', NAM_CONG_BO: 'Năm công bố' }
const isText = (field: string) => !['DIEM_CONG_TRINH', 'NAM_CONG_BO'].includes(field)
const roles: Record<string, string> = { TAC_GIA_CHINH: 'Tác giả chính', DONG_TAC_GIA: 'Đồng tác giả', TAC_GIA_LIEN_HE: 'Tác giả liên hệ' }

export function ConversionRules() {
  const [rules, setRules] = useState<Rule[]>([])
  const [types, setTypes] = useState<ProductGroup[]>([])
  const [classes, setClasses] = useState<Classification[]>([])
  const [edit, setEdit] = useState<Rule | null>(null)
  const [tab, setTab] = useState('rules')
  const [busy, setBusy] = useState(false)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [revision, setRevision] = useState(0)
  const [filter, setFilter] = useState(0)
  const [authorCount, setAuthorCount] = useState(3)
  const formRef = useAutoReveal<HTMLFormElement>(edit !== null, edit?.id ?? 'new')
  useEffect(() => {
    let active = true; setLoading(true)
    Promise.all([api<Rule[]>('/admin/conversion-rules'), api<ProductGroup[]>('/product-types'), api<Classification[]>('/product-classifications')])
      .then(([r, t, c]) => { if (active) { setRules(r); setTypes(t); setClasses(c) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [revision])
  const change = (patch: Partial<Rule>) => setEdit(value => value ? { ...value, ...patch } : null)
  function open(rule: Rule) { setError(''); setMessage(''); setEdit(structuredClone(rule)) }
  function newVersion(rule: Rule) {
    open({ ...rule, id: undefined, isUsed: false, version: Math.max(...rules.filter(r => r.code === rule.code).map(r => r.version)) + 1, effectiveFrom: '', effectiveTo: null })
  }
  async function save(e: FormEvent) {
    e.preventDefault(); if (!edit) return
    if (edit.authorSharingMode === 'HE_SO' && !edit.authorCoefficients.length) { setError('Vui lòng nhập ít nhất hệ số tác giả chính cho cách chia hệ số.'); return }
    setBusy(true); setError('')
    try { await api(`/admin/conversion-rules${edit.id ? `/${edit.id}` : ''}`, edit.id ? 'PUT' : 'POST', edit); setEdit(null); setRevision(x => x + 1); setMessage('Đã lưu quy định. Kết quả đã chốt được giữ nguyên.') }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function remove(rule: Rule) {
    if (!window.confirm(`Xóa quy định chưa sử dụng ${rule.code}, phiên bản ${rule.version}?`)) return
    setBusy(true); setError('')
    try { await api(`/admin/conversion-rules/${rule.id}`, 'DELETE'); setRevision(x => x + 1) } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  const className = (id: number | null) => classes.find(x => x.id === id)?.name ?? 'Quy định chung cho nhóm'
  return <div className="conversion-page">
    <p className="eyebrow">QUY ĐỔI VÀ ĐÁNH GIÁ</p><h1>Quy định quy đổi NCKH</h1>
    <p className="muted">Chọn nhóm → phân loại → cấu hình mức giờ hoặc điểm và cách chia tác giả theo văn bản của trường.</p>
    <div className="actions conversion-tabs"><button className={tab === 'rules' ? 'primary' : ''} onClick={() => setTab('rules')}>Quy định quy đổi</button><button className={tab === 'catalog' ? 'primary' : ''} onClick={() => { setTab('catalog'); setEdit(null) }}>Danh mục nhóm và phân loại</button></div>
    {message && <p role="status">{message}</p>}
    {error && <p className="alert" role="alert">{error}</p>}
    {tab === 'catalog' ? <ProductTaxonomy onChanged={() => setRevision(x => x + 1)} /> : <>
        <p>Ưu tiên quy định riêng phù hợp với phân loại. Nếu chưa có, dùng quy định chung cho nhóm theo đơn vị giờ hoặc điểm đang đánh giá. Trong cùng phạm vi, xét ưu tiên rồi phiên bản; nếu vẫn trùng thì báo lỗi.</p>
      {edit && <form ref={formRef} className="panel auto-reveal-target" onSubmit={save}>
        <div className="section-top"><h2>{edit.id ? 'Sửa quy định chưa sử dụng' : 'Thêm quy định / phiên bản'}</h2><button type="button" onClick={() => setEdit(null)}>Đóng</button></div>
        <h3>Đối tượng áp dụng</h3><div className="form-grid">
          <label>Nhóm sản phẩm<select required value={edit.productTypeId || ''} onChange={e => change({ productTypeId: Number(e.target.value), classificationId: null, criteria: [] })}><option value="">Chọn nhóm</option>{types.filter(t => t.isActive || t.id === edit.productTypeId).map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
          <label>Phân loại quy đổi<select value={edit.classificationId ?? ''} onChange={e => change({ classificationId: Number(e.target.value) || null })}><option value="">Quy định chung cho nhóm</option>{classes.filter(c => c.productTypeId === edit.productTypeId && (c.isActive || c.id === edit.classificationId)).map(c => <option key={c.id} value={c.id}>{c.name}</option>)}</select><small>Chọn một phân loại để áp dụng riêng; để quy định chung nếu áp dụng cho cả nhóm khi chưa có mức riêng phù hợp.</small></label>
          <label>Mã quy định<input required maxLength={50} value={edit.code} onChange={e => change({ code: e.target.value })} placeholder="QD_SCOPUS_GIO" /></label>
          <label>Phiên bản<input type="number" min="1" required value={edit.version} onChange={e => change({ version: Number(e.target.value) })} /></label>
          <label className="wide">Tên quy định<input required maxLength={300} value={edit.name} onChange={e => change({ name: e.target.value })} /></label>
        </div><h3>Mức quy đổi và hiệu lực</h3><div className="form-grid">
          <label>Mức gốc của một công trình<input type="number" min="0" step="0.0001" required value={edit.conversionValue} onChange={e => change({ conversionValue: Number(e.target.value) })} /><small>Nhập theo văn bản căn cứ, chưa chia cho tác giả.</small></label>
          <label>Đơn vị<select value={edit.unit} onChange={e => change({ unit: e.target.value })}><option value="GIO">Giờ NCKH</option><option value="DIEM">Điểm NCKH</option></select><small>Mỗi quy định chỉ có một đơn vị. Điểm cần căn cứ riêng.</small></label>
          <label>Hiệu lực từ<input type="date" required value={edit.effectiveFrom} onChange={e => change({ effectiveFrom: e.target.value })} /></label>
          <label>Hiệu lực đến<input type="date" min={edit.effectiveFrom} value={edit.effectiveTo ?? ''} onChange={e => change({ effectiveTo: e.target.value || null })} /></label>
          <label className="wide">Văn bản căn cứ<input required maxLength={512} value={edit.legalBasis} onChange={e => change({ legalBasis: e.target.value })} placeholder="Số quyết định, ngày ban hành, điều/mục áp dụng" /></label>
        </div>
        <p className="muted">Mức này được tính một lần cho công trình trong đợt đánh giá. Quy định theo năm của đề tài nhiều năm cần hồ sơ xác nhận khối lượng từng năm; chưa dùng mức giờ/năm như mức một lần cho cả đề tài.</p>
        <h3>Cách chia cho tác giả</h3>
        <label>Phương pháp<select value={edit.authorSharingMode} onChange={e => change({ authorSharingMode: e.target.value, authorRuleDescription: e.target.value === 'CHIA_136' ? blank().authorRuleDescription : edit.authorRuleDescription })}><option value="CHIA_136">Theo QĐ 136: 1/3 tác giả chính + 2/3 chia đều</option><option value="HE_SO">Theo hệ số vai trò được cấu hình</option></select></label>
        {edit.authorSharingMode === 'CHIA_136' ? <div className="conversion-preview"><label>Thử với tổng số tác giả<input type="number" min="1" max="10001" value={authorCount} onChange={e => setAuthorCount(Math.max(1, Number(e.target.value)))} /></label><p>Tổng mức: {edit.conversionValue} {edit.unit === 'GIO' ? 'giờ' : 'điểm'} · Tác giả chính: {(edit.conversionValue * (1 / 3 + 2 / (3 * authorCount))).toFixed(4)} · Mỗi người còn lại: {authorCount === 1 ? 'Không có' : (edit.conversionValue * 2 / (3 * authorCount)).toFixed(4)}</p><small>Số tác giả tính cả người ngoài trường. Người ngoài trường được khai báo bằng số lượng tại hồ sơ sản phẩm.</small></div> : <>
          {edit.authorCoefficients.map((c, i) => <div className="inline-editor coefficient" key={i}><label>Vai trò<select required value={c.authorRole} onChange={e => change({ authorCoefficients: edit.authorCoefficients.map((x, n) => n === i ? { ...x, authorRole: e.target.value } : x) })}><option value="">Chọn vai trò</option>{Object.entries(roles).map(([v, label]) => <option key={v} value={v}>{label}</option>)}</select></label><label>Hệ số<input type="number" required min="0" step="0.000001" value={c.coefficient} onChange={e => change({ authorCoefficients: edit.authorCoefficients.map((x, n) => n === i ? { ...x, coefficient: Number(e.target.value) } : x) })} /></label><button type="button" onClick={() => change({ authorCoefficients: edit.authorCoefficients.filter((_, n) => n !== i) })}>Bỏ</button></div>)}
          <button type="button" onClick={() => change({ authorCoefficients: [...edit.authorCoefficients, { authorRole: '', coefficient: 1 }] })}>Thêm hệ số vai trò</button><p className="muted">Thiếu hệ số của một tác giả sẽ được báo khi đánh giá, không tự bỏ qua công trình.</p>
        </>}
        <label>Giải thích cách chia<textarea required value={edit.authorRuleDescription} onChange={e => change({ authorRuleDescription: e.target.value })} /></label>
        <details><summary>Điều kiện bổ sung và độ ưu tiên</summary><p>Các điều kiện bên dưới phải đồng thời thỏa. Mô tả bằng chữ không thay thế các điều kiện tính tự động.</p>
          <label>Độ ưu tiên<input type="number" min="0" required value={edit.priority} onChange={e => change({ priority: Number(e.target.value) })} /><small>Số lớn hơn được xét trước trong cùng phạm vi.</small></label>
          {edit.criteria.map((c, i) => <div className="inline-editor" key={i}>
            <select aria-label="Trường điều kiện" value={c.field} onChange={e => { const field = e.target.value; change({ criteria: edit.criteria.map((x, n) => n === i ? { field, operator: 'BANG', stringValue: isText(field) ? (field === 'HINH_THUC_CONG_BO' ? 'TAP_CHI' : '') : null, numericValue: isText(field) ? null : 0 } : x) }) }}>{Object.entries(fields).filter(([f]) => f !== 'CAP_DE_TAI' || types.find(t => t.id === edit.productTypeId)?.code === 'DE_TAI' || f === c.field).map(([v, n]) => <option key={v} value={v}>{n}</option>)}</select>
            <select aria-label="Toán tử" value={c.operator} onChange={e => change({ criteria: edit.criteria.map((x, n) => n === i ? { ...x, operator: e.target.value } : x) })}><option value="BANG">Bằng</option><option value="KHAC">Khác</option>{!isText(c.field) && <><option value="TU">Từ</option><option value="DEN">Đến</option></>}</select>
            {c.field === 'HINH_THUC_CONG_BO' ? <select aria-label="Hình thức" value={c.stringValue ?? 'TAP_CHI'} onChange={e => change({ criteria: edit.criteria.map((x, n) => n === i ? { ...x, stringValue: e.target.value } : x) })}><option value="TAP_CHI">Tạp chí</option><option value="HOI_NGHI">Hội nghị</option></select> : <input aria-label="Giá trị điều kiện" required type={isText(c.field) ? 'text' : 'number'} step="any" value={c.stringValue ?? c.numericValue ?? ''} onChange={e => change({ criteria: edit.criteria.map((x, n) => n === i ? { ...x, stringValue: isText(c.field) ? e.target.value : null, numericValue: isText(c.field) ? null : Number(e.target.value) } : x) })} />}
            <button type="button" onClick={() => change({ criteria: edit.criteria.filter((_, n) => n !== i) })}>Bỏ</button>
          </div>)}<button type="button" onClick={() => change({ criteria: [...edit.criteria, { field: 'NAM_CONG_BO', operator: 'TU', stringValue: null, numericValue: new Date().getFullYear() }] })}>Thêm điều kiện</button>
        </details>
        <label>Mô tả điều kiện và minh chứng cần kiểm tra<textarea required value={edit.conditionDescription} onChange={e => change({ conditionDescription: e.target.value })} /></label>
        {error && <p className="alert" role="alert">{error}</p>}
        <div className="actions form-actions"><button className="primary" disabled={busy}>Lưu quy định</button><button type="button" onClick={() => setEdit(null)}>Hủy</button></div>
      </form>}
      <section className="panel"><div className="section-top"><h2>Danh sách quy định</h2><button className="primary" disabled={loading || busy} onClick={() => open(blank())}>Thêm quy định</button></div>
        <label>Lọc theo nhóm<select value={filter} onChange={e => setFilter(Number(e.target.value))}><option value="0">Tất cả nhóm</option>{types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></label>
        {loading ? <p>Đang tải…</p> : <div className="table-wrap"><table><thead><tr><th>Quy định / phiên bản</th><th>Nhóm / phân loại</th><th>Mức gốc</th><th>Cách chia</th><th>Hiệu lực</th><th>Thao tác</th></tr></thead><tbody>{rules.filter(r => !filter || r.productTypeId === filter).map(r => <tr key={r.id}><td>{r.name}<br /><small>{r.code} · v{r.version}</small></td><td>{types.find(t => t.id === r.productTypeId)?.name}<br /><small>{className(r.classificationId)}</small></td><td>{r.conversionValue} {r.unit === 'GIO' ? 'giờ' : 'điểm'} / công trình</td><td>{r.authorSharingMode === 'CHIA_136' ? '1/3 + 2/3 chia đều' : 'Hệ số vai trò'}</td><td>{r.effectiveFrom} → {r.effectiveTo ?? 'Không giới hạn'}</td><td><div className="actions">{!r.isUsed && <button disabled={busy} onClick={() => open(r)}>Sửa</button>}<button disabled={busy} onClick={() => newVersion(r)}>Tạo phiên bản mới</button>{!r.isUsed && <button disabled={busy} className="danger" onClick={() => void remove(r)}>Xóa</button>}</div>{r.isUsed && <small>Đã dùng đánh giá · chỉ đọc</small>}</td></tr>)}</tbody></table></div>}
        {!loading && classes.filter(c => c.isActive && (!filter || c.productTypeId === filter)).map(c => {
          const configured = rules.filter(r => r.classificationId === c.id)
          return <p className="muted" key={c.id}>{c.name}: {configured.some(r => r.unit === 'GIO') ? 'Đã cấu hình giờ' : 'Chưa cấu hình giờ'} · {configured.some(r => r.unit === 'DIEM') ? 'Đã cấu hình điểm' : 'Chưa có quy định điểm'} (kiểm tra thêm thời gian hiệu lực).</p>
        })}
      </section>
    </>}
  </div>
}
