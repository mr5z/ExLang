# ExLang Showcase

A fictional e-commerce payment system demonstrating all language features in a single cohesive example.

---

## DTOs

Pure data, always immutable, always serializable.

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

Abstract dependency boundaries, signatures only.

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
}

contract Notifier {
    notify(userId: u32, message: String);
}
```

---

## Objects

Self-contained behavior, value semantics, private methods by default.

### Money

A classic value object. Methods are private unless explicitly marked `@Exposed`.

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

    @Exposed
    add(other: Money): Money {
        // ...
    }

    @Exposed
    subtract(other: Money): Money {
        // ...
    }

    @Exposed
    isZero(): Bool {
        // ...
    }

    @Exposed
    isGreaterThan(other: Money): Bool {
        // ...
    }

    // private: internal rounding, not part of public interface
    roundToTwoDecimals(value: f32): f32 {
        // ...
    }
}
```

### Discount

Marked `@Extensible` so specialized discounts can inherit from it.

```
@Extensible
object Discount {
    _rate: f32;

    rate: f32 {
        get => _rate;
    }

    @Exposed
    apply(price: Money): Money {
        // ...
    }

    // private helper
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

An `object` implementing a contract. Can be used structurally wherever `Printable` is expected, but is never DI-managed.

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

    @Exposed
    print() {
        // ...
    }

    // private: formatting internals
    formatLine(item: OrderItem): String {
        // ...
    }

    formatTotal(): String {
        // ...
    }
}
```

---

## Function Aliases

Operators mapped to contract method signatures.

```
contract Numeric {
    @Alias("+")
    plus(other: Self): Self;

    @Alias("-")
    minus(other: Self): Self;

    @Alias(">")
    greaterThan(other: Self): Bool;
}

@Implements(Numeric)
object u32 {
    plus(other: u32): u32 => self._value + other._value;
    minus(other: u32): u32 => self._value - other._value;
    greaterThan(other: u32): Bool => self._value > other._value;
}
```

---

## Services

Stateful, DI-managed, public methods by default.

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

    // private: shared formatting logic
    @Hidden
    format(level: String, message: String): String {
        // ...
    }
}
```

### FileLogger

Implements two contracts in a single annotation.

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

### StripeGateway

`Logger` is a contract field — automatically injected by the compiler. No boilerplate required.

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
    save(order: OrderRequest): u32 {
        // ...
    }

    findById(id: u32): OrderRequest? {
        // ...
    }

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

An extensible base service. Child services that `@Inherits` this automatically inherit its `logger` dependency.

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

The core domain service. Demonstrates inheritance, multiple injected dependencies, mutable state, `def`, type inference, and `is`/`no` conditionals.

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
        // type inferred as Money
        def total = calculateTotal(request.items);
        def discount = resolveDiscount(request.userId);
        def finalAmount = discount.apply(total);

        def reference = buildReference(request.userId);
        def result = gateway.charge(finalAmount, reference);

        is result.success {
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

        is order == null {
            // ...
        }

        def reference = buildReference(order.userId);
        gateway.refund(reference);
        audit("order.cancelled");
        notifier.notify(order.userId, "Your order has been cancelled.");
    }

    @Hidden
    calculateTotal(items: List<OrderItem>): Money {
        // ...
    }

    @Hidden
    resolveDiscount(userId: u32): Discount {
        // ...
    }

    @Hidden
    buildReference(userId: u32): String {
        // ...
    }
}
```

---

## Tagging

Signal compute bounds for linter awareness. The linter warns when `@Tag(.IO)` and `@Tag(.CPU)` functions are mixed at the same call site.

```
service ReportService(
    repository: OrderRepository,
    logger: Logger
) {
    @Tag(.IO)
    fetchOrderData(orderId: u32): OrderRequest? {
        // ...
    }

    @Tag(.CPU)
    computeSummary(orders: List<OrderRequest>): f32 {
        // ...
    }

    // linter warns: mixing IO and CPU bounds in one call site
    generateReport(orderId: u32) {
        def order = fetchOrderData(orderId);
        def summary = computeSummary(List(order));
        // ...
    }
}
```

---

## Modules

Declare and bind the full dependency graph. The compiler statically validates the entire graph — circular dependencies, lifetime mismatches, missing bindings, and unused bindings are all compile errors.

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
}
```

---

## Test Module

Shadows all production bindings with fakes using `@Mock`.

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
}

@Implements(Notifier)
service SilentNotifier {
    notify(userId: u32, message: String) { }
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
}
```
