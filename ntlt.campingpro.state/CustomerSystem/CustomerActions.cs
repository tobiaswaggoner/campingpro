using System;
using System.Linq;
using ntlt.campingpro.state.Extensions;

namespace ntlt.campingpro.state.CustomerSystem
{
    public sealed record AddCustomerCommand(Guid Id, string Name);

    public sealed record ModifyCustomerCommand(Guid Id, Option<string> NewName);

    public static class CustomerActions
    {
        public static void Execute(this CustomerState state, AddCustomerCommand cmd)
        {
            if (state.Customers.Any(c => c.Id == cmd.Id)) return;
            state.Store.StoreEvent(new CustomerAddedEvent(cmd.Id, cmd.Name));
        }

        public static void Execute(this CustomerState state, ModifyCustomerCommand cmd)
        {
            if (state.Customers.All(c => c.Id != cmd.Id)) return;
            state.Store.StoreEvent(new CustomerModifiedEvent(cmd.Id, cmd.NewName));
        }
    }
}