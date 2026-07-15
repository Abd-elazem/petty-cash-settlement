using System.Runtime.CompilerServices;

// Lets Infrastructure.Tests assert against internal DbSets directly for persistence
// verification (e.g. confirming an audit entry landed in the table) without needing a
// public read API that nothing in Application actually asks for. Test-only visibility,
// not a general relaxation of the "DbContext never leaves Infrastructure" rule — the test
// project is still part of the same architectural boundary as Infrastructure, unlike
// Application or Domain, which have no reference to this assembly at all.
[assembly: InternalsVisibleTo("PettyCash.Infrastructure.Tests")]
