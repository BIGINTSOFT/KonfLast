using ERP.Core.Domain.Authorization;
using KP = ERP.Konfeksiyon.Domain.Authorization.KonfeksiyonPermissions;

namespace ERP.Konfeksiyon.Domain.Authorization;

/// <summary>
/// Sistem rollerinin Konfeksiyon yetkilerindeki başlangıç atamaları (modülün ilk kurulum seed'i).
/// Roller çekirdektedir; kurulumdan sonra yetkiler veritabanından yönetilir.
/// </summary>
public static class KonfeksiyonRoles
{
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DefaultPermissions =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [Roles.Admin] = KonfeksiyonPermissions.All.Select(p => p.Code).ToList(),
            [Roles.ProductionManager] =
            [
                KP.Dashboard.View, KP.Report.View, KP.ProductionOrder.View, KP.ProductionOrder.Create,
                KP.ProductionOrder.Update, KP.ProductionLine.View, KP.ProductionLine.Update, KP.Bundle.View,
                KP.Bundle.Create, KP.Production.View, KP.Machine.View, KP.Machine.Update, KP.Operator.View,
                KP.Operator.Update, KP.Quality.View, KP.ProductionRoute.View, KP.ProductionRoute.Manage,
                KP.Tablet.View, KP.Tablet.Manage, KP.RfidCard.View, KP.RfidCard.Manage, KP.Purchase.View,
                KP.Shipment.View, KP.Payroll.View, KP.Costing.View, KP.ProductionOrder.Approve, KP.SalesOrder.View,
                KP.SalesOrder.Manage, KP.SalesOrder.Approve, KP.Production.ManualReport, KP.Bundle.ReleaseCard,
                KP.ProductionOrder.Cancel, KP.ProductionOrder.Complete, KP.SalesOrder.Cancel,
                KP.ProductionRoute.Activate, KP.Operator.AssignFloorRole, KP.Operator.AssignCard,
                KP.Operator.ViewPersonal, KP.Production.ReleaseJob, KP.WorkSession.Close, KP.Machine.Manage,
                KP.Machine.ResolveBreakdown, KP.SalesOrder.OverridePrice, KP.Stock.Reserve, KP.Report.Personnel,
                KP.Report.Financial, KP.Report.Export, KP.Purchase.ViewPrices, KP.SalesOrder.ViewPrices,
                KP.Bundle.Cancel, KP.LinePlan.Manage, KP.Quality.ManageDefectTypes,
            ],
            [Roles.ProductionPlanner] =
            [
                KP.Dashboard.View, KP.Report.View, KP.ProductionOrder.View, KP.ProductionOrder.Create,
                KP.ProductionOrder.Update, KP.ProductionLine.View, KP.ProductionLine.Update, KP.Bundle.View,
                KP.Bundle.Create, KP.Production.View, KP.Machine.View, KP.Operator.View, KP.ProductionRoute.View,
                KP.ProductionRoute.Manage, KP.RfidCard.View, KP.RfidCard.Manage, KP.Purchase.View, KP.Purchase.Manage,
                KP.Shipment.View, KP.ProductionOrder.Approve, KP.Purchase.Approve, KP.SalesOrder.View,
                KP.SalesOrder.Manage, KP.Production.ManualReport, KP.Bundle.ReleaseCard, KP.ProductionOrder.Cancel,
                KP.ProductionOrder.Complete, KP.SalesOrder.Cancel, KP.ProductionRoute.Activate,
                KP.Operator.ViewPersonal, KP.SalesOrder.OverridePrice, KP.Stock.Reserve, KP.Report.Personnel,
                KP.Report.Financial, KP.Report.Export, KP.Purchase.ViewPrices, KP.SalesOrder.ViewPrices,
                KP.Bundle.Cancel, KP.LinePlan.Manage, KP.Purchase.Close, KP.Purchase.Cancel,
            ],
            [Roles.LineManager] =
            [
                KP.Dashboard.View, KP.ProductionOrder.View, KP.ProductionLine.View, KP.Bundle.View,
                KP.Production.View, KP.Machine.View, KP.Machine.Update, KP.Operator.View, KP.ProductionRoute.View,
                KP.Tablet.View, KP.RfidCard.View, KP.Floor.Pause, KP.Floor.ReportBreakdown, KP.Floor.ResolveBreakdown,
                KP.Production.ManualReport, KP.Bundle.ReleaseCard, KP.Machine.Manage, KP.Machine.ResolveBreakdown,
                KP.Operator.ViewPersonal, KP.Floor.ReportReject,
            ],
            [Roles.Quality] =
            [
                KP.Quality.View, KP.Quality.Create, KP.Bundle.View, KP.Production.View, KP.Report.View,
                KP.Quality.ReleaseHold, KP.Quality.Reject, KP.Report.Personnel, KP.Report.Financial, KP.Report.Export,
            ],
            [Roles.Warehouse] =
            [
                KP.Stock.View, KP.Stock.Manage, KP.Bundle.View, KP.Purchase.View, KP.Shipment.View,
                KP.Shipment.Manage, KP.Report.Warehouse, KP.Stock.Adjust, KP.Costing.Manage, KP.Shipment.Ship,
                KP.Stock.EditLot, KP.Warehouse.Manage, KP.Report.Financial, KP.Report.Export, KP.Purchase.ViewPrices,
            ],
            [Roles.Maintenance] =
            [
                KP.Machine.View, KP.Machine.Update, KP.Tablet.View, KP.Floor.ReportBreakdown,
                KP.Floor.ResolveBreakdown, KP.Machine.Manage, KP.Machine.ResolveBreakdown, KP.Floor.ReportReject,
            ],
            [Roles.Operator] =
            [
                KP.Bundle.View, KP.Bundle.Start, KP.Bundle.Complete, KP.Production.Report, KP.Floor.ReportReject,
            ],
            [Roles.Management] =
            [
                KP.Dashboard.View, KP.Report.View, KP.ProductionOrder.View, KP.ProductionLine.View,
                KP.Production.View, KP.Quality.View, KP.Stock.View, KP.Purchase.View, KP.Shipment.View,
                KP.Payroll.View, KP.Costing.View, KP.SalesOrder.View, KP.SalesOrder.Approve, KP.Report.Personnel,
                KP.Report.Financial, KP.Report.Export, KP.Purchase.ViewPrices, KP.SalesOrder.ViewPrices,
            ],
            [Roles.Purchasing] =
            [
                KP.Dashboard.View, KP.Purchase.View, KP.Purchase.Manage, KP.Purchase.Approve, KP.Stock.View,
                KP.ProductionOrder.View, KP.Purchase.Close, KP.Purchase.Cancel, KP.Purchase.ViewPrices,
            ],
        };
}
