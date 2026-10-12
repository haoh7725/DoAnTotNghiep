import { useEffect, useState } from 'react'
import { api } from './api'
import './Notifications.css'

type Notification = {
  id: number
  type: string
  content: string
  channel: string
  deliveryStatus: string
  researchPlanItemId: number | null
  researchPlanId: number | null
  scientificProductId: number | null
  scheduledAt: string
  sentAt: string | null
  readAt: string | null
  isRead: boolean
}

const typeLabel: Record<string, string> = {
  PHAN_HOI: 'Phản hồi',
  NHAC_HAN: 'Nhắc hạn',
  QUA_HAN: 'Quá hạn',
  HE_THONG: 'Hệ thống',
}

const typeIcon: Record<string, string> = {
  PHAN_HOI: '💬',
  NHAC_HAN: '⏰',
  QUA_HAN: '⚠️',
  HE_THONG: 'ℹ️',
}

export function NotificationInbox() {
  const [items, setItems] = useState<Notification[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')
  const [busy, setBusy] = useState(false)
  const [filter, setFilter] = useState<'all' | 'unread' | 'feedback'>('all')

  function load() {
    setLoading(true)
    setError('')
    api<Notification[]>('/notifications')
      .then(setItems)
      .catch(e => setError((e as Error).message))
      .finally(() => setLoading(false))
  }

  useEffect(() => { load() }, [])

  async function markRead(id: number) {
    try {
      const updated = await api<Notification>(`/notifications/${id}/read`, 'PUT')
      setItems(prev => prev.map(n => n.id === id ? updated : n))
    } catch (e) { setError((e as Error).message) }
  }

  async function markAllRead() {
    setBusy(true); setError('')
    try {
      await api('/notifications/read-all', 'PUT')
      setItems(prev => prev.map(n => ({ ...n, isRead: true, readAt: new Date().toISOString() })))
    } catch (e) { setError((e as Error).message) }
    finally { setBusy(false) }
  }

  const filtered = items.filter(n => {
    if (filter === 'unread') return !n.isRead
    if (filter === 'feedback') return n.type === 'PHAN_HOI'
    return true
  })

  const unreadCount = items.filter(n => !n.isRead).length
  const feedbackCount = items.filter(n => n.type === 'PHAN_HOI' && !n.isRead).length

  return (
    <>
      <p className="eyebrow">THÔNG BÁO</p>
      <h1>Hộp thư thông báo</h1>
      <p className="muted">
        Phản hồi từ Phòng quản lý khoa học, nhắc hạn và thông báo hệ thống về sản phẩm của bạn.
      </p>

      {error && <p className="alert" role="alert">{error}</p>}

      <section className="panel">
        <div className="section-top">
          <div className="notif-filter-row">
            <button
              className={filter === 'all' ? 'active' : ''}
              onClick={() => setFilter('all')}
            >
              Tất cả
              {unreadCount > 0 && <span className="notif-badge">{unreadCount}</span>}
            </button>
            <button
              className={filter === 'feedback' ? 'active' : ''}
              onClick={() => setFilter('feedback')}
            >
              Phản hồi QLKH
              {feedbackCount > 0 && <span className="notif-badge notif-badge--feedback">{feedbackCount}</span>}
            </button>
            <button
              className={filter === 'unread' ? 'active' : ''}
              onClick={() => setFilter('unread')}
            >
              Chưa đọc
            </button>
          </div>
          <div className="actions">
            <button onClick={load} disabled={loading}>↺ Làm mới</button>
            {unreadCount > 0 && (
              <button className="primary" onClick={markAllRead} disabled={busy}>
                {busy ? 'Đang xử lý…' : 'Đánh dấu tất cả đã đọc'}
              </button>
            )}
          </div>
        </div>

        {loading ? (
          <p role="status">Đang tải thông báo…</p>
        ) : filtered.length === 0 ? (
          <p className="empty">
            {filter === 'unread' ? 'Không có thông báo chưa đọc.' :
             filter === 'feedback' ? 'Chưa có phản hồi nào từ Phòng QLKH.' :
             'Chưa có thông báo nào.'}
          </p>
        ) : (
          <ul className="notif-list" role="list">
            {filtered.map(n => (
              <li
                key={n.id}
                className={`notif-item${!n.isRead ? ' notif-item--unread' : ''}${n.type === 'PHAN_HOI' ? ' notif-item--feedback' : ''}`}
              >
                <span className="notif-icon" aria-hidden="true">{typeIcon[n.type] ?? 'ℹ️'}</span>
                <div className="notif-body">
                  <div className="notif-meta">
                    <span className={`notif-type notif-type--${n.type.toLowerCase()}`}>
                      {typeLabel[n.type] ?? n.type}
                    </span>
                    {n.scientificProductId && (
                      <span className="notif-ref">Sản phẩm #{n.scientificProductId}</span>
                    )}
                    {n.researchPlanItemId && !n.scientificProductId && (
                      <span className="notif-ref">Nội dung kế hoạch #{n.researchPlanItemId}</span>
                    )}
                    {!n.isRead && <span className="notif-dot" aria-label="Chưa đọc" />}
                  </div>
                  <p className="notif-content">{n.content}</p>
                  <div className="notif-foot">
                    <time dateTime={n.scheduledAt} className="notif-time">
                      {new Date(n.scheduledAt).toLocaleString('vi-VN')}
                    </time>
                    {n.readAt && (
                      <span className="notif-read-time">
                        · Đã đọc lúc {new Date(n.readAt).toLocaleString('vi-VN')}
                      </span>
                    )}
                  </div>
                </div>
                {!n.isRead && (
                  <button
                    className="notif-read-btn"
                    onClick={() => markRead(n.id)}
                    title="Đánh dấu đã đọc"
                    aria-label="Đánh dấu thông báo này đã đọc"
                  >
                    ✓
                  </button>
                )}
              </li>
            ))}
          </ul>
        )}
      </section>
    </>
  )
}

/** Badge số thông báo chưa đọc dùng cho sidebar */
export function useUnreadCount(): number {
  const [count, setCount] = useState(0)
  useEffect(() => {
    let active = true
    const fetchCount = () =>
      api<{ unreadCount: number }>('/notifications/unread-count')
        .then(r => { if (active) setCount(r.unreadCount) })
        .catch(() => { /* im lặng */ })
    fetchCount()
    const id = setInterval(fetchCount, 60_000)
    return () => { active = false; clearInterval(id) }
  }, [])
  return count
}
