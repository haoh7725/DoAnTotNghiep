using ResearchManagement.Application.Common;
using ResearchManagement.Domain.Constants;

namespace ResearchManagement.Application.Products;

public static class ProductReviewPolicy
{
    public static (string Role, string NextStatus) Next(string status, bool departmentHeadIsAuthor, bool deanIsAuthor)
    {
        if (status == ProductStatuses.Submitted && !departmentHeadIsAuthor)
            return (Roles.DepartmentHead, ProductStatuses.DepartmentHeadApproved);
        if ((status == ProductStatuses.Submitted && departmentHeadIsAuthor ||
             status == ProductStatuses.DepartmentHeadApproved) && !deanIsAuthor)
            return (Roles.FacultyDean, ProductStatuses.FacultyDeanApproved);
        if (status is ProductStatuses.Submitted or ProductStatuses.DepartmentHeadApproved or
            ProductStatuses.FacultyDeanApproved or ProductStatuses.ResearchOfficeApproved)
            return (Roles.ResearchOffice, ProductStatuses.Approved);
        throw new BusinessRuleException("Sản phẩm không ở bước xét duyệt.");
    }
}
