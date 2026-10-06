import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api, roleNames, scopeNames } from './api'
import type { AcademicYear, Department, Faculty, Permission } from './api'
import './AdminPanel.css'

type Account = { id: number; username: string; fullName: string; email: string | null; status: string }
type Entity = Faculty | Department | AcademicYear | Account
type AccountAction = { type: 'permissions' | 'password'; account: Account }
const tabs = [{ key: 'faculties', label: 'Khoa' }, { key: 'departments', label: 'Bộ môn' }, { key: 'academic-years', label: 'Năm học' }, { key: 'accounts', label: 'Tài khoản' }]

export function AdminPanel({ onChanged, currentAccountId }: { onChanged: () => void; currentAccountId: number }) {
  const [tab, setTab] = useState('faculties'); const [items, setItems] = useState<Entity[]>([])
  const [faculties, setFaculties] = useState<Faculty[]>([]); const [departments, setDepartments] = useState<Department[]>([])
  const [error, setError] = useState(''); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false)
  const [edit, setEdit] = useState<Entity | null | undefined>(undefined); const [accountAction, setAccountAction] = useState<AccountAction | null>(null)
  const [revision, setRevision] = useState(0); const [page, setPage] = useState(1); const [total, setTotal] = useState(0)
  useEffect(() => {
    let active = true; setLoading(true); setError(''); setItems([])
    Promise.all([api<Entity[] | { items: Account[]; total: number }>(`/admin/${tab}${tab === 'accounts' ? `?page=${page}&pageSize=20` : ''}`), api<Faculty[]>('/admin/faculties'), api<Department[]>('/admin/departments')])
      .then(([data, fs, ds]) => { if (active) { setItems(Array.isArray(data) ? data : data.items); setTotal(Array.isArray(data) ? data.length : data.total); setFaculties(fs); setDepartments(ds) } })
      .catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [tab, revision, page])
  async function save(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const data = Object.fromEntries(new FormData(e.currentTarget))
    const payload = tab === 'departments' ? { ...data, facultyId: Number(data.facultyId) } : tab === 'accounts' ? { ...data, email: data.email || null } : data
    setBusy(true); setError('')
    try { await api(`/admin/${tab}${edit ? `/${edit.id}` : ''}`, edit ? 'PUT' : 'POST', payload); setEdit(undefined); setRevision(x => x + 1); onChanged() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function remove(item: Entity) {
    if (!window.confirm('Xóa bản ghi này? Dữ liệu đang được sử dụng sẽ không thể xóa.')) return
    setBusy(true); setError('')
    try { await api(`/admin/${tab}/${item.id}`, 'DELETE'); setRevision(x => x + 1); onChanged() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  const value = (key: string) => edit && key in edit ? String((edit as unknown as Record<string, unknown>)[key] ?? '') : ''
  const closeAccountAction = () => { setAccountAction(null); setError('') }
  return <><p className="eyebrow">QUẢN TRỊ</p><h1>Dữ liệu nền</h1><p className="muted">Quản lý đơn vị, năm học, tài khoản và quyền sử dụng hệ thống.</p>
    <div className="tabs" role="group" aria-label="Loại dữ liệu">{tabs.map(t => <button disabled={busy} key={t.key} className={t.key === tab ? 'active' : ''} onClick={() => { setTab(t.key); setEdit(undefined); setAccountAction(null); setPage(1) }}>{t.label}</button>)}</div>
    {error && <p className="alert" role="alert">{error}</p>}
    {accountAction?.type === 'permissions' && <PermissionEditor account={accountAction.account} faculties={faculties} departments={departments} disabled={accountAction.account.id === currentAccountId} onClose={closeAccountAction} onSaved={() => { closeAccountAction(); setRevision(x => x + 1); onChanged() }} />}
    {accountAction?.type === 'password' && <ResetPasswordForm account={accountAction.account} onClose={closeAccountAction} />}
    {edit !== undefined && <form key={`${tab}-${edit?.id ?? 'new'}`} className="panel" onSubmit={save}><h2>{edit ? 'Cập nhật' : 'Thêm mới'} {tabs.find(t => t.key === tab)?.label.toLowerCase()}</h2><div className="form-grid">
      {tab === 'accounts' ? <>{!edit && <label>Tên đăng nhập<input name="username" required maxLength={100} pattern="[a-zA-Z0-9._-]+" /></label>}<label>Họ tên<input name="fullName" required maxLength={200} defaultValue={value('fullName')} /></label><label>Email<input type="email" name="email" maxLength={254} defaultValue={value('email')} /></label>{!edit ? <label>Mật khẩu ban đầu<input type="password" name="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label> : <label>Trạng thái<select name="status" defaultValue={value('status')}><option value="HOAT_DONG">Hoạt động</option><option value="KHOA">Khóa</option></select></label>}</>
        : <><label>Mã<input name="code" required maxLength={tab === 'academic-years' ? 20 : 30} defaultValue={value('code')} /></label>{tab !== 'academic-years' && <label>Tên<input name="name" required maxLength={200} defaultValue={value('name')} /></label>}{tab === 'departments' && <label>Khoa<select name="facultyId" required defaultValue={value('facultyId')}><option value="">Chọn khoa</option>{faculties.map(f => <option value={f.id} key={f.id}>{f.name}</option>)}</select></label>}{tab === 'academic-years' && <><label>Ngày bắt đầu<input type="date" name="startDate" required defaultValue={value('startDate')} /></label><label>Ngày kết thúc<input type="date" name="endDate" required defaultValue={value('endDate')} /></label></>}</>}
    </div><div className="actions"><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu'}</button><button type="button" disabled={busy} onClick={() => setEdit(undefined)}>Hủy</button></div></form>}
    <section className="panel"><div className="section-top"><h2>{tabs.find(t => t.key === tab)?.label}</h2><button className="primary" disabled={busy || loading} onClick={() => { setEdit(null); setAccountAction(null); setError('') }}>+ Thêm mới</button></div>
      {loading ? <p role="status">Đang tải…</p> : <><div className="table-wrap"><table><thead><tr><th>Mã / tài khoản</th><th>Tên / thời gian</th><th>Thông tin</th><th>Thao tác</th></tr></thead><tbody>
        {items.map(item => <tr key={item.id}><td>{'username' in item ? item.username : item.code}</td><td>{'fullName' in item ? item.fullName : 'name' in item ? item.name : `${item.startDate} → ${item.endDate}`}</td><td>{'status' in item ? item.status === 'HOAT_DONG' ? 'Hoạt động' : 'Khóa' : 'facultyId' in item ? faculties.find(f => f.id === item.facultyId)?.name : '—'}</td><td><div className="actions"><button disabled={busy} onClick={() => { setEdit(item); setAccountAction(null); setError('') }}>Sửa</button>{'username' in item && <><button disabled={busy} onClick={() => { setEdit(undefined); setAccountAction({ type: 'permissions', account: item }); setError('') }}>Phân quyền</button><button disabled={busy} onClick={() => { setEdit(undefined); setAccountAction({ type: 'password', account: item }); setError('') }}>Đặt mật khẩu</button></>}<button disabled={busy || ('username' in item && item.id === currentAccountId)} className="danger" onClick={() => remove(item)}>Xóa</button></div></td></tr>)}
      </tbody></table></div>{!items.length && <p className="empty">Chưa có dữ liệu.</p>}{tab === 'accounts' && <div className="actions"><button disabled={page === 1} onClick={() => setPage(x => x - 1)}>Trước</button><span>Trang {page} · {total} tài khoản</span><button disabled={page * 20 >= total} onClick={() => setPage(x => x + 1)}>Sau</button></div>}{error && <button onClick={() => setRevision(x => x + 1)}>Tải lại</button>}</>}
    </section></>
}

function PermissionEditor({ account, faculties, departments, disabled, onClose, onSaved }: { account: Account; faculties: Faculty[]; departments: Department[]; disabled: boolean; onClose: () => void; onSaved: () => void }) {
  const [permissions, setPermissions] = useState<Permission[]>([]); const [loading, setLoading] = useState(true); const [busy, setBusy] = useState(false); const [error, setError] = useState('')
  useEffect(() => { let active = true; api<Permission[]>(`/admin/accounts/${account.id}/permissions`).then(data => { if (active) setPermissions(data) }).catch(e => { if (active) setError(e.message) }).finally(() => { if (active) setLoading(false) }); return () => { active = false } }, [account.id])
  function changeRole(index: number, role: string) {
    const defaults: Record<string, Permission> = { GIANG_VIEN: { role, scope: 'CA_NHAN', facultyId: null, departmentId: null }, TRUONG_BO_MON: { role, scope: 'BO_MON', facultyId: faculties[0]?.id ?? null, departmentId: null }, TRUONG_KHOA: { role, scope: 'KHOA', facultyId: faculties[0]?.id ?? null, departmentId: null }, PHONG_QLKH: { role, scope: 'TOAN_TRUONG', facultyId: null, departmentId: null }, BAN_GIAM_HIEU: { role, scope: 'TOAN_TRUONG', facultyId: null, departmentId: null }, QUAN_TRI: { role, scope: 'TOAN_TRUONG', facultyId: null, departmentId: null } }
    setPermissions(items => items.map((p, i) => i === index ? defaults[role] : p))
  }
  const update = (index: number, patch: Partial<Permission>) => setPermissions(items => items.map((p, i) => i === index ? { ...p, ...patch } : p))
  async function save() {
    const incomplete = permissions.some(p =>
      (p.scope === 'KHOA' && !p.facultyId) ||
      (p.scope === 'BO_MON' && (!p.facultyId || !p.departmentId)))
    if (incomplete) { setError('Vui lòng chọn đủ khoa và bộ môn cho từng quyền.'); return }
    const keys = permissions.map(p => `${p.role}|${p.scope}|${p.facultyId ?? ''}|${p.departmentId ?? ''}`)
    if (new Set(keys).size !== keys.length) { setError('Danh sách có quyền bị trùng. Vui lòng bỏ dòng trùng trước khi lưu.'); return }
    setBusy(true); setError('')
    try { await api(`/admin/accounts/${account.id}/permissions`, 'PUT', permissions); onSaved() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <section className="panel"><div className="section-top"><div><h2>Phân quyền: {account.fullName}</h2><p className="muted">@{account.username}</p></div><button onClick={onClose}>Đóng</button></div>{disabled && <p className="alert">Không thể tự thay đổi quyền của tài khoản đang sử dụng. Hãy dùng một quản trị viên khác.</p>}{error && <p className="alert" role="alert">{error}</p>}{loading ? <p role="status">Đang tải quyền…</p> : <><div className="permission-editor">{permissions.map((permission, index) => { const availableDepartments = departments.filter(d => d.facultyId === permission.facultyId); return <div className="permission-row" key={index}><label>Vai trò<select value={permission.role} disabled={busy || disabled} onChange={e => changeRole(index, e.target.value)}>{Object.entries(roleNames).map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label><label>Phạm vi<input value={scopeNames[permission.scope] ?? permission.scope} disabled /></label>{(permission.scope === 'KHOA' || permission.scope === 'BO_MON') && <label>Khoa<select value={permission.facultyId ?? ''} required disabled={busy || disabled} onChange={e => update(index, { facultyId: Number(e.target.value), departmentId: null })}><option value="">Chọn khoa</option>{faculties.map(f => <option key={f.id} value={f.id}>{f.name}</option>)}</select></label>}{permission.scope === 'BO_MON' && <label>Bộ môn<select value={permission.departmentId ?? ''} required disabled={busy || disabled} onChange={e => update(index, { departmentId: Number(e.target.value) })}><option value="">Chọn bộ môn</option>{availableDepartments.map(d => <option key={d.id} value={d.id}>{d.name}</option>)}</select></label>}<button className="danger" disabled={busy || disabled} onClick={() => setPermissions(items => items.filter((_, i) => i !== index))}>Bỏ quyền</button></div>})}</div>{!permissions.length && <p className="empty">Tài khoản chưa có quyền.</p>}<div className="actions"><button disabled={busy || disabled} onClick={() => setPermissions(items => [...items, { role: 'GIANG_VIEN', scope: 'CA_NHAN', facultyId: null, departmentId: null }])}>+ Thêm quyền</button><button className="primary" disabled={busy || disabled} onClick={save}>{busy ? 'Đang lưu…' : 'Lưu quyền'}</button></div></>}</section>
}

function ResetPasswordForm({ account, onClose }: { account: Account; onClose: () => void }) {
  const [busy, setBusy] = useState(false); const [error, setError] = useState(''); const [done, setDone] = useState(false)
  async function submit(e: FormEvent<HTMLFormElement>) { e.preventDefault(); const data = new FormData(e.currentTarget); if (data.get('password') !== data.get('confirm')) { setError('Mật khẩu xác nhận chưa khớp.'); return } setBusy(true); setError(''); setDone(false); try { await api(`/admin/accounts/${account.id}/password`, 'PUT', { password: data.get('password') }); setDone(true); e.currentTarget.reset() } catch (e) { setError((e as Error).message) } finally { setBusy(false) } }
  return <form className="panel narrow" onSubmit={submit}><div className="section-top"><div><h2>Đặt lại mật khẩu</h2><p className="muted">{account.fullName} · @{account.username}</p></div><button type="button" onClick={onClose}>Đóng</button></div>{error && <p className="alert" role="alert">{error}</p>}{done && <p className="success" role="status">Đã đặt lại mật khẩu. Người dùng cần đăng nhập lại.</p>}<label>Mật khẩu mới<input type="password" name="password" required minLength={12} maxLength={128} autoComplete="new-password" /></label><label>Nhập lại mật khẩu<input type="password" name="confirm" required minLength={12} maxLength={128} autoComplete="new-password" /></label><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Đặt lại mật khẩu'}</button></form>
}
