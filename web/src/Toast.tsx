import { useEffect, useRef } from 'react'

type Props = {
  message: string
  kind: 'success' | 'error'
  onClose: () => void
}

export function Toast({ message, kind, onClose }: Props) {
  const onCloseRef = useRef(onClose)
  useEffect(() => { onCloseRef.current = onClose }, [onClose])
  useEffect(() => {
    if (!message) return
    const timer = window.setTimeout(() => onCloseRef.current(), kind === 'error' ? 7000 : 4500)
    return () => window.clearTimeout(timer)
  }, [message, kind])

  if (!message) return null
  return <div className={`toast toast--${kind}`} role={kind === 'error' ? 'alert' : 'status'}>
    <span>{kind === 'error' ? '!' : '✓'}</span>
    <p>{message}</p>
    <button type="button" onClick={onClose} aria-label="Đóng thông báo">×</button>
  </div>
}
