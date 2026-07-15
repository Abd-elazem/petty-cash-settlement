namespace PettyCash.Application.Common;

/// <summary>
/// Marker for a command: an operation that changes state. Deliberately not using
/// MediatR or any mediator library — with ~10 use cases, a hand-rolled interface pair
/// is simpler to read and debug than pulling in a dispatch pipeline we don't need yet.
/// Revisit only if the number of use cases grows enough that a mediator earns its keep.
/// </summary>
public interface ICommand<TResult>
{
}

public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    Task<TResult> HandleAsync(TCommand command, CancellationToken cancellationToken = default);
}

/// <summary>Marker for a query: a read that does not change state.</summary>
public interface IQuery<TResult>
{
}

public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    Task<TResult> HandleAsync(TQuery query, CancellationToken cancellationToken = default);
}
