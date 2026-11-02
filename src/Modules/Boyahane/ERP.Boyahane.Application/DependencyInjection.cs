using ERP.Boyahane.Application.Features.Warehousing;
using ERP.Boyahane.Application.Features.Archive;
using ERP.Boyahane.Application.Features.Batches;
using ERP.Boyahane.Application.Features.Customers;
using ERP.Boyahane.Application.Features.Floor;
using ERP.Boyahane.Application.Features.Insights;
using ERP.Boyahane.Application.Features.Inspection;
using ERP.Boyahane.Application.Features.Lab;
using ERP.Boyahane.Application.Features.Materials;
using ERP.Boyahane.Application.Features.Operations;
using ERP.Boyahane.Application.Features.Orders;
using ERP.Boyahane.Application.Features.Packing;
using ERP.Boyahane.Application.Features.Planning;
using ERP.Boyahane.Application.Features.Quotations;
using ERP.Boyahane.Application.Features.Accounts;
using ERP.Boyahane.Application.Features.Purchasing;
using ERP.Boyahane.Application.Features.Returns;
using ERP.Boyahane.Application.Features.Receipts;
using ERP.Boyahane.Application.Features.Shipping;
using ERP.Boyahane.Application.Features.Stock;
using ERP.Core.Application.Features.MasterData;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Boyahane.Application;

public static class DependencyInjection
{
    /// <summary>Boyahane modülünün uygulama servisleri (süreçler tanımlandıkça buraya eklenir).</summary>
    public static IServiceCollection AddBoyahaneApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddScoped<IMasterDataService<BoyahaneCustomerDto, BoyahaneCustomerRequest>, BoyahaneCustomerService>();
        services.AddScoped<IBoyahaneCustomerDetailService, BoyahaneCustomerDetailService>();
        services.AddScoped<IMasterDataService<BoyahaneFabricDto, BoyahaneFabricRequest>, BoyahaneFabricService>();
        services.AddScoped<IMasterDataService<BoyahaneYarnDto, BoyahaneYarnRequest>, BoyahaneYarnService>();
        services.AddScoped<IBoyahaneFabricReceiptService, BoyahaneFabricReceiptService>();
        services.AddScoped<ERP.Core.Application.Abstractions.IFabricTransferReceiver, BoyahaneFabricTransferReceiver>();
        services.AddScoped<IBoyahaneYarnLotService, BoyahaneYarnLotService>();
        services.AddScoped<IMasterDataService<BoyahaneDefectTypeDto, BoyahaneDefectTypeRequest>, BoyahaneDefectTypeService>();
        services.AddScoped<IBoyahaneInspectionService, BoyahaneInspectionService>();
        services.AddScoped<IBoyahaneOrderService, BoyahaneOrderService>();
        services.AddScoped<IBoyahaneProformaService, BoyahaneProformaService>();
        services.AddScoped<IMasterDataService<BoyahaneDyeChemicalDto, BoyahaneDyeChemicalRequest>, BoyahaneDyeChemicalService>();
        services.AddScoped<IBoyahaneLabService, BoyahaneLabService>();
        services.AddScoped<IMasterDataService<BoyahaneMachineDto, BoyahaneMachineRequest>, BoyahaneMachineService>();
        services.AddScoped<IBoyahaneBatchService, BoyahaneBatchService>();
        services.AddScoped<IMasterDataService<BoyahaneOperatorDto, BoyahaneOperatorRequest>, BoyahaneOperatorService>();
        services.AddScoped<IBoyahaneOperatorCardService, BoyahaneOperatorCardService>();
        services.AddScoped<IMasterDataService<BoyahaneTabletDto, BoyahaneTabletRequest>, BoyahaneTabletService>();
        services.AddScoped<IBoyahaneTabletDeviceService, BoyahaneTabletDeviceService>();
        services.AddScoped<IMasterDataService<BoyahaneRfidCardDto, BoyahaneRfidCardRequest>, BoyahaneRfidCardService>();
        services.AddScoped<IMasterDataService<BoyahaneStopReasonDto, BoyahaneStopReasonRequest>, BoyahaneStopReasonService>();
        services.AddScoped<IBoyahaneMachineStatusService, BoyahaneMachineStatusService>();
        services.AddScoped<IMasterDataService<BoyahaneOperationDto, BoyahaneOperationRequest>, BoyahaneOperationService>();
        services.AddScoped<IBoyahaneRouteService, BoyahaneRouteService>();
        services.AddScoped<IBoyahaneInsightService, BoyahaneInsightService>();
        services.AddScoped<IBoyahanePackingService, BoyahanePackingService>();
        services.AddScoped<IBoyahaneShippingService, BoyahaneShippingService>();
        services.AddScoped<IBoyahaneChemicalStockService, BoyahaneChemicalStockService>();
        services.AddScoped<IMasterDataService<BoyahaneSupplierDto, BoyahaneSupplierRequest>, BoyahaneSupplierService>();
        services.AddScoped<IBoyahanePurchasingService, BoyahanePurchasingService>();
        services.AddScoped<IBoyahaneAccountService, BoyahaneAccountService>();
        services.AddScoped<IBoyahaneGoodsReturnService, BoyahaneGoodsReturnService>();
        services.AddScoped<IMasterDataService<BoyahaneWarehouseDto, BoyahaneWarehouseRequest>, BoyahaneWarehouseService>();
        services.AddScoped<IMasterDataService<BoyahaneWarehouseLocationDto, BoyahaneWarehouseLocationRequest>, BoyahaneWarehouseLocationService>();
        services.AddScoped<IBoyahaneWarehousingService, BoyahaneWarehousingService>();
        services.AddScoped<IBoyahaneRecipeService, BoyahaneRecipeService>();
        services.AddScoped<IBoyahaneWorkSessionService, BoyahaneWorkSessionService>();
        services.AddScoped<IMasterDataService<BoyahaneShiftDto, BoyahaneShiftRequest>, BoyahaneShiftService>();
        services.AddScoped<IMasterDataService<BoyahaneBreakGroupDto, BoyahaneBreakGroupRequest>, BoyahaneBreakGroupService>();
        services.AddScoped<IMasterDataService<BoyahaneBreakScheduleDto, BoyahaneBreakScheduleRequest>, BoyahaneBreakScheduleService>();
        services.AddScoped<IMasterDataService<BoyahaneColorCardDto, BoyahaneColorCardRequest>, BoyahaneColorCardService>();
        services.AddScoped<IBoyahaneColorCardDetailService, BoyahaneColorCardDetailService>();
        services.AddScoped<IMasterDataService<BoyahanePatternDto, BoyahanePatternRequest>, BoyahanePatternService>();
        services.AddScoped<IBoyahanePatternDetailService, BoyahanePatternDetailService>();
        services.AddScoped<IBoyahaneStrippingRecipeService, BoyahaneStrippingRecipeService>();
        services.AddScoped<IBoyahaneCostSettingsService, BoyahaneCostSettingsService>();
        services.AddScoped<IBoyahaneQuotationService, BoyahaneQuotationService>();
        services.AddScoped<IBoyahaneCapacityPlanService, BoyahaneCapacityPlanService>();
        return services;
    }
}
