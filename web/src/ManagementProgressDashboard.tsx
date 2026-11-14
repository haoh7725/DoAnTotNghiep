import { useEffect, useMemo, useState } from 'react'
import { Bar, BarChart, CartesianGrid, ResponsiveContainer, Tooltip, XAxis, YAxis } from 'recharts'
import { api, preferredAcademicYear } from './api'
import type { Lookups } from './api'
import './Reports.css'

type Target = {
  id: number
  facultyId: number
  productTypeId: number
  academicYearId: number
  assignedArticleCount: number
  status: string
}

type Progress = {
  assignedArticleCount: number
  approvedArticles: number
  completionPercent: number
  isCompleted: boolean
  units: Array<{
    departmentId: number
    departmentName: string
    allocatedArticles: number
    approvedArticles: number
    completionPercent: number
    isCompleted: boolean
  }>
}

const shortDepartment = (name: string) => name.replace(/^Bộ môn\s+/i, '')

export function ManagementProgressDashboard({ lookups, onOpenTargets }: {
  lookups: Lookups | null
  onOpenTargets: () => void
}) {
  const [targets, setTargets] = useState<Target[]>([])
  const [types, setTypes] = useState<{ id: number; name: string }[]>([])
  const [selectedId, setSelectedId] = useState<number | null>(null)
  const [progress, setProgress] = useState<Progress | null>(null)
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState('')

  useEffect(() => {
    let active = true
    setLoading(true)
    api<{ id: number; name: string }[]>('/product-types').then(rows => { if (active) setTypes(rows) }).catch(() => {})
    api<Target[]>('/research-targets')
      .then(data => {
        if (!active) return
        const available = data.filter(target => target.status !== 'DA_HUY')
        setTargets(available)
        const currentYearId = lookups?.academicYears.length
          ? preferredAcademicYear(lookups.academicYears).id
          : undefined
        const preferred = available.find(target => target.academicYearId === currentYearId && target.status === 'DA_KHOA')
          ?? available.find(target => target.status === 'DA_KHOA')
          ?? available[0]
        setSelectedId(preferred?.id ?? null)
      })
      .catch(reason => { if (active) setError((reason as Error).message) })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [lookups])

  useEffect(() => {
    if (selectedId === null) { setProgress(null); return }
    let active = true
    setLoading(true)
    setError('')
    api<Progress>(`/research-progress/targets/${selectedId}`)
      .then(data => { if (active) setProgress(data) })
      .catch(reason => { if (active) { setProgress(null); setError((reason as Error).message) } })
      .finally(() => { if (active) setLoading(false) })
    return () => { active = false }
  }, [selectedId])

  const selected = targets.find(target => target.id === selectedId)
  const targetLabel = (target: Target) => {
    const faculty = lookups?.faculties.find(item => item.id === target.facultyId)?.name ?? `Khoa #${target.facultyId}`
    const year = lookups?.academicYears.find(item => item.id === target.academicYearId)?.code ?? `Năm học #${target.academicYearId}`
    return `${types.find(t => t.id === target.productTypeId)?.name ?? 'NCKH'} · ${faculty} — ${year} · #${target.id}`
  }
  const chartData = useMemo(() => progress?.units.map(unit => ({
    ...unit,
    label: shortDepartment(unit.departmentName),
  })) ?? [], [progress])

  return <section className="panel management-progress-dashboard">
    <div className="section-top">
      <div>
        <p className="eyebrow">GIÁM SÁT CHỈ TIÊU</p>
        <h2>Tiến độ hoàn thành theo đơn vị</h2>
        <p className="muted">Tỷ lệ tính từ sản phẩm đã công nhận theo từng chỉ tiêu và phạm vi đơn vị bạn quản lý.</p>
      </div>
      <button onClick={onOpenTargets}>Mở nghiệp vụ chỉ tiêu</button>
    </div>
    <div className="management-progress-summary">
      <div><span>Chỉ tiêu đang quản lý</span><strong>{targets.length}</strong></div>
      <div><span>Đang phân bổ</span><strong>{targets.filter(t => ['DA_TIEP_NHAN', 'CONG_BO'].includes(t.status)).length}</strong></div>
      <div><span>Đã khóa phân bổ</span><strong>{targets.filter(t => t.status === 'DA_KHOA').length}</strong></div>
    </div>
    {targets.length > 1 && <label>Chỉ tiêu đang theo dõi
      <select value={selectedId ?? ''} onChange={event => setSelectedId(Number(event.target.value) || null)}>
        {targets.map(target => <option value={target.id} key={target.id}>{targetLabel(target)}</option>)}
      </select>
    </label>}
    {loading ? <p role="status">Đang tải tiến độ…</p> : error ? <p className="alert" role="alert">{error}</p> : !selected || !progress ? <p className="empty">Chưa có chỉ tiêu phù hợp để giám sát.</p> : <>
      <div className="management-progress-summary">
        <div><span>Được giao</span><strong>{progress.assignedArticleCount} sản phẩm</strong></div>
        <div><span>Đã công nhận</span><strong>{progress.approvedArticles} sản phẩm</strong></div>
        <div><span>Hoàn thành</span><strong>{progress.completionPercent}%</strong></div>
      </div>
      <div className="management-progress-total" aria-label={`Hoàn thành ${progress.completionPercent}%`}>
        <span style={{ width: `${Math.min(progress.completionPercent, 100)}%` }} />
      </div>
      {chartData.length ? <div className="management-progress-chart">
        <ResponsiveContainer width="100%" height={Math.max(230, chartData.length * 54)}>
          <BarChart data={chartData} layout="vertical" margin={{ left: 8, right: 24 }}>
            <CartesianGrid strokeDasharray="3 3" />
            <XAxis type="number" domain={[0, 100]} unit="%" />
            <YAxis type="category" dataKey="label" width={150} tick={{ fontSize: 12, fill: '#43566a' }} />
            <Tooltip formatter={(value) => [`${value}%`, 'Hoàn thành']} labelFormatter={(_, payload) => payload[0]?.payload?.departmentName ?? ''} />
            <Bar dataKey="completionPercent" name="Hoàn thành" fill="#168c8c" radius={[0, 6, 6, 0]} />
          </BarChart>
        </ResponsiveContainer>
      </div> : <p className="empty">Chỉ tiêu chưa được phân bổ cho đơn vị trong phạm vi của bạn.</p>}
    </>}
  </section>
}
