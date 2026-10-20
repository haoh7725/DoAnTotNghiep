
import { useCallback, useEffect, useState } from 'react'

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
      setError('Vui lòng nhập tên tạp chí/hội nghị.')
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
          ? 'Cập nhật danh mục thành công.'
          : 'Thêm danh mục thành công.'
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
      <h1>Danh mục tạp chí/hội nghị</h1>
      <p>
        Quản lý thông tin tạp chí, hội nghị khoa học và phân loại
        phục vụ hoạt động nghiên cứu.
      </p>

      {message && <p role="status">{message}</p>}
      {error && <p role="alert" style={{ color: '#b42318' }}>{error}</p>}

      <div className="card" style={{ marginTop: 24 }}>
        <h3>Danh sách tạp chí và hội nghị</h3>

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
            placeholder="Tìm theo tên..."
            value={keyword}
            onChange={event => setKeyword(event.target.value)}
            aria-label="Tìm kiếm danh mục"
          />

          <select
            value={filterType}
            onChange={event => setFilterType(event.target.value)}
            aria-label="Lọc loại danh mục"
          >
            <option value="">Tất cả</option>
            <option value="TAP_CHI">Tạp chí</option>
            <option value="HOI_NGHI">Hội nghị</option>
          </select>

          {canEdit && (
            <button type="button" onClick={startCreate}>
              + Thêm danh mục
            </button>
          )}
        </div>

        {loading ? (
          <p>Đang tải danh mục...</p>
        ) : items.length === 0 ? (
          <p>Không tìm thấy tạp chí hoặc hội nghị phù hợp.</p>
        ) : (
          <div style={{ overflowX: 'auto' }}>
            <table style={{ width: '100%' }}>
              <thead>
                <tr>
                  <th>Tên</th>
                  <th>Loại</th>
                  <th>ISSN</th>
                  <th>Chỉ số</th>
                  <th>Phân loại</th>
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
        <div className="card" style={{ marginTop: 20 }}>
          <h3>
            {editingId === null ? 'Thêm danh mục mới' : 'Sửa danh mục'}
          </h3>

          <form onSubmit={saveItem}>
            <div style={{ display: 'grid', gap: 14 }}>
              <label>
                Tên tạp chí/hội nghị *
                <input
                  value={form.name}
                  maxLength={300}
                  required
                  onChange={event => updateField('name', event.target.value)}
                />
              </label>

              <label>
                Loại *
                <select
                  value={form.type}
                  onChange={event =>
                    updateField('type', event.target.value as CatalogType)
                  }
                >
                  <option value="TAP_CHI">Tạp chí</option>
                  <option value="HOI_NGHI">Hội nghị</option>
                </select>
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
                Chỉ số
                <input
                  value={form.index}
                  maxLength={100}
                  placeholder="Ví dụ: Scopus, Web of Science"
                  onChange={event => updateField('index', event.target.value)}
                />
              </label>

              <label>
                Phân loại
                <input
                  value={form.classification}
                  maxLength={100}
                  placeholder="Ví dụ: Q1, Q2"
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
                  {saving ? 'Đang lưu...' : 'Lưu danh mục'}
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
