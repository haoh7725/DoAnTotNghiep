
import { useCallback, useEffect, useState } from 'react'
import { useAutoReveal } from './useAutoReveal'

type CatalogType = 'TAP_CHI' | 'HOI_NGHI'

type CatalogItem = {
  id: number
  name: string
  type: CatalogType
  issn: string | null
  index: string | null
  classification: string | null
  notes: string | null
  createdAt: string
}

type CatalogForm = {
  name: string
  type: CatalogType
  issn: string
  index: string
  classification: string
  notes: string
}

type Props = {
  canEdit: boolean
}

const emptyForm: CatalogForm = {
  name: '',
  type: 'TAP_CHI',
  issn: '',
  index: '',
  classification: '',
  notes: ''
}

const API_URL = '/api/journal-conferences'

async function request<T>(
  url: string,
  options: RequestInit = {}
): Promise<T> {
  const response = await fetch(url, {
    ...options,
    credentials: 'include',
    headers: {
      'Content-Type': 'application/json',
      'X-Requested-With': 'ResearchHub',
      ...options.headers
    }
  })

  if (!response.ok) {
    let message = `Yêu cầu thất bại (HTTP ${response.status}).`

    try {
      const data = await response.json()
      message = data.title || data.message || message
    } catch {
      // Giữ thông báo HTTP mặc định.
    }

    throw new Error(message)
  }

  return response.json() as Promise<T>
}

