using System;
using System.Collections.Immutable;
using System.Linq;
using ntlt.campingpro.eventstore;
using ntlt.campingpro.state.Extensions;

namespace ntlt.campingpro.state.CustomerSystem
{
    public sealed record Customer(Guid Id, string Name);

    public sealed record CustomerAddedEvent(Guid Id, string Name) : DomainEvent(Guid.NewGuid());

    public sealed record CustomerModifiedEvent(Guid Id, Option<string> NewName) : DomainEvent(Guid.NewGuid());
    public sealed record CustomerDeletedEvent(Guid Id) : DomainEvent(Guid.NewGuid());

    public sealed class CustomerState
    {
        public event EventHandler OnStateChanged;
        
        public CustomerState(IEventStore store)
        {
            Store = store;
            Store.DomainEvents.Subscribe(HandleDomainEvent);
            Reset();
        }

        public IEventStore Store { get; }
        public ImmutableList<Customer> Customers { get; private set; }

        private void HandleDomainEvent(DomainEvent evt)
        {
            var _ = evt switch
            {
                CustomerAddedEvent castedEvt => ApplyEventToState(castedEvt),
                CustomerModifiedEvent castedEvt => ApplyEventToState(castedEvt),
                CustomerDeletedEvent castedEvt => ApplyEventToState(castedEvt),
                RebuildStateEvent castedEvt => ApplyEventToState(castedEvt),
                _ => false,
            };
            
            RaiseOnStateChanged();
        }

        private bool ApplyEventToState(CustomerAddedEvent evt)
        {
            Customers = Customers.Add(new Customer(evt.Id, evt.Name));
            return true;
        }

        private bool ApplyEventToState(CustomerModifiedEvent evt)
        {
            var customer = Customers.FirstOrDefault(c => evt.Id == c.Id);
            if (customer == null) return false;

            Customers = Customers.Replace(customer,
                customer with { Name = evt.NewName.ReplaceIfSome(customer.Name) });
            return true;
        }

        private bool ApplyEventToState(CustomerDeletedEvent evt)
        {
            Customers = Customers.Where(c => evt.Id != c.Id)
                .ToImmutableList();
            return true;
        }

        private bool ApplyEventToState(RebuildStateEvent evt)
        {
            Reset();
            return true;
        }

        private void Reset()
        {
            Customers = ImmutableList<Customer>.Empty;
        }

        private void RaiseOnStateChanged()
        {
            OnStateChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}