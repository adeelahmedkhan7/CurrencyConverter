using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.CurrencyConverter.Response;
using CurrencyConverter.DTO.Shared;
using CurrencyConverter.Services.CurrencyConverter;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Net;
using System.Text;
using Assert = Microsoft.VisualStudio.TestTools.UnitTesting.Assert;

namespace CurrencyConverter.Test.Service
{
    [TestFixture]
    public class CurrencyServiceTests
    {
        private Mock<IHttpClientFactory> _httpClientFactoryMock;
        private Mock<IMemoryCache> _cacheMock;
        private SettingDto _settings;
        private Mock<CurrencyService> _currencyService;

        [SetUp]
        public void Setup()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _cacheMock = new Mock<IMemoryCache>();
            _settings = new SettingDto
            {
                ExchangeApiSettings = new ExchangeApiSettingDto
                {
                    UnsupportedCurrencies = new[] { "TRY", "PLN", "THB", "MXN" }
                }
            };
            _currencyService = new Mock<CurrencyService>(_httpClientFactoryMock.Object, _settings, _cacheMock.Object)
            {
                CallBase = true
            };
        }

        private void SetupMemoryCache(string key, object value)
        {
            var mockCacheEntry = new Mock<ICacheEntry>();
            _cacheMock.Setup(m => m.CreateEntry(It.IsAny<object>())).Returns(mockCacheEntry.Object);
            _cacheMock.Setup(m => m.TryGetValue(It.IsAny<string>(), out value)).Returns(true);
        }
        
        private HttpClient CreateMockHttpClient(HttpResponseMessage httpResponse)
        {
            var httpClientHandlerMock = new Mock<HttpMessageHandler>();
            httpClientHandlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(httpResponse);

            return new HttpClient(httpClientHandlerMock.Object) { BaseAddress = new Uri("https://api.frankfurter.app/") };
        }

        [Test]
        public async Task GetLatestRatesAsync_CachedResponse_ReturnsCachedData()
        {
            // Arrange
            var baseCurrency = "USD";
            var cacheKey = $"LatestRates_{baseCurrency.ToUpper()}";
            object cachedResponse = new ExchangeRateDto
            {
                Base = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
            };

            SetupMemoryCache(cacheKey, cachedResponse);

            // Act
            var result = await _currencyService.Object.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(cachedResponse, result.Data);
        }

        [Test]
        public async Task GetLatestRatesAsync_NoCachedResponse_FetchesFromApi()
        {
            // Arrange
            var baseCurrency = "USD";
            var cacheKey = $"LatestRates_{baseCurrency.ToUpper()}";
            object cachedResponse = null;
            var exchangeRateDto = new ExchangeRateDto
            {
                Base = baseCurrency,
                Rates = new Dictionary<string, decimal> { { "EUR", 0.85m } }
            };
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(exchangeRateDto))
            };

            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cachedResponse)).Returns(false);
            // Mocking the SetCacheKey method
            _currencyService.Setup(x => x.SetCacheKey(It.IsAny<string>(), It.IsAny<object>()));

            var httpClient = CreateMockHttpClient(httpResponse);
            _httpClientFactoryMock.Setup(x => x.CreateClient("Frankfurter")).Returns(httpClient);

            // Act
            var result = await _currencyService.Object.GetLatestRatesAsync(baseCurrency);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(exchangeRateDto.Rates.FirstOrDefault().Value, result.Data.Rates.FirstOrDefault().Value);
        }

        [Test]
        public async Task ConvertCurrencyAsync_UnsupportedCurrency_ReturnsError()
        {
            // Arrange
            var request = new ConversionRequestDto
            {
                From = "USD",
                To = "TRY",
                Amount = 100
            };

            // Act
            var result = await _currencyService.Object.ConvertCurrencyAsync(request);

            // Assert
            Assert.IsFalse(result.Success);
            Assert.AreEqual("Conversion for the specified currency is not supported.", result.Message);
        }

        [Test]
        public async Task ConvertCurrencyAsync_SupportedCurrency_ReturnsConvertedAmount()
        {
            // Arrange
            var request = new ConversionRequestDto
            {
                From = "USD",
                To = "EUR",
                Amount = 100
            };
            var cacheKey = $"Conversion_{request.From.ToUpper()}_{request.To.ToUpper()}";
            object cachedResponse = null;
            var exchangeRateDto = new ExchangeRateDto
            {
                Base = request.From,
                Rates = new Dictionary<string, decimal> { { request.To, 0.85m } }
            };
            var httpResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(JsonConvert.SerializeObject(exchangeRateDto))
            };

            _cacheMock.Setup(x => x.TryGetValue(cacheKey, out cachedResponse)).Returns(false);
            // Mocking the SetCacheKey method
            _currencyService.Setup(x => x.SetCacheKey(It.IsAny<string>(), It.IsAny<object>()));
            var httpClient = CreateMockHttpClient(httpResponse);
            _httpClientFactoryMock.Setup(x => x.CreateClient("Frankfurter")).Returns(httpClient);

            // Act
            var result = await _currencyService.Object.ConvertCurrencyAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(request.Amount * 0.85m, result.Data.ConvertedAmount);
        }

        [Test]
        public async Task GetHistoricalRatesAsync_CachedResponse_ReturnsCachedData()
        {
            // Arrange
            var request = new HistoricalRatesRequestDto
            {
                StartDate = DateTime.UtcNow.AddDays(-10),
                EndDate = DateTime.UtcNow,
                BaseCurrency = "USD",
                Page = 1,
                PageSize = 10
            };
            // Mocking the SetCacheKey method
            _currencyService.Setup(x => x.SetCacheKey(It.IsAny<string>(), It.IsAny<object>()));

            var cacheKey = $"HistoricalRates_{request.StartDate}_{request.EndDate}_{request.BaseCurrency}";
            var cachedResponse = new HistoricalRatesResponseDto
            {
                Rates = new Dictionary<string, Dictionary<string, decimal>>
                {
                    { "2023-05-01", new Dictionary<string, decimal> { { "EUR", 0.85m } } }
                }
            };

            SetupMemoryCache(cacheKey, cachedResponse);

            // Act
            var result = await _currencyService.Object.GetHistoricalRatesAsync(request);

            // Assert
            Assert.IsTrue(result.Success);
            Assert.AreEqual(cachedResponse.Rates.FirstOrDefault().Key, result.Data.Rates.FirstOrDefault().Key);
        }
    }
}
