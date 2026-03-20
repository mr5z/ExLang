# ExLang Showcase

A fictional e-commerce payment system demonstrating all language features in a single cohesive example.

---

## DTOs

```
dto Address {
    street: String;
    city: String;
    country: String;
    postalCode: String;
}

dto OrderItem {
    productId: u32;
    name: String;
    quantity: u32;
    unitPrice: f32;
}

dto OrderRequest {
    userId: u32;
    items: List<OrderItem>;
    shippingAddress: Address;
}

dto OrderResponse {
    orderId: u32;
    status: String;
    total: f32;
}
```

---

## Contracts

```
contract Logger {
    log(message: String);
    warn(message: String);
}

contract PaymentGateway {
    charge(amount: Money, reference: String): Result;
    refund(reference: String): Result;
}

contract OrderRepository {
    save(order: OrderRequest): u32;
    findById(id: u32): OrderRequest?;
    findByStatus(status: OrderStatus): List<OrderRequest>;
}

contract Notifier {
    notify(userId: u32, message: String);
}

contract ReportGenerator {
    generate(orders: List<OrderRequest>): Report;
}
```

---

## Objects

### Money

```
object Money {
    _amount: f32;
    _currency: String;

    amount: f32 {
        get => _amount;
    }

    currency: String {
        get => _currency;
    }

    @Const
    @Exposed
    add(other: Money): Money {
        // ...
    }

    @Const
    @Exposed
    subtract(other: Money): Money {
        // ...
    }

    @Const
    @Exposed
    isZero(): Bool {
        // ...
    }

    @Const
    @Exposed
    isGreaterThan(other: Money): Bool {
        // ...
    }

    @Const
    roundToTwoDecimals(value: f32): f32 {
        // ...
    }
}
```

### Discount

```
@Extensible
object Discount {
    _rate: f32;

    rate: f32 {
        get => _rate;
    }

    @Const
    @Exposed
    apply(price: Money): Money {
        // ...
    }

    clampRate(rate: f32): f32 {
        // ...
    }
}

@Inherits(Discount)
object SeasonalDiscount {
    _label: String;

    label: String {
        get => _label;
    }

    @Exposed
    describe(): String {
        // ...
    }
}
```

### Receipt

```
@Implements(Printable)
object Receipt {
    _orderId: u32;
    _total: Money;
    _items: List<OrderItem>;

    orderId: u32 {
        get => _orderId;
    }

    total: Money {
        get => _total;
    }

    @Const
    @Exposed
    print() {
        // ...
    }

    @Const
    formatLine(item: OrderItem): String {
        // ...
    }

    @Const
    formatTotal(): String {
        // ...
    }
}
```

---

## Function Aliases

```
contract Mergeable {
    @Alias("|")
    merge(other: Self): Self;
}

contract Discountable {
    @Alias(">>")
    applyDiscount(discount: Discount): Self;
}

@Implements(Mergeable)
@Implements(Discountable)
object Cart {
    _items: List<OrderItem>;

    items: List<OrderItem> {
        get => _items;
    }

    merge(other: Cart): Cart => Cart(_items: _items + other._items);

    applyDiscount(discount: Discount): Cart {
        // ...
    }
}
```

```
def guestCart = Cart(_items: List(ItemA, ItemB));
def savedCart = Cart(_items: List(ItemC));

def combined = guestCart.merge(savedCart);
def merged   = guestCart | savedCart;

def seasonal = SeasonalDiscount(_rate: 0.1, _label: "Summer Sale");

def discounted = guestCart.applyDiscount(seasonal);
def aliased    = guestCart >> seasonal;
```

---

## Services

### ConsoleLogger

```
@Implements(Logger)
service ConsoleLogger {
    log(message: String) {
        // ...
    }

    warn(message: String) {
        // ...
    }

    @Hidden
    format(level: String, message: String): String {
        // ...
    }
}
```

### FileLogger

```
@Implements(Logger, Disposable)
service FileLogger {
    _path: String;

    log(message: String) {
        // ...
    }

    warn(message: String) {
        // ...
    }

    dispose() {
        // ...
    }

    @Hidden
    format(level: String, message: String): String {
        // ...
    }

    @Hidden
    writeLine(line: String) {
        // ...
    }
}
```

### NetworkLogger

```
@Implements(Logger)
@Implements(Disposable)
service NetworkLogger {
    log(message: String) {
        // ...
    }

    warn(message: String) {
        // ...
    }

    dispose() {
        // ...
    }
}
```

### StripeGateway

```
@Implements(PaymentGateway)
service StripeGateway(
    logger: Logger
) {
    charge(amount: Money, reference: String): Result {
        // ...
    }

    refund(reference: String): Result {
        // ...
    }

    @Const
    @Hidden
    buildPayload(amount: Money, reference: String): String {
        // ...
    }

    @Hidden
    parseResponse(raw: String): Result {
        // ...
    }
}
```

### PostgresOrderRepository

