using PettyCash.Application.Abstractions;

namespace PettyCash.Application.Tests.Fakes;

public sealed class FakeCurrentUserContext : ICurrentUserContext
{
    public FakeCurrentUserContext(CurrentUser current)
    {
        Current = current;
    }

    public CurrentUser Current { get; }

    public static FakeCurrentUserContext ForSpender(string userId = "spender-1", string email = "spender1@canex.com") =>
        new(new CurrentUser(userId, email, new[] { UserRole.Spender }));

    public static FakeCurrentUserContext ForApprover(string email = "manager@canex.com", string userId = "approver-1") =>
        new(new CurrentUser(userId, email, new[] { UserRole.Approver }));

    public static FakeCurrentUserContext ForSystem() =>
        new(new CurrentUser("system", "flow@canex.com", new[] { UserRole.System }));

    public static FakeCurrentUserContext ForApAccountant() =>
        new(new CurrentUser("ap-1", "ap@canex.com", new[] { UserRole.ApAccountant }));
}
