using EShop.Application.DTOs;

namespace EShop.Application.Interfaces;

public interface ISalesReportService
{
    Task<SalesReportDto> GenerateAsync(SalesReportFilterDto filter);
}