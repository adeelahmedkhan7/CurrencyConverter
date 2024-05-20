using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.CurrencyConverter.Response;
using CurrencyConverter.DTO.Shared;
using CurrencyConverter.Services.CurrencyConverter.Interface;
using CurrencyConverter.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace CurrencyConverter.Test.Controller
{
    public class CurrencyControllerTests
    {
        private Mock<ICurrencyService> _currencyServiceMock;
        private CurrencyController _currencyController;

        [SetUp]
        public void SetUp()
        {
            _currencyServiceMock = new Mock<ICurrencyService>();
            _currencyController = new CurrencyController(_currencyServiceMock.Object);
        }

        [Test]
        public async Task GetLatestRates_ShouldReturnOkResult_WithValidData()
        {
            // Arrange
            var response = new ApiResponseDto<ExchangeRateDto>
            {
                Success = true,
                Data = new ExchangeRateDto
                {
                    Rates = new Dictionary<string, decimal> { { "USD", 1.2m } }
                }
            };

            _currencyServiceMock.Setup(x => x.GetLatestRatesAsync("EUR")).ReturnsAsync(response);

            // Act
            var result = await _currencyController.GetLatestRates("EUR") as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
            Assert.AreEqual(response.Data, result.Value);
        }

        [Test]
        public async Task ConvertCurrency_ShouldReturnBadRequest_ForUnsupportedCurrency()
        {
            // Arrange
            var request = new ConversionRequestDto { From = "EUR", To = "TRY", Amount = 100 };
            var response = new ApiResponseDto<ConversionResponseDto>
            {
                Success = false,
                Message = "Conversion for the specified currency is not supported."
            };

            _currencyServiceMock.Setup(x => x.ConvertCurrencyAsync(request)).ReturnsAsync(response);

            // Act
            var result = await _currencyController.ConvertCurrency(request) as BadRequestObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(400, result.StatusCode);
            Assert.AreEqual(response.Message, result.Value);
        }

        [Test]
        public async Task GetHistoricalRates_ShouldReturnOkResult_WithValidData()
        {
            // Arrange
            var request = new HistoricalRatesRequestDto
            {
                StartDate = new DateTime(2022, 1, 1),
                EndDate = new DateTime(2022, 1, 31),
                BaseCurrency = "EUR"
            };

            var response = new PaginatedApiResponseDto<HistoricalRatesResponseDto>
            {
                Success = true,
                Data = new HistoricalRatesResponseDto
                {
                    Rates = new Dictionary<string, Dictionary<string, decimal>>
                    {
                        {
                            "2022-01-01", new Dictionary<string, decimal> { { "USD", 1.2m } }
                        },
                        {
                            "2022-01-02", new Dictionary<string, decimal> { { "USD", 1.3m } }
                        }
                    }
                }
            };

            _currencyServiceMock.Setup(x => x.GetHistoricalRatesAsync(request)).ReturnsAsync(response);

            // Act
            var result = await _currencyController.GetHistoricalRates(request) as OkObjectResult;

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual(200, result.StatusCode);
        }
    }
}
