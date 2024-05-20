# Conversion Project

## Overview

The Conversion Service Project is a .NET-based application designed to handle currency conversion requests. It supports caching of conversion responses to enhance performance and reduce the number of requests to the external exchange rate API.

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
    git clone https://github.com/your-username/conversion-service.git
    cd conversion-service
    ```

2. **Restore dependencies**

    ```sh
    dotnet restore
    ```

3. **Build the project**

    ```sh
    dotnet build
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
