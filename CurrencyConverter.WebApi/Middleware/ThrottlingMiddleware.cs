using CurrencyConverter.DTO.Shared;

namespace CurrencyConverter.WebApi.Middleware
{
    /// <summary>
    /// Manage the number of concurrent request 
    /// </summary>
    public class ThrottlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SemaphoreSlim _semaphore;

        public ThrottlingMiddleware(RequestDelegate next, SettingDto setting)
        {
            _next = next;
            _semaphore = new SemaphoreSlim(setting.ConcurrentThrottleRequest);
        }

        public async Task Invoke(HttpContext context)
        {
            await _semaphore.WaitAsync();
            try
            {
                await _next(context);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
