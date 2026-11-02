import type { ReactNode } from 'react'
import { BrowserRouter, Navigate, Route, Routes } from 'react-router-dom'
import { AuditLogsPage } from './features/auditLogs/AuditLogsPage'
import { BatchCardPage } from './features/boyahane/BatchCardPage'
import { BatchDetailPage } from './features/boyahane/BatchDetailPage'
import { BatchesPage } from './features/boyahane/BatchesPage'
import { BoyahaneOrderDetailPage } from './features/boyahane/BoyahaneOrderDetailPage'
import { AccountsPage, AccountStatementPage, OpenInvoicesPage, OpeningBalancesPage, PaymentDetailPage, PaymentsPage } from './features/boyahane/AccountPages'
import { BoyahaneLocationLabelPage, LocationMovementsPage, RecipesPage, WarehouseGoodsPage } from './features/boyahane/WarehousePages'
import { BoyahaneDashboardPage } from './features/boyahane/BoyahaneDashboardPage'
import { GoodsReturnDetailPage, GoodsReturnPrintPage, GoodsReturnsPage } from './features/boyahane/GoodsReturnPages'
import { PurchaseOrderDetailPage as BoyahanePurchaseOrderDetailPage, PurchaseOrdersPage as BoyahanePurchaseOrdersPage, SupplySuggestionsPage as BoyahaneSupplySuggestionsPage } from './features/boyahane/PurchasingPages'
import { ChemicalLotsPage, ChemicalMovementsPage, ChemicalStockPage } from './features/boyahane/ChemicalStockPages'
import { DespatchDetailPage, DespatchesPage, DespatchPrintPage, InvoiceDetailPage, InvoicePrintPage, InvoicesPage } from './features/boyahane/ShippingPages'
import { PackingLabelPage, PackingListDetailPage, PackingListPrintPage, PackingListsPage } from './features/boyahane/PackingPages'
import {
  BoyahaneCardReleasePage,
  BoyahaneGoodsPage,
  BoyahaneOrderTrackingPage,
  BoyahanePlanningPage,
  BoyahaneReportsPage,
  BoyahaneTrackingPage,
} from './features/boyahane/BoyahaneInsightPages'
import { BoyahaneRoutesPage } from './features/boyahane/BoyahaneRoutesPage'
import { BoyahaneSetupPage } from './features/boyahane/BoyahaneSetupPage'
import { BoyahaneWorkSessionsPage } from './features/boyahane/BoyahaneWorkSessionsPage'
import { BoyahaneOrdersPage } from './features/boyahane/BoyahaneOrdersPage'
import { FabricInspectionPage } from './features/boyahane/FabricInspectionPage'
import { FabricReceiptDetailPage } from './features/boyahane/FabricReceiptDetailPage'
import { FabricReceiptsPage } from './features/boyahane/FabricReceiptsPage'
import { FabricRollLabelPage } from './features/boyahane/FabricRollLabelPage'
import { LabRequestDetailPage } from './features/boyahane/LabRequestDetailPage'
import { LabRequestsPage } from './features/boyahane/LabRequestsPage'
import { YarnLotsPage } from './features/boyahane/YarnLotsPage'
import { DokumaOrderDetailPage } from './features/dokuma/DokumaOrderDetailPage'
import { DokumaOrdersPage } from './features/dokuma/DokumaOrdersPage'
import { DokumaYarnLotsPage } from './features/dokuma/DokumaYarnLotsPage'
import { OrmeAccountsPage, OrmeAccountStatementPage, OrmeOpenInvoicesPage, OrmeOpeningBalancesPage, OrmePaymentDetailPage, OrmePaymentsPage } from './features/orme/OrmeAccountPages'
import { OrmePackingListDetailPage, OrmePackingListPrintPage, OrmePackingListsPage } from './features/orme/OrmePackingPages'
import { OrmeDespatchDetailPage, OrmeDespatchesPage, OrmeDespatchPrintPage, OrmeInvoiceDetailPage, OrmeInvoicePrintPage, OrmeInvoicesPage } from './features/orme/OrmeShippingPages'
import { OrmeYarnReturnsPage } from './features/orme/OrmeYarnReturnsPage'
import { OrmeMachineStopsPage, OrmeWorkSessionsPage } from './features/orme/OrmeFloorPages'
import { OrmeDashboardPage } from './features/orme/OrmeDashboardPage'
import { OrmeInspectionPage } from './features/orme/OrmeInspectionPage'
import { OrmeReportsPage } from './features/orme/OrmeReportsPage'
import { OrmeKnitOrderDetailPage } from './features/orme/OrmeKnitOrderDetailPage'
import { OrmeKnitOrdersPage } from './features/orme/OrmeKnitOrdersPage'
import { OrmeMachineBoardPage } from './features/orme/OrmeMachineBoardPage'
import { OrmeOrderDetailPage } from './features/orme/OrmeOrderDetailPage'
import { OrmeOrdersPage } from './features/orme/OrmeOrdersPage'
import { OrmeRollLabelPage } from './features/orme/OrmeRollLabelPage'
import { OrmeRollsPage } from './features/orme/OrmeRollsPage'
import { OrmeYarnLotsPage } from './features/orme/OrmeYarnLotsPage'
import { DokumaAccountsPage, DokumaAccountStatementPage, DokumaOpenInvoicesPage, DokumaOpeningBalancesPage, DokumaPaymentDetailPage, DokumaPaymentsPage } from './features/dokuma/DokumaAccountPages'
import { DokumaPackingListDetailPage, DokumaPackingListPrintPage, DokumaPackingListsPage } from './features/dokuma/DokumaPackingPages'
import { DokumaDespatchDetailPage, DokumaDespatchesPage, DokumaDespatchPrintPage, DokumaInvoiceDetailPage, DokumaInvoicePrintPage, DokumaInvoicesPage } from './features/dokuma/DokumaShippingPages'
import { DokumaYarnReturnsPage } from './features/dokuma/DokumaYarnReturnsPage'
import { DokumaLoomStopsPage, DokumaWorkSessionsPage } from './features/dokuma/DokumaFloorPages'
import { DokumaDashboardPage } from './features/dokuma/DokumaDashboardPage'
import { DokumaInspectionPage } from './features/dokuma/DokumaInspectionPage'
import { DokumaReportsPage } from './features/dokuma/DokumaReportsPage'
import { DokumaBeamWeavingPage } from './features/dokuma/DokumaBeamWeavingPage'
import { DokumaLoomBoardPage } from './features/dokuma/DokumaLoomBoardPage'
import { DokumaRollLabelPage } from './features/dokuma/DokumaRollLabelPage'
import { DokumaRollsPage } from './features/dokuma/DokumaRollsPage'
import { DokumaBeamsPage } from './features/dokuma/DokumaBeamsPage'
import { DokumaWarpOrderDetailPage } from './features/dokuma/DokumaWarpOrderDetailPage'
import { DokumaWarpOrdersPage } from './features/dokuma/DokumaWarpOrdersPage'
import { IntegrationsPage } from './features/integrations/IntegrationsPage'
import { ChangePasswordPage } from './features/auth/ChangePasswordPage'
import { LoginPage } from './features/auth/LoginPage'
import { ForgotPasswordPage, ResetPasswordPage } from './features/auth/PasswordResetPages'
import { RequireAuth, RequirePermission } from './features/auth/RequireAuth'
import { MasterDataPage } from './features/masterData/MasterDataPage'
import { resources } from './features/masterData/resources'
import { BundleDetailPage } from './features/bundles/BundleDetailPage'
import { BundlesPage } from './features/bundles/BundlesPage'
import { CardReleasePage } from './features/bundles/CardReleasePage'
import { LineBalancingPage } from './features/lineBalancing/LineBalancingPage'
import { DashboardPage } from './features/dashboard/DashboardPage'
import { PlanningPage } from './features/planning/PlanningPage'
import { LineDetailPage } from './features/productionLines/LineDetailPage'
import { ProductionOrderDetailPage } from './features/productionOrders/ProductionOrderDetailPage'
import { ProductionOrderCreatePage, ProductionOrderEditPage } from './features/productionOrders/ProductionOrderFormPage'
import { ProductionOrdersPage } from './features/productionOrders/ProductionOrdersPage'
import { PerformancePage } from './features/productionTracking/PerformancePage'
import { ProductionTrackingPage } from './features/productionTracking/ProductionTrackingPage'
import { ProductionRouteDetailPage } from './features/productionRoutes/ProductionRouteDetailPage'
import { ProductionRouteCreatePage, ProductionRouteEditPage } from './features/productionRoutes/ProductionRouteFormPage'
import { ProductionRoutesPage } from './features/productionRoutes/ProductionRoutesPage'
import { PurchaseOrderDetailPage } from './features/purchasing/PurchaseOrderDetailPage'
import { PurchaseOrdersPage } from './features/purchasing/PurchaseOrdersPage'
import { SupplySuggestionsPage } from './features/purchasing/SupplySuggestionsPage'
import { QualityPage } from './features/quality/QualityPage'
import { ReportsPage } from './features/reports/ReportsPage'
import { RolesPage } from './features/roles/RolesPage'
import { SalesOrderDetailPage } from './features/salesOrders/SalesOrderDetailPage'
import { SalesOrderCreatePage, SalesOrderEditPage } from './features/salesOrders/SalesOrderFormPage'
import { SalesOrderProformaPage } from './features/salesOrders/SalesOrderProformaPage'
import { ApprovalFlowPage } from './features/approvals/ApprovalFlowPage'
import { ExchangeRatesPage } from './features/finance/ExchangeRatesPage'
import { CustomerDiscountsPage } from './features/pricing/CustomerDiscountsPage'
import { PriceListsPage } from './features/pricing/PriceListsPage'
import { SalesOrdersPage } from './features/salesOrders/SalesOrdersPage'
import { ShipmentDetailPage } from './features/shipping/ShipmentDetailPage'
import { SalesAccountsPage, SalesAccountStatementPage, SalesOpenInvoicesPage, SalesOpeningBalancesPage, SalesPaymentDetailPage, SalesPaymentsPage } from './features/invoicing/AccountPages'
import { SalesInvoiceDetailPage, SalesInvoicePrintPage, SalesInvoicesPage } from './features/invoicing/InvoicePages'
import { ShipmentsPage } from './features/shipping/ShipmentsPage'
import { LabelPrintPage } from './features/labels/LabelPrintPage'
import { PayrollPage } from './features/payroll/PayrollPage'
import { ProductionReceiptPage } from './features/stock/ProductionReceiptPage'
import { SetupGuidePage } from './features/workspace/SetupGuidePage'
import { LotLabelPage } from './features/stock/LotLabelPage'
import { LocationLabelPage } from './features/stock/LocationLabelPage'
import { StockCountDetailPage } from './features/stock/StockCountDetailPage'
import { StockCountsPage } from './features/stock/StockCountsPage'
import { StockLotsPage } from './features/stock/StockLotsPage'
import { StockPage } from './features/stock/StockPage'
import { StockTransactionsPage } from './features/stock/StockTransactionsPage'
import { HandheldPage, TerminalPage } from './features/terminal/TerminalPage'
import { UsersPage } from './features/users/UsersPage'
import { WorkSessionsPage } from './features/workSessions/WorkSessionsPage'
import { PickListPage } from './features/costing/PickListPage'
import { AppLayout } from './layouts/AppLayout'
import { BoyahaneProformaPage } from './features/boyahane/BoyahaneProformaPage'
import { OrmeProformaPage } from './features/orme/OrmeProformaPage'
import { DokumaProformaPage } from './features/dokuma/DokumaProformaPage'
import { AssortmentTemplatesPage } from './features/shipping/AssortmentTemplatesPage'
import { ShipmentCartonLabelsPage, ShipmentDespatchNotePage, ShipmentPackingListPage } from './features/shipping/ShipmentPrintPages'
import { CuttingPlanDetailPage } from './features/cutting/CuttingPlanDetailPage'
import { CuttingPlansPage } from './features/cutting/CuttingPlansPage'
import { CuttingSheetPage } from './features/cutting/CuttingSheetPage'
import { OrmeCapacityPlanPage } from './features/orme/OrmeCapacityPlanPage'
import { OrmePurchaseOrderDetailPage, OrmePurchaseOrderPrintPage, OrmePurchaseOrdersPage, OrmeYarnPlanPage } from './features/orme/OrmePurchasingPages'
import { ColorCardDetailPage, ColorCardsPage } from './features/boyahane/ColorCardPages'
import { PatternDetailPage, PatternsPage } from './features/boyahane/PatternPages'
import { StrippingRecipesPage } from './features/boyahane/StrippingRecipesPage'
import { DokumaPurchaseOrderDetailPage, DokumaPurchaseOrderPrintPage, DokumaPurchaseOrdersPage, DokumaSupplySuggestionsPage } from './features/dokuma/DokumaPurchasingPages'
import { DokumaMaterialMovementsPage, DokumaMaterialStockPage } from './features/dokuma/DokumaMaterialStockPages'
import { DokumaSizingRecipesPage } from './features/dokuma/DokumaSizingRecipesPage'
import { DokumaSizingDocumentPage } from './features/dokuma/DokumaSizingDocumentPage'
import { OrmeQuotationDetailPage, OrmeQuotationPrintPage, OrmeQuotationsPage } from './features/orme/OrmeQuotationPages'
import { OrmeCostParametersPage } from './features/orme/OrmeCostParametersPage'
import { DokumaQuotationsPage } from './features/dokuma/DokumaQuotationsPage'
import { DokumaQuotationDetailPage } from './features/dokuma/DokumaQuotationDetailPage'
import { DokumaQuotationPrintPage } from './features/dokuma/DokumaQuotationPrintPage'
import { DokumaCostSettingsPage } from './features/dokuma/DokumaCostSettingsPage'
import { DokumaCapacityPlanPage } from './features/dokuma/DokumaCapacityPlanPage'
import { BoyahaneQuotationDetailPage, BoyahaneQuotationPrintPage, BoyahaneQuotationsPage } from './features/boyahane/BoyahaneQuotationPages'
import { BoyahaneCostSettingsPage } from './features/boyahane/BoyahaneCostSettingsPage'
import { SubcontractPricesPage } from './features/subcontract/SubcontractPricesPage'
import { SubcontractSendsPage } from './features/subcontract/SubcontractSendsPage'
import { SubcontractSendDetailPage } from './features/subcontract/SubcontractSendDetailPage'
import { SubcontractSendPrintPage } from './features/subcontract/SubcontractSendPrintPage'
import { SubcontractReturnsPage } from './features/subcontract/SubcontractReturnsPage'
import { QuotationsPage } from './features/quotations/QuotationsPage'
import { QuotationDetailPage } from './features/quotations/QuotationDetailPage'
import { QuotationPrintPage } from './features/quotations/QuotationPrintPage'
import { CostSettingsPage } from './features/quotations/CostSettingsPage'
import { HomePage, NotFoundPage } from './pages/HomePage'
import { Permissions } from './types/permissions'

