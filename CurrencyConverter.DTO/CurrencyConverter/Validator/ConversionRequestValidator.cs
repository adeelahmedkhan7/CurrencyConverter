using CurrencyConverter.DTO.CurrencyConverter.Request;
using CurrencyConverter.DTO.Shared;
using FluentValidation;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.DTO.CurrencyConverter.Validator
{
    public class ConversionRequestValidator : AbstractValidator<ConversionRequestDto>
    {
        public ConversionRequestValidator(SettingDto settings)
        {
            var unsupportedCurrencies = settings.ExchangeApiSettings.UnsupportedCurrencies;

            RuleFor(x => x.From).NotEmpty().WithMessage("From currency is required.");
            RuleFor(x => x.To).NotEmpty().WithMessage("To currency is required.")
                .Must(to => !unsupportedCurrencies.Contains(to))
                .WithMessage("Conversion for the specified currency is not supported.");
            RuleFor(x => x.Amount).GreaterThan(0).WithMessage("Amount must be greater than zero.");
        }
    }
}
