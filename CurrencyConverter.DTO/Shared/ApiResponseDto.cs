namespace CurrencyConverter.DTO.Shared
{
    public class ApiResponseDto<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
    public class PaginatedApiResponseDto<T>: ApiResponseDto<T>
    {
        public int PageNo { get; set; }
        public int PageSize { get; set; }
        public int Total { get; set; }
    }
}