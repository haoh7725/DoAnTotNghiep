const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const ts = require('typescript')
const React = require('react')
const { renderToStaticMarkup } = require('react-dom/server')

// Load the existing TSX component for a regression check without adding a test runtime.
for (const extension of ['.ts', '.tsx']) {
  require.extensions[extension] = (module, filename) => {
    const { outputText } = ts.transpileModule(fs.readFileSync(filename, 'utf8'), {
      compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022 },
    })
    module._compile(outputText, filename)
  }
}
require.extensions['.css'] = () => {}
const { ProfileSection, profileDetailsPayload } = require(path.resolve(__dirname, '../src/ScientificProfile.tsx'))
const html = renderToStaticMarkup(React.createElement(ProfileSection, {
  lecturer: { id: 1, fullName: 'Giảng viên', facultyName: 'Khoa', departmentName: 'Bộ môn' },
  profile: { lecturerId: 1, updatedAt: null, details: {
    researchProjects: [{ title: 'Đề tài đã kê khai', startYear: 2024, endYear: 2027, level: 'Trường', responsibility: 'Thành viên' }],
    publications: [{ title: 'Công bố cũ', year: 2024, journal: 'Tạp chí' }],
  } },
  approvedResearch: [{ id: 10, title: 'Chứng nhận đã duyệt', productType: 'Chứng nhận', publishedDate: '2026-11-10' }],
  canEdit: true,
  onSaved() {},
}))
assert.ok(!html.includes('Đề tài đã kê khai'), 'Legacy project declarations must not appear in section IV')
assert.ok(!html.includes('Công bố cũ'), 'Legacy publication declarations must not appear in section IV')
assert.ok(!html.includes('Chứng nhận đã duyệt'), 'Section IV contains only the four requested product groups')
for (const heading of ['IV.1. Đề tài nghiên cứu khoa học', 'IV.2. Bài báo khoa học', 'IV.3. Hội nghị / Hội thảo', 'IV.4. Sách và giáo trình'])
  assert.ok(html.includes(heading), heading)
const legacy = { researchProjects: [{ title: ' Đề tài cũ ', startYear: 2024, endYear: 2027, level: 'Trường', responsibility: 'Thành viên' }],
  publications: [{ title: 'Công bố cũ', year: 2024, journal: 'Tạp chí' }], education: [], employment: [], languages: [], birthPlace: ' Huế ' }
const payload = profileDetailsPayload(legacy)
assert.deepEqual(payload.researchProjects, legacy.researchProjects, 'Saving unrelated profile fields must preserve hidden projects exactly')
assert.deepEqual(payload.publications, legacy.publications, 'Saving unrelated profile fields must preserve hidden publications exactly')
assert.equal(payload.birthPlace, 'Huế', 'Existing personal-field normalization is unchanged')
console.log('PASS: four approved-only groups; hidden declarations preserved when saving other fields')
const recognized = [
  ['DE_TAI', 'Đề tài nghiên cứu khoa học'], ['BAI_BAO', 'Bài báo khoa học'],
  ['HOI_NGHI_HOI_THAO', 'Hội nghị / Hội thảo'], ['SACH', 'Sách và giáo trình'],
]
const products = Array.from({ length: 80 }, (_, i) => ({
  id: i, title: `Sản phẩm kiểm thử số ${i} kết thúc`, productTypeCode: recognized[i % 4][0],
  productType: recognized[i % 4][1], publishedDate: '2026-11-16', publicationInfo: 'Nơi công bố', authorRole: 'TAC_GIA_CHINH',
}))
const renderProducts = approvedResearch => renderToStaticMarkup(React.createElement(ProfileSection, {
  lecturer: { id: 1, fullName: 'GV', facultyName: 'Khoa', departmentName: 'Bộ môn' },
  profile: { lecturerId: 1, details: {} }, approvedResearch, canEdit: false, onSaved() {},
}))
const populated = renderProducts(products)
for (const p of products) assert.equal(populated.split(p.title).length - 1, 1, 'Approved product must appear once')
assert.equal((populated.match(/class="research-group"/g) || []).length, 4)
assert.ok(populated.includes('Tác giả chính'))
assert.ok(populated.includes('Nơi công bố'))
assert.ok(!populated.includes('class="panel"'))
const empty = renderProducts([])
assert.equal((empty.match(/Chưa có sản phẩm được công nhận\./g) || []).length, 4)
console.log('PASS: four sequential groups, 80 products without duplicate auto rows, compact empty states')
const duplicate = renderToStaticMarkup(React.createElement(ProfileSection, {
  lecturer: { id: 1, fullName: 'GV' },
  profile: { lecturerId: 1, details: { publications: [{ title: products[0].title, year: 2026, journal: 'Nơi công bố' }] } },
  approvedResearch: [products[0], products[0]], canEdit: false, onSaved() {},
}))
assert.equal(duplicate.split(products[0].title).length - 1, 1, 'Overlapping years and manual duplicate must display once')
