import type { AcademicYear, Profile } from './api'
import { ResearchPlans } from './ResearchPlans'
import { MyResearchTargets } from './MyResearchTargets'
import { ProductWorkspace } from './Products'

export function ResearchWorkspace({ years, yearId, onYearChange, section, onSectionChange, user, productId }: {
  years: AcademicYear[]; yearId: string; onYearChange: (value: string) => void
  section: string; onSectionChange: (value: string) => void; user: Profile; productId: number | null
}) {
  const active = section === 'research' ? 'research-plans' : section
  return <div className="research-workspace">
    <div className="section-top"><div><p className="eyebrow">KHÔNG GIAN CÁ NHÂN</p><h1>Nghiên cứu của tôi</h1></div>
      <label>Năm học<select value={yearId} onChange={e => onYearChange(e.target.value)}>{years.map(y => <option key={y.id} value={y.id}>{y.code}</option>)}</select></label>
    </div>
    <nav className="tabs" aria-label="Nghiên cứu cá nhân">
      {[['research-plans', 'Kế hoạch'], ['my-targets', 'Chỉ tiêu'], ['products', 'Sản phẩm']].map(([key, label]) =>
        <button key={key} className={active === key ? 'active' : ''} onClick={() => onSectionChange(key)}>{label}</button>)}
    </nav>
    {active === 'research-plans' && <ResearchPlans academicYears={years} selectedYearId={yearId} onYearChange={onYearChange} />}
    {active === 'my-targets' && <MyResearchTargets yearId={yearId} onOpenProducts={() => onSectionChange('products')} onOpenProduct={id => onSectionChange(`product:${id}`)} />}
    {active === 'products' && <ProductWorkspace key={yearId} user={user} initialProductId={productId} yearId={yearId} />}
  </div>
}
