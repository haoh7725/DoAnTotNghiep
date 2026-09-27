using ResearchManagement.Domain.Common;

namespace ResearchManagement.Domain.Entities;

public sealed class Faculty : BaseEntity
{
    private Faculty() { }

    public Faculty(string code, string name)
    {
        Code = code;
        Name = name;
    }

    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
}
