
import { useEffect, useState } from 'react'
import type { FormEvent } from 'react'
import { api } from './api'
import type { AcademicYear } from './api'

type Lecturer = {
    id: number
}

type ResearchPlan = {
    id: number
    lecturerId: number
    academicYearId: number
    committedProductCount: number
    planDate: string
    status: string
    deadline: string | null
}


type ResearchPlanItem = {
    id: number
    researchPlanId: number
    productTypeId: number
    name: string
    committedQuantity: number
    deadline: string | null
    status: string
    result: string | null
    difficultyProposal: string | null
    updatedAt: string
}

type ProgressHistory = {
    id: number
    researchPlanItemId: number
    updatedByAccountId: number
    status: string
    result: string | null
    difficultyProposal: string | null
    updatedAt: string
}
type ProductType = {
    id: number
    code: string
    name: string
}
const progressLabels: Record<string, string> = {
    CHUA_THUC_HIEN: 'Chưa thực hiện',
    DANG_THUC_HIEN: 'Đang thực hiện',
    DA_THUC_HIEN: 'Đã thực hiện',
}


type Props = {
    academicYears: AcademicYear[]
}

export function ResearchPlans({ academicYears }: Props) {
    const [lecturerId, setLecturerId] = useState<number | null>(null)
    const [yearId, setYearId] = useState('')
    const [plan, setPlan] = useState<ResearchPlan | null>(null)
    const [editingPlan, setEditingPlan] = useState(false)
    const [loading, setLoading] = useState(true)
    const [busy, setBusy] = useState(false)
    const [error, setError] = useState('')
    const [message, setMessage] = useState('')
    const [items, setItems] = useState<ResearchPlanItem[]>([])
    const [editingItem, setEditingItem] = useState<ResearchPlanItem | null>(null)
    const [productTypes, setProductTypes] = useState<ProductType[]>([])
    const [productTypesLoading, setProductTypesLoading] = useState(true)
    const [productTypesError, setProductTypesError] = useState('')
    const [itemsLoading, setItemsLoading] = useState(false)
    const [selectedItem, setSelectedItem] = useState<number | null>(null)
    const [histories, setHistories] = useState<ProgressHistory[]>([])
    const [historyLoading, setHistoryLoading] = useState(false)
    const [itemBusy, setItemBusy] = useState(false)
    const [progressStatus, setProgressStatus] = useState('CHUA_THUC_HIEN')
    const [progressResult, setProgressResult] = useState('')
    const [progressDifficulty, setProgressDifficulty] = useState('')

    useEffect(() => {
        let active = true

        api<ProductType[]>('/product-types')
            .then(data => {
                if (active) setProductTypes(data)
            })
            .catch(e => {
                if (active) setProductTypesError((e as Error).message)
            })
            .finally(() => {
                if (active) setProductTypesLoading(false)
            })

        return () => {
            active = false
        }
    }, [])

    useEffect(() => {
        if (academicYears.length && !yearId) {
            setYearId(String(academicYears[0].id))
        }
    }, [academicYears, yearId])

    useEffect(() => {
        let active = true

        api<Lecturer>('/lecturers/me')
            .then(data => {
                if (active) setLecturerId(data.id)
            })
            .catch(e => {
                if (active) setError((e as Error).message)
            })
            .finally(() => {
                if (active) setLoading(false)
            })

        return () => { active = false }
    }, [])

    useEffect(() => {
        if (!lecturerId || !yearId) return

        let active = true
        setLoading(true)
        setError('')
        setPlan(null)

        api<ResearchPlan | undefined>(
            `/research-plans?lecturerId=${lecturerId}&academicYearId=${yearId}`
        )
            .then(data => {
                if (active) setPlan(data ?? null)
            })
            .catch(e => {
                if (active) setError((e as Error).message)
            })
            .finally(() => {
                if (active) setLoading(false)
            })

        return () => { active = false }
    }, [lecturerId, yearId])

    useEffect(() => {
        if (!plan) {
            setItems([])
            setSelectedItem(null)
            setHistories([])
            return
        }

        let active = true
        setItemsLoading(true)
        setItems([])
        setSelectedItem(null)
        setHistories([])

        api<ResearchPlanItem[]>(`/research-plans/${plan.id}/items`)
            .then(data => {
                if (active) setItems(data)
            })
            .catch(e => {
                if (active) setError((e as Error).message)
            })
            .finally(() => {
                if (active) setItemsLoading(false)
            })

        return () => { active = false }
    }, [plan?.id])

    async function createPlan(e: FormEvent<HTMLFormElement>) {
        e.preventDefault()
        if (!lecturerId || !yearId) return

        const form = new FormData(e.currentTarget)
        setBusy(true)
        setError('')
        setMessage('')

        try {
            const result = await api<ResearchPlan>(
                '/research-plans',
                'POST',
                {
                    lecturerId,
                    academicYearId: Number(yearId),
                    committedProductCount: Number(form.get('count')),
                    planDate: form.get('planDate'),
                    deadline: form.get('deadline') || null,
                }
            )

            setPlan(result)
            setMessage('Tạo kế hoạch thành công.')
        } catch (e) {
            setError((e as Error).message)
        } finally {
            setBusy(false)
        }
    }

    async function submitPlan() {
        if (!plan || !window.confirm('Xác nhận đăng ký kế hoạch? Sau khi đăng ký, bạn không thể chỉnh sửa kế hoạch và nội dung.')) return

        setBusy(true)
        setError('')
        setMessage('')

        try {
            const result = await api<ResearchPlan>(
                `/research-plans/${plan.id}/submit`,
                'POST'
            )
            setPlan(result)
            setMessage('Đăng ký kế hoạch thành công.')
        } catch (e) {
            setError((e as Error).message)
        } finally {
            setBusy(false)
        }
    }


    async function saveEditPlan(e: FormEvent<HTMLFormElement>) {
        e.preventDefault()

        if (!plan || plan.status !== 'NHAP') return

        const form = new FormData(e.currentTarget)
        const committedProductCount = Number(form.get('committedProductCount'))
        const deadline = String(form.get('deadline') ?? '')

        if (!Number.isInteger(committedProductCount) || committedProductCount < 0) {
            setError('Số sản phẩm cam kết phải là số nguyên không âm.')
            return
        }

        if (deadline && deadline < plan.planDate.slice(0, 10)) {
            setError('Hạn hoàn thành không được trước ngày lập kế hoạch.')
            return
        }

        setBusy(true)
        setError('')
        setMessage('')

        try {
            const updated = await api<ResearchPlan>(
                `/research-plans/${plan.id}`,
                'PUT',
                {
                    committedProductCount,
                    deadline: deadline || null,
                }
            )

            setPlan(updated)
            setEditingPlan(false)
            setMessage('Cập nhật thông tin kế hoạch thành công.')
        } catch (error) {
            setError((error as Error).message)
        } finally {
            setBusy(false)
        }
    }


    async function createItem(e: FormEvent<HTMLFormElement>) {
        e.preventDefault()
        if (!plan || plan.status !== 'NHAP') return
        const formElement = e.currentTarget
        const form = new FormData(e.currentTarget)

        const productTypeId = Number(form.get('productTypeId'))
        const name = String(form.get('name') ?? '').trim()
        const committedQuantity = Number(form.get('committedQuantity'))
        const deadline = String(form.get('deadline') ?? '')

        if (!Number.isInteger(productTypeId) || productTypeId <= 0) {
            setError('Vui lòng chọn loại sản phẩm hợp lệ.')
            return
        }

        if (!name || !Number.isInteger(committedQuantity) || committedQuantity < 1) {
            setError('Vui lòng nhập tên và số lượng cam kết hợp lệ.')
            return
        }

        setItemBusy(true)
        setError('')
        setMessage('')

        try {
            const created = await api<ResearchPlanItem>(
                `/research-plans/${plan.id}/items`,
                'POST',
                {
                    productTypeId,
                    name,
                    committedQuantity,
                    deadline: deadline || null,
                }
            )

            setItems(previous => [...previous, created])
            formElement.reset()
            setMessage('Thêm nội dung nghiên cứu thành công.')
        } catch (e) {
            setError((e as Error).message)
        } finally {
            setItemBusy(false)
        }
    }


    function startEditItem(item: ResearchPlanItem) {
        if (plan?.status !== 'NHAP') return

        setEditingItem(item)
        setError('')
        setMessage('')
    }

    async function saveEditItem(e: FormEvent<HTMLFormElement>) {
        e.preventDefault()

        if (!editingItem || plan?.status !== 'NHAP') return

        const form = new FormData(e.currentTarget)

        const name = String(form.get('name') ?? '').trim()
        const committedQuantity = Number(form.get('committedQuantity'))
        const deadline = String(form.get('deadline') ?? '')

        if (!name || name.length > 500) {
            setError('Tên nội dung phải có từ 1 đến 500 ký tự.')
            return
        }

        if (
            !Number.isInteger(committedQuantity) ||
            committedQuantity < 1
        ) {
            setError('Số lượng cam kết phải là số nguyên từ 1 trở lên.')
            return
        }

        setItemBusy(true)
        setError('')
        setMessage('')

        try {
            const updated = await api<ResearchPlanItem>(
                `/research-plan-items/${editingItem.id}`,
                'PUT',
                {
                    name,
                    committedQuantity,
                    deadline: deadline || null,
                }
            )

            setItems(previous =>
                previous.map(item =>
                    item.id === updated.id ? updated : item
                )
            )

            setEditingItem(null)
            setMessage('Cập nhật nội dung nghiên cứu thành công.')
        } catch (error) {
            setError((error as Error).message)
        } finally {
            setItemBusy(false)
        }
    }


    async function openProgress(item: ResearchPlanItem) {
        setSelectedItem(item.id)
        setProgressStatus(item.status)
        setProgressResult(item.result ?? '')
        setProgressDifficulty(item.difficultyProposal ?? '')
        setHistories([])
        setHistoryLoading(true)
        setError('')

        try {
            const data = await api<ProgressHistory[]>(
                `/research-plan-items/${item.id}/progress-history`
            )
            setHistories(data)
        } catch (e) {
            setError((e as Error).message)
        } finally {
            setHistoryLoading(false)
        }
    }

    async function saveProgress(e: FormEvent<HTMLFormElement>) {
        e.preventDefault()
        if (selectedItem === null) return

        setItemBusy(true)
        setError('')
        setMessage('')

        try {
            const updated = await api<ResearchPlanItem>(
                `/research-plan-items/${selectedItem}/progress`,
                'PUT',
                {
                    status: progressStatus,
                    result: progressResult.trim() || null,
                    difficultyProposal: progressDifficulty.trim() || null,
                }
            )

            setItems(previous =>
                previous.map(item =>
                    item.id === updated.id ? updated : item
                )
            )

            const history = await api<ProgressHistory[]>(
                `/research-plan-items/${selectedItem}/progress-history`
            )
            setHistories(history)
            setMessage('Cập nhật tiến độ thành công.')
        } catch (e) {
            setError((e as Error).message)
        } finally {
            setItemBusy(false)
        }
    }

    return (
        <div className="research-plans">
            <p className="eyebrow">QUẢN LÝ NGHIÊN CỨU</p>
            <h1>Kế hoạch nghiên cứu khoa học</h1>
            <p className="muted">
                Lập kế hoạch và theo dõi tiến độ nghiên cứu theo năm học.
            </p>

            {error && <p className="alert" role="alert">{error}</p>}
            {message && <p role="status">{message}</p>}

            <section className="panel">
                <h2>Chọn năm học</h2>
                <label>
                    Năm học
                    <select
                        value={yearId}
                        onChange={e => {
                            setYearId(e.target.value)
                            setMessage('')
                        }}
                        disabled={busy}
                    >
                        {!academicYears.length && (
                            <option value="">Chưa có năm học</option>
                        )}
                        {academicYears.map(year => (
                            <option key={year.id} value={year.id}>
                                {year.code}
                            </option>
                        ))}
                    </select>
                </label>
            </section>

            {loading ? (
                <section className="panel">
                    <p role="status">Đang tải kế hoạch…</p>
                </section>
            ) : !lecturerId ? (
                <section className="panel">
                    <p>Không tìm thấy hồ sơ giảng viên của tài khoản.</p>
                </section>
            ) : !yearId ? (
                <section className="panel">
                    <p>Chưa có dữ liệu năm học.</p>
                </section>
            ) : !plan ? (
                <section className="panel">
                    <h2>Tạo kế hoạch mới</h2>
                    <p className="muted">
                        Chưa có kế hoạch trong năm học đã chọn.
                    </p>

                    <form onSubmit={createPlan}>
                        <label>
                            Số sản phẩm cam kết
                            <input
                                name="count"
                                type="number"
                                min="0"
                                defaultValue="1"
                                required
                            />
                        </label>

                        <label>
                            Ngày lập kế hoạch
                            <input
                                name="planDate"
                                type="date"
                                defaultValue={new Date().toLocaleDateString('en-CA')}
                                required
                            />
                        </label>

                        <label>
                            Hạn hoàn thành
                            <input name="deadline" type="date" />
                        </label>

                        <button className="primary" disabled={busy}>
                            {busy ? 'Đang lưu…' : 'Tạo kế hoạch'}
                        </button>
                    </form>
                </section>
            ) : (
                <>
                    <section className="panel">
                        <h2>Nội dung kế hoạch</h2>

                        {plan.status === 'NHAP' && (
                            <form onSubmit={createItem}>
                                <h3>Thêm nội dung nghiên cứu</h3>

                                <label>
                                    Loại sản phẩm khoa học
                                    <select
                                        name="productTypeId"
                                        defaultValue=""
                                        required
                                        disabled={
                                            productTypesLoading ||
                                            !!productTypesError ||
                                            productTypes.length === 0
                                        }
                                    >
                                        <option value="">
                                            {productTypesLoading
                                                ? 'Đang tải loại sản phẩm…'
                                                : 'Chọn loại sản phẩm khoa học'}
                                        </option>

                                        {productTypes.map(type => (
                                            <option key={type.id} value={type.id}>
                                                {type.name}
                                            </option>
                                        ))}
                                    </select>
                                </label>

                                <label>
                                    Tên nội dung nghiên cứu
                                    <input
                                        name="name"
                                        type="text"
                                        maxLength={500}
                                        placeholder="Nhập tên nội dung nghiên cứu"
                                        required
                                    />
                                </label>

                                <label>
                                    Số lượng cam kết
                                    <input
                                        name="committedQuantity"
                                        type="number"
                                        min="1"
                                        step="1"
                                        defaultValue="1"
                                        required
                                    />
                                </label>

                                <label>
                                    Hạn hoàn thành
                                    <input name="deadline" type="date" />
                                </label>

                                <button className="primary" disabled={itemBusy}>
                                    {itemBusy ? 'Đang thêm…' : 'Thêm nội dung'}
                                </button>
                            </form>
                        )}

                        {itemsLoading ? (
                            <p role="status">Đang tải nội dung…</p>
                        ) : items.length === 0 ? (
                            <p className="muted">Kế hoạch chưa có nội dung nghiên cứu.</p>
                        ) : (
                            <div className="plan-items">
                                {items.map(item => (
                                    <article key={item.id} className="plan-item">
                                        <h3>{item.name}</h3>

                                        <p>
                                            <strong>Trạng thái:</strong>{' '}
                                            {progressLabels[item.status] ?? item.status}
                                        </p>

                                        <p>
                                            <strong>Số lượng cam kết:</strong>{' '}
                                            {item.committedQuantity}
                                        </p>

                                        <p>
                                            <strong>Hạn hoàn thành:</strong>{' '}
                                            {item.deadline ?? 'Chưa thiết lập'}
                                        </p>

                                        {plan.status === 'NHAP' && (
                                            <button
                                                type="button"
                                                onClick={() => startEditItem(item)}
                                                disabled={itemBusy}
                                            >
                                                Chỉnh sửa
                                            </button>
                                        )}

                                        <button
                                            type="button"
                                            onClick={() => openProgress(item)}
                                            disabled={itemBusy}
                                        >
                                            Cập nhật tiến độ / Xem lịch sử
                                        </button>
                                    </article>
                                ))}
                            </div>
                        )}

                        {plan.status === 'NHAP' && editingItem && (
                            <form
                                key={editingItem.id}
                                onSubmit={saveEditItem}
                            >
                                <h3>Chỉnh sửa nội dung nghiên cứu</h3>

                                <label>
                                    Tên nội dung nghiên cứu
                                    <input
                                        name="name"
                                        type="text"
                                        maxLength={500}
                                        defaultValue={editingItem.name}
                                        required
                                    />
                                </label>

                                <label>
                                    Số lượng cam kết
                                    <input
                                        name="committedQuantity"
                                        type="number"
                                        min="1"
                                        step="1"
                                        defaultValue={editingItem.committedQuantity}
                                        required
                                    />
                                </label>

                                <label>
                                    Hạn hoàn thành
                                    <input
                                        name="deadline"
                                        type="date"
                                        defaultValue={editingItem.deadline?.slice(0, 10) ?? ''}
                                    />
                                </label>

                                <div className="plan-item-actions">
                                    <button
                                        className="primary"
                                        type="submit"
                                        disabled={itemBusy}
                                    >
                                        {itemBusy ? 'Đang lưu…' : 'Lưu chỉnh sửa'}
                                    </button>

                                    <button
                                        type="button"
                                        disabled={itemBusy}
                                        onClick={() => setEditingItem(null)}
                                    >
                                        Hủy
                                    </button>
                                </div>
                            </form>
                        )}
                    </section>

                    {selectedItem !== null && (
                        <section className="panel">
                            <h2>Cập nhật tiến độ</h2>

                            <form onSubmit={saveProgress}>
                                <label>
                                    Trạng thái
                                    <select
                                        value={progressStatus}
                                        onChange={e => setProgressStatus(e.target.value)}
                                        disabled={itemBusy}
                                    >
                                        <option value="CHUA_THUC_HIEN">Chưa thực hiện</option>
                                        <option value="DANG_THUC_HIEN">Đang thực hiện</option>
                                        <option value="DA_THUC_HIEN">Đã thực hiện</option>
                                    </select>
                                </label>

                                <label>
                                    Kết quả thực hiện
                                    <textarea
                                        value={progressResult}
                                        onChange={e => setProgressResult(e.target.value)}
                                        disabled={itemBusy}
                                        rows={3}
                                        placeholder="Nhập kết quả nghiên cứu…"
                                    />
                                </label>

                                <label>
                                    Khó khăn / Đề xuất
                                    <textarea
                                        value={progressDifficulty}
                                        onChange={e => setProgressDifficulty(e.target.value)}
                                        disabled={itemBusy}
                                        rows={3}
                                        placeholder="Nhập khó khăn hoặc đề xuất…"
                                    />
                                </label>

                                <button className="primary" disabled={itemBusy}>
                                    {itemBusy ? 'Đang lưu…' : 'Lưu tiến độ'}
                                </button>
                            </form>

                            <h3>Lịch sử cập nhật</h3>

                            {historyLoading ? (
                                <p role="status">Đang tải lịch sử…</p>
                            ) : histories.length === 0 ? (
                                <p className="muted">Chưa có lịch sử cập nhật.</p>
                            ) : (
                                <div>
                                    {histories.map(history => (
                                        <article key={history.id} className="plan-history">
                                            <p>
                                                <strong>Thời gian:</strong>{' '}
                                                {new Date(history.updatedAt).toLocaleString('vi-VN')}
                                            </p>
                                            <p>
                                                <strong>Trạng thái:</strong>{' '}
                                                {progressLabels[history.status] ?? history.status}
                                            </p>
                                            <p>
                                                <strong>Kết quả:</strong>{' '}
                                                {history.result || 'Chưa có'}
                                            </p>
                                            <p>
                                                <strong>Khó khăn / Đề xuất:</strong>{' '}
                                                {history.difficultyProposal || 'Không có'}
                                            </p>
                                        </article>
                                    ))}
                                </div>
                            )}
                        </section>
                    )}


                    <section className="panel">
                        <h2>Thông tin kế hoạch</h2>

                        <p>
                            <strong>Trạng thái:</strong>{' '}
                            {plan.status === 'NHAP' ? 'Bản nháp' : 'Đã đăng ký'}
                        </p>

                        <p>
                            <strong>Số sản phẩm cam kết:</strong>{' '}
                            {plan.committedProductCount}
                        </p>

                        <p>
                            <strong>Ngày lập:</strong> {plan.planDate}
                        </p>

                        <p>
                            <strong>Hạn hoàn thành:</strong>{' '}
                            {plan.deadline ?? 'Chưa thiết lập'}
                        </p>

                        {plan.status === 'NHAP' && (
                            <>
                                {!editingPlan ? (
                                    <button
                                        type="button"
                                        onClick={() => setEditingPlan(true)}
                                        disabled={busy}
                                    >
                                        Chỉnh sửa kế hoạch
                                    </button>
                                ) : (
                                    <form onSubmit={saveEditPlan}>
                                        <h3>Chỉnh sửa thông tin kế hoạch</h3>

                                        <label>
                                            Số sản phẩm cam kết
                                            <input
                                                name="committedProductCount"
                                                type="number"
                                                min="0"
                                                step="1"
                                                defaultValue={plan.committedProductCount}
                                                required
                                            />
                                        </label>

                                        <label>
                                            Hạn hoàn thành
                                            <input
                                                name="deadline"
                                                type="date"
                                                defaultValue={plan.deadline?.slice(0, 10) ?? ''}
                                            />
                                        </label>

                                        <div className="plan-item-actions">
                                            <button
                                                className="primary"
                                                type="submit"
                                                disabled={busy}
                                            >
                                                {busy ? 'Đang lưu…' : 'Lưu chỉnh sửa'}
                                            </button>

                                            <button
                                                type="button"
                                                disabled={busy}
                                                onClick={() => setEditingPlan(false)}
                                            >
                                                Hủy
                                            </button>
                                        </div>
                                    </form>
                                )}

                                {!editingPlan && (
                                    <button
                                        className="primary"
                                        type="button"
                                        onClick={submitPlan}
                                        disabled={busy}
                                    >
                                        {busy ? 'Đang xử lý…' : 'Đăng ký kế hoạch'}
                                    </button>
                                )}
                            </>
                        )}
                    </section>

                </>
            )}
        </div>
    )
}

