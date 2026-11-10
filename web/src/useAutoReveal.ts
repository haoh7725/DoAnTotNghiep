import { useEffect, useRef } from 'react'

/** Đưa vùng vừa mở vào màn hình và focus trường nhập đầu tiên. */
export function useAutoReveal<T extends HTMLElement>(visible: boolean, identity?: unknown) {
  const ref = useRef<T | null>(null)

  useEffect(() => {
    if (!visible) return
    let focusTimer: number | undefined
    const frame = window.requestAnimationFrame(() => {
      const element = ref.current
      if (!element) return
      const reduceMotion = window.matchMedia('(prefers-reduced-motion: reduce)').matches
      element.scrollIntoView({ behavior: reduceMotion ? 'auto' : 'smooth', block: 'start' })
      focusTimer = window.setTimeout(() => {
        const field = element.querySelector<HTMLElement>(
          'input:not([disabled]), select:not([disabled]), textarea:not([disabled])'
        ) ?? element.querySelector<HTMLElement>('button:not([disabled])')
        field?.focus({ preventScroll: true })
      }, reduceMotion ? 0 : 320)
    })
    return () => {
      window.cancelAnimationFrame(frame)
      if (focusTimer !== undefined) window.clearTimeout(focusTimer)
    }
  }, [visible, identity])

  return ref
}
