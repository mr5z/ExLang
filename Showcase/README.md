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
    isPaid: Bool;
    isShipped: Bool;
    isCancelled: Bool;
}

dto OrderResponse {
    orderId: u32;
    status: OrderStatus;
    total: f32;
}
```

---

## Enums

```
enum MoneyError {
    NegativeAmount {
        message: String;
    };
    ExceedsLimit;
}

enum CurrencyError {
    UnsupportedCurrency {
        message: String;
    };
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
    findAll(): List<OrderRequest>;
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
    amount: f32;
    currency: String;

    init(amount: f32, currency: String) {
        if amount < 0 {
            throw .NegativeAmount { message: "Amount cannot be negative" };
        }
        if amount > 1_000_000 {
            throw .ExceedsLimit;
        }
        self.amount = amount;
        self.currency = currency;
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
}
```

### Discount

```
@Extensible
object Discount {
    rate: f32;

    init(rate: f32) {
        self.rate = rate < 0.0 ? 0.0 : rate > 1.0 ? 1.0 : rate;
    }

    @Const
    @Exposed
    apply(price: Money): Money {
        // ...
    }
}

@Inherits(Discount)
object SeasonalDiscount {
    label: String;

    init(rate: f32, label: String) {
        self.rate = rate < 0.0 ? 0.0 : rate > 1.0 ? 1.0 : rate;
        self.label = label;
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
    orderId: u32;
    total: Money;
    items: List<OrderItem>;

    init(orderId: u32, total: Money, items: List<OrderItem>) {
        self.orderId = orderId;
        self.total = total;
        self.items = items;
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
    items: List<OrderItem>;

    init(items: List<OrderItem>) {
        self.items = items;
    }

    merge(other: Cart): Cart {
        // ...
    }

    applyDiscount(discount: Discount): Cart {
        // ...
    }
}
```

```
def guestCart = new Cart(items: List(ItemA, ItemB));
def savedCart = new Cart(items: List(ItemC));

def combined = guestCart.merge(savedCart);
def merged   = guestCart | savedCart;

def seasonal = new SeasonalDiscount(rate: 0.1, label: "Summer Sale");

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
    findAll(): List<OrderRequest> {
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
    @Mutable
    processedCount: i32;

    placeOrder(request: OrderRequest): OrderResponse {
        def total = try new Money(amount: calculateTotal(request.items), currency: "USD");

        def discount = resolveDiscount(request.userId);

        @Mutable
        def finalAmount: Money = discount.apply(total);

        if request.items.length > 10 {
            def bulkDiscount = try new Discount(rate: 0.05);
            finalAmount = bulkDiscount.apply(finalAmount);
        }

        def reference: String = buildReference(request.userId);
        def result: Result = gateway.charge(finalAmount, reference);

        if result.success {
            def orderId: u32 = repository.save(request);
            processedCount++;
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
        def order: OrderRequest? = repository.findById(orderId);

        if order == null {
            // ...
        }

        def reference: String = buildReference(order.userId);
        gateway.refund(reference);
        audit("order.cancelled");
        notifier.notify(order.userId, "Your order has been cancelled.");
    }

    routeOrder(order: OrderRequest) {
        conditions OrderStatus {
            Cancelled: order.isCancelled;
            Pending:   !order.isPaid && !order.isCancelled;
            Paid:      order.isPaid && !order.isShipped;
            Shipped:   order.isPaid && order.isShipped;
        }

        switch OrderStatus(order) {
            case .Pending   => processPending(order);
            case .Paid      => processPayment(order);
            case .Shipped   => notifyShipped(order);
            case .Cancelled => processCancellation(order);
        }
    }

    @Const
    @Hidden
    calculateTotal(items: List<OrderItem>): f32 {
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
    fetchOrders(): List<OrderRequest> {
        return repository.findAll();
    }

    @Tag(.CPU)
    generate(orders: List<OrderRequest>): Report {
        // ...
    }

    @Const
    @Hidden
    priorityLabel(order: OrderRequest): String {
        return switch OrderStatus(order) {
            case .Cancelled => "none";
            case .Pending   => "high";
            case .Paid      => "medium";
            case .Shipped   => "low";
        };
    }

    // TODO: fetchOrders() is @Tag(.IO) and generate() is @Tag(.CPU): mixed bounds.
    // Once async is a language feature, fetchOrders() should be awaited
    // before handing off to generate() on a separate execution context.
    summarize(status: OrderStatus): Report {
        def orders: List<OrderRequest> = fetchOrders();
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
    findAll(): List<OrderRequest> { }
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
