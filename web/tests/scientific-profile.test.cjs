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
const { ProfileSection } = require(path.resolve(__dirname, '../src/ScientificProfile.tsx'))
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
assert.ok(html.includes('Đề tài đã kê khai'), 'Previously declared projects must be visible')
assert.ok(html.includes('Trách nhiệm tham gia'), 'Project participation information must remain visible')
assert.ok(html.includes('Công bố cũ'), 'Manual publications must remain visible')
assert.ok(html.includes('Chứng nhận đã duyệt'), 'Approved certificates must remain visible')
assert.ok(html.includes('2027'), 'Ongoing projects must retain their expected completion year')
console.log('PASS: scientific profile preserves projects, publications and approved certificates')
