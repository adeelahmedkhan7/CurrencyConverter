using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.Services.CurrencyConverter.Interface;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace CurrencyConverter.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    
    /// <summary>
    /// This controller manages the currency related operation
    /// </summary>
    public class CurrencyController : ControllerBase
    {
        private readonly ICurrencyService _currencyService;

        public CurrencyController(ICurrencyService currencyService)
        {
            _currencyService = currencyService;
        }

        ///  <summary>
        ///  This endpoint fetches all latest currencies against the provided <paramref name="baseCurrency"/>
        /// </summary>
        /// <param name="baseCurrency">Required stirng parameter for against which the record return</param>
        ///  <returns>Returns exchange response dto containig all of the exchange amount against the base currency.
        /// </returns>
        /// <response code="500">Signifies an issue.</response>
        /// <response code="200">Returns a paginated filter list of donors.</response>
        /// <response code="400">Signifies a validation issue or bad request.</response>
        [HttpGet("Latest")]
        public async Task<IActionResult> GetLatestRates(string baseCurrency)
        {
            var result = await _currencyService.GetLatestRatesAsync(baseCurrency);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }

        ///  <summary>
        ///  This endpoint return the converted amount against the provided <paramref name="request"/>
        /// </summary>
        /// <param name="request">Required query parameter for against which the conversion calcualtion occurr</param>
        ///  <returns>Returns single conversion response dto object.
        /// </returns>
        /// <response code="500">Signifies an issue.</response>
        /// <response code="200">Returns a paginated filter list of donors.</response>
        /// <response code="400">Signifies a validation issue or bad request.</response>
        [HttpGet("Convert")]
        public async Task<IActionResult> ConvertCurrency([FromQuery] ConversionRequestDto request)
        {
            var result = await _currencyService.ConvertCurrencyAsync(request);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result.Data);
        }

        [HttpGet("historical")]
        ///  <summary>
        ///  This endpoint fetches all paginated historical rates against the provided <paramref name="request"/>
        /// </summary>
        /// <param name="request">Required query stirng parameter for against which the historical record return</param>
        ///  <returns>Returns the paginated historical response containig all of the exchange amount against the base currency.
        /// </returns>
        /// <response code="500">Signifies an issue.</response>
        /// <response code="200">Returns a paginated filter list of donors.</response>
        /// <response code="400">Signifies a validation issue or bad request.</response>
        public async Task<IActionResult> GetHistoricalRates([FromQuery] HistoricalRatesRequestDto request)
        {
            var result = await _currencyService.GetHistoricalRatesAsync(request);
            if (!result.Success)
            {
                return BadRequest(result.Message);
            }

            return Ok(result);
        }
    }
}