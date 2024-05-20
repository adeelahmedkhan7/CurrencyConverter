# Currency Conversion Project

## Overview

The Currency Conversion Service Project is a .NET-based application designed to handle currency conversion requests. It supports caching of conversion responses to enhance performance and reduce the number of requests to the external exchange rate API.

## Features

- Converts currency amounts based on the latest exchange rates.
- Caches conversion responses to improve performance.
- Paginated record retrieval.
- Excludes weekends and holidays from date ranges.

## Table of Contents

1. [Requirements](#requirements)
2. [Installation](#installation)
3. [Configuration](#configuration)
4. [Usage](#usage)
5. [Running Tests](#running-tests)
6. [Packages Used](#packages-used)
7. [License](#license)

## Requirements

- .NET SDK 6.0 or later
- Visual Studio 2022 or later / Visual Studio Code
- Internet connection for accessing external exchange rate API

## Installation

1. **Clone the repository**

    ```sh
    git clone https://github.com/your-username/CurrencyConverter.git
    ```

2. **Restore dependencies**

    ```sh
    dotnet restore
    ```

3. **Build the project**

    ```sh
    dotnet build
    ```
4. **Run the project**
    Before run the command select the webapi project.
    ```sh
    dotnet run
    ```

## Configuration

### AppSettings

Configure your `appsettings.json` file with the necessary API keys and settings:

```json
{
  "ExchangeApiSettings": {
    "FrankfurterBaseUrl": "https://api.frankfurter.app/",
    "UnsupportedCurrencies": [ "TRY", "PLN", "THB", "MXN" ]
  },
  // Set the Concurrent request number 
  "ConcurrentThrottleRequest": 100, 
  "IpRateLimiting": {
    "EnableEndpointRateLimiting": true,
    "StackBlockedRequests": true,
    "RealIpHeader": "X-Real-IP",
    "ClientIdHeader": "X-ClientId",
    "HttpStatusCode": 429
  },
  "IpRateLimitPolicies": {
    "General": [
      {
        "Endpoint": "*",
        "Period": "1m",
        "Limit": 100
      }
    ]
  }
}
```

### Dependency Injection

The Conversion Service Project utilizes dependency injection to manage the application's services and components. Dependency injection allows for loose coupling between classes, making the codebase more maintainable and testable.

In the `Startup.cs` or `Program.cs` file, you can find the service registration configuration. Here's how it's typically done:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddControllers();
    services.AddMemoryCache();
    services.AddScoped<IExchangeRateService, ExchangeRateService>();
    services.AddScoped<IConversionService, ConversionService>();
}
In this configuration:

1. AddMemoryCache(): Registers the in-memory cache service provided by ASP.NET Core. This cache is used to store and retrieve conversion responses.
2. AddScoped<IExchangeRateService, ExchangeRateService>(): Registers the ExchangeRateService implementation as a scoped service. This service is responsible for fetching exchange rates from the external             API.AddScoped<IConversionService, ConversionService>(): Registers the ConversionService implementation as a scoped service. This service handles currency conversion requests and caching of conversion             responses.
```

## Usage

### Running the Application
```sh
dotnet run
```
#### API Endpoints
Convert Currency
URL: /api/convert
Method: POST
Request Body:

```json
{
  "from": "USD",
  "to": "EUR",
  "amount": 100
}
```
Response:

```json
{
  "from": "USD",
  "to": "EUR",
  "amount": 100,
  "convertedAmount": 85
}
```

### Running Tests
Unit tests are written using NUnit and Moq. To run the tests, use the following command:

```sh
dotnet test
```
#### Sample Unit Test
A sample unit test for verifying the caching operation might look like this:

```csharp
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
```
### Packages Used
Newtonsoft.Json - For JSON serialization and deserialization.
Moq - For mocking dependencies in unit tests.
NUnit - For unit testing.
Microsoft.Extensions.Caching.Memory - For in-memory caching.
Adding Packages
AspNetCoreRateLimit - For rate limiting
To add the necessary packages, run the following commands:

```sh
dotnet add package Newtonsoft.Json
dotnet add package Moq
dotnet add package NUnit
dotnet add package Microsoft.Extensions.Caching.Memory
dotnet add pacakage AspNetCoreRateLimit
```
### License
This project is licensed under the MIT License - see the LICENSE file for details.
