namespace ERP.Core.Application.Common;

/// <summary>API'nin döndüğü business error code'ları.</summary>
public static class ErrorCodes
{
    public const string ValidationError = "VALIDATION_ERROR";
    public const string Unauthorized = "UNAUTHORIZED";
    public const string Forbidden = "FORBIDDEN";
    public const string NotFound = "NOT_FOUND";
    public const string RateLimited = "RATE_LIMITED";
    public const string InternalError = "INTERNAL_ERROR";

    public const string InvalidCredentials = "INVALID_CREDENTIALS";
    public const string AccountLocked = "ACCOUNT_LOCKED";
    public const string AccountInactive = "ACCOUNT_INACTIVE";
    public const string InvalidRefreshToken = "INVALID_REFRESH_TOKEN";
    public const string InvalidCurrentPassword = "INVALID_CURRENT_PASSWORD";

    public const string UserNotFound = "USER_NOT_FOUND";
    public const string UserNameAlreadyExists = "USER_NAME_ALREADY_EXISTS";
    public const string CannotModifySelf = "CANNOT_MODIFY_SELF";
    public const string LastAdminRequired = "LAST_ADMIN_REQUIRED";

    public const string CodeAlreadyExists = "CODE_ALREADY_EXISTS";
    public const string InvalidReference = "INVALID_REFERENCE";
    public const string RecordInUse = "RECORD_IN_USE";
    public const string MachineTypeMismatch = "MACHINE_TYPE_MISMATCH";

    public const string ConcurrencyConflict = "CONCURRENCY_CONFLICT";
    public const string OrderNotEditable = "ORDER_NOT_EDITABLE";
    public const string OrderHasNoItems = "ORDER_HAS_NO_ITEMS";
    public const string InvalidStatusTransition = "INVALID_STATUS_TRANSITION";
    public const string ProductCustomerMismatch = "PRODUCT_CUSTOMER_MISMATCH";

    public const string RouteNotEditable = "ROUTE_NOT_EDITABLE";
    public const string RouteHasNoOperations = "ROUTE_HAS_NO_OPERATIONS";
    public const string RouteDependencyCycle = "ROUTE_DEPENDENCY_CYCLE";
    public const string RouteInvalidDependency = "ROUTE_INVALID_DEPENDENCY";
    public const string RouteHasInactiveOperation = "ROUTE_HAS_INACTIVE_OPERATION";

    public const string MachineAlreadyAssigned = "MACHINE_ALREADY_ASSIGNED";
    public const string MachineAlreadyOnLine = "MACHINE_ALREADY_ON_LINE";
    public const string LinePlanOverlap = "LINE_PLAN_OVERLAP";
    public const string LinePlanNotEditable = "LINE_PLAN_NOT_EDITABLE";
    public const string OrderNotPlannable = "ORDER_NOT_PLANNABLE";
    public const string PlanExceedsOrderQuantity = "PLAN_EXCEEDS_ORDER_QUANTITY";
    public const string OrderHasLinePlans = "ORDER_HAS_LINE_PLANS";
    public const string ResourceBusy = "RESOURCE_BUSY";

    public const string InvalidPairingPassword = "INVALID_PAIRING_PASSWORD";
    public const string InvalidDeviceCredentials = "INVALID_DEVICE_CREDENTIALS";
    public const string TabletInactive = "TABLET_INACTIVE";
    public const string TabletAlreadyEnrolled = "TABLET_ALREADY_ENROLLED";
    public const string TabletNotAssigned = "TABLET_NOT_ASSIGNED";
    public const string MachineAlreadyHasTablet = "MACHINE_ALREADY_HAS_TABLET";

    public const string RouteRequired = "ROUTE_REQUIRED";
    public const string OrderNotBundleable = "ORDER_NOT_BUNDLEABLE";
    public const string BundleExceedsOrderQuantity = "BUNDLE_EXCEEDS_ORDER_QUANTITY";
    public const string NothingToBundle = "NOTHING_TO_BUNDLE";
    public const string BundleNotCancellable = "BUNDLE_NOT_CANCELLABLE";
    public const string OrderHasBundles = "ORDER_HAS_BUNDLES";
    public const string OrderHasOpenBundles = "ORDER_HAS_OPEN_BUNDLES";