const guarded = (permission: string, page: ReactNode) => (
  <RequirePermission permission={permission}>{page}</RequirePermission>
)

/** Oturum açıldıktan sonraki ekranlar. Her çalışma sekmesi bu yolları kendi adresiyle çözer. */
const screenRoutes = (
          <>
            <Route index element={<HomePage />} />
            <Route path="dashboard" element={guarded(Permissions.DashboardView, <DashboardPage />)} />
            <Route path="sales-orders" element={guarded(Permissions.SalesOrderView, <SalesOrdersPage />)} />
            <Route path="exchange-rates" element={guarded(Permissions.PricingView, <ExchangeRatesPage />)} />
            <Route path="price-lists" element={guarded(Permissions.PricingView, <PriceListsPage />)} />
            <Route path="customer-discounts" element={guarded(Permissions.PricingView, <CustomerDiscountsPage />)} />
            <Route
              path="sales-orders/new"
              element={guarded(Permissions.SalesOrderManage, guarded(Permissions.SalesOrderViewPrices, <SalesOrderCreatePage />))}
            />
            <Route path="sales-orders/:id" element={guarded(Permissions.SalesOrderView, <SalesOrderDetailPage />)} />
            <Route
              path="sales-orders/:id/edit"
              element={guarded(Permissions.SalesOrderManage, guarded(Permissions.SalesOrderViewPrices, <SalesOrderEditPage />))}
            />
            <Route path="sales-orders/:id/proforma" element={guarded(Permissions.SalesOrderView, <SalesOrderProformaPage />)} />
            <Route
              path="production-orders"
              element={guarded(Permissions.ProductionOrderView, <ProductionOrdersPage />)}
            />
            <Route
              path="production-orders/new"
              element={guarded(Permissions.ProductionOrderCreate, <ProductionOrderCreatePage />)}
            />
            <Route
              path="production-orders/:id"
              element={guarded(Permissions.ProductionOrderView, <ProductionOrderDetailPage />)}
            />
            <Route
              path="production-orders/:id/pick-list"
              element={guarded(Permissions.ProductionOrderView, <PickListPage />)}
            />
            <Route
              path="production-orders/:id/edit"
              element={guarded(Permissions.ProductionOrderUpdate, <ProductionOrderEditPage />)}
            />
            <Route
              path="production-routes"
              element={guarded(Permissions.ProductionRouteView, <ProductionRoutesPage />)}
            />
            <Route
              path="production-routes/new"
              element={guarded(Permissions.ProductionRouteManage, <ProductionRouteCreatePage />)}
            />
            <Route
              path="production-routes/:id"
              element={guarded(Permissions.ProductionRouteView, <ProductionRouteDetailPage />)}
            />
            <Route
              path="production-routes/:id/edit"
              element={guarded(Permissions.ProductionRouteManage, <ProductionRouteEditPage />)}
            />
            <Route
              path="production-tracking"
              element={guarded(Permissions.ProductionView, <ProductionTrackingPage />)}
            />
            <Route path="performance" element={guarded(Permissions.ProductionView, <PerformancePage />)} />
            <Route path="quality" element={guarded(Permissions.QualityView, <QualityPage />)} />
            <Route path="reports" element={guarded(Permissions.ReportView, <ReportsPage />)} />
            <Route path="warehouse-reports" element={guarded(Permissions.ReportWarehouse, <ReportsPage group="Depo" />)} />
            <Route path="planning" element={guarded(Permissions.ProductionLineView, <PlanningPage />)} />
            <Route path="line-balancing" element={guarded(Permissions.ProductionLineView, <LineBalancingPage />)} />
            <Route path="supply-suggestions" element={guarded(Permissions.PurchaseView, <SupplySuggestionsPage />)} />
            <Route path="purchase-orders" element={guarded(Permissions.PurchaseView, <PurchaseOrdersPage />)} />
            <Route path="purchase-orders/:id" element={guarded(Permissions.PurchaseView, <PurchaseOrderDetailPage />)} />
            <Route path="shipments" element={guarded(Permissions.ShipmentView, <ShipmentsPage />)} />
            <Route path="shipments/:id" element={guarded(Permissions.ShipmentView, <ShipmentDetailPage />)} />
            <Route path="shipments/:id/packing-list" element={guarded(Permissions.ShipmentView, <ShipmentPackingListPage />)} />
            <Route path="shipments/:id/carton-labels" element={guarded(Permissions.ShipmentView, <ShipmentCartonLabelsPage />)} />
            <Route path="shipments/:id/despatch-note" element={guarded(Permissions.ShipmentView, <ShipmentDespatchNotePage />)} />
            <Route path="assortment-templates" element={guarded(Permissions.ShipmentView, <AssortmentTemplatesPage />)} />
            <Route path="invoices" element={guarded(Permissions.InvoiceView, <SalesInvoicesPage />)} />
            <Route path="invoices/:id" element={guarded(Permissions.InvoiceView, <SalesInvoiceDetailPage />)} />
            <Route path="invoices/:id/print" element={guarded(Permissions.InvoiceView, <SalesInvoicePrintPage />)} />
            <Route path="payments" element={guarded(Permissions.AccountView, <SalesPaymentsPage />)} />
            <Route path="payments/:id" element={guarded(Permissions.AccountView, <SalesPaymentDetailPage />)} />
            <Route path="accounts" element={guarded(Permissions.AccountView, <SalesAccountsPage />)} />
            <Route path="accounts/:customerId" element={guarded(Permissions.AccountView, <SalesAccountStatementPage />)} />
            <Route path="open-invoices" element={guarded(Permissions.AccountView, <SalesOpenInvoicesPage />)} />
            <Route path="opening-balances" element={guarded(Permissions.AccountView, <SalesOpeningBalancesPage />)} />
            <Route path="stock" element={guarded(Permissions.StockView, <StockPage />)} />
            <Route path="stock-lots" element={guarded(Permissions.StockView, <StockLotsPage />)} />
            <Route path="stock-lots/labels" element={guarded(Permissions.StockView, <LotLabelPage />)} />
            <Route path="stock-counts" element={guarded(Permissions.StockView, <StockCountsPage />)} />
            <Route path="stock-counts/:id" element={guarded(Permissions.StockView, <StockCountDetailPage />)} />
            <Route path="terminal" element={guarded(Permissions.StockView, <TerminalPage />)} />
            <Route path="location-labels" element={guarded(Permissions.StockView, <LocationLabelPage />)} />
            <Route path="stock-transactions" element={guarded(Permissions.StockView, <StockTransactionsPage />)} />
            <Route path="production-receipts" element={guarded(Permissions.StockView, <ProductionReceiptPage />)} />
            <Route path="bundles" element={guarded(Permissions.BundleView, <BundlesPage />)} />
            <Route path="quotations" element={guarded(Permissions.QuotationView, <QuotationsPage />)} />
            <Route path="quotations/:id" element={guarded(Permissions.QuotationView, <QuotationDetailPage />)} />
            <Route path="quotations/:id/print" element={guarded(Permissions.QuotationView, <QuotationPrintPage />)} />
            <Route path="cost-settings" element={guarded(Permissions.QuotationView, <CostSettingsPage />)} />
            <Route path="subcontract/sends" element={guarded(Permissions.SubcontractView, <SubcontractSendsPage />)} />
            <Route path="subcontract/sends/:id" element={guarded(Permissions.SubcontractView, <SubcontractSendDetailPage />)} />
            <Route path="subcontract/sends/:id/print" element={guarded(Permissions.SubcontractView, <SubcontractSendPrintPage />)} />
            <Route path="subcontract/returns" element={guarded(Permissions.SubcontractView, <SubcontractReturnsPage />)} />
            <Route path="subcontract/prices" element={guarded(Permissions.SubcontractView, <SubcontractPricesPage />)} />
            <Route path="cutting-plans" element={guarded(Permissions.CuttingPlanView, <CuttingPlansPage />)} />
            <Route path="cutting-plans/:id" element={guarded(Permissions.CuttingPlanView, <CuttingPlanDetailPage />)} />
            <Route path="cutting-plans/:id/sheet" element={guarded(Permissions.CuttingPlanView, <CuttingSheetPage />)} />
            <Route path="bundles/labels" element={guarded(Permissions.BundleView, <LabelPrintPage />)} />
            <Route path="bundles/card-release" element={guarded(Permissions.BundleReleaseCard, <CardReleasePage />)} />
            <Route path="bundles/:id" element={guarded(Permissions.BundleView, <BundleDetailPage />)} />
            <Route path="payroll" element={guarded(Permissions.PayrollView, <PayrollPage />)} />
            <Route path="setup" element={guarded(Permissions.MasterDataView, <SetupGuidePage />)} />
            <Route path="boyahane/yarn-lots" element={guarded(Permissions.BoyahaneReceiptView, <YarnLotsPage />)} />
            <Route path="boyahane/packing-lists" element={guarded(Permissions.BoyahanePackingView, <PackingListsPage />)} />
            <Route path="boyahane/packing-lists/:id" element={guarded(Permissions.BoyahanePackingView, <PackingListDetailPage />)} />
            <Route path="boyahane/packing-lists/:id/print" element={guarded(Permissions.BoyahanePackingView, <PackingListPrintPage />)} />
            <Route path="boyahane/packing-lists/:id/labels" element={guarded(Permissions.BoyahanePackingView, <PackingLabelPage />)} />
            <Route path="boyahane/despatches" element={guarded(Permissions.BoyahaneShipmentView, <DespatchesPage />)} />
            <Route path="boyahane/despatches/:id" element={guarded(Permissions.BoyahaneShipmentView, <DespatchDetailPage />)} />
            <Route path="boyahane/despatches/:id/print" element={guarded(Permissions.BoyahaneShipmentView, <DespatchPrintPage />)} />
            <Route path="boyahane/invoices" element={guarded(Permissions.BoyahaneInvoiceView, <InvoicesPage />)} />
            <Route path="boyahane/invoices/:id" element={guarded(Permissions.BoyahaneInvoiceView, <InvoiceDetailPage />)} />
            <Route path="boyahane/invoices/:id/print" element={guarded(Permissions.BoyahaneInvoiceView, <InvoicePrintPage />)} />
            <Route path="boyahane/chemical-stock" element={guarded(Permissions.BoyahaneChemicalStockView, <ChemicalStockPage />)} />
            <Route path="boyahane/chemical-lots" element={guarded(Permissions.BoyahaneChemicalStockView, <ChemicalLotsPage />)} />
            <Route path="boyahane/chemical-movements" element={guarded(Permissions.BoyahaneChemicalStockView, <ChemicalMovementsPage />)} />
            <Route path="boyahane/purchase-orders" element={guarded(Permissions.BoyahanePurchaseView, <BoyahanePurchaseOrdersPage />)} />
            <Route path="boyahane/purchase-orders/:id" element={guarded(Permissions.BoyahanePurchaseView, <BoyahanePurchaseOrderDetailPage />)} />
            <Route path="boyahane/supply-suggestions" element={guarded(Permissions.BoyahanePurchaseView, <BoyahaneSupplySuggestionsPage />)} />
            <Route path="boyahane/recipes" element={guarded(Permissions.BoyahaneLabView, <RecipesPage />)} />
            <Route path="boyahane/color-cards" element={guarded(Permissions.BoyahaneLabView, <ColorCardsPage />)} />
            <Route path="boyahane/color-cards/:id" element={guarded(Permissions.BoyahaneLabView, <ColorCardDetailPage />)} />
            <Route path="boyahane/patterns" element={guarded(Permissions.BoyahaneLabView, <PatternsPage />)} />
            <Route path="boyahane/patterns/:id" element={guarded(Permissions.BoyahaneLabView, <PatternDetailPage />)} />
            <Route path="boyahane/stripping-recipes" element={guarded(Permissions.BoyahaneLabView, <StrippingRecipesPage />)} />
            <Route path="boyahane/warehouse-goods" element={guarded(Permissions.BoyahaneChemicalStockView, <WarehouseGoodsPage />)} />
            <Route path="boyahane/location-movements" element={guarded(Permissions.BoyahaneChemicalStockView, <LocationMovementsPage />)} />
            <Route path="boyahane/location-labels" element={guarded(Permissions.BoyahaneChemicalStockView, <BoyahaneLocationLabelPage />)} />
            <Route path="boyahane/opening-balances" element={guarded(Permissions.BoyahaneAccountView, <OpeningBalancesPage />)} />
            <Route path="boyahane/goods-returns" element={guarded(Permissions.BoyahaneShipmentView, <GoodsReturnsPage />)} />
            <Route path="boyahane/goods-returns/:id" element={guarded(Permissions.BoyahaneShipmentView, <GoodsReturnDetailPage />)} />
            <Route path="boyahane/goods-returns/:id/print" element={guarded(Permissions.BoyahaneShipmentView, <GoodsReturnPrintPage />)} />
            <Route path="boyahane/payments" element={guarded(Permissions.BoyahaneAccountView, <PaymentsPage />)} />
            <Route path="boyahane/payments/:id" element={guarded(Permissions.BoyahaneAccountView, <PaymentDetailPage />)} />
            <Route path="boyahane/accounts" element={guarded(Permissions.BoyahaneAccountView, <AccountsPage />)} />
            <Route path="boyahane/accounts/:customerId" element={guarded(Permissions.BoyahaneAccountView, <AccountStatementPage />)} />
            <Route path="boyahane/open-invoices" element={guarded(Permissions.BoyahaneAccountView, <OpenInvoicesPage />)} />
            <Route path="boyahane/dashboard" element={guarded(Permissions.BoyahaneBatchView, <BoyahaneDashboardPage />)} />
            <Route path="boyahane/setup" element={guarded(Permissions.BoyahaneView, <BoyahaneSetupPage />)} />
            <Route path="boyahane/order-tracking" element={guarded(Permissions.BoyahaneOrderView, <BoyahaneOrderTrackingPage />)} />
            <Route path="boyahane/goods" element={guarded(Permissions.BoyahaneReceiptView, <BoyahaneGoodsPage />)} />
            <Route path="boyahane/planning" element={guarded(Permissions.BoyahaneBatchView, <BoyahanePlanningPage />)} />
            <Route path="boyahane/routes" element={guarded(Permissions.BoyahaneOperationView, <BoyahaneRoutesPage />)} />
            <Route path="boyahane/tracking" element={guarded(Permissions.BoyahaneBatchView, <BoyahaneTrackingPage />)} />
            <Route path="boyahane/card-release" element={guarded(Permissions.BoyahaneBatchManage, <BoyahaneCardReleasePage />)} />
            <Route path="boyahane/reports" element={guarded(Permissions.BoyahaneReportView, <BoyahaneReportsPage />)} />
            <Route path="boyahane/work-sessions" element={guarded(Permissions.BoyahaneFloorView, <BoyahaneWorkSessionsPage />)} />
            <Route path="boyahane/batches" element={guarded(Permissions.BoyahaneBatchView, <BatchesPage />)} />
            <Route path="boyahane/batches/:id" element={guarded(Permissions.BoyahaneBatchView, <BatchDetailPage />)} />
            <Route path="boyahane/batches/:id/card" element={guarded(Permissions.BoyahaneBatchView, <BatchCardPage />)} />
            <Route path="boyahane/lab" element={guarded(Permissions.BoyahaneLabView, <LabRequestsPage />)} />
            <Route path="boyahane/lab/:id" element={guarded(Permissions.BoyahaneLabView, <LabRequestDetailPage />)} />
            <Route path="boyahane/orders" element={guarded(Permissions.BoyahaneOrderView, <BoyahaneOrdersPage />)} />
            <Route path="boyahane/orders/:id" element={guarded(Permissions.BoyahaneOrderView, <BoyahaneOrderDetailPage />)} />
            <Route path="boyahane/quotations" element={guarded(Permissions.BoyahaneQuotationView, <BoyahaneQuotationsPage />)} />
            <Route path="boyahane/quotations/:id" element={guarded(Permissions.BoyahaneQuotationView, <BoyahaneQuotationDetailPage />)} />
            <Route path="boyahane/quotations/:id/print" element={guarded(Permissions.BoyahaneQuotationView, <BoyahaneQuotationPrintPage />)} />
            <Route path="boyahane/cost-settings" element={guarded(Permissions.BoyahaneQuotationView, <BoyahaneCostSettingsPage />)} />
            <Route path="boyahane/orders/:id/proforma" element={guarded(Permissions.BoyahaneOrderView, <BoyahaneProformaPage />)} />
            <Route path="boyahane/inspection" element={guarded(Permissions.BoyahaneInspectionView, <FabricInspectionPage />)} />
            <Route path="boyahane/receipts" element={guarded(Permissions.BoyahaneReceiptView, <FabricReceiptsPage />)} />
            <Route path="boyahane/receipts/:id" element={guarded(Permissions.BoyahaneReceiptView, <FabricReceiptDetailPage />)} />
            <Route path="boyahane/receipts/:id/labels" element={guarded(Permissions.BoyahaneReceiptView, <FabricRollLabelPage />)} />
            <Route path="boyahane" element={<Navigate to="/boyahane/dashboard" replace />} />
            <Route path="dokuma" element={<Navigate to="/dokuma/dashboard" replace />} />
            <Route path="dokuma/dashboard" element={guarded(Permissions.DokumaView, <DokumaDashboardPage />)} />
            <Route path="orme" element={<Navigate to="/orme/dashboard" replace />} />
            <Route path="orme/dashboard" element={guarded(Permissions.OrmeView, <OrmeDashboardPage />)} />
            <Route path="orme/orders" element={guarded(Permissions.OrmeOrderView, <OrmeOrdersPage />)} />
            <Route path="orme/orders/:id" element={guarded(Permissions.OrmeOrderView, <OrmeOrderDetailPage />)} />
            <Route path="orme/quotations" element={guarded(Permissions.OrmeQuotationView, <OrmeQuotationsPage />)} />
            <Route path="orme/quotations/:id" element={guarded(Permissions.OrmeQuotationView, <OrmeQuotationDetailPage />)} />
            <Route path="orme/quotations/:id/print" element={guarded(Permissions.OrmeQuotationView, <OrmeQuotationPrintPage />)} />
            <Route path="orme/cost-parameters" element={guarded(Permissions.OrmeQuotationView, <OrmeCostParametersPage />)} />
            <Route path="orme/orders/:id/proforma" element={guarded(Permissions.OrmeOrderView, <OrmeProformaPage />)} />
            <Route path="orme/purchase-orders" element={guarded(Permissions.OrmePurchaseView, <OrmePurchaseOrdersPage />)} />
            <Route path="orme/purchase-orders/:id" element={guarded(Permissions.OrmePurchaseView, <OrmePurchaseOrderDetailPage />)} />
            <Route path="orme/purchase-orders/:id/print" element={guarded(Permissions.OrmePurchaseView, <OrmePurchaseOrderPrintPage />)} />
            <Route path="orme/yarn-plan" element={guarded(Permissions.OrmePurchaseView, <OrmeYarnPlanPage />)} />
            <Route path="orme/capacity-plan" element={guarded(Permissions.OrmePlanningView, <OrmeCapacityPlanPage />)} />
            <Route path="orme/yarn-lots" element={guarded(Permissions.OrmeReceiptView, <OrmeYarnLotsPage />)} />
            <Route path="orme/knit-orders" element={guarded(Permissions.OrmeKnittingView, <OrmeKnitOrdersPage />)} />
            <Route path="orme/knit-orders/:id" element={guarded(Permissions.OrmeKnittingView, <OrmeKnitOrderDetailPage />)} />
            <Route path="orme/knit-orders/:id/labels" element={guarded(Permissions.OrmeKnittingView, <OrmeRollLabelPage />)} />
            <Route path="orme/machine-board" element={guarded(Permissions.OrmeKnittingView, <OrmeMachineBoardPage />)} />
            <Route path="orme/rolls" element={guarded(Permissions.OrmeKnittingView, <OrmeRollsPage />)} />
            <Route path="orme/inspection" element={guarded(Permissions.OrmeInspectionView, <OrmeInspectionPage />)} />
            <Route path="orme/machine-stops" element={guarded(Permissions.OrmeKnittingView, <OrmeMachineStopsPage />)} />
            <Route path="orme/work-sessions" element={guarded(Permissions.OrmeFloorView, <OrmeWorkSessionsPage />)} />
            <Route path="orme/reports" element={guarded(Permissions.OrmeReportView, <OrmeReportsPage />)} />
            <Route path="orme/yarn-returns" element={guarded(Permissions.OrmeReceiptView, <OrmeYarnReturnsPage />)} />
            <Route path="orme/packing-lists" element={guarded(Permissions.OrmePackingView, <OrmePackingListsPage />)} />
            <Route path="orme/packing-lists/:id" element={guarded(Permissions.OrmePackingView, <OrmePackingListDetailPage />)} />
            <Route path="orme/packing-lists/:id/print" element={guarded(Permissions.OrmePackingView, <OrmePackingListPrintPage />)} />
            <Route path="orme/despatches" element={guarded(Permissions.OrmeShipmentView, <OrmeDespatchesPage />)} />
            <Route path="orme/despatches/:id" element={guarded(Permissions.OrmeShipmentView, <OrmeDespatchDetailPage />)} />
            <Route path="orme/despatches/:id/print" element={guarded(Permissions.OrmeShipmentView, <OrmeDespatchPrintPage />)} />
            <Route path="orme/invoices" element={guarded(Permissions.OrmeInvoiceView, <OrmeInvoicesPage />)} />
            <Route path="orme/invoices/:id" element={guarded(Permissions.OrmeInvoiceView, <OrmeInvoiceDetailPage />)} />
            <Route path="orme/invoices/:id/print" element={guarded(Permissions.OrmeInvoiceView, <OrmeInvoicePrintPage />)} />
            <Route path="orme/payments" element={guarded(Permissions.OrmeAccountView, <OrmePaymentsPage />)} />
            <Route path="orme/payments/:id" element={guarded(Permissions.OrmeAccountView, <OrmePaymentDetailPage />)} />
            <Route path="orme/accounts" element={guarded(Permissions.OrmeAccountView, <OrmeAccountsPage />)} />
            <Route path="orme/accounts/:customerId" element={guarded(Permissions.OrmeAccountView, <OrmeAccountStatementPage />)} />
            <Route path="orme/open-invoices" element={guarded(Permissions.OrmeAccountView, <OrmeOpenInvoicesPage />)} />
            <Route path="orme/opening-balances" element={guarded(Permissions.OrmeAccountView, <OrmeOpeningBalancesPage />)} />
            <Route path="dokuma/orders" element={guarded(Permissions.DokumaOrderView, <DokumaOrdersPage />)} />
            <Route path="dokuma/orders/:id" element={guarded(Permissions.DokumaOrderView, <DokumaOrderDetailPage />)} />
            <Route path="dokuma/quotations" element={guarded(Permissions.DokumaQuotationView, <DokumaQuotationsPage />)} />
            <Route path="dokuma/quotations/:id" element={guarded(Permissions.DokumaQuotationView, <DokumaQuotationDetailPage />)} />
            <Route path="dokuma/quotations/:id/print" element={guarded(Permissions.DokumaQuotationView, <DokumaQuotationPrintPage />)} />
            <Route path="dokuma/cost-settings" element={guarded(Permissions.DokumaQuotationView, <DokumaCostSettingsPage />)} />
            <Route path="dokuma/capacity-plan" element={guarded(Permissions.DokumaPlanningView, <DokumaCapacityPlanPage />)} />
            <Route path="dokuma/orders/:id/proforma" element={guarded(Permissions.DokumaOrderView, <DokumaProformaPage />)} />
            <Route path="dokuma/yarn-lots" element={guarded(Permissions.DokumaReceiptView, <DokumaYarnLotsPage />)} />
            <Route path="dokuma/warp-orders" element={guarded(Permissions.DokumaWarpView, <DokumaWarpOrdersPage />)} />
            <Route path="dokuma/warp-orders/:id" element={guarded(Permissions.DokumaWarpView, <DokumaWarpOrderDetailPage />)} />
            <Route path="dokuma/purchase-orders" element={guarded(Permissions.DokumaPurchaseOrderView, <DokumaPurchaseOrdersPage />)} />
            <Route path="dokuma/purchase-orders/:id" element={guarded(Permissions.DokumaPurchaseOrderView, <DokumaPurchaseOrderDetailPage />)} />
            <Route path="dokuma/purchase-orders/:id/print" element={guarded(Permissions.DokumaPurchaseOrderView, <DokumaPurchaseOrderPrintPage />)} />
            <Route path="dokuma/supply-suggestions" element={guarded(Permissions.DokumaPurchaseOrderView, <DokumaSupplySuggestionsPage />)} />
            <Route path="dokuma/material-stock" element={guarded(Permissions.DokumaStockView, <DokumaMaterialStockPage />)} />
            <Route path="dokuma/material-movements" element={guarded(Permissions.DokumaStockView, <DokumaMaterialMovementsPage />)} />
            <Route path="dokuma/sizing-recipes" element={guarded(Permissions.DokumaMaterialView, <DokumaSizingRecipesPage />)} />
            <Route path="dokuma/beams/:id/sizing-document" element={guarded(Permissions.DokumaWarpView, <DokumaSizingDocumentPage />)} />
            <Route path="dokuma/beams" element={guarded(Permissions.DokumaWarpView, <DokumaBeamsPage />)} />
            <Route path="dokuma/weaving" element={guarded(Permissions.DokumaWeavingView, <DokumaLoomBoardPage />)} />
            <Route path="dokuma/weaving/beams/:id" element={guarded(Permissions.DokumaWeavingView, <DokumaBeamWeavingPage />)} />
            <Route path="dokuma/weaving/beams/:id/labels" element={guarded(Permissions.DokumaWeavingView, <DokumaRollLabelPage />)} />
            <Route path="dokuma/rolls" element={guarded(Permissions.DokumaWeavingView, <DokumaRollsPage />)} />
            <Route path="dokuma/inspection" element={guarded(Permissions.DokumaInspectionView, <DokumaInspectionPage />)} />
            <Route path="dokuma/loom-stops" element={guarded(Permissions.DokumaWeavingView, <DokumaLoomStopsPage />)} />
            <Route path="dokuma/work-sessions" element={guarded(Permissions.DokumaFloorView, <DokumaWorkSessionsPage />)} />
            <Route path="dokuma/reports" element={guarded(Permissions.DokumaReportView, <DokumaReportsPage />)} />
            <Route path="dokuma/yarn-returns" element={guarded(Permissions.DokumaReceiptView, <DokumaYarnReturnsPage />)} />
            <Route path="dokuma/packing-lists" element={guarded(Permissions.DokumaPackingView, <DokumaPackingListsPage />)} />
            <Route path="dokuma/packing-lists/:id" element={guarded(Permissions.DokumaPackingView, <DokumaPackingListDetailPage />)} />
            <Route path="dokuma/packing-lists/:id/print" element={guarded(Permissions.DokumaPackingView, <DokumaPackingListPrintPage />)} />
            <Route path="dokuma/despatches" element={guarded(Permissions.DokumaShipmentView, <DokumaDespatchesPage />)} />
            <Route path="dokuma/despatches/:id" element={guarded(Permissions.DokumaShipmentView, <DokumaDespatchDetailPage />)} />
            <Route path="dokuma/despatches/:id/print" element={guarded(Permissions.DokumaShipmentView, <DokumaDespatchPrintPage />)} />
            <Route path="dokuma/invoices" element={guarded(Permissions.DokumaInvoiceView, <DokumaInvoicesPage />)} />
            <Route path="dokuma/invoices/:id" element={guarded(Permissions.DokumaInvoiceView, <DokumaInvoiceDetailPage />)} />
            <Route path="dokuma/invoices/:id/print" element={guarded(Permissions.DokumaInvoiceView, <DokumaInvoicePrintPage />)} />
            <Route path="dokuma/payments" element={guarded(Permissions.DokumaAccountView, <DokumaPaymentsPage />)} />
            <Route path="dokuma/payments/:id" element={guarded(Permissions.DokumaAccountView, <DokumaPaymentDetailPage />)} />
            <Route path="dokuma/accounts" element={guarded(Permissions.DokumaAccountView, <DokumaAccountsPage />)} />
            <Route path="dokuma/accounts/:customerId" element={guarded(Permissions.DokumaAccountView, <DokumaAccountStatementPage />)} />
            <Route path="dokuma/open-invoices" element={guarded(Permissions.DokumaAccountView, <DokumaOpenInvoicesPage />)} />
            <Route path="dokuma/opening-balances" element={guarded(Permissions.DokumaAccountView, <DokumaOpeningBalancesPage />)} />
            <Route
              path="production-lines/:id"
              element={guarded(Permissions.ProductionLineView, <LineDetailPage />)}
            />
            {resources.map((resource) => (
              <Route
                key={resource.key}
                path={resource.key}
                // key: kaynak değişince arama / sayfa durumu sıfırlansın.
                element={guarded(resource.viewPermission, <MasterDataPage key={resource.key} config={resource} />)}
              />
            ))}
            <Route path="work-sessions" element={guarded(Permissions.OperatorView, <WorkSessionsPage />)} />
            <Route path="users" element={guarded(Permissions.UserView, <UsersPage />)} />
            <Route path="roles" element={guarded(Permissions.RoleView, <RolesPage />)} />
            <Route path="approval-flows" element={guarded(Permissions.RoleView, <ApprovalFlowPage />)} />
            <Route path="audit-logs" element={guarded(Permissions.AuditLogView, <AuditLogsPage />)} />
            <Route path="integrations" element={guarded(Permissions.IntegrationView, <IntegrationsPage />)} />
            <Route path="account/password" element={<ChangePasswordPage />} />
            <Route path="*" element={<NotFoundPage />} />
          </>
)

export function App() {
  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route path="/forgot-password" element={<ForgotPasswordPage />} />
        <Route path="/reset-password" element={<ResetPasswordPage />} />
        <Route element={<RequireAuth />}>
          {/* El cihazı görünümü: menü ve sekmeler olmadan yalnızca depo terminali. */}
          <Route path="/handheld" element={<HandheldPage />} />
          <Route path="*" element={<AppLayout routes={screenRoutes} />} />
        </Route>
      </Routes>
    </BrowserRouter>
  )
}
