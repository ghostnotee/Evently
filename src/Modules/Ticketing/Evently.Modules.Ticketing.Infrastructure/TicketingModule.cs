using Evently.Common.Presentation.Endpoints;
using Evently.Modules.Ticketing.Presentation;
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
            // string connectionString = builder.Configuration.GetConnectionString("evently-db")
            //                           ?? throw new InvalidOperationException(
            //                               "Connection string 'evently-db' was not found.");
        }
    }
}
