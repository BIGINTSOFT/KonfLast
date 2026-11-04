using ERP.Core.Application.Abstractions;
using ERP.Core.Application.Features.Integrations;
using ERP.Core.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Bundles;
using ERP.Konfeksiyon.Application.Features.Costing;
using ERP.Konfeksiyon.Application.Features.Cutting;
using ERP.Konfeksiyon.Application.Features.Integrations;
using ERP.Konfeksiyon.Application.Features.Invoicing;
using ERP.Konfeksiyon.Application.Features.MasterData;
using ERP.Konfeksiyon.Application.Features.Payroll;
using ERP.Konfeksiyon.Application.Features.Pricing;
using ERP.Konfeksiyon.Application.Features.Planning;
using ERP.Konfeksiyon.Application.Features.Production;
using ERP.Konfeksiyon.Application.Features.ProductionLines;
using ERP.Konfeksiyon.Application.Features.ProductionOrders;
using ERP.Konfeksiyon.Application.Features.ProductionRoutes;
using ERP.Konfeksiyon.Application.Features.ProductionTracking;
using ERP.Konfeksiyon.Application.Features.Purchasing;
using ERP.Konfeksiyon.Application.Features.Quality;
using ERP.Konfeksiyon.Application.Features.Reports;
using ERP.Konfeksiyon.Application.Features.Rfid;
using ERP.Konfeksiyon.Application.Features.Sales;
using ERP.Konfeksiyon.Application.Features.Shifts;
using ERP.Konfeksiyon.Application.Features.Quotations;
using ERP.Konfeksiyon.Application.Features.Shipping;
using ERP.Konfeksiyon.Application.Features.Subcontracting;
using ERP.Konfeksiyon.Application.Features.Tablets;
using ERP.Konfeksiyon.Application.Features.Warehousing;
using ERP.Konfeksiyon.Application.Features.WorkSessions;
using ERP.Konfeksiyon.Application.Features.Workspace;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Konfeksiyon.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddKonfeksiyonApplication(this IServiceCollection services)
    {
        services.AddScoped<IPriceListService, PriceListService>();
        services.AddScoped<ICustomerDiscountService, CustomerDiscountService>();
        services.AddScoped<IPricingService, PricingService>();
        services.AddScoped<IInvoiceService, InvoiceService>();
        services.AddScoped<ICustomerAccountService, CustomerAccountService>();
        services.AddScoped<ICustomerAddressService, CustomerAddressService>();
        services.AddScoped<ICustomerBuyerService, CustomerBuyerService>();
        services.AddScoped<IMasterDataService<CustomerDto, CustomerRequest>, CustomerService>();
        services.AddScoped<IMasterDataService<ModelDto, ModelRequest>, ModelService>();
        services.AddScoped<IMasterDataService<ProductDto, ProductRequest>, ProductService>();
        services.AddScoped<IMasterDataService<ProductGroupDto, ProductGroupRequest>, ProductGroupService>();
        services.AddScoped<IMasterDataService<ColorDto, ColorRequest>, ColorService>();
        services.AddScoped<IMasterDataService<SizeDto, SizeRequest>, SizeService>();

        services.AddOptions<ProductionOrderOptions>()
            .BindConfiguration(ProductionOrderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IProductionOrderService, ProductionOrderService>();

        services.AddOptions<SalesOrderOptions>()
            .BindConfiguration(SalesOrderOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<ISalesOrderService, SalesOrderService>();
        services.AddScoped<IApprovalFlowService, ApprovalFlowService>();
        services.AddScoped<ISalesOrderFulfillment, SalesOrderFulfillment>();
        services.AddScoped<ISalesOrderReservationService, SalesOrderReservationService>();
        services.AddScoped<ISalesOrderProductionChangesService, SalesOrderProductionChangesService>();
        services.AddScoped<IProductionRouteService, ProductionRouteService>();

        services.AddOptions<BundleOptions>()
            .BindConfiguration(BundleOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IBundleService, BundleService>();
        services.AddScoped<ICuttingPlanService, CuttingPlanService>();
        services.AddScoped<ISubcontractPriceService, SubcontractPriceService>();
        services.AddScoped<ISubcontractService, SubcontractService>();
        services.AddScoped<ICostSettingService, CostSettingService>();
        services.AddScoped<IQuotationService, QuotationService>();

        services.AddOptions<RfidOptions>()
            .BindConfiguration(RfidOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IMasterDataService<RfidCardDto, RfidCardRequest>, RfidCardService>();
        services.AddScoped<IRfidAssignmentService, RfidAssignmentService>();
        services.AddScoped<IRfidReadService, RfidReadService>();

        services.AddOptions<ProductionOptions>().BindConfiguration(ProductionOptions.SectionName);
        services.AddScoped<IStationResolver, StationResolver>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IOperatorQualificationService, OperatorQualificationService>();
        services.AddScoped<IOperatorPhotoService, OperatorPhotoService>();
        services.AddScoped<IFloorAuthorization, FloorAuthorization>();
        services.AddSingleton<IFactoryClock, FactoryClock>();
        services.AddScoped<ISessionInterruptions, SessionInterruptions>();
        services.AddScoped<IWorkSessionService, WorkSessionService>();
        services.AddScoped<IMasterDataService<ShiftDto, ShiftRequest>, ShiftService>();
        services.AddScoped<IMasterDataService<BreakGroupDto, BreakGroupRequest>, BreakGroupService>();
        services.AddScoped<IMasterDataService<BreakScheduleDto, BreakScheduleRequest>, BreakScheduleService>();
        services.AddScoped<IProductionTrackingService, ProductionTrackingService>();
        services.AddScoped<IPerformanceService, PerformanceService>();
        services.AddScoped<ITabletPerformanceService, TabletPerformanceService>();
        services.AddScoped<ILiveDashboardService, LiveDashboardService>();
        services.AddScoped<IMasterDataService<DefectTypeDto, DefectTypeRequest>, DefectTypeService>();
        services.AddScoped<IMasterDataService<StopReasonDto, StopReasonRequest>, StopReasonService>();
        services.AddScoped<IStopReasonLookup, StopReasonLookup>();
        services.AddScoped<IStaffRoleSettingService, StaffRoleSettingService>();
        services.AddScoped<IQualityService, QualityService>();

        services.AddScoped<IWorkspaceService, WorkspaceService>();
        services.AddScoped<IPayrollService, PayrollService>();
        services.AddScoped<IProductMaterialService, ProductMaterialService>();
        services.AddScoped<IProductCostService, ProductCostService>();
        services.AddScoped<ICostingService, CostingService>();

        services.AddScoped<IMasterDataService<WarehouseDto, WarehouseRequest>, WarehouseService>();
        services.AddScoped<IMasterDataService<WarehouseLocationDto, WarehouseLocationRequest>, WarehouseLocationService>();
        services.AddScoped<IMasterDataService<StockItemDto, StockItemRequest>, StockItemService>();
        services.AddScoped<StockService>();
        services.AddScoped<IStockService>(provider => provider.GetRequiredService<StockService>());
        services.AddScoped<IStockLedger>(provider => provider.GetRequiredService<StockService>());
        services.AddScoped<IStockReservationService, StockReservationService>();
        services.AddOptions<StockCountOptions>()
            .BindConfiguration(StockCountOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IStockCountService, StockCountService>();
        services.AddScoped<IStockScanService, StockScanService>();
        services.AddScoped<IWarehouseLocationBulkService, WarehouseLocationBulkService>();

        services.AddOptions<PurchasingOptions>()
            .BindConfiguration(PurchasingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IMasterDataService<SupplierDto, SupplierRequest>, SupplierService>();
        services.AddScoped<IPurchaseOrderService, PurchaseOrderService>();
        services.AddScoped<ISupplyPlanningService, SupplyPlanningService>();

        services.AddOptions<ShippingOptions>()
            .BindConfiguration(ShippingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IShipmentService, ShipmentService>();
        services.AddScoped<IShipmentPackingService, ShipmentPackingService>();
        services.AddScoped<IShipmentPickingService, ShipmentPickingService>();
        services.AddScoped<IAssortmentTemplateService, AssortmentTemplateService>();

        // Raporlar: her IReport kataloğa kendiliğinden girer.
        foreach (var reportType in typeof(DependencyInjection).Assembly.GetTypes()
                     .Where(t => t is { IsClass: true, IsAbstract: false } && typeof(IReport).IsAssignableFrom(t))
                     .OrderBy(t => t.Name, StringComparer.Ordinal))
        {
            services.AddScoped(typeof(IReport), reportType);
        }

        services.AddScoped<IReportService, ReportService>();
        services.AddSingleton<IReportExporter, CsvReportExporter>();

        services.AddScoped<IMasterDataService<MachineTypeDto, MachineTypeRequest>, MachineTypeService>();
        services.AddScoped<IMasterDataService<OperationDto, OperationRequest>, OperationService>();
        services.AddScoped<IMasterDataService<MachineDto, MachineRequest>, MachineService>();
        services.AddScoped<IMachineOperationService, MachineOperationService>();
        services.AddScoped<IMasterDataService<OperatorDto, OperatorRequest>, OperatorService>();
        services.AddScoped<IMasterDataService<FactoryDto, FactoryRequest>, FactoryService>();
        services.AddScoped<IMasterDataService<ProductionAreaDto, ProductionAreaRequest>, ProductionAreaService>();
        services.AddScoped<IMasterDataService<ProductionLineDto, ProductionLineRequest>, ProductionLineService>();

        services.AddOptions<TabletOptions>()
            .BindConfiguration(TabletOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();
        services.AddScoped<IMasterDataService<TabletDto, TabletRequest>, TabletService>();
        services.AddScoped<ITabletDeviceService, TabletDeviceService>();
        services.AddScoped<IMachineStatusService, MachineStatusService>();

        services.AddScoped<ILineMachineService, LineMachineService>();
        services.AddScoped<ILinePlanService, LinePlanService>();
        services.AddScoped<ILineBalancingService, LineBalancingService>();

        services.AddOptions<PlanningOptions>().BindConfiguration(PlanningOptions.SectionName);
        services.AddScoped<IPlanningService, PlanningService>();
        services.AddScoped<IPlanningDemandService, PlanningDemandService>();
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);
        services.AddSingleton<IIntegrationEventCatalog, KonfeksiyonIntegrationEventCatalog>();
        services.AddScoped<IReferenceUsageProbe, KonfeksiyonReferenceUsageProbe>();
        services.AddSingleton<ILiveTopicSource, KonfeksiyonLiveTopics>();
        services.AddSingleton<ILiveTargetSource, KonfeksiyonLiveTargets>();
        services.AddScoped<ILiveTabletDirectory, KonfeksiyonLiveTablets>();

        return services;
    }
}
