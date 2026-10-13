
import { useCallback, useEffect, useState } from 'react'
import { api } from './api'
import './Notifications.css'

export type Notification = {
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

type NotificationFilter = 'all' | 'unread' | 'read'

type NotificationsProps = {
  onUnreadCountChange?: (count: number) => void
}

function formatDate(value: string) {
  const date = new Date(value)
  if (Number.isNaN(date.getTime())) return 'Không xác định'

  return new Intl.DateTimeFormat('vi-VN', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

export default function Notifications({
  onUnreadCountChange,
}: NotificationsProps) {
  const [notifications, setNotifications] = useState<Notification[]>([])
  const [filter, setFilter] = useState<NotificationFilter>('all')
  const [loading, setLoading] = useState(true)
  const [busy, setBusy] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')

  const loadNotifications = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const data = await api<Notification[]>('/notifications')
      setNotifications(data)

      const count = await api<{ unreadCount: number }>(
        '/notifications/unread-count'
      )
      onUnreadCountChange?.(count.unreadCount)
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Không thể tải danh sách thông báo.'
      )
    } finally {
      setLoading(false)
    }
  }, [onUnreadCountChange])

  useEffect(() => {
    void loadNotifications()
  }, [loadNotifications])

  async function markAsRead(id: number) {
    setBusy(true)
    setError('')
    setMessage('')

    try {
      const updated = await api<Notification>(
        `/notifications/${id}/read`,
        'PUT'
      )

      setNotifications(previous =>
        previous.map(item => item.id === id ? updated : item)
      )

      const count = await api<{ unreadCount: number }>(
        '/notifications/unread-count'
      )
      onUnreadCountChange?.(count.unreadCount)

      setMessage('Đã đánh dấu thông báo là đã đọc.')
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Không thể cập nhật thông báo.'
      )
    } finally {
      setBusy(false)
    }
  }

  async function markAllAsRead() {
    setBusy(true)
    setError('')
    setMessage('')

    try {
      await api<{ updatedCount: number }>(
        '/notifications/read-all',
        'PUT'
      )

      await loadNotifications()
      setMessage('Đã đánh dấu tất cả thông báo là đã đọc.')
    } catch (err) {
      setError(
        err instanceof Error
          ? err.message
          : 'Không thể cập nhật thông báo.'
      )
    } finally {
      setBusy(false)
    }
  }

  const unreadCount = notifications.filter(item => !item.isRead).length

  const filteredNotifications = notifications.filter(item => {
    if (filter === 'unread') return !item.isRead
    if (filter === 'read') return item.isRead
    return true
  })

  return (
    <div className="notifications-page">
      <p className="eyebrow">TRUNG TÂM THÔNG BÁO</p>

      <div className="notifications-heading">
        <div>
          <h1>Thông báo & Nhắc hạn</h1>
          <p className="muted">
            Theo dõi các thông báo và nhắc hạn liên quan đến
            hoạt động nghiên cứu khoa học.
          </p>
        </div>

        <button
          type="button"
          className="notifications-refresh"
          onClick={() => void loadNotifications()}
          disabled={loading || busy}
        >
          ↻ Làm mới
        </button>
      </div>

      <section className="panel notifications-panel">
        <div className="notifications-toolbar">
          <div className="notifications-filters">
            <button
              type="button"
              className={filter === 'all' ? 'selected' : ''}
              onClick={() => setFilter('all')}
            >
              Tất cả ({notifications.length})
            </button>

            <button
              type="button"
              className={filter === 'unread' ? 'selected' : ''}
              onClick={() => setFilter('unread')}
            >
              Chưa đọc ({unreadCount})
            </button>

            <button
              type="button"
              className={filter === 'read' ? 'selected' : ''}
              onClick={() => setFilter('read')}
            >
              Đã đọc
            </button>
          </div>

          <button
            type="button"
            className="notifications-read-all"
            disabled={busy || loading || unreadCount === 0}
            onClick={() => void markAllAsRead()}
          >
            ✓ Đánh dấu tất cả đã đọc
          </button>
        </div>

        {error && (
          <div className="notifications-error" role="alert">
            <p>{error}</p>
            <button
              type="button"
              onClick={() => void loadNotifications()}
            >
              Thử lại
            </button>
          </div>
        )}

        {message && (
          <p className="notifications-success" role="status">
            {message}
          </p>
        )}

        {loading ? (
          <p className="notifications-empty" role="status">
            Đang tải danh sách thông báo...
          </p>
        ) : filteredNotifications.length === 0 ? (
          <div className="notifications-empty">
            <span className="notifications-empty-icon">♧</span>
            <h3>Chưa có thông báo</h3>
            <p>
              {filter === 'unread'
                ? 'Bạn đã đọc tất cả thông báo.'
                : filter === 'read'
                  ? 'Bạn chưa có thông báo đã đọc.'
                  : 'Các thông báo mới sẽ xuất hiện tại đây.'}
            </p>
          </div>
        ) : (
          <div className="notifications-list">
            {filteredNotifications.map(item => (
              <article
                key={item.id}
                className={`notification-item ${
                  item.isRead ? 'is-read' : 'is-unread'
                }`}
              >
                <div className="notification-symbol" aria-hidden="true">
                  🔔
                </div>

                <div className="notification-body">
                  <div className="notification-meta">
                    <span className="notification-type">
                      {item.type.replaceAll('_', ' ')}
                    </span>

                    {!item.isRead && (
                      <span className="notification-unread-label">
                        Chưa đọc
                      </span>
                    )}
                  </div>

                  <p className="notification-content">
                    {item.content}
                  </p>

                  <div className="notification-details">
                    <span>
                      {formatDate(
                        item.sentAt ?? item.scheduledAt
                      )}
                    </span>

                    <span>
                      {item.sentAt
                        ? 'Đã gửi'
                        : 'Thời gian dự kiến'}
                    </span>
                  </div>
                </div>

                {!item.isRead && (
                  <button
                    type="button"
                    className="notification-mark-read"
                    disabled={busy}
                    onClick={() => void markAsRead(item.id)}
                  >
                    Đánh dấu đã đọc
                  </button>
                )}
              </article>
            ))}
          </div>
        )}
      </section>
    </div>
  )
}
