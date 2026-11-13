import { useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { Lecturer } from './Lecturers'
import './ScientificProfile.css'

type Value = string | number | null
type Row = Record<string, Value>
type ListKey = 'education' | 'languages' | 'employment' | 'researchProjects' | 'publications'
type Details = Record<string, Value | Row[]>
export type ScientificProfile = {
  lecturerId: number; expertise: string | null; researchFields: string | null
  researchDirections: string | null; activitySummary: string | null; updatedAt: string | null
  details: Details
}
export type ApprovedResearch = {
  id: number; title: string; productType: string; productTypeCode: string; academicYear: string
  startYear: number; endYear: number; publishedDate: string | null; publicationInfo: string | null
  projectLevel: string | null; authorRole: string | null
}
type Field = {
  key: string; label: string; type?: 'number' | 'date' | 'textarea'; max?: number; required?: boolean
  options?: Record<string, string>; future?: boolean
}
const levels = { DAI_HOC: 'Đại học', DAI_HOC_2: 'Bằng đại học 2', THAC_SI: 'Thạc sĩ', TIEN_SI: 'Tiến sĩ' }
const personal: Field[] = [
  { key: 'birthPlace', label: 'Nơi sinh', max: 200 }, { key: 'hometown', label: 'Quê quán', max: 200 },
  { key: 'ethnicity', label: 'Dân tộc', max: 100 }, { key: 'highestDegreeYear', label: 'Năm nhận học vị cao nhất', type: 'number' },
  { key: 'highestDegreeCountry', label: 'Nước nhận học vị', max: 100 }, { key: 'academicRankYear', label: 'Năm bổ nhiệm chức danh khoa học', type: 'number' },
  { key: 'workUnit', label: 'Đơn vị công tác hiện tại hoặc trước khi nghỉ hưu', max: 300 },
  { key: 'contactAddress', label: 'Chỗ ở riêng / địa chỉ liên lạc', max: 500 },
  { key: 'officePhone', label: 'Điện thoại cơ quan', max: 30 }, { key: 'homePhone', label: 'Điện thoại nhà riêng', max: 30 },
  { key: 'fax', label: 'Fax', max: 30 }, { key: 'identityNumber', label: 'Số CMND / CCCD', max: 30 },
  { key: 'identityIssuedDate', label: 'Ngày cấp CMND / CCCD', type: 'date' },
  { key: 'identityIssuedPlace', label: 'Nơi cấp CMND / CCCD', max: 200 },
]
const declaration: Field[] = [
  { key: 'declarationPlace', label: 'Địa điểm kê khai', max: 200 },
  { key: 'declarationDate', label: 'Ngày kê khai', type: 'date' },
]
const summaries: Field[] = [
  { key: 'expertise', label: 'Chuyên môn', type: 'textarea', max: 4000 },
  { key: 'researchFields', label: 'Lĩnh vực nghiên cứu', type: 'textarea', max: 4000 },
  { key: 'researchDirections', label: 'Hướng nghiên cứu', type: 'textarea', max: 4000 },
  { key: 'activitySummary', label: 'Tóm tắt hoạt động khoa học', type: 'textarea', max: 4000 },
]
const groups: { key: ListKey; title: string; hint: string; max: number; fields: Field[] }[] = [
  { key: 'education', title: 'II. Quá trình đào tạo', hint: 'Khai riêng từng bằng đại học, bằng đại học 2, thạc sĩ và tiến sĩ. Tên luận án khai tại dòng tiến sĩ.', max: 100, fields: [
    { key: 'level', label: 'Bậc đào tạo', options: levels, required: true },
    { key: 'trainingMode', label: 'Hệ đào tạo', max: 100 }, { key: 'institution', label: 'Nơi đào tạo', required: true, max: 300 },
    { key: 'specialization', label: 'Ngành / chuyên ngành', required: true, max: 200 },
    { key: 'country', label: 'Nước đào tạo', max: 100 }, { key: 'graduationYear', label: 'Năm tốt nghiệp / cấp bằng', type: 'number' },
    { key: 'thesisTitle', label: 'Tên luận án', type: 'textarea', max: 2000 },
  ] },
  { key: 'languages', title: 'Ngoại ngữ', hint: 'Mỗi ngoại ngữ có mức độ sử dụng riêng (ví dụ: đọc hiểu tốt, IELTS 6.5, B2).', max: 100, fields: [
    { key: 'name', label: 'Ngoại ngữ', required: true, max: 100 }, { key: 'proficiency', label: 'Mức độ sử dụng', required: true, max: 300 },
  ] },
  { key: 'employment', title: 'III. Quá trình công tác chuyên môn', hint: 'Để trống ngày kết thúc nếu đang công tác tại đơn vị này.', max: 100, fields: [
    { key: 'fromDate', label: 'Từ ngày', type: 'date' }, { key: 'toDate', label: 'Đến ngày', type: 'date' },
    { key: 'organization', label: 'Nơi công tác', required: true, max: 300 },
    { key: 'position', label: 'Công việc đảm nhiệm', required: true, max: 1000 },
  ] },
  { key: 'researchProjects', title: 'IV.1. Đề tài nghiên cứu đã và đang tham gia', hint: 'Kê khai cả đề tài đang thực hiện và đã hoàn thành. Năm hoàn thành có thể là năm dự kiến.', max: 500, fields: [
    { key: 'title', label: 'Tên đề tài', required: true, max: 1000 },
    { key: 'startYear', label: 'Năm bắt đầu', type: 'number' },
    { key: 'endYear', label: 'Năm hoàn thành / dự kiến', type: 'number', future: true },
    { key: 'level', label: 'Cấp đề tài', required: true, max: 200 },
    { key: 'responsibility', label: 'Trách nhiệm tham gia', required: true, max: 300 },
  ] },
  { key: 'publications', title: 'IV.2. Sản phẩm khoa học đã công bố hoặc được công nhận', hint: 'Bao gồm đề tài, bài báo, sách và chứng nhận đã được hệ thống phê duyệt. Sản phẩm thủ công có thể bổ sung khi chưa có trong hệ thống.', max: 500, fields: [
    { key: 'title', label: 'Tên công trình', required: true, max: 1000 }, { key: 'year', label: 'Năm công bố', type: 'number' },
    { key: 'journal', label: 'Loại sản phẩm / nơi công bố', required: true, max: 500 },
  ] },
]
const display = (value: Value | undefined, field?: Field) => {
  if (value === null || value === undefined || value === '') return '—'
  if (field?.options) return field.options[String(value)] ?? String(value)
  if (field?.type === 'date') return new Date(`${value}T00:00:00`).toLocaleDateString('vi-VN')
  return String(value)
}
const localToday = () => {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}
const rowsOf = (details: Details, key: ListKey): Row[] => Array.isArray(details[key]) ? details[key] as Row[] : []
const scalarOf = (details: Details, key: string): Value => Array.isArray(details[key]) ? null : details[key] as Value ?? null
const cleanRow = (fields: Field[], row: Row): Row => Object.fromEntries(fields.map(f => {
  const text = String(row[f.key] ?? '').trim()
  return [f.key, !text ? null : f.type === 'number' ? Number(text) : text]
}))

function FieldInput({ field, value, onChange }: { field: Field; value: Value; onChange: (value: string) => void }) {
  const { label, required, max, options, type } = field
  return <label>{label}{required ? ' *' : ''}
    {options ? <select value={String(value ?? '')} required={required} onChange={e => onChange(e.target.value)}>
      <option value="">— Chọn —</option>{Object.entries(options).map(([key, text]) => <option key={key} value={key}>{text}</option>)}
    </select> : type === 'textarea' ? <textarea rows={3} maxLength={max} value={value ?? ''} onChange={e => onChange(e.target.value)} />
      : <input type={type ?? 'text'} required={required} maxLength={max} value={value ?? ''}
        min={type === 'number' ? 1900 : type === 'date' ? '1900-01-01' : undefined}
        max={type === 'number' ? (field.future ? 2100 : new Date().getFullYear()) : type === 'date' ? localToday() : undefined}
        step={type === 'number' ? 1 : undefined} onChange={e => onChange(e.target.value)} />}
  </label>
}

export function ProfileSection({ lecturer, profile, approvedResearch = [], canEdit, onSaved }: {
  lecturer: Lecturer; profile: ScientificProfile; approvedResearch?: ApprovedResearch[]; canEdit: boolean; onSaved: (p: ScientificProfile) => void
}) {
  const [editing, setEditing] = useState(false)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [saved, setSaved] = useState(false)
  const [details, setDetails] = useState<Details>(profile.details ?? {})
  const [summary, setSummary] = useState<Row>(() => Object.fromEntries(summaries.map(f => [f.key, profile[f.key as keyof ScientificProfile] as Value])))
  const current = editing ? details : profile.details ?? {}
  const normalizedTitle = (value: Value | undefined) => String(value ?? '').trim().toLocaleLowerCase('vi-VN')
  const withApprovedResearch = (key: ListKey, manualRows: Row[]) => {
    if (key !== 'publications') return manualRows
    const known = new Set(manualRows.map(row => normalizedTitle(row.title)))
    const automatic: Row[] = approvedResearch
      .filter(item => !known.has(normalizedTitle(item.title)))
      .map(item => ({
        title: item.title,
        year: item.publishedDate ? Number(item.publishedDate.slice(0, 4)) : null,
        journal: [item.productType, item.publicationInfo].filter(Boolean).join(' · '),
      } as Row))
    return [...manualRows, ...automatic]
  }
  const approvedRowsFor = (key: ListKey, manualRows: Row[]) =>
    withApprovedResearch(key, manualRows).slice(manualRows.length)
  const updateScalar = (key: string, value: string) => setDetails(d => ({ ...d, [key]: value }))
  function updateRows(key: ListKey, rows: Row[]) { setDetails(d => ({ ...d, [key]: rows })) }
  function beginEdit() {
    setDetails(structuredClone(profile.details ?? {}))
    setSummary(Object.fromEntries(summaries.map(f => [f.key, profile[f.key as keyof ScientificProfile] as Value])))
    setEditing(true); setSaved(false); setError('')
  }
  async function save(e: FormEvent) {
    e.preventDefault(); setError(''); setSaved(false)
    for (const row of rowsOf(details, 'employment')) {
      if (row.fromDate && row.toDate && String(row.fromDate) > String(row.toDate)) {
        setError('Ngày kết thúc công tác phải từ ngày bắt đầu.'); return
      }
    }
    for (const row of rowsOf(details, 'researchProjects')) {
      if (row.startYear && row.endYear && Number(row.startYear) > Number(row.endYear)) {
        setError('Năm hoàn thành đề tài phải từ năm bắt đầu.'); return
      }
    }
    const payload = { ...cleanRow(personal.concat(declaration), details as Row),
      ...Object.fromEntries(groups.map(g => [g.key, rowsOf(details, g.key).map(r => cleanRow(g.fields, r))])) }
    setBusy(true)
    try {
      const result = await api<ScientificProfile>(`/lecturers/${lecturer.id}/scientific-profile`, 'PUT', { ...cleanRow(summaries, summary), details: payload })
      onSaved(result); setEditing(false); setSaved(true)
    } catch (err) { setError((err as Error).message) } finally { setBusy(false) }
  }
  const basic: [string, Value][] = [
    ['Họ và tên', lecturer.fullName], ['Giới tính', ({ NAM: 'Nam', NU: 'Nữ', KHAC: 'Khác' } as Record<string, string>)[lecturer.gender ?? ''] ?? null],
    ['Ngày sinh', lecturer.birthDate ? display(lecturer.birthDate, { key: '', label: '', type: 'date' }) : null],
    ['Học vị cao nhất', lecturer.degree], ['Chức danh khoa học cao nhất', lecturer.academicRank],
    ['Chức vụ hiện tại hoặc trước khi nghỉ hưu', lecturer.position], ['Khoa / bộ môn', `${lecturer.facultyName} / ${lecturer.departmentName}`],
    ['Điện thoại di động', lecturer.phone], ['Email', lecturer.email],
  ]
  function scalarSection(fields: Field[]) {
    return editing ? <div className="form-grid">{fields.map(f => <FieldInput key={f.key} field={f} value={scalarOf(current, f.key)} onChange={value => updateScalar(f.key, value)} />)}</div>
      : <dl className="lec-dl">{fields.map(f => <div key={f.key}><dt>{f.label}</dt><dd>{display(scalarOf(current, f.key), f)}</dd></div>)}</dl>
  }
  return <section className="panel scientific-profile">
    <div className="section-top"><div><h2>Lý lịch khoa học</h2>
      <p className="muted lec-sub">Theo Phụ lục V Thông tư 08/2011/TT-BGDĐT ngày 17/02/2011.</p>
      <p className="muted lec-sub">{profile.updatedAt ? `Cập nhật: ${new Date(profile.updatedAt).toLocaleString('vi-VN')}` : 'Chưa lập lý lịch khoa học.'}</p></div>
      {!editing && <div className="actions scientific-controls"><button onClick={() => window.print()}>In / Lưu PDF</button>
        {canEdit && <button className="primary" onClick={beginEdit}>{profile.updatedAt ? 'Chỉnh sửa lý lịch' : 'Lập lý lịch'}</button>}</div>}</div>
    {saved && <p className="lec-ok scientific-controls" role="status">Đã lưu đầy đủ lý lịch khoa học.</p>}
    <form onSubmit={save}>
      {error && <p className="alert" role="alert">{error}</p>}
      <fieldset disabled={busy} className="scientific-fieldset">
        <section className="scientific-section"><h3>I. Lý lịch sơ lược</h3>
          <dl className="lec-dl">{basic.map(([key, value]) => <div key={key}><dt>{key}</dt><dd>{display(value)}</dd></div>)}</dl>
          {editing && <p className="muted">Họ tên, ngày sinh, giới tính, học hàm, học vị, chức vụ, email và di động lấy từ thông tin hồ sơ phía trên. Các mục khác có thể bổ sung dần; dòng có dấu * cần điền đủ hoặc xóa dòng.</p>}
          {scalarSection(personal)}
        </section>
        {groups.map(g => {
          const rows = rowsOf(current, g.key)
          const visibleRows = editing ? rows : withApprovedResearch(g.key, rows)
          const approvedRows = editing ? approvedRowsFor(g.key, rows) : []
          return <section key={g.key} className="scientific-section"><div className="section-top"><h3>{g.title}</h3>
            {editing && <button type="button" disabled={rows.length >= g.max} onClick={() => updateRows(g.key, [...rows, g.key === 'education' ? { level: 'DAI_HOC' } : {}])}>+ Thêm dòng ({rows.length}/{g.max})</button>}</div>
            <p className="muted scientific-controls">{g.hint}</p>
            {editing ? rows.map((row, index) => <div className="scientific-entry" key={index}>
              <div className="section-top"><strong>Dòng {index + 1}</strong><button type="button" onClick={() => updateRows(g.key, rows.filter((_, i) => i !== index))}>Xóa dòng {index + 1}</button></div>
              <div className="form-grid">{g.fields.map(f => <FieldInput key={f.key} field={f} value={row[f.key] ?? null}
                onChange={value => updateRows(g.key, rows.map((r, i) => i === index ? { ...r, [f.key]: value } : r))} />)}</div>
            </div>) : null}
            {editing && approvedRows.length > 0 && <>
              <h4>Sản phẩm đã được duyệt</h4>
              <div className="table-wrap"><table><thead><tr><th scope="col">TT</th>{g.fields.map(f => <th scope="col" key={f.key}>{f.label}</th>)}</tr></thead>
                <tbody>{approvedRows.map((row, index) => <tr key={index}><td>{index + 1}</td>{g.fields.map(f => <td key={f.key}>{display(row[f.key], f)}</td>)}</tr>)}</tbody></table></div>
            </>}
            {!editing && visibleRows.length > 0 ? <div className="table-wrap"><table><thead><tr><th scope="col">TT</th>{g.fields.map(f => <th scope="col" key={f.key}>{f.label}</th>)}</tr></thead>
              <tbody>{visibleRows.map((row, index) => <tr key={index}><td>{index + 1}</td>{g.fields.map(f => <td key={f.key}>{display(row[f.key], f)}</td>)}</tr>)}</tbody></table></div>
              : !editing && <p className="muted">Chưa kê khai.</p>}
          </section>
        })}
        <section className="scientific-section"><h3>Thông tin kê khai</h3>{scalarSection(declaration)}
          <div className="scientific-signatures"><p><strong>Xác nhận của cơ quan</strong><br /><span className="muted">(Ký tên, đóng dấu trên bản in)</span></p>
            <p><strong>Người khai ký tên</strong><br /><span className="muted">(Ghi rõ chức danh, học vị)</span><br /><br />{[lecturer.academicRank, lecturer.degree, lecturer.fullName].filter(Boolean).join(' · ')}</p></div>
        </section>
        <details className="scientific-section scientific-summary" open={editing || undefined}><summary>Thông tin chuyên môn và tóm tắt hoạt động</summary>
          <p className="muted">Thông tin tổng hợp đã khai trước đây được giữ riêng với các bảng chi tiết.</p>
          {editing ? summaries.map(f => <FieldInput key={f.key} field={f} value={summary[f.key] ?? null} onChange={value => setSummary(s => ({ ...s, [f.key]: value }))} />)
            : summaries.map(f => <article key={f.key}><h4>{f.label}</h4><p className="lec-text">{display(profile[f.key as keyof ScientificProfile] as Value)}</p></article>)}
        </details>
        {editing && <div className="actions scientific-controls"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu lý lịch'}</button>
          <button type="button" disabled={busy} onClick={() => { setEditing(false); setError('') }}>Hủy thay đổi</button></div>}
      </fieldset>
    </form>
  </section>
}
