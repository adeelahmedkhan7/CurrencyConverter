using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.CurrencyConverter.Response;
using CurrencyConverter.DTO.Shared;
using CurrencyConverter.Services.CurrencyConverter.Interface;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Polly;
using System.Net.Http;

namespace CurrencyConverter.Services.CurrencyConverter
{
    /// <summary>
    /// Currency service class to manages the operation of the currency
    /// </summary>
    /// <seealso cref="CurrencyConverter.Services.CurrencyConverter.Interface.ICurrencyService" />
    public class CurrencyService : ICurrencyService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SettingDto _settings;
        private readonly IMemoryCache _cache;
        public CurrencyService(IHttpClientFactory httpClientFactory, SettingDto settings, IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _settings = settings;
            _cache = cache;
        }

        /// <summary>
        /// Executes the with retry asynchronous.
        /// </summary>
        /// <param name="action">The action which will be trigger on fialure 3 times.</param>
        /// <returns>Returns the http response of the third party api</returns>
        private async Task<HttpResponseMessage> ExecuteWithRetryAsync(Func<Task<HttpResponseMessage>> action)
        {
            var retryPolicy = Policy
                .Handle<HttpRequestException>()
                .OrResult<HttpResponseMessage>(response => !response.IsSuccessStatusCode)
                .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(1));

