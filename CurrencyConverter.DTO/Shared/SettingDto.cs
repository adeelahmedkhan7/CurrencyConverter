using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.DTO.Shared
{
    public class SettingDto
    {
        public int ConcurrentThrottleRequest { get; set; }
        public ExchangeApiSettingDto ExchangeApiSettings { get; set; }
    }
    public class ExchangeApiSettingDto
    {
        public string FrankfurterBaseUrl { get; set; }
        public string[] UnsupportedCurrencies { get; set; }
    }
}
