using ERP.Core.Application.Features.MasterData;
using ERP.Orme.Application.Features.Accounts;
using ERP.Orme.Application.Features.Customers;
using ERP.Orme.Application.Features.Dashboard;
using ERP.Orme.Application.Features.Floor;
using ERP.Orme.Application.Features.Inspection;
using ERP.Orme.Application.Features.Knitting;
using ERP.Orme.Application.Features.Materials;
using ERP.Orme.Application.Features.Orders;
using ERP.Orme.Application.Features.Packing;
using ERP.Orme.Application.Features.Planning;
using ERP.Orme.Application.Features.Purchasing;
using ERP.Orme.Application.Features.Quotations;
using ERP.Orme.Application.Features.Receipts;
using ERP.Orme.Application.Features.Reports;
using ERP.Orme.Application.Features.Shipping;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Orme.Application;

public static class DependencyInjection
{
    /// <summary>Örme modülünün uygulama servisleri (süreçler tanımlandıkça buraya eklenir).</summary>
    public static IServiceCollection AddOrmeApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IMasterDataService<OrmeCustomerDto, OrmeCustomerRequest>, OrmeCustomerService>();
        services.AddScoped<IOrmeCustomerDetailService, OrmeCustomerDetailService>();
        services.AddScoped<IMasterDataService<OrmeSupplierDto, OrmeSupplierRequest>, OrmeSupplierService>();
        services.AddScoped<IMasterDataService<OrmeYarnDto, OrmeYarnRequest>, OrmeYarnService>();
        services.AddScoped<IMasterDataService<OrmeMachineDto, OrmeMachineRequest>, OrmeMachineService>();
        services.AddScoped<IMasterDataService<OrmeFabricDto, OrmeFabricRequest>, OrmeFabricService>();
        services.AddScoped<IOrmeFabricYarnService, OrmeFabricYarnService>();
        services.AddScoped<IOrmeOrderService, OrmeOrderService>();
        services.AddScoped<IOrmeYarnLotService, OrmeYarnLotService>();
        services.AddScoped<IOrmeKnitOrderService, OrmeKnitOrderService>();
        services.AddScoped<IMasterDataService<OrmeDefectTypeDto, OrmeDefectTypeRequest>, OrmeDefectTypeService>();
        services.AddScoped<IOrmeInspectionService, OrmeInspectionService>();
        services.AddScoped<IOrmePackingService, OrmePackingService>();
        services.AddScoped<IOrmeShippingService, OrmeShippingService>();
        services.AddScoped<IOrmeAccountService, OrmeAccountService>();
        services.AddScoped<IOrmeYarnReturnService, OrmeYarnReturnService>();
        services.AddScoped<IMasterDataService<OrmeOperatorDto, OrmeOperatorRequest>, OrmeOperatorService>();
        services.AddScoped<IMasterDataService<OrmeRfidCardDto, OrmeRfidCardRequest>, OrmeRfidCardService>();
        services.AddScoped<IMasterDataService<OrmeStopReasonDto, OrmeStopReasonRequest>, OrmeStopReasonService>();
        services.AddScoped<IMasterDataService<OrmeTabletDto, OrmeTabletRequest>, OrmeTabletService>();
        services.AddScoped<IOrmeWorkSessionService, OrmeWorkSessionService>();
        services.AddScoped<IMasterDataService<OrmeShiftDto, OrmeShiftRequest>, OrmeShiftService>();
        services.AddScoped<IMasterDataService<OrmeBreakGroupDto, OrmeBreakGroupRequest>, OrmeBreakGroupService>();
        services.AddScoped<IMasterDataService<OrmeBreakScheduleDto, OrmeBreakScheduleRequest>, OrmeBreakScheduleService>();
        services.AddScoped<IOrmeMachineStopService, OrmeMachineStopService>();
        services.AddScoped<IOrmeTabletDeviceService, OrmeTabletDeviceService>();
        services.AddScoped<IOrmeOperatorCardService, OrmeOperatorCardService>();
        services.AddScoped<IOrmeReportService, OrmeReportService>();
        services.AddScoped<IOrmeDashboardService, OrmeDashboardService>();
        services.AddScoped<IOrmePurchasingService, OrmePurchasingService>();
        services.AddScoped<IOrmeYarnPlanService, OrmeYarnPlanService>();
        services.AddScoped<IOrmeCapacityPlanService, OrmeCapacityPlanService>();
        services.AddScoped<IOrmeCostSettingsService, OrmeCostSettingsService>();
        services.AddScoped<IOrmeQuotationService, OrmeQuotationService>();
        return services;
    }
}
