import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api, ApiError, roleNames, scopeNames } from './api'
import type { Lookups, Profile } from './api'
import { AdminPanel } from './AdminPanel'
import './App.css'

export default function App() {
  const [user, setUser] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [tab, setTab] = useState('overview')
  const [lookups, setLookups] = useState<Lookups | null>(null)
  const [lookupError, setLookupError] = useState('')
  const [retry, setRetry] = useState(0)
  const administrator = user?.roles.some(p => p.role === 'QUAN_TRI' && p.scope === 'TOAN_TRUONG') ?? false
  useEffect(() => {
    let active = true
    api<Profile>('/auth/me', 'GET', undefined, false).then(u => { if (active) setUser(u) }).catch(e => {
      if (active && (!(e instanceof ApiError) || e.status !== 401)) setError(e.message)
    }).finally(() => { if (active) setLoading(false) })
    const expired = () => { setUser(null); setLookups(null); setTab('overview'); setError('Phiên đăng nhập đã hết hạn hoặc tài khoản đã thay đổi. Vui lòng đăng nhập lại.') }
    window.addEventListener('session-expired', expired)
    return () => { active = false; window.removeEventListener('session-expired', expired) }
  }, [])
  useEffect(() => {
    if (!user) return
    let active = true
    setLookups(null); setLookupError('')
    api<Lookups>('/lookups').then(data => { if (active) setLookups(data) }).catch(e => { if (active) setLookupError(e.message) })
    return () => { active = false }
  }, [user, retry])
  async function login(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const data = new FormData(e.currentTarget); setBusy(true); setError('')
    try {
      await api('/auth/login', 'POST', { username: data.get('username'), password: data.get('password') })
      setUser(await api<Profile>('/auth/me')); setTab('overview')
    } catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  async function logout() {
    setBusy(true); setError('')
    try { await api('/auth/logout', 'POST'); setUser(null); setLookups(null); setTab('overview') }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  if (loading) return <main className="loading" role="status">Đang kiểm tra phiên đăng nhập…</main>
  if (!user) return <main className="login-layout">
    <section className="login-story"><div className="brand"><span className="brand-mark">R</span> ResearchHub <small>HUIT</small></div>
      <div><p className="eyebrow">NGHIÊN CỨU KHOA HỌC</p><h1>Mỗi đóng góp.<br />Một hành trình.</h1><p className="story-text">Không gian làm việc chung cho hồ sơ, kế hoạch và kết quả nghiên cứu của giảng viên.</p></div>
      <p className="story-foot">Hồ sơ khoa học · Kế hoạch nghiên cứu · Đánh giá kết quả</p>
    </section>
    <section className="login-form-wrap"><form className="login-form" onSubmit={login}>
      <span className="eyebrow">CHÀO MỪNG TRỞ LẠI</span><h2>Đăng nhập</h2><p className="muted">Sử dụng tài khoản được cấp để vào hệ thống.</p>
      {error && <p className="alert" role="alert">{error}</p>}
      <label>Tên đăng nhập<input name="username" autoComplete="username" required maxLength={100} autoFocus placeholder="Nhập tên đăng nhập" /></label>
      <label>Mật khẩu<input name="password" type="password" autoComplete="current-password" required maxLength={128} placeholder="Nhập mật khẩu" /></label>
      <button className="primary" disabled={busy}>{busy ? 'Đang đăng nhập…' : 'Đăng nhập →'}</button>
      <p className="help">Chưa có tài khoản hoặc quên mật khẩu? Liên hệ quản trị viên của đơn vị.</p>
    </form></section>
  </main>
  return <div className="workspace">
    <aside className="sidebar"><div className="brand"><span className="brand-mark">R</span> ResearchHub</div><p className="nav-label">KHÔNG GIAN LÀM VIỆC</p>
      <nav aria-label="Điều hướng chính"><button className={tab === 'overview' ? 'active' : ''} onClick={() => setTab('overview')}>Tổng quan tài khoản</button>
        {administrator && <button className={tab === 'admin' ? 'active' : ''} onClick={() => setTab('admin')}>Quản trị dữ liệu nền</button>}
        <button className={tab === 'password' ? 'active' : ''} onClick={() => setTab('password')}>Đổi mật khẩu</button></nav>
      <div className="sidebar-bottom"><strong>{user.fullName}</strong><span>@{user.username}</span><button onClick={logout} disabled={busy}>Đăng xuất</button></div>
    </aside>
    <main className="content"><header className="page-top"><span>HUIT / Nghiên cứu khoa học</span><span className="status">Đã đăng nhập</span></header>
      {error && <p className="alert" role="alert">{error}</p>}
      {tab === 'overview' && <><p className="eyebrow">TỔNG QUAN</p><h1>Xin chào, {user.fullName}</h1><p className="muted">Thông tin tài khoản và phạm vi truy cập của bạn.</p>
        <section className="panel"><h2>Quyền truy cập</h2>{user.roles.length ? <div className="permission-grid">{user.roles.map((p, i) => <article key={i} className="permission"><strong>{roleNames[p.role] ?? p.role}</strong><span>{scopeNames[p.scope] ?? p.scope}</span>{p.facultyId && <small>{lookups?.faculties.find(f => f.id === p.facultyId)?.name ?? `Khoa #${p.facultyId}`}</small>}{p.departmentId && <small>{lookups?.departments.find(d => d.id === p.departmentId)?.name ?? `Bộ môn #${p.departmentId}`}</small>}</article>)}</div> : <p>Tài khoản chưa được gán quyền. Vui lòng liên hệ quản trị viên.</p>}</section>
        <section className="panel"><h2>Đơn vị và năm học</h2>{lookupError ? <><p className="alert" role="alert">{lookupError}</p><button onClick={() => setRetry(x => x + 1)}>Thử lại</button></> : !lookups ? <p role="status">Đang tải dữ liệu…</p> : <div className="summary-grid"><div><h3>Khoa</h3>{lookups.faculties.map(f => <p key={f.id}>{f.name}</p>)}{!lookups.faculties.length && <p className="muted">Chưa có đơn vị được liên kết.</p>}</div><div><h3>Bộ môn</h3>{lookups.departments.map(d => <p key={d.id}>{d.name}</p>)}{!lookups.departments.length && <p className="muted">Chưa có bộ môn được liên kết.</p>}</div><div><h3>Năm học</h3>{lookups.academicYears.map(y => <p key={y.id}>{y.code}</p>)}{!lookups.academicYears.length && <p className="muted">Chưa có năm học.</p>}</div></div>}</section>
      </>}
      {tab === 'admin' && (administrator ? <AdminPanel onChanged={() => setRetry(x => x + 1)} /> : <p role="alert">Bạn không có quyền quản trị.</p>)}
      {tab === 'password' && <PasswordForm onChanged={() => { setUser(null); setError('Đã đổi mật khẩu. Vui lòng đăng nhập lại.'); setTab('overview') }} />}
    </main>
  </div>
}
function PasswordForm({ onChanged }: { onChanged: () => void }) {
  const [error, setError] = useState(''); const [busy, setBusy] = useState(false)
  async function submit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault(); const data = new FormData(e.currentTarget); setError('')
    if (data.get('newPassword') !== data.get('confirm')) { setError('Mật khẩu xác nhận chưa khớp.'); return }
    setBusy(true)
    try { await api('/auth/password', 'PUT', { currentPassword: data.get('currentPassword'), newPassword: data.get('newPassword') }); onChanged() }
    catch (e) { setError((e as Error).message) } finally { setBusy(false) }
  }
  return <><h1>Đổi mật khẩu</h1><form className="panel narrow" onSubmit={submit}>{error && <p className="alert" role="alert">{error}</p>}
    <label>Mật khẩu hiện tại<input type="password" name="currentPassword" required maxLength={128} autoComplete="current-password" /></label>
    <label>Mật khẩu mới<input type="password" name="newPassword" required minLength={12} maxLength={128} autoComplete="new-password" /></label>
    <label>Nhập lại mật khẩu mới<input type="password" name="confirm" required minLength={12} maxLength={128} autoComplete="new-password" /></label>
    <p className="muted">Từ 12 đến 128 ký tự. Các phiên đăng nhập cũ sẽ hết hiệu lực.</p><button className="primary" disabled={busy}>{busy ? 'Đang lưu…' : 'Lưu mật khẩu'}</button>
  </form></>
}
