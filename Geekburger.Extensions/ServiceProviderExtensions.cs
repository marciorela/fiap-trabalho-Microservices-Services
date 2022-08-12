using Microsoft.Extensions.DependencyInjection;

namespace Geekburger.Extensions
{
    public static class ServiceProviderExtensions
    {
        public static async Task ExecuteAsync<T>(this IServiceProvider svc, Func<T, Task> func)
        {
            using var scope = svc.CreateScope();

            var service = scope.ServiceProvider.GetService<T>();
            if (service is not null)
            {
                await func(service);
            }
        }

        public static void Execute<T>(this IServiceProvider svc, Action<T> func)
        {
            using var scope = svc.CreateScope();

            var service = scope.ServiceProvider.GetService<T>();
            if (service is not null)
            {
                func(service);
            }
        }
    }
}