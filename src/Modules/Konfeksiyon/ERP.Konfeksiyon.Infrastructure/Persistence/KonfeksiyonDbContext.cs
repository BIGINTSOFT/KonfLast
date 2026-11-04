using ERP.Core.Domain.Entities;
using ERP.Core.Domain.Entities.Identity;
using ERP.Core.Infrastructure.Persistence;
using ERP.Konfeksiyon.Application.Abstractions;
using ERP.Konfeksiyon.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace ERP.Konfeksiyon.Infrastructure.Persistence;

/// <summary>
/// <see cref="IKonfeksiyonDbContext"/>'in gerçeklemesi: ayrı bir DbContext değil, isteğin
/// <see cref="AppDbContext"/> örneğine açılan görünümdür. Modelde Konfeksiyon tabloları modül
/// konfigürasyonlarıyla yer alır.
/// </summary>
public sealed class KonfeksiyonDbContext(AppDbContext db) : IKonfeksiyonDbContext
{
    public DbSet<User> Users => db.Users;
    public DbSet<Role> Roles => db.Roles;
    public DbSet<Permission> Permissions => db.Permissions;
    public DbSet<UserRole> UserRoles => db.UserRoles;
    public DbSet<RolePermission> RolePermissions => db.RolePermissions;
    public DbSet<RefreshToken> RefreshTokens => db.RefreshTokens;
    public DbSet<Customer> Customers => db.Set<Customer>();
    public DbSet<CustomerAddress> CustomerAddresses => db.Set<CustomerAddress>();
    public DbSet<CustomerBuyer> CustomerBuyers => db.Set<CustomerBuyer>();
    public DbSet<Currency> Currencies => db.Currencies;
    public DbSet<ExchangeRate> ExchangeRates => db.ExchangeRates;
    public DbSet<PriceList> PriceLists => db.Set<PriceList>();
    public DbSet<PriceListItem> PriceListItems => db.Set<PriceListItem>();
    public DbSet<CustomerDiscount> CustomerDiscounts => db.Set<CustomerDiscount>();
    public DbSet<Model> Models => db.Set<Model>();
    public DbSet<Product> Products => db.Set<Product>();
    public DbSet<ProductGroup> ProductGroups => db.Set<ProductGroup>();
    public DbSet<Color> Colors => db.Set<Color>();
    public DbSet<Size> Sizes => db.Set<Size>();
    public DbSet<UserShortcut> UserShortcuts => db.UserShortcuts;
    public DbSet<UserFormDraft> UserFormDrafts => db.UserFormDrafts;
    public DbSet<PasswordResetToken> PasswordResetTokens => db.PasswordResetTokens;
    public DbSet<Supplier> Suppliers => db.Set<Supplier>();
    public DbSet<IntegrationMessage> IntegrationMessages => db.IntegrationMessages;
    public DbSet<IntegrationDelivery> IntegrationDeliveries => db.IntegrationDeliveries;
    public DbSet<AuditLog> AuditLogs => db.AuditLogs;

    public DbSet<ApprovalFlow> ApprovalFlows => db.Set<ApprovalFlow>();
    public DbSet<ApprovalFlowStep> ApprovalFlowSteps => db.Set<ApprovalFlowStep>();
    public DbSet<ApprovalFlowStepUser> ApprovalFlowStepUsers => db.Set<ApprovalFlowStepUser>();
    public DbSet<SalesOrderApprovalStep> SalesOrderApprovalSteps => db.Set<SalesOrderApprovalStep>();
    public DbSet<SalesOrderApprovalEvent> SalesOrderApprovalEvents => db.Set<SalesOrderApprovalEvent>();
    public DbSet<SalesOrderReservation> SalesOrderReservations => db.Set<SalesOrderReservation>();
    public DbSet<MachineType> MachineTypes => db.Set<MachineType>();
    public DbSet<Operation> Operations => db.Set<Operation>();
    public DbSet<Machine> Machines => db.Set<Machine>();
    public DbSet<Operator> Operators => db.Set<Operator>();
    public DbSet<ProductionOrder> ProductionOrders => db.Set<ProductionOrder>();
    public DbSet<ProductionOrderItem> ProductionOrderItems => db.Set<ProductionOrderItem>();
    public DbSet<SalesOrder> SalesOrders => db.Set<SalesOrder>();
    public DbSet<SalesOrderLine> SalesOrderLines => db.Set<SalesOrderLine>();
    public DbSet<ProductionRoute> ProductionRoutes => db.Set<ProductionRoute>();
    public DbSet<ProductionRouteOperation> ProductionRouteOperations => db.Set<ProductionRouteOperation>();
    public DbSet<OperationDependency> OperationDependencies => db.Set<OperationDependency>();
    public DbSet<Factory> Factories => db.Set<Factory>();
    public DbSet<ProductionArea> ProductionAreas => db.Set<ProductionArea>();
    public DbSet<ProductionLine> ProductionLines => db.Set<ProductionLine>();
    public DbSet<LineMachineAssignment> LineMachineAssignments => db.Set<LineMachineAssignment>();
    public DbSet<LineProductionPlan> LineProductionPlans => db.Set<LineProductionPlan>();
    public DbSet<Tablet> Tablets => db.Set<Tablet>();
    public DbSet<MachineStatusLog> MachineStatusLogs => db.Set<MachineStatusLog>();
    public DbSet<Bundle> Bundles => db.Set<Bundle>();
    public DbSet<BundleOperation> BundleOperations => db.Set<BundleOperation>();
    public DbSet<BundleOperationEvent> BundleOperationEvents => db.Set<BundleOperationEvent>();
    public DbSet<CuttingPlan> CuttingPlans => db.Set<CuttingPlan>();
    public DbSet<CuttingPlanSize> CuttingPlanSizes => db.Set<CuttingPlanSize>();
    public DbSet<CuttingPlanLot> CuttingPlanLots => db.Set<CuttingPlanLot>();
    public DbSet<WorkSession> WorkSessions => db.Set<WorkSession>();
    public DbSet<OperatorOperationQualification> OperatorOperationQualifications => db.Set<OperatorOperationQualification>();
    public DbSet<OperatorMachine> OperatorMachines => db.Set<OperatorMachine>();
    public DbSet<MachineOperation> MachineOperations => db.Set<MachineOperation>();
    public DbSet<OperatorPhoto> OperatorPhotos => db.Set<OperatorPhoto>();
    public DbSet<StopReason> StopReasons => db.Set<StopReason>();
    public DbSet<WorkSessionPause> WorkSessionPauses => db.Set<WorkSessionPause>();
    public DbSet<DefectType> DefectTypes => db.Set<DefectType>();
    public DbSet<QualityInspection> QualityInspections => db.Set<QualityInspection>();
    public DbSet<QualityRecord> QualityRecords => db.Set<QualityRecord>();
    public DbSet<ReworkTask> ReworkTasks => db.Set<ReworkTask>();
    public DbSet<Warehouse> Warehouses => db.Set<Warehouse>();
    public DbSet<WarehouseLocation> WarehouseLocations => db.Set<WarehouseLocation>();
    public DbSet<StockItem> StockItems => db.Set<StockItem>();
    public DbSet<StockLot> StockLots => db.Set<StockLot>();
    public DbSet<Stock> Stocks => db.Set<Stock>();
    public DbSet<StockTransaction> StockTransactions => db.Set<StockTransaction>();
    public DbSet<StockReservation> StockReservations => db.Set<StockReservation>();
    public DbSet<StockCount> StockCounts => db.Set<StockCount>();
    public DbSet<StockCountLine> StockCountLines => db.Set<StockCountLine>();
    public DbSet<PurchaseOrder> PurchaseOrders => db.Set<PurchaseOrder>();
    public DbSet<PurchaseOrderItem> PurchaseOrderItems => db.Set<PurchaseOrderItem>();
    public DbSet<GoodsReceipt> GoodsReceipts => db.Set<GoodsReceipt>();
    public DbSet<GoodsReceiptLine> GoodsReceiptLines => db.Set<GoodsReceiptLine>();
    public DbSet<Shipment> Shipments => db.Set<Shipment>();
    public DbSet<ShipmentItem> ShipmentItems => db.Set<ShipmentItem>();
    public DbSet<ShipmentCarton> ShipmentCartons => db.Set<ShipmentCarton>();
    public DbSet<ShipmentCartonLine> ShipmentCartonLines => db.Set<ShipmentCartonLine>();
    public DbSet<ShipmentPick> ShipmentPicks => db.Set<ShipmentPick>();
    public DbSet<AssortmentTemplate> AssortmentTemplates => db.Set<AssortmentTemplate>();
    public DbSet<AssortmentTemplateLine> AssortmentTemplateLines => db.Set<AssortmentTemplateLine>();
    public DbSet<Invoice> Invoices => db.Set<Invoice>();
    public DbSet<InvoiceLine> InvoiceLines => db.Set<InvoiceLine>();
    public DbSet<CustomerPayment> CustomerPayments => db.Set<CustomerPayment>();
    public DbSet<PaymentAllocation> PaymentAllocations => db.Set<PaymentAllocation>();
    public DbSet<Shift> Shifts => db.Set<Shift>();
    public DbSet<BreakGroup> BreakGroups => db.Set<BreakGroup>();
    public DbSet<BreakSchedule> BreakSchedules => db.Set<BreakSchedule>();
    public DbSet<RfidCard> RfidCards => db.Set<RfidCard>();
    public DbSet<BundleCardAssignment> BundleCardAssignments => db.Set<BundleCardAssignment>();
    public DbSet<OperatorCard> OperatorCards => db.Set<OperatorCard>();
    public DbSet<RfidReadLog> RfidReadLogs => db.Set<RfidReadLog>();
    public DbSet<ProductMaterial> ProductMaterials => db.Set<ProductMaterial>();
    public DbSet<PayrollSetting> PayrollSettings => db.Set<PayrollSetting>();
    public DbSet<PayrollBonusTier> PayrollBonusTiers => db.Set<PayrollBonusTier>();
    public DbSet<SubcontractPrice> SubcontractPrices => db.Set<SubcontractPrice>();
    public DbSet<SubcontractSend> SubcontractSends => db.Set<SubcontractSend>();
    public DbSet<SubcontractSendLine> SubcontractSendLines => db.Set<SubcontractSendLine>();
    public DbSet<SubcontractReturn> SubcontractReturns => db.Set<SubcontractReturn>();
    public DbSet<SubcontractReturnLine> SubcontractReturnLines => db.Set<SubcontractReturnLine>();
    public DbSet<CostSetting> CostSettings => db.Set<CostSetting>();
    public DbSet<Quotation> Quotations => db.Set<Quotation>();
    public DbSet<QuotationLine> QuotationLines => db.Set<QuotationLine>();
    public DbSet<QuotationLineMaterial> QuotationLineMaterials => db.Set<QuotationLineMaterial>();

    public DbSet<TEntity> Set<TEntity>() where TEntity : class => db.Set<TEntity>();

    public EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class => db.Entry(entity);

    public DatabaseFacade Database => db.Database;

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        db.SaveChangesAsync(cancellationToken);
}
