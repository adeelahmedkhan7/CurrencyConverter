using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.CurrencyConverter.Response;
using CurrencyConverter.DTO.Shared;

namespace CurrencyConverter.Services.CurrencyConverter.Interface
{
    /// <summary>
    /// Expose the currency operations
    /// </summary>
    public interface ICurrencyService
    {
        /// <summary>
        /// Gets the latest rates asynchronous.
        /// </summary>
        /// <param name="baseCurrency">The base currency against which the record returns</param>
        /// <returns>Reutrns the exchange rate api response dto</returns>
        Task<ApiResponseDto<ExchangeRateDto>> GetLatestRatesAsync(string baseCurrency);

        /// <summary>
        /// Converts the currency asynchronous.
        /// </summary>
        /// <param name="request">Conversin request which contain the required parameter agisnt which the processing done.</param>
        /// <returns>Returns the conversion response api response dto</returns>
        Task<ApiResponseDto<ConversionResponseDto>> ConvertCurrencyAsync(ConversionRequestDto request);

        /// <summary>
        /// Gets the paginated historical rates asynchronous 
        /// </summary>
        /// <param name="request">Request object contain all the parameter agisnt which the rates return.</param>
        /// <returns>Return the paginated histrical rates paginated api response dtos </returns>
        Task<PaginatedApiResponseDto<HistoricalRatesResponseDto>> GetHistoricalRatesAsync(HistoricalRatesRequestDto request);
    }
}
