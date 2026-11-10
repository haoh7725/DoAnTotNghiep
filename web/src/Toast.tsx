import { useEffect } from 'react'

type Props = {
  message: string
  kind: 'success' | 'error'
  onClose: () => void
}

export function Toast({ message, kind, onClose }: Props) {
  useEffect(() => {
    if (!message) return
    const timer = window.setTimeout(onClose, kind === 'error' ? 7000 : 4500)
    return () => window.clearTimeout(timer)
  }, [message, kind, onClose])

  if (!message) return null
  return <div className={`toast toast--${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
    <span>{kind === 'error' ? '!' : '✓'}</span>
    <p>{message}</p>
    <button type="button" onClick={onClose} aria-label="Đóng thông báo">×</button>
  </div>
}
