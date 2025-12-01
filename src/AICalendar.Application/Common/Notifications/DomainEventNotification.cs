using AICalendar.Domain.Common;
using MediatR;

namespace AICalendar.Application.Common.Notifications;

/// <summary>
/// A generic wrapper that adapts a pure Domain Event into a MediatR Notification.
/// This allows us to keep the Domain layer free of MediatR dependencies while still
/// using MediatR's dispatching capabilities in the Application layer.
/// </summary>
/// <typeparam name="TDomainEvent">The type of the domain event.</typeparam>
public record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