export default function JournalConferences({ canEdit }: Props) {
  const [items, setItems] = useState<CatalogItem[]>([])
  const [keyword, setKeyword] = useState('')
  const [filterType, setFilterType] = useState('')
  const [loading, setLoading] = useState(false)
  const [saving, setSaving] = useState(false)
  const [error, setError] = useState('')
  const [message, setMessage] = useState('')
  const [editingId, setEditingId] = useState<number | null>(null)
  const [showForm, setShowForm] = useState(false)
  const [form, setForm] = useState<CatalogForm>(emptyForm)
  const formRef = useAutoReveal<HTMLDivElement>(showForm, editingId ?? 'new')

  const loadItems = useCallback(async () => {
    setLoading(true)
    setError('')

    try {
      const params = new URLSearchParams()
      if (keyword.trim()) params.set('keyword', keyword.trim())
      if (filterType) params.set('type', filterType)

      const query = params.toString()
      const url = query ? `${API_URL}?${query}` : API_URL

      const data = await request<CatalogItem[]>(url)
      setItems(data)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể tải danh mục.')
    } finally {
      setLoading(false)
    }
  }, [keyword, filterType])

  useEffect(() => {
    void loadItems()
  }, [loadItems])

  function startCreate() {
    setEditingId(null)
    setForm({ ...emptyForm })
    setError('')
    setMessage('')
    setShowForm(true)
  }

  function startEdit(item: CatalogItem) {
    setEditingId(item.id)
    setForm({
      name: item.name,
      type: item.type,
      issn: item.issn ?? '',
      index: item.index ?? '',
      classification: item.classification ?? '',
      notes: item.notes ?? ''
    })
    setError('')
    setMessage('')
    setShowForm(true)
  }

  function updateField<K extends keyof CatalogForm>(
    field: K,
    value: CatalogForm[K]
  ) {
    setForm(previous => ({ ...previous, [field]: value }))
  }

  async function saveItem(event: React.FormEvent<HTMLFormElement>) {
    event.preventDefault()
    if (!canEdit || saving) return

    if (!form.name.trim()) {
      setError('Vui lòng nhập tên nơi công bố (tạp chí hoặc hội nghị).')
      return
    }

    setSaving(true)
    setError('')
    setMessage('')

    try {
      const isEditing = editingId !== null
      const url = isEditing ? `${API_URL}/${editingId}` : API_URL

      await request<CatalogItem>(url, {
        method: isEditing ? 'PUT' : 'POST',
        body: JSON.stringify({
          name: form.name.trim(),
          type: form.type,
          issn: form.issn.trim() || null,
          index: form.index.trim() || null,
          classification: form.classification.trim() || null,
          notes: form.notes.trim() || null
        })
      })

      setShowForm(false)
      setEditingId(null)
      setMessage(
        isEditing
          ? 'Cập nhật nơi công bố thành công.'
          : 'Thêm nơi công bố thành công.'
      )
      await loadItems()
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Không thể lưu danh mục.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <section>
      <p className="eyebrow">QUẢN LÝ NGHIÊN CỨU KHOA HỌC</p>
      <h1>Nơi công bố bài báo</h1>
      <p>
        Quản lý nơi công bố bài báo. Admin và Phòng QLKH có thể nhập thêm tên tạp chí hoặc hội nghị mới.
        Nhóm sản phẩm và phân loại tính giờ/điểm được quản lý riêng trong Quy định quy đổi.
      </p>

      {message && <p role="status">{message}</p>}
      {error && <p role="alert" style={{ color: '#b42318' }}>{error}</p>}

      <div className="card" style={{ marginTop: 24 }}>
        <h3>Danh sách nơi công bố</h3>

        <div
          style={{
            display: 'flex',
            gap: 12,
            flexWrap: 'wrap',
            marginBottom: 16
          }}
        >
          <input
            type="search"
            placeholder="Tìm tên tạp chí hoặc hội nghị..."
            value={keyword}
            onChange={event => setKeyword(event.target.value)}
            aria-label="Tìm kiếm nơi công bố"
          />

          <select
            value={filterType}
            onChange={event => setFilterType(event.target.value)}
            aria-label="Lọc hình thức công bố"
          >
            <option value="">Tất cả hình thức công bố</option>
            <option value="TAP_CHI">Tạp chí</option>
            <option value="HOI_NGHI">Hội nghị</option>
          </select>

          {canEdit && (
            <button type="button" onClick={startCreate}>
              + Thêm nơi công bố
            </button>
          )}
        </div>

        {loading ? (
          <p>Đang tải nơi công bố...</p>
        ) : items.length === 0 ? (
          <p>Không tìm thấy tạp chí hoặc hội nghị phù hợp.</p>
        ) : (
          <div className="table-wrap">
            <table style={{ width: '100%' }}>
              <thead>
                <tr>
                  <th>Tên nơi công bố</th>
                  <th>Hình thức công bố</th>
                  <th>ISSN</th>
                  <th>Chỉ mục tham khảo</th>
                  <th>Phân hạng tham khảo</th>
                  <th>Ghi chú</th>
                  {canEdit && <th>Thao tác</th>}
                </tr>
              </thead>
              <tbody>
                {items.map(item => (
                  <tr key={item.id}>
                    <td>{item.name}</td>
                    <td>{item.type === 'TAP_CHI' ? 'Tạp chí' : 'Hội nghị'}</td>
                    <td>{item.issn || '—'}</td>
                    <td>{item.index || '—'}</td>
                    <td>{item.classification || '—'}</td>
                    <td>{item.notes || '—'}</td>
                    {canEdit && (
                      <td>
                        <button type="button" onClick={() => startEdit(item)}>
                          Sửa
                        </button>
                      </td>
                    )}
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {canEdit && showForm && (
        <div ref={formRef} className="card auto-reveal-target" style={{ marginTop: 20 }}>
          <h3>
            {editingId === null ? 'Thêm nơi công bố' : 'Sửa nơi công bố'}
          </h3>

          <form onSubmit={saveItem}>
            <div style={{ display: 'grid', gap: 14 }}>
              <label>
                Tên nơi công bố (tạp chí hoặc hội nghị) *
                <input
                  value={form.name}
                  maxLength={300}
                  required
                  onChange={event => updateField('name', event.target.value)}
                />
              </label>

              <label>
                Hình thức công bố *
                <select
                  value={form.type}
                  onChange={event =>
                    updateField('type', event.target.value as CatalogType)
                  }
                >
                  <option value="TAP_CHI">Tạp chí</option>
                  <option value="HOI_NGHI">Hội nghị</option>
                </select>
                <small className="muted">Tạp chí và Hội nghị là hình thức nơi công bố, không phải toàn bộ nhóm sản phẩm NCKH. Bạn tự nhập tên nơi công bố ở ô phía trên.</small>
              </label>

              <label>
                ISSN
                <input
                  value={form.issn}
                  maxLength={30}
                  onChange={event => updateField('issn', event.target.value)}
                />
              </label>

              <label>
                Chỉ mục tham khảo
                <input
                  value={form.index}
                  maxLength={100}
                  placeholder="Ví dụ: Scopus, Web of Science"
                  onChange={event => updateField('index', event.target.value)}
                />
              </label>

              <label>
                Phân hạng tham khảo
                <input
                  value={form.classification}
                  maxLength={100}
                  placeholder="Ví dụ: Q1, Q2; cần kiểm tra năm áp dụng"
                  onChange={event =>
                    updateField('classification', event.target.value)
                  }
                />
              </label>

              <label>
                Ghi chú
                <textarea
                  value={form.notes}
                  rows={3}
                  onChange={event => updateField('notes', event.target.value)}
                />
              </label>

              <div style={{ display: 'flex', gap: 10 }}>
                <button type="submit" disabled={saving}>
                  {saving ? 'Đang lưu...' : 'Lưu nơi công bố'}
                </button>
                <button
                  type="button"
                  disabled={saving}
                  onClick={() => {
                    setShowForm(false)
                    setError('')
                  }}
                >
                  Hủy
                </button>
              </div>
            </div>
          </form>
        </div>
      )}
    </section>
  )
}
