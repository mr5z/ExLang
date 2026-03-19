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
}

contract Notifier {
    notify(userId: u32, message: String);
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

    @Exposed
    print() {
        // ...
    }

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

```
def a: u32 = 10;
def b: u32 = 3;

def c = a.plus(b);
def d = a + b;
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
        def finalAmount = discount.apply(total);

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

## Control Flow

### if / no

```
if order == null {
    // ...
}
no {
    // ...
}
```

### switch

```
service ShippingService {
    calculateRoute(direction: Direction): i32 {
        switch direction {
            case .North { return turn(90); }
            case .South { return turn(270); }
            case .East  { return turn(0); }
            case .West  { return turn(180); }
        }
    }

    @Hidden
    turn(degrees: i32): i32 {
        // ...
    }
}
```

---

## Mutability

```
service PricingService {
    applyPromotions(basePrice: Money, promoCount: i32): Money {
        def originalPrice = basePrice;

        @Mutable
        def adjusted = basePrice;

        adjusted = adjusted.subtract(computeDiscount(promoCount));

        return adjusted;
    }

    @Hidden
    computeDiscount(promoCount: i32): Money {
        // ...
    }
}
```

---

## Type Inference

```
service InferenceDemo {
    run() {
        def x = 0;
        def result: i8 = doSomething();
        def resultList: Stream<i8> = doSomething();
    }

    @Hidden
    doSomething() {
        // ...
    }
}
```

---

## @Const

```
service Lexer {
    _position: u32;
    _text: String;

    position: u32 {
        get => _position;
    }

    @Const
    peek(): String {
        def current = _text;
        return current;
    }

    advance() {
        if _position <= _text.length {
            _position++;
        }
    }
}
```

---

## Tagging

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

    generateReport(orderId: u32) {
        def order = fetchOrderData(orderId);
        def summary = computeSummary(List(order));
        // ...
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