    public const string InvalidRfidUid = "INVALID_RFID_UID";
    public const string RfidNotRegistered = "RFID_NOT_REGISTERED";
    public const string RfidCardInactive = "RFID_CARD_INACTIVE";
    public const string RfidCardTypeMismatch = "RFID_CARD_TYPE_MISMATCH";
    public const string RfidCardNotAssigned = "RFID_CARD_NOT_ASSIGNED";
    public const string RfidCardAlreadyAssigned = "RFID_CARD_ALREADY_ASSIGNED";
    public const string BundleAlreadyHasCard = "BUNDLE_ALREADY_HAS_CARD";
    public const string OperatorAlreadyHasCard = "OPERATOR_ALREADY_HAS_CARD";
    public const string BundleNotActive = "BUNDLE_NOT_ACTIVE";
    public const string OperatorInactive = "OPERATOR_INACTIVE";
    public const string MachineInactive = "MACHINE_INACTIVE";
    public const string MachineNotAvailable = "MACHINE_NOT_AVAILABLE";
    public const string MachineHasNoOperation = "MACHINE_HAS_NO_OPERATION";
    public const string MachineBusy = "MACHINE_BUSY";
    public const string OperatorBusy = "OPERATOR_BUSY";
    public const string OperationMachineMismatch = "OPERATION_MACHINE_MISMATCH";
    public const string OrderNotInProduction = "ORDER_NOT_IN_PRODUCTION";
    public const string BundleNotReleased = "BUNDLE_NOT_RELEASED";
    public const string BundleOnQualityHold = "BUNDLE_ON_QUALITY_HOLD";
    public const string BundleInUse = "BUNDLE_IN_USE";
    public const string PredecessorsNotCompleted = "PREDECESSORS_NOT_COMPLETED";
    public const string OperationUnfinished = "OPERATION_UNFINISHED";
    public const string OperationAlreadyCompleted = "OPERATION_ALREADY_COMPLETED";
    public const string OperationCancelled = "OPERATION_CANCELLED";
    public const string OperationNotInProgress = "OPERATION_NOT_IN_PROGRESS";
    public const string OperationOnAnotherMachine = "OPERATION_ON_ANOTHER_MACHINE";
    public const string QuantityExceedsRemaining = "QUANTITY_EXCEEDS_REMAINING";
    public const string RequestIdReused = "REQUEST_ID_REUSED";
    public const string NoActiveSession = "NO_ACTIVE_SESSION";
    public const string SessionNotActive = "SESSION_NOT_ACTIVE";
    public const string OperatorNotQualified = "OPERATOR_NOT_QUALIFIED";
    public const string WorkInProgress = "WORK_IN_PROGRESS";
    public const string BreakOutsideShift = "BREAK_OUTSIDE_SHIFT";
    public const string NoBreakDue = "NO_BREAK_DUE";
    public const string BreakAfterWork = "BREAK_AFTER_WORK";
    public const string SessionInterrupted = "SESSION_INTERRUPTED";
    public const string AuthorizationCardRequired = "AUTHORIZATION_CARD_REQUIRED";
    public const string NotAuthorizedForAction = "NOT_AUTHORIZED_FOR_ACTION";
    public const string QualityQuantityInvalid = "QUALITY_QUANTITY_INVALID";
    public const string QualityConsumedDownstream = "QUALITY_CONSUMED_DOWNSTREAM";
    public const string ReworkNotDone = "REWORK_NOT_DONE";
    public const string BundleNotOnHold = "BUNDLE_NOT_ON_HOLD";

