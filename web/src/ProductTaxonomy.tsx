import { useEffect, useState } from 'react'
import { api } from './api'

export type ProductGroup = { id: number; code: string; name: string; isActive: boolean }
export type Classification = { id: number; productTypeId: number; code: string; name: string; isActive: boolean }

export function ProductTaxonomy({ onChanged }: { onChanged: () => void }) {
  const [groups, setGroups] = useState<ProductGroup[]>([])
  const [items, setItems] = useState<Classification[]>([])
  const [selected, setSelected] = useState(0)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [groupName, setGroupName] = useState('')
  const [groupCode, setGroupCode] = useState('')
  const [name, setName] = useState('')
  const [code, setCode] = useState('')
  const [editing, setEditing] = useState<Classification | null>(null)
  const [renaming, setRenaming] = useState(false)
  async function load() {
    const [g, c] = await Promise.all([api<ProductGroup[]>('/product-types'), api<Classification[]>('/product-classifications')])
    setGroups(g); setItems(c)
    setSelected(prev => prev || g[0]?.id || 0)
  }
  useEffect(() => { load().catch(e => setError(e.message)) }, [])
  async function run(action: () => Promise<unknown>) {
    setBusy(true); setError('')
    try { await action(); await load(); onChanged() } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }
  const group = groups.find(x => x.id === selected)
  return <section className="panel conversion-catalog">
    <h2>Nhóm và phân loại sản phẩm</h2>
    <p className="muted">Nhóm là Bài báo, Đề tài, Sách… Phân loại là Scopus, SCI, Cấp Bộ… Tên tạp chí/hội nghị cụ thể được quản lý ở danh mục nơi công bố.</p>
    {error && <p role="alert" className="alert">{error}</p>}
    <form className="form-grid" onSubmit={e => { e.preventDefault(); void run(async () => { await api('/product-types', 'POST', { code: groupCode.trim(), name: groupName.trim() }); setGroupCode(''); setGroupName('') }) }}>
      <label>Mã nhóm mới<input required pattern="[A-Z][A-Z0-9_]*" maxLength={30} placeholder="Ví dụ: HOI_NGHI" value={groupCode} onChange={e => setGroupCode(e.target.value.toUpperCase())} /></label>
      <label>Tên nhóm mới<input required maxLength={100} placeholder="Ví dụ: Bài hội nghị/hội thảo" value={groupName} onChange={e => setGroupName(e.target.value)} /></label>
      <div><button disabled={busy}>Thêm nhóm</button></div>
    </form>
    <hr />
    <label>Nhóm đang quản lý<select value={selected} onChange={e => { setSelected(Number(e.target.value)); setEditing(null); setCode(''); setName(''); setRenaming(false) }}>
      {groups.map(g => <option value={g.id} key={g.id}>{g.name}{g.isActive ? '' : ' · Ngừng dùng'}</option>)}
    </select></label>
    {group && <div className="actions form-actions">
      <button type="button" disabled={busy} onClick={() => { setRenaming(!renaming); setGroupName(group.name) }}>Đổi tên nhóm</button>
      <button type="button" disabled={busy} onClick={() => void run(() => group.isActive ? api(`/product-types/${group.id}`, 'DELETE') : api(`/product-types/${group.id}/activate`, 'POST'))}>{group.isActive ? 'Ngừng dùng nhóm' : 'Dùng lại nhóm'}</button>
    </div>}
    {renaming && group && <form className="inline-editor coefficient" onSubmit={e => { e.preventDefault(); void run(async () => { await api(`/product-types/${group.id}`, 'PUT', { name: groupName }); setRenaming(false) }) }}>
      <label>Tên nhóm<input required maxLength={100} value={groupName} onChange={e => setGroupName(e.target.value)} /></label><button disabled={busy}>Lưu tên nhóm</button>
    </form>}
    <h3>{editing ? 'Sửa phân loại' : 'Thêm phân loại vào nhóm'}</h3>
    <form className="form-grid" onSubmit={e => { e.preventDefault(); void run(async () => { await api(`/product-classifications${editing ? `/${editing.id}` : ''}`, editing ? 'PUT' : 'POST', { productTypeId: selected, code: code.trim(), name: name.trim(), isActive: editing?.isActive ?? true }); setEditing(null); setName(''); setCode('') }) }}>
      <label>Mã phân loại<input required disabled={!!editing} pattern="[A-Z][A-Z0-9_]*" maxLength={50} value={code} onChange={e => setCode(e.target.value.toUpperCase())} placeholder="Ví dụ: SCOPUS" /></label>
      <label>Tên phân loại<input required maxLength={300} value={name} onChange={e => setName(e.target.value)} placeholder="Ví dụ: Bài báo tạp chí Scopus" /></label>
      <div className="actions"><button disabled={busy || !group?.isActive}>{editing ? 'Lưu thay đổi' : 'Thêm phân loại'}</button>{editing && <button type="button" onClick={() => { setEditing(null); setName(''); setCode('') }}>Hủy</button>}</div>
    </form>
    <div className="table-wrap"><table><thead><tr><th>Mã</th><th>Phân loại</th><th>Trạng thái</th><th>Thao tác</th></tr></thead><tbody>
      {items.filter(x => x.productTypeId === selected).map(x => <tr key={x.id}><td>{x.code}</td><td>{x.name}</td><td>{x.isActive ? 'Đang dùng' : 'Ngừng dùng'}</td><td><div className="actions"><button disabled={busy} onClick={() => { setEditing(x); setName(x.name); setCode(x.code) }}>Sửa tên</button><button disabled={busy} onClick={() => void run(() => api(`/product-classifications/${x.id}`, 'PUT', { ...x, isActive: !x.isActive }))}>{x.isActive ? 'Ngừng dùng' : 'Dùng lại'}</button></div></td></tr>)}
    </tbody></table></div>
    {items.every(x => x.productTypeId !== selected) && <p className="muted">Chưa có phân loại. Thêm phân loại rồi cấu hình mức giờ/điểm ở tab Quy định quy đổi.</p>}
    <p className="muted">Ngừng dùng chỉ ẩn khỏi lựa chọn mới; dữ liệu và kết quả cũ vẫn được giữ.</p>
  </section>
}