            return await retryPolicy.ExecuteAsync(action);
        }

        /// <summary>
        /// Gets the latest rates asynchronous.
        /// </summary>
        /// <param name="baseCurrency">The base currency against which the record returns</param>
        /// <returns>Reutrns the exchange rate api response dto</returns>
        public async Task<ApiResponseDto<ExchangeRateDto>> GetLatestRatesAsync(string baseCurrency)
        {
            var cacheKey = $"LatestRates_{baseCurrency.ToUpper()}";
            ApiResponseDto<ExchangeRateDto> result = new ApiResponseDto<ExchangeRateDto>() { Success = true };

            // Check if the response is cached
            if (!_cache.TryGetValue(cacheKey, out ExchangeRateDto cachedResponse))
            {
                // Get the currency api response
                var response = await ExecuteWithRetryAsync(async () =>
                {
                    var httpClient = _httpClientFactory.CreateClient("Frankfurter");
                    return await httpClient.GetAsync($"latest?base={baseCurrency}");
                });

                // Check for the repsonse of the api
                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponseDto<ExchangeRateDto>
                    {
                        Success = false,
                        Message = "Failed to retrieve latest rates."
                    };
                }

                // Read the content
                var content = await response.Content.ReadAsStringAsync();

                //Deseralize the response
                var exchangeRateDto = JsonConvert.DeserializeObject<ExchangeRateDto>(content);

                result.Data = exchangeRateDto;

                // Cache the response with a specified expiration time
                SetCacheKey(cacheKey, exchangeRateDto); // Cache for 1 minutes

            }
            else
            {
                result.Data = cachedResponse;
            }

            // Return cached response
            return result;
        }

        /// <summary>
        /// Sets the cache key.
        /// </summary>
        /// <param name="cacheKey">The cache key.It is of string tyype</param>
        /// <param name="exchangeRateDto">The exchange rate dto of any type.</param>
        public virtual void SetCacheKey(string cacheKey, object? exchangeRateDto)
        {
            _cache.Set(cacheKey, exchangeRateDto, TimeSpan.FromMinutes(1));
        }

        /// <summary>
        /// Converts the currency asynchronous.
        /// </summary>
        /// <param name="request">Conversin request which contain the required parameter agisnt which the processing done.</param>
        /// <returns>Returns the conversion response api response dto</returns>
        public async Task<ApiResponseDto<ConversionResponseDto>> ConvertCurrencyAsync(ConversionRequestDto request)
        {
            var cacheKey = $"Conversion_{request.From.ToUpper()}_{request.To.ToUpper()}";
            ApiResponseDto<ConversionResponseDto> result=new ApiResponseDto<ConversionResponseDto>() { Success=true};
            // Check if the response is cached
            if (!_cache.TryGetValue(cacheKey, out ConversionResponseDto cachedResponse))
            {
                // Check for the unsupported currencies
                if (Array.Exists(_settings.ExchangeApiSettings.UnsupportedCurrencies, currency => currency == request.To))
                {
                    return new ApiResponseDto<ConversionResponseDto>
                    {
                        Success = false,
                        Message = "Conversion for the specified currency is not supported."
                    };
                }
                // Get currency api response
                var response = await ExecuteWithRetryAsync(async () =>
                {
                    var httpClient = _httpClientFactory.CreateClient("Frankfurter");
                    return await httpClient.GetAsync($"latest?base={request.From}");
                });

                // Check the response of the third party api
                if (!response.IsSuccessStatusCode)
                {
                    return new ApiResponseDto<ConversionResponseDto>
                    {
                        Success = false,
                        Message = "Failed to retrieve exchange rates."
                    };
                }
                // Read the content
                var content = await response.Content.ReadAsStringAsync();

                // Deseralize the content
                var exchangeRateDto = JsonConvert.DeserializeObject<ExchangeRateDto>(content);

                if (!exchangeRateDto.Rates.TryGetValue(request.To, out var rate))
                {
                    return new ApiResponseDto<ConversionResponseDto>
                    {
                        Success = false,
                        Message = "Unsupported currency."
                    };
                }

                var convertedAmount = request.Amount * rate;

                var conversionResponse = new ConversionResponseDto
                {
                    From = request.From,
                    To = request.To,
                    Amount = request.Amount,
                    ConvertedAmount = convertedAmount
                };
                // Cache the response
                SetCacheKey(cacheKey, conversionResponse);

                result.Data = conversionResponse;
              
            }
            else
            {
                result.Data = cachedResponse;
            }
            // Return cached response
            return result;
        }

        /// <summary>
        /// Gets the paginated historical rates asynchronous 
        /// </summary>
        /// <param name="request">Request object contain all the parameter agisnt which the rates return.</param>
        /// <returns>Return the paginated histrical rates paginated api response dtos </returns>
        public async Task<PaginatedApiResponseDto<HistoricalRatesResponseDto>> GetHistoricalRatesAsync(HistoricalRatesRequestDto request)
        {
            var cacheKey = $"HistoricalRates_{request.StartDate}_{request.EndDate}_{request.BaseCurrency}";
            PaginatedApiResponseDto<HistoricalRatesResponseDto> result = new PaginatedApiResponseDto<HistoricalRatesResponseDto>() { Success = true,PageNo=request.Page,PageSize=request.PageSize };

            // Check if the response is cached
            if (!_cache.TryGetValue(cacheKey, out HistoricalRatesResponseDto cachedResponse))
            {
                var response = await ExecuteWithRetryAsync(async () =>
                {
                    var httpClient = _httpClientFactory.CreateClient("Frankfurter");
                    return await httpClient.GetAsync($"{request.StartDate.ToString("yyyy-MM-dd")}..{request.EndDate.ToString("yyyy-MM-dd")}?base={request.BaseCurrency}");
                });

                // Check the response of api
                if (!response.IsSuccessStatusCode)
                {
                    return new PaginatedApiResponseDto<HistoricalRatesResponseDto>
                    {
                        Success = false,
                        Message = "Failed to retrieve historical rates."
                    };
                }

                var content = await response.Content.ReadAsStringAsync();
                var rates = JsonConvert.DeserializeObject<HistoricalRatesResponseDto> (content);

                // Calculate skip and take based on pagination parameters
                int skip = (request.Page - 1) * request.PageSize;
                int take = request.PageSize;

                // Apply pagination logic while deserializing rates
                var paginatedRates = rates.Rates.Skip(skip).Take(take).ToDictionary(x => x.Key, x => x.Value);

                var historicalRatesResponse = new HistoricalRatesResponseDto
                {
                    Rates = paginatedRates
                };

                // Cache the response
                SetCacheKey(cacheKey, rates);
                result.Data = historicalRatesResponse;
                result.Total = rates.Rates.Count;
            }
            else
            {
                // Calculate skip and take based on pagination parameters
                int skip = (request.Page - 1) * request.PageSize;
                int take = request.PageSize;

                // Apply pagination logic while deserializing rates
                var paginatedRates = cachedResponse.Rates.Skip(skip).Take(take).ToDictionary(x => x.Key, x => x.Value);
                result.Total = cachedResponse.Rates.Count();
                result.Data = new HistoricalRatesResponseDto { Rates = paginatedRates };
            }
            // Return cached response
            
            return result;
        }

        #region HelperMethod        
        /// <summary>
        /// Generates the dates excluding weekends and off days.
        /// </summary>
        /// <param name="startDate">The start date.</param>
        /// <param name="endDate">The end date.</param>
        /// <param name="pageSize">Size of the page.</param>
        /// <param name="page">The page no.</param>
        /// <returns>Return the tuple of start and end date</returns>
        private (DateTime startDate,DateTime endDate) GenerateDatesExcludingWeekendsAndOffDays(DateTime startDate, DateTime endDate,int pageSize,int page)
        {
            List<DateTime> result = new List<DateTime>();

            DateTime currentDate = startDate;

            while (currentDate <= endDate)
            {
                if (!IsWeekend(currentDate))
                {
                    result.Add(currentDate);
                }

                currentDate = currentDate.AddDays(1);
            }
            int skip = (page - 1) * pageSize;
            int take = pageSize;

            result= result.Skip(skip).Take(take).ToList();
            return (result.FirstOrDefault(), result.LastOrDefault());
        }
        /// <summary>
        /// Determines whether the specified date is weekend.
        /// </summary>
        /// <param name="date">The date.</param>
        /// <returns>
        ///   <c>true</c> if the specified date is weekend; otherwise, <c>false</c>.
        /// </returns>
        private bool IsWeekend(DateTime date)
        {
            return date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday;
        }
        #endregion
    }
}