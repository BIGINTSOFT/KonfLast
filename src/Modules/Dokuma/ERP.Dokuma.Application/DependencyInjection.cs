using ERP.Core.Application.Features.MasterData;
using ERP.Dokuma.Application.Features.Accounts;
using ERP.Dokuma.Application.Features.Customers;
using ERP.Dokuma.Application.Features.Dashboard;
using ERP.Dokuma.Application.Features.Floor;
using ERP.Dokuma.Application.Features.Inspection;
using ERP.Dokuma.Application.Features.Materials;
using ERP.Dokuma.Application.Features.Orders;
using ERP.Dokuma.Application.Features.Packing;
using ERP.Dokuma.Application.Features.Planning;
using ERP.Dokuma.Application.Features.Quotations;
using ERP.Dokuma.Application.Features.Purchasing;
using ERP.Dokuma.Application.Features.Receipts;
using ERP.Dokuma.Application.Features.Reports;
using ERP.Dokuma.Application.Features.Shipping;
using ERP.Dokuma.Application.Features.Sizing;
using ERP.Dokuma.Application.Features.Stock;
using ERP.Dokuma.Application.Features.Warping;
using ERP.Dokuma.Application.Features.Weaving;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Dokuma.Application;

public static class DependencyInjection
{
    /// <summary>Dokuma modülünün uygulama servisleri (süreçler tanımlandıkça buraya eklenir).</summary>
    public static IServiceCollection AddDokumaApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IMasterDataService<DokumaCustomerDto, DokumaCustomerRequest>, DokumaCustomerService>();
        services.AddScoped<IDokumaCustomerDetailService, DokumaCustomerDetailService>();
        services.AddScoped<IMasterDataService<DokumaSupplierDto, DokumaSupplierRequest>, DokumaSupplierService>();
        services.AddScoped<IMasterDataService<DokumaYarnDto, DokumaYarnRequest>, DokumaYarnService>();
        services.AddScoped<IMasterDataService<DokumaLoomDto, DokumaLoomRequest>, DokumaLoomService>();
        services.AddScoped<IMasterDataService<DokumaArticleDto, DokumaArticleRequest>, DokumaArticleService>();
        services.AddScoped<IDokumaArticleYarnService, DokumaArticleYarnService>();
        services.AddScoped<IDokumaOrderService, DokumaOrderService>();
        services.AddScoped<IDokumaYarnLotService, DokumaYarnLotService>();
        services.AddScoped<IDokumaWarpService, DokumaWarpService>();
        services.AddScoped<IDokumaWeavingService, DokumaWeavingService>();
        services.AddScoped<IMasterDataService<DokumaDefectTypeDto, DokumaDefectTypeRequest>, DokumaDefectTypeService>();
        services.AddScoped<IDokumaInspectionService, DokumaInspectionService>();
        services.AddScoped<IDokumaPackingService, DokumaPackingService>();
        services.AddScoped<IDokumaShippingService, DokumaShippingService>();
        services.AddScoped<IDokumaAccountService, DokumaAccountService>();
        services.AddScoped<IDokumaYarnReturnService, DokumaYarnReturnService>();
        services.AddScoped<IDokumaYarnTakeBackService, DokumaYarnTakeBackService>();
        services.AddScoped<IMasterDataService<DokumaOperatorDto, DokumaOperatorRequest>, DokumaOperatorService>();
        services.AddScoped<IMasterDataService<DokumaRfidCardDto, DokumaRfidCardRequest>, DokumaRfidCardService>();
        services.AddScoped<IMasterDataService<DokumaStopReasonDto, DokumaStopReasonRequest>, DokumaStopReasonService>();
        services.AddScoped<IMasterDataService<DokumaTabletDto, DokumaTabletRequest>, DokumaTabletService>();
        services.AddScoped<IDokumaWorkSessionService, DokumaWorkSessionService>();
        services.AddScoped<IMasterDataService<DokumaShiftDto, DokumaShiftRequest>, DokumaShiftService>();
        services.AddScoped<IMasterDataService<DokumaBreakGroupDto, DokumaBreakGroupRequest>, DokumaBreakGroupService>();
        services.AddScoped<IMasterDataService<DokumaBreakScheduleDto, DokumaBreakScheduleRequest>, DokumaBreakScheduleService>();
        services.AddScoped<IDokumaLoomStopService, DokumaLoomStopService>();
        services.AddScoped<IDokumaTabletDeviceService, DokumaTabletDeviceService>();
        services.AddScoped<IDokumaOperatorCardService, DokumaOperatorCardService>();
        services.AddScoped<IDokumaReportService, DokumaReportService>();
        services.AddScoped<IDokumaDashboardService, DokumaDashboardService>();
        services.AddScoped<IMasterDataService<DokumaMaterialDto, DokumaMaterialRequest>, DokumaMaterialService>();
        services.AddScoped<IDokumaMaterialStockService, DokumaMaterialStockService>();
        services.AddScoped<IDokumaSizingRecipeService, DokumaSizingRecipeService>();
        services.AddScoped<IDokumaPurchasingService, DokumaPurchasingService>();
        services.AddScoped<IDokumaCostSettingsService, DokumaCostSettingsService>();
        services.AddScoped<IDokumaQuotationService, DokumaQuotationService>();
        services.AddScoped<IDokumaCapacityPlanService, DokumaCapacityPlanService>();
        return services;
    }
}
