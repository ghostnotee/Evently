using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Ticketing.Application.Carts;
using Evently.Modules.Ticketing.Presentation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Evently.Modules.Ticketing.Infrastructure;

public static class TicketingModule
{
    extension(IHostApplicationBuilder builder)
    {
        public void AddTicketingModule()
        {
            builder.AddInfrastructure();
            builder.Services.AddEndpoints(AssemblyReference.Assembly);
        }

        private void AddInfrastructure()
        {
            builder.Services.AddSingleton<CartService>();
            
            // string connectionString = builder.Configuration.GetConnectionString("evently-db")
            //                           ?? throw new InvalidOperationException(
            //                               "Connection string 'evently-db' was not found.");
        }
    }
}