    public const string InsufficientStock = "INSUFFICIENT_STOCK";
    public const string StockQuantityInvalid = "STOCK_QUANTITY_INVALID";
    public const string StockNoChange = "STOCK_NO_CHANGE";
    public const string StockItemSystemManaged = "STOCK_ITEM_SYSTEM_MANAGED";
    public const string StockItemUnitLocked = "STOCK_ITEM_UNIT_LOCKED";
    public const string BundleNotCompleted = "BUNDLE_NOT_COMPLETED";
    public const string BundleAlreadyReceived = "BUNDLE_ALREADY_RECEIVED";
    public const string NothingToReceive = "NOTHING_TO_RECEIVE";

    public const string WarehouseLocationLocked = "WAREHOUSE_LOCATION_LOCKED";
    public const string StockCountInProgress = "STOCK_COUNT_IN_PROGRESS";
    public const string StockCountNotOpen = "STOCK_COUNT_NOT_OPEN";
    public const string StockCountEmpty = "STOCK_COUNT_EMPTY";
    public const string StockCountLineExists = "STOCK_COUNT_LINE_EXISTS";
    public const string StockLotRequired = "STOCK_LOT_REQUIRED";
    public const string StockLotNotAllowed = "STOCK_LOT_NOT_ALLOWED";
    public const string StockItemLotTrackingLocked = "STOCK_ITEM_LOT_TRACKING_LOCKED";

    public const string MaterialWarehouseRequired = "MATERIAL_WAREHOUSE_REQUIRED";
    public const string OrderHasNoRecipe = "ORDER_HAS_NO_RECIPE";
    public const string MaterialNotAllowed = "MATERIAL_NOT_ALLOWED";

    public const string ScanAmbiguous = "SCAN_AMBIGUOUS";
    public const string SalesOrderHasProduction = "SALES_ORDER_HAS_PRODUCTION";
    public const string SupplySupplierRequired = "SUPPLY_SUPPLIER_REQUIRED";
    public const string PurchaseOrderNotEditable = "PURCHASE_ORDER_NOT_EDITABLE";
    public const string PurchaseOrderNotReceivable = "PURCHASE_ORDER_NOT_RECEIVABLE";
    public const string ReceiptExceedsOrdered = "RECEIPT_EXCEEDS_ORDERED";
    public const string ExchangeRateRequired = "EXCHANGE_RATE_REQUIRED";

    public const string ShipmentNotEditable = "SHIPMENT_NOT_EDITABLE";
    public const string ShipmentHasNoItems = "SHIPMENT_HAS_NO_ITEMS";
    public const string ShipmentItemNotFinishedGood = "SHIPMENT_ITEM_NOT_FINISHED_GOOD";
    public const string ShipmentOrderMismatch = "SHIPMENT_ORDER_MISMATCH";
    public const string ShipmentExceedsOrder = "SHIPMENT_EXCEEDS_ORDER";
    public const string OrderLinkedToSalesOrder = "ORDER_LINKED_TO_SALES_ORDER";
    public const string SalesOrderHasShipments = "SALES_ORDER_HAS_SHIPMENTS";
    public const string SalesOrderNothingDelivered = "SALES_ORDER_NOTHING_DELIVERED";
    public const string DuplicateCustomerOrderNo = "DUPLICATE_CUSTOMER_ORDER_NO";
    public const string ExchangeRateSourceUnavailable = "EXCHANGE_RATE_SOURCE_UNAVAILABLE";
    public const string DiscountOverlap = "DISCOUNT_OVERLAP";
    public const string NotYourApproval = "NOT_YOUR_APPROVAL";
    public const string EmailNotConfigured = "EMAIL_NOT_CONFIGURED";
    public const string EmailSendFailed = "EMAIL_SEND_FAILED";
    public const string InvalidResetToken = "INVALID_RESET_TOKEN";

    public const string IntegrationDeliveryPending = "INTEGRATION_DELIVERY_PENDING";

    public const string RoleNotFound = "ROLE_NOT_FOUND";
    public const string RoleNotEditable = "ROLE_NOT_EDITABLE";
}
