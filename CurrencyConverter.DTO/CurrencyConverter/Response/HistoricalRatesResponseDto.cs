using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace CurrencyConverter.DTO.CurrencyConverter.Response
{
    public class HistoricalRatesResponseDto
    {
       
        [JsonProperty("rates")]
        public Dictionary<string, Dictionary<string, decimal>> Rates { get; set; }
    }
}
