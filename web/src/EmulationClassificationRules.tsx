import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import { useAutoReveal } from './useAutoReveal'

type Year = { id: number; code: string }
type Rule = { id: number; academicYearId: number; name: string; minimumPercent: number; sortOrder: number }

export function EmulationClassificationRules({ years }: { years: Year[] }) {
  const [yearId, setYearId] = useState('')
  const [rules, setRules] = useState<Rule[]>([])
  const [editing, setEditing] = useState<Rule | null>(null)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const formRef = useAutoReveal<HTMLElement>(editing !== null, editing?.id)

  async function load(id = yearId) {
    if (!id) { setRules([]); return }
    try { setError(''); setRules(await api<Rule[]>(`/emulation-classification-rules?academicYearId=${id}`)) }
    catch (e) { setError((e as Error).message) }
  }
  useEffect(() => { if (years.length && !yearId) setYearId(String(years[0].id)) }, [years, yearId])
  useEffect(() => { void load() }, [yearId])
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); if (!yearId) return
    const f = new FormData(e.currentTarget); setBusy(true); setError('')
    const body = { academicYearId: Number(yearId), name: String(f.get('name') ?? '').trim(), minimumPercent: Number(f.get('minimumPercent')), sortOrder: Number(f.get('sortOrder')) }
    try { await api(editing ? `/emulation-classification-rules/${editing.id}` : '/emulation-classification-rules', editing ? 'PUT' : 'POST', body); setEditing(null); await load() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function remove(id: number) { if (!confirm('Xóa ngưỡng xếp loại này?')) return; setBusy(true); try { await api(`/emulation-classification-rules/${id}`, 'DELETE'); await load() } catch (e) { setError((e as Error).message) } finally { setBusy(false) } }
  return <>
    <h1>Quy chế xếp loại thi đua</h1><p className="muted">Hệ thống so sánh tỷ lệ hoàn thành định mức giờ và điểm NCKH, sau đó dùng tỷ lệ cao hơn để xếp loại.</p>
    {error && <p className="alert">{error}</p>}
    <section className="panel"><label>Năm học<select value={yearId} onChange={e => setYearId(e.target.value)}><option value="">Chọn năm học</option>{years.map(y => <option key={y.id} value={y.id}>{y.code}</option>)}</select></label>
      <div className="table-wrap"><table><thead><tr><th>Xếp loại</th><th>Từ tỷ lệ đạt giờ hoặc điểm</th><th>Thứ tự</th><th /></tr></thead><tbody>{rules.map(r => <tr key={r.id}><td>{r.name}</td><td>≥ {r.minimumPercent}%</td><td>{r.sortOrder}</td><td><button onClick={() => setEditing(r)}>Sửa</button>{' '}<button className="danger" disabled={busy} onClick={() => void remove(r.id)}>Xóa</button></td></tr>)}</tbody></table></div>
      {!rules.length && yearId && <p className="muted">Chưa có ngưỡng nào; chưa thể chốt tự động đánh giá của năm học này.</p>}
    </section>
    {yearId && <section ref={formRef} className="panel auto-reveal-target"><h2>{editing ? 'Sửa ngưỡng' : 'Thêm ngưỡng'}</h2><form key={editing?.id ?? 'new'} onSubmit={save}><label>Tên xếp loại<input name="name" required maxLength={100} defaultValue={editing?.name ?? ''} /></label><label>Tỷ lệ tối thiểu (%)<input name="minimumPercent" type="number" min="0" step="0.01" required defaultValue={editing?.minimumPercent ?? ''} /></label><label>Thứ tự khi cùng ngưỡng<input name="sortOrder" type="number" min="0" required defaultValue={editing?.sortOrder ?? rules.length + 1} /></label><button className="primary" disabled={busy}>{busy ? 'Đang lưu...' : 'Lưu ngưỡng'}</button>{editing && <button type="button" onClick={() => setEditing(null)}>Hủy</button>}</form></section>}
  </>
}
