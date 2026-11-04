using ERP.Core.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace ERP.Konfeksiyon.Application.Abstractions;

/// <summary>
/// Konfeksiyon modülünün veritabanı erişimi: Core tablolarına ek olarak modülün kendi tabloları.
/// Core ile aynı DbContext örneğini kullanır; tek SaveChanges, tek transaction.
/// </summary>
public interface IKonfeksiyonDbContext : IAppDbContext
{
    DbSet<Customer> Customers { get; }
    DbSet<CustomerAddress> CustomerAddresses { get; }
    DbSet<CustomerBuyer> CustomerBuyers { get; }
    DbSet<PriceList> PriceLists { get; }
    DbSet<PriceListItem> PriceListItems { get; }
    DbSet<CustomerDiscount> CustomerDiscounts { get; }
    DbSet<Model> Models { get; }
    DbSet<Product> Products { get; }
    DbSet<ProductGroup> ProductGroups { get; }
    DbSet<Color> Colors { get; }
    DbSet<Size> Sizes { get; }
    DbSet<Supplier> Suppliers { get; }
    DbSet<ApprovalFlow> ApprovalFlows { get; }
    DbSet<ApprovalFlowStep> ApprovalFlowSteps { get; }
    DbSet<ApprovalFlowStepUser> ApprovalFlowStepUsers { get; }
    DbSet<SalesOrderApprovalStep> SalesOrderApprovalSteps { get; }
    DbSet<SalesOrderApprovalEvent> SalesOrderApprovalEvents { get; }
    DbSet<SalesOrderReservation> SalesOrderReservations { get; }
    DbSet<MachineType> MachineTypes { get; }
    DbSet<Operation> Operations { get; }
    DbSet<Machine> Machines { get; }
    DbSet<Operator> Operators { get; }
    DbSet<ProductionOrder> ProductionOrders { get; }
    DbSet<ProductionOrderItem> ProductionOrderItems { get; }
    DbSet<SalesOrder> SalesOrders { get; }
    DbSet<SalesOrderLine> SalesOrderLines { get; }
    DbSet<ProductionRoute> ProductionRoutes { get; }
    DbSet<ProductionRouteOperation> ProductionRouteOperations { get; }
    DbSet<OperationDependency> OperationDependencies { get; }
    DbSet<Factory> Factories { get; }
    DbSet<ProductionArea> ProductionAreas { get; }
    DbSet<ProductionLine> ProductionLines { get; }
    DbSet<LineMachineAssignment> LineMachineAssignments { get; }
    DbSet<LineProductionPlan> LineProductionPlans { get; }
    DbSet<Tablet> Tablets { get; }
    DbSet<MachineStatusLog> MachineStatusLogs { get; }
    DbSet<Bundle> Bundles { get; }
    DbSet<BundleOperation> BundleOperations { get; }
    DbSet<BundleOperationEvent> BundleOperationEvents { get; }
    DbSet<CuttingPlan> CuttingPlans { get; }
    DbSet<CuttingPlanSize> CuttingPlanSizes { get; }
    DbSet<CuttingPlanLot> CuttingPlanLots { get; }
    DbSet<WorkSession> WorkSessions { get; }
    DbSet<OperatorOperationQualification> OperatorOperationQualifications { get; }
    DbSet<OperatorMachine> OperatorMachines { get; }
    DbSet<MachineOperation> MachineOperations { get; }
    DbSet<OperatorPhoto> OperatorPhotos { get; }
    DbSet<StopReason> StopReasons { get; }
    DbSet<WorkSessionPause> WorkSessionPauses { get; }
    DbSet<DefectType> DefectTypes { get; }
    DbSet<QualityInspection> QualityInspections { get; }
    DbSet<QualityRecord> QualityRecords { get; }
    DbSet<ReworkTask> ReworkTasks { get; }
    DbSet<Warehouse> Warehouses { get; }
    DbSet<WarehouseLocation> WarehouseLocations { get; }
    DbSet<StockItem> StockItems { get; }
    DbSet<StockLot> StockLots { get; }
    DbSet<Stock> Stocks { get; }
    DbSet<StockTransaction> StockTransactions { get; }
    DbSet<StockReservation> StockReservations { get; }
    DbSet<StockCount> StockCounts { get; }
    DbSet<StockCountLine> StockCountLines { get; }
    DbSet<PurchaseOrder> PurchaseOrders { get; }
    DbSet<PurchaseOrderItem> PurchaseOrderItems { get; }
    DbSet<GoodsReceipt> GoodsReceipts { get; }
    DbSet<GoodsReceiptLine> GoodsReceiptLines { get; }
    DbSet<Shipment> Shipments { get; }
    DbSet<ShipmentItem> ShipmentItems { get; }
    DbSet<ShipmentCarton> ShipmentCartons { get; }
    DbSet<ShipmentCartonLine> ShipmentCartonLines { get; }
    DbSet<ShipmentPick> ShipmentPicks { get; }
    DbSet<AssortmentTemplate> AssortmentTemplates { get; }
    DbSet<AssortmentTemplateLine> AssortmentTemplateLines { get; }
    DbSet<Invoice> Invoices { get; }
    DbSet<InvoiceLine> InvoiceLines { get; }
    DbSet<CustomerPayment> CustomerPayments { get; }
    DbSet<PaymentAllocation> PaymentAllocations { get; }
    DbSet<Shift> Shifts { get; }
    DbSet<BreakGroup> BreakGroups { get; }
    DbSet<BreakSchedule> BreakSchedules { get; }
    DbSet<RfidCard> RfidCards { get; }
    DbSet<BundleCardAssignment> BundleCardAssignments { get; }
    DbSet<OperatorCard> OperatorCards { get; }
    DbSet<RfidReadLog> RfidReadLogs { get; }
    DbSet<ProductMaterial> ProductMaterials { get; }
    DbSet<PayrollSetting> PayrollSettings { get; }
    DbSet<PayrollBonusTier> PayrollBonusTiers { get; }
    DbSet<SubcontractPrice> SubcontractPrices { get; }
    DbSet<SubcontractSend> SubcontractSends { get; }
    DbSet<SubcontractSendLine> SubcontractSendLines { get; }
    DbSet<SubcontractReturn> SubcontractReturns { get; }
    DbSet<SubcontractReturnLine> SubcontractReturnLines { get; }
    DbSet<CostSetting> CostSettings { get; }
    DbSet<Quotation> Quotations { get; }
    DbSet<QuotationLine> QuotationLines { get; }
    DbSet<QuotationLineMaterial> QuotationLineMaterials { get; }
}
