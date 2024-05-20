using CurrencyConverter.DTO.CurrencyConverter.Request;
using FluentValidation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CurrencyConverter.DTO.CurrencyConverter.Validator
{
    public class HistoricalRatesRequestValidator : AbstractValidator<HistoricalRatesRequestDto>
    {
        public HistoricalRatesRequestValidator()
        {
            RuleFor(x => x.BaseCurrency)
                .NotEmpty().WithMessage("Base currency is required.")
                .Length(3).WithMessage("Base currency should be a 3-letter code.");

            RuleFor(x => x.StartDate)
                .NotEmpty().WithMessage("Start date is required.")
                .Must(BeAValidDate).WithMessage("Start date must be a valid date.");

            RuleFor(x => x.EndDate)
                .NotEmpty().WithMessage("End date is required.")
                .Must(BeAValidDate).WithMessage("End date must be a valid date.");

            RuleFor(x => x.Page)
                .GreaterThan(0).WithMessage("Page number must be greater than zero.");

            RuleFor(x => x.PageSize)
                .GreaterThan(0).WithMessage("Page size must be greater than zero.");
        }

        private bool BeAValidDate(DateTime date)
        {
            return date!=DateTime.MinValue;
        }
    }
}
