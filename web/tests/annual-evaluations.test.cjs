const assert = require('node:assert/strict')
const fs = require('node:fs')
const path = require('node:path')
const ts = require('typescript')
const React = require('react')
const { renderToStaticMarkup } = require('react-dom/server')

for (const extension of ['.ts', '.tsx']) {
  require.extensions[extension] = (module, filename) => {
    const { outputText } = ts.transpileModule(fs.readFileSync(filename, 'utf8'), {
      compilerOptions: { module: ts.ModuleKind.CommonJS, jsx: ts.JsxEmit.ReactJSX, target: ts.ScriptTarget.ES2022 },
    })
    module._compile(outputText, filename)
  }
}
const { AnnualEvaluations } = require(path.resolve(__dirname, '../src/AnnualEvaluations.tsx'))
const row = {
  lecturerId: 1, lecturerName: 'Giảng viên thử nghiệm', lecturerCode: 'GV001',
  totalHours: 0, assignedTargets: 0, completedTargets: 0,
  classification: 'Chưa hoàn thành', products: [], targets: [], issues: [],
}
const originalUseState = React.useState
function render({ rows = [], detail = null, nominations = null }) {
  const states = ['2026', rows, detail, '', '', false, '', '', nominations]
  let index = 0
  React.useState = () => [states[index++], () => {}]
  try {
    return renderToStaticMarkup(React.createElement(AnnualEvaluations, {
      user: { roles: [{ role: 'TRUONG_KHOA', facultyId: 1 }] },
      lookups: { academicYears: [], faculties: [] },
    }))
  } finally { React.useState = originalUseState }
}

assert.ok(render({ rows: [row] }).includes('Chưa có dữ liệu'), 'Old API rows without requiredHours must still render')
assert.ok(render({ detail: row }).includes('Giảng viên thử nghiệm'), 'Detail must render without requiredHours')
assert.ok(render({ rows: [{ ...row, requiredHours: 586 }] }).includes('586'), 'Configured hours must be preserved')
assert.ok(render({ nominations: {
  population: 0, eligible: 0, requiredHours: null,
  advancedLaborQuota: 0, emulationFighterQuota: 0, policy: '', candidates: [],
} }).includes('Chưa có dữ liệu'), 'An empty faculty with null requiredHours must still render')
console.log('PASS: annual evaluations render missing, null and configured required hours')
const nominationHtml = render({ nominations: {
  population: 10, eligible: 10, requiredHours: 586, advancedLaborQuota: 2, topThirtyQuota: 3, emulationFighterQuota: 1,
  policy: '', candidates: [
    { lecturerId: 1, lecturerName: 'GV đầu', lecturerCode: 'GV01', totalHours: 1000, rank: 1, proposedTitle: 'Chiến sĩ thi đua', tiedAtBoundary: true },
    { lecturerId: 2, lecturerName: 'GV tiếp', lecturerCode: 'GV02', totalHours: 900, rank: 2, proposedTitle: 'Lao động tiên tiến', tiedAtBoundary: false },
  ],
} })
assert.ok(nominationHtml.includes('top 10% tổng giờ là Chiến sĩ thi đua'))
assert.ok(nominationHtml.includes('đến hết top 30% là Lao động tiên tiến'))
assert.ok(nominationHtml.includes('2 Lao động tiên tiến và 1 Chiến sĩ thi đua'))
assert.ok(nominationHtml.includes('Đồng giờ tại ranh giới'))
console.log('PASS: new award policy, exclusive quotas and boundary warning render')