```
@Implements(OrderRepository)
service PostgresOrderRepository(
    logger: Logger
) {
    @Tag(.IO)
    save(order: OrderRequest): u32 {
        // ...
    }

    @Tag(.IO)
    findById(id: u32): OrderRequest? {
        // ...
    }

    @Tag(.IO)
    findByStatus(status: OrderStatus): List<OrderRequest> {
        // ...
    }

    @Const
    @Hidden
    serialize(order: OrderRequest): String {
        // ...
    }

    @Hidden
    deserialize(raw: String): OrderRequest {
        // ...
    }
}
```

### EmailNotifier

```
@Implements(Notifier)
service EmailNotifier(
    logger: Logger
) {
    notify(userId: u32, message: String) {
        // ...
    }

    @Hidden
    resolveEmail(userId: u32): String {
        // ...
    }
}
```

### BaseAuditService

```
@Extensible
service BaseAuditService(
    logger: Logger
) {
    @Hidden
    audit(event: String) {
        // ...
    }
}
```

### OrderService

```
@Inherits(BaseAuditService)
service OrderService(
    gateway: PaymentGateway,
    repository: OrderRepository,
    notifier: Notifier
) {
    _processedCount: i32;

    processedCount: i32 {
        get => _processedCount;
    }

    placeOrder(request: OrderRequest): OrderResponse {
        def total = calculateTotal(request.items);
        def discount = resolveDiscount(request.userId);

        @Mutable
        def finalAmount: Money = discount.apply(total);

        if request.items.length > 10 {
            def bulkDiscount = Discount(_rate: 0.05);
            finalAmount = bulkDiscount.apply(finalAmount);
        }

        def reference = buildReference(request.userId);
        def result = gateway.charge(finalAmount, reference);

        if result.success {
            def orderId = repository.save(request);
            _processedCount++;
            audit("order.placed");
            notifier.notify(request.userId, "Your order has been placed.");
            // ...
        }
        no {
            audit("order.failed");
            // ...
        }
    }

    cancelOrder(orderId: u32) {
        def order = repository.findById(orderId);

        if order == null {
            // ...
        }

        def reference = buildReference(order.userId);
        gateway.refund(reference);
        audit("order.cancelled");
        notifier.notify(order.userId, "Your order has been cancelled.");
    }

    routeOrder(order: OrderRequest) {
        switch order.status {
            case .Pending   { processPending(order); }
            case .Paid      { processPayment(order); }
            case .Shipped   { notifyShipped(order); }
            case .Cancelled { processCancellation(order); }
        }
    }

    @Const
    @Hidden
    calculateTotal(items: List<OrderItem>): Money {
        // ...
    }

    @Const
    @Hidden
    resolveDiscount(userId: u32): Discount {
        // ...
    }

    @Const
    @Hidden
    buildReference(userId: u32): String {
        // ...
    }

    @Hidden
    processPending(order: OrderRequest) {
        // ...
    }

    @Hidden
    processPayment(order: OrderRequest) {
        // ...
    }

    @Hidden
    notifyShipped(order: OrderRequest) {
        // ...
    }

    @Hidden
    processCancellation(order: OrderRequest) {
        // ...
    }
}
```

### OrderReportService

```
@Implements(ReportGenerator)
service OrderReportService(
    repository: OrderRepository,
    logger: Logger
) {
    @Tag(.IO)
    fetchPendingOrders(): List<OrderRequest> {
        return repository.findByStatus(.Pending);
    }

    @Tag(.CPU)
    generate(orders: List<OrderRequest>): Report {
        // ...
    }

    // TODO: fetchPendingOrders() is @Tag(.IO) and generate() is @Tag(.CPU) -- mixed bounds.
    // Once async is a language feature, fetchPendingOrders() should be awaited
    // before handing off to generate() on a separate execution context.
    summarize(status: OrderStatus): Report {
        def orders = fetchPendingOrders();
        return generate(orders);
    }
}
```

---

## Modules

```
module AppModule {
    @Singleton(Logger)
    ConsoleLogger;

    @Scoped(PaymentGateway)
    StripeGateway;

    @Scoped(OrderRepository)
    PostgresOrderRepository;

    @Transient(Notifier)
    EmailNotifier;

    @Scoped(ReportGenerator)
    OrderReportService;
}
```

---

## Test Module

```
@Implements(Logger)
service MockLogger {
    log(message: String) { }
    warn(message: String) { }
}

@Implements(PaymentGateway)
service StubGateway {
    charge(amount: Money, reference: String): Result { }
    refund(reference: String): Result { }
}

@Implements(OrderRepository)
service InMemoryOrderRepository {
    save(order: OrderRequest): u32 { }
    findById(id: u32): OrderRequest? { }
    findByStatus(status: OrderStatus): List<OrderRequest> { }
}

@Implements(Notifier)
service SilentNotifier {
    notify(userId: u32, message: String) { }
}

@Implements(ReportGenerator)
service StubReportGenerator {
    generate(orders: List<OrderRequest>): Report { }
}

@Mock(AppModule)
module TestModule {
    @Singleton(Logger)
    MockLogger;

    @Scoped(PaymentGateway)
    StubGateway;

    @Scoped(OrderRepository)
    InMemoryOrderRepository;

    @Transient(Notifier)
    SilentNotifier;

    @Scoped(ReportGenerator)
    StubReportGenerator;
}
```
