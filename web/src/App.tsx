import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api, ApiError, roleNames, scopeNames } from './api'
import type { Lookups, Profile } from './api'
import { AdminPanel } from './AdminPanel'
import { LecturerDirectory, MyLecturerProfile, canBrowseLecturers } from './Lecturers'
import { ConversionRules } from './ConversionRules'
import { ProductWorkspace, ProductReviewWorkspace } from './Products'
import { ResearchReports } from './Reports'
import { ResearchPlans } from './ResearchPlans'
import { ResearchNorms } from './ResearchNorms'
import { ResearchTargets } from './ResearchTargets'
import { MyResearchTargets } from './MyResearchTargets'
import JournalConferences from './JournalConferences'
import Notifications from './Notifications'
import { Evaluations } from './Evaluations'
import { EmulationClassificationRules } from './EmulationClassificationRules'
import { ManagementProgressDashboard } from './ManagementProgressDashboard'
import './App.css'

export default function App() {
  const [user, setUser] = useState<Profile | null>(null)
  const [loading, setLoading] = useState(true)
  const [connectionError, setConnectionError] = useState('')
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [tab, setTab] = useState('overview')
  const [productToOpen, setProductToOpen] = useState<number | null>(null)
  const [reviewProductToOpen, setReviewProductToOpen] = useState<number | null>(null)
  const [unreadCount, setUnreadCount] = useState(0)
  const [lookups, setLookups] = useState<Lookups | null>(null)
  const [lookupError, setLookupError] = useState('')
  const [retry, setRetry] = useState(0)
  const [hasPersonalLecturerProfile, setHasPersonalLecturerProfile] = useState(false)
  const administrator = user?.roles.some(p => p.role === 'QUAN_TRI' && p.scope === 'TOAN_TRUONG') ?? false
  const executive = user?.roles.some(p => p.role === 'BAN_GIAM_HIEU') ?? false
  const officeAccount = user?.roles.some(p => ['PHONG_QLKH', 'BAN_GIAM_HIEU', 'QUAN_TRI'].includes(p.role)) ?? false
  const personalProfileCandidate = !officeAccount && (user?.roles.some(p => ['GIANG_VIEN', 'TRUONG_BO_MON', 'TRUONG_KHOA'].includes(p.role)) ?? false)
  const manager = user?.roles.some(p => ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'BAN_GIAM_HIEU', 'QUAN_TRI'].includes(p.role)) ?? false
  const canViewResearchNorms = user?.roles.some(p => ['GIANG_VIEN', 'TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'BAN_GIAM_HIEU'].includes(p.role)) ?? false
  const canEditResearchNorms = user?.roles.some(p => p.role === 'PHONG_QLKH' && p.scope === 'TOAN_TRUONG') ?? false
  const canViewResearchTargets = user?.roles.some(p =>
    ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'BAN_GIAM_HIEU', 'QUAN_TRI'].includes(p.role)
  ) ?? false

  const canEditResearchTargets = user?.roles.some(p =>
    ['PHONG_QLKH', 'QUAN_TRI'].includes(p.role)
  ) ?? false

  const canEditResearchAllocations = user?.roles.some(p =>
    ['TRUONG_KHOA', 'QUAN_TRI'].includes(p.role)
  ) ?? false
  const canViewJournalConferences = user?.roles.some(p =>
    ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'BAN_GIAM_HIEU', 'QUAN_TRI'].includes(p.role)
  ) ?? false

  const canEditJournalConferences = user?.roles.some(p =>
    ['PHONG_QLKH', 'QUAN_TRI'].includes(p.role)
  ) ?? false

  const canEvaluate = user?.roles.some(p =>
    p.role === 'PHONG_QLKH' || p.role === 'QUAN_TRI'
  ) ?? false
  const canManageConversionRules = user?.roles.some(p =>
    p.role === 'PHONG_QLKH' || p.role === 'QUAN_TRI'
  ) ?? false
  const targetProgressManager = user?.roles.some(p => ['TRUONG_BO_MON', 'TRUONG_KHOA'].includes(p.role)) ?? false
  useEffect(() => {
    let active = true
    api<Profile>('/auth/me', 'GET', undefined, false)
      .then(u => {
        if (active) {
          setUser(u)
          setConnectionError('')
        }
      })
      .catch(e => {
        if (!active) return

        if (e instanceof ApiError && e.status === 401) {
          setUser(null)
          setConnectionError('')
        } else {
          setConnectionError(
            'Không thể kết nối hoặc kiểm tra phiên đăng nhập. Vui lòng thử lại.'
          )
        }
      })
      .finally(() => {
        if (active) setLoading(false)
      })
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

  useEffect(() => {
    let active = true

    if (!user || !personalProfileCandidate) {
      setHasPersonalLecturerProfile(false)
      return () => { active = false }
    }

    setHasPersonalLecturerProfile(false)
    api('/lecturers/me')
      .then(() => { if (active) setHasPersonalLecturerProfile(true) })
      .catch(() => { if (active) setHasPersonalLecturerProfile(false) })

    return () => { active = false }
  }, [user, personalProfileCandidate])

  useEffect(() => {
    if (!user) {
      setUnreadCount(0)
      return
    }

    let active = true

    async function loadUnreadCount() {
      try {
        const result = await api<{ unreadCount: number }>(
          '/notifications/unread-count'
        )

        if (active) {
          setUnreadCount(result.unreadCount)
        }
      } catch {
        // Không làm gián đoạn giao diện nếu chưa tải được số thông báo.
      }
    }

    void loadUnreadCount()

    const interval = window.setInterval(() => {
      void loadUnreadCount()
    }, 60000)

    return () => {
      active = false
      window.clearInterval(interval)
    }
  }, [user])

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
  function openNotificationAction(actionPath: string) {
    const [destination, rawId] = actionPath.split(':')
    const id = Number(rawId) || null
    if (destination === 'products') { setProductToOpen(id); setTab('products') }
    else if (destination === 'product-reviews') { setReviewProductToOpen(id); setTab('product-reviews') }
    else if (destination === 'research-targets' && canViewResearchTargets) setTab('research-targets')
    else if (destination === 'research-plans') setTab('research-plans')
    else if (destination === 'my-targets') setTab('my-targets')
    else if (destination === 'evaluations') setTab('evaluations')
  }
  if (loading) return <main className="loading" role="status">Đang kiểm tra phiên đăng nhập…</main>
  if (connectionError && !user) {
    return (
      <main className="connection-error-page">
        <section className="connection-error-card">
          <div className="connection-error-icon" aria-hidden="true">
            !
          </div>

          <h2>Không thể kết nối máy chủ</h2>

          <p className="connection-error-description">
            Hệ thống hiện không thể xác minh phiên đăng nhập.
            Vui lòng kiểm tra kết nối và thử lại.
          </p>

          <p className="alert" role="alert">
            {connectionError}
          </p>

          <button
            type="button"
            className="primary"
            onClick={() => window.location.reload()}
          >
            Thử lại
          </button>
        </section>
      </main>
    )
  }
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
      <nav aria-label="Điều hướng chính"><button className={tab === 'overview' ? 'active' : ''} onClick={() => setTab('overview')}>{executive ? 'Tổng quan toàn trường' : 'Tổng quan tài khoản'}</button>
        <p className="nav-section">CÁ NHÂN</p>
        {hasPersonalLecturerProfile && <button className={tab === 'my-profile' ? 'active' : ''} onClick={() => setTab('my-profile')}>Hồ sơ của tôi</button>}
        {user.roles.some(r => r.role === 'GIANG_VIEN') && (
          <button
            className={tab === 'research-plans' ? 'active' : ''}
            onClick={() => setTab('research-plans')}
          >
            Kế hoạch & Tiến độ
          </button>
        )}
        {user.roles.some(r => r.role === 'GIANG_VIEN') && <button className={tab === 'my-targets' ? 'active' : ''} onClick={() => setTab('my-targets')}>Chỉ tiêu của tôi</button>}
        <button
          className={`notif-nav-btn${tab === 'notifications' ? ' active' : ''}`}
          onClick={() => setTab('notifications')}
        >
          Thông báo
        </button>
        {user.roles.some(r => r.role === 'GIANG_VIEN') && <button className={tab === 'products' ? 'active' : ''} onClick={() => { setProductToOpen(null); setTab('products') }}>Sản phẩm khoa học</button>}
        {(user.roles.some(r => ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'QUAN_TRI'].includes(r.role)) || canBrowseLecturers(user) || canViewResearchNorms || canViewResearchTargets || canViewJournalConferences) && <p className="nav-section">QUẢN LÝ, ĐÁNH GIÁ & BÁO CÁO</p>}
        {user.roles.some(r =>
          ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'QUAN_TRI']
            .includes(r.role)
        ) && (
            <button
              className={tab === 'product-reviews' ? 'active' : ''}
              onClick={() => { setReviewProductToOpen(null); setTab('product-reviews') }}
            >
              Sản phẩm chờ duyệt
            </button>
          )}
        {(canEvaluate || manager || user.roles.some(r => r.role === 'GIANG_VIEN')) && <button className={tab === 'evaluations' ? 'active' : ''} onClick={() => setTab('evaluations')}>{canEvaluate ? 'Đánh giá thi đua' : executive ? 'Kết quả thi đua toàn trường' : manager ? 'Kết quả thi đua đơn vị' : 'Kết quả thi đua của tôi'}</button>}
        {canEvaluate && <button className={tab === 'emulation-rules' ? 'active' : ''} onClick={() => setTab('emulation-rules')}>Quy chế xếp loại</button>}
        {canBrowseLecturers(user) && <button className={tab === 'lecturers' ? 'active' : ''} onClick={() => setTab('lecturers')}>Hồ sơ giảng viên</button>}
        {canViewResearchNorms && <button className={tab === 'research-norms' ? 'active' : ''} onClick={() => setTab('research-norms')}>Định mức NCKH</button>}
        {canViewResearchTargets && (
          <button
            className={tab === 'research-targets' ? 'active' : ''}
            onClick={() => setTab('research-targets')}
          >
            Chỉ tiêu và phân bổ NCKH
          </button>
        )}
        {canViewJournalConferences && (
          <button
            className={tab === 'journal-conferences' ? 'active' : ''}
            onClick={() => setTab('journal-conferences')}
          >
            Danh mục tạp chí/hội nghị
          </button>
        )}
        {manager && !executive && <button className={tab === 'reports' ? 'active' : ''} onClick={() => setTab('reports')}>Thống kê kết quả</button>}
        {(administrator || canManageConversionRules) && <p className="nav-section">CẤU HÌNH HỆ THỐNG</p>}
        {administrator && <button className={tab === 'admin' ? 'active' : ''} onClick={() => setTab('admin')}>Quản trị dữ liệu nền</button>}
        {canManageConversionRules && <button className={tab === 'conversion-rules' ? 'active' : ''} onClick={() => setTab('conversion-rules')}>Quy định quy đổi</button>}
        <button className={tab === 'password' ? 'active' : ''} onClick={() => setTab('password')}>Đổi mật khẩu</button></nav>
      <div className="sidebar-bottom"><strong>{user.fullName}</strong><span>@{user.username}</span><button onClick={logout} disabled={busy}>Đăng xuất</button></div>
    </aside>
    <main className="content">
      <header className="page-top">
        <span>HUIT / Nghiên cứu khoa học</span>

        <div className="notification-header-action">
          <button
            type="button"
            className="notification-bell"
            title="Xem thông báo"
            aria-label={`Thông báo, ${unreadCount} chưa đọc`}
            onClick={() => setTab('notifications')}
          >
            🔔

            {unreadCount > 0 && (
              <span className="notification-bell-count">
                {unreadCount > 99 ? '99+' : unreadCount}
              </span>
            )}
          </button>

          <span className="status">Đã đăng nhập</span>
        </div>
      </header>
      {error && <p className="alert" role="alert">{error}</p>}
      {tab === 'overview' && executive && <ResearchReports lookups={lookups} executive />}
      {tab === 'overview' && !executive && <><p className="eyebrow">TỔNG QUAN</p><h1>Xin chào, {user.fullName}</h1><p className="muted">Thông tin tài khoản và phạm vi truy cập của bạn.</p>
        {targetProgressManager && <ManagementProgressDashboard lookups={lookups} onOpenTargets={() => setTab('research-targets')} />}
        <section className="panel"><h2>Quyền truy cập</h2>{user.roles.length ? <div className="permission-grid">{user.roles.map((p, i) => <article key={i} className="permission"><strong>{roleNames[p.role] ?? p.role}</strong><span>{scopeNames[p.scope] ?? p.scope}</span>{p.facultyId && <small>{lookups?.faculties.find(f => f.id === p.facultyId)?.name ?? `Khoa #${p.facultyId}`}</small>}{p.departmentId && <small>{lookups?.departments.find(d => d.id === p.departmentId)?.name ?? `Bộ môn #${p.departmentId}`}</small>}</article>)}</div> : <p>Tài khoản chưa được gán quyền. Vui lòng liên hệ quản trị viên.</p>}</section>
        <section className="panel"><h2>Đơn vị và năm học</h2>{lookupError ? <><p className="alert" role="alert">{lookupError}</p><button onClick={() => setRetry(x => x + 1)}>Thử lại</button></> : !lookups ? <p role="status">Đang tải dữ liệu…</p> : <div className="summary-grid"><div><h3>Khoa</h3>{lookups.faculties.map(f => <p key={f.id}>{f.name}</p>)}{!lookups.faculties.length && <p className="muted">Chưa có đơn vị được liên kết.</p>}</div><div><h3>Bộ môn</h3>{lookups.departments.map(d => <p key={d.id}>{d.name}</p>)}{!lookups.departments.length && <p className="muted">Chưa có bộ môn được liên kết.</p>}</div><div><h3>Năm học</h3>{lookups.academicYears.map(y => <p key={y.id}>{y.code}</p>)}{!lookups.academicYears.length && <p className="muted">Chưa có năm học.</p>}</div></div>}</section>
      </>}
      {tab === 'my-profile' && (hasPersonalLecturerProfile ? <MyLecturerProfile user={user} lookups={lookups} /> : <p role="alert">Tài khoản không được liên kết với hồ sơ giảng viên.</p>)}
      {tab === 'research-plans' &&
        user.roles.some(r => r.role === 'GIANG_VIEN') && (
          <ResearchPlans academicYears={lookups?.academicYears ?? []} />
        )}
      {tab === 'my-targets' && user.roles.some(r => r.role === 'GIANG_VIEN') && <MyResearchTargets
        onOpenProducts={() => { setProductToOpen(null); setTab('products') }}
        onOpenProduct={id => { setProductToOpen(id); setTab('products') }}
      />}
      {tab === 'notifications' && (
        <Notifications onUnreadCountChange={setUnreadCount} onOpenAction={openNotificationAction} />
      )}
      {tab === 'products' && <ProductWorkspace user={user} initialProductId={productToOpen} />}
      {tab === 'product-reviews' &&
        user.roles.some(r =>
          ['TRUONG_BO_MON', 'TRUONG_KHOA', 'PHONG_QLKH', 'QUAN_TRI']
            .includes(r.role)
        ) && (
          <ProductReviewWorkspace user={user} initialProductId={reviewProductToOpen} />
        )
      }
      {tab === 'evaluations' && <Evaluations user={user} lookups={lookups} />}
      {tab === 'emulation-rules' && (canEvaluate ? <EmulationClassificationRules years={lookups?.academicYears ?? []} /> : <p role="alert">Bạn không có quyền cấu hình quy chế.</p>)}
      {tab === 'lecturers' && (canBrowseLecturers(user) ? <LecturerDirectory user={user} lookups={lookups} /> : <p role="alert">Bạn không có quyền xem danh sách giảng viên.</p>)}
      {tab === 'research-norms' && (canViewResearchNorms ? <ResearchNorms academicYears={lookups?.academicYears ?? []} canEdit={canEditResearchNorms} /> : <p role="alert">Bạn không có quyền xem định mức NCKH.</p>)}
      {tab === 'research-targets' && (
        canViewResearchTargets
          ? <ResearchTargets
            lookups={lookups}
            canEdit={canEditResearchTargets}
            canEditAllocations={canEditResearchAllocations}
            roles={user?.roles.map(x => x.role) ?? []}
          />
          : <p role="alert">Bạn không có quyền xem chỉ tiêu NCKH.</p>
      )}
      {tab === 'journal-conferences' && (
        canViewJournalConferences
          ? <JournalConferences canEdit={canEditJournalConferences} />
          : <p role="alert">Bạn không có quyền xem danh mục tạp chí/hội nghị.</p>
      )}
      {tab === 'admin' && (administrator ? <AdminPanel currentAccountId={user.id} onChanged={() => setRetry(x => x + 1)} /> : <p role="alert">Bạn không có quyền quản trị.</p>)}
      {tab === 'conversion-rules' && (canManageConversionRules ? <ConversionRules /> : <p role="alert">Chỉ Phòng QLKH hoặc quản trị viên được cấu hình quy định quy đổi.</p>)}
      {tab === 'reports' && (manager ? <ResearchReports lookups={lookups} /> : <p role="alert">Bạn không có quyền xem báo cáo.</p>)}
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

