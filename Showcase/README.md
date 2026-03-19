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

DTO fields are always public and read-only. Attempting to reassign them is a compile error.

```
def p = Address();
p.street = "123 Main St";  // error: dto fields are read-only
```

---

## Contracts

Abstract dependency boundaries, signatures only. No fields, no definitions, no default implementations.

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

Both call forms are valid once an alias is declared.

```
def a: u32 = 10;
def b: u32 = 3;

def c = a.plus(b);   // explicit
def d = a + b;       // alias
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

Implements two contracts using a single annotation.

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

Implements two contracts using separate `@Implements` annotations. This is equivalent to the single-annotation form above.

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

`Logger` is a contract parameter -- automatically injected by the compiler. No boilerplate required.

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

The core domain service. Demonstrates inheritance, multiple injected dependencies, mutable state, `def`, type inference, and `if`/`no` conditionals.

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

`if` is equivalent to a conditional branch, `no` is equivalent to `else`. There is no `else if`; use `switch` for multi-branch logic.

### if / no

```
if order == null {
    // handle missing order
}
no {
    // proceed normally
}
```

### switch

`switch` is used for multi-branch logic. Each `case` matches on enum values.

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

Local variables are immutable by default. Use `@Mutable` to allow reassignment. Parameters are always immutable and cannot be overridden.

```
service PricingService {
    applyPromotions(basePrice: Money, promoCount: i32): Money {
        // immutable by default
        def originalPrice = basePrice;

        @Mutable
        def adjusted = basePrice;

        adjusted = adjusted.subtract(computeDiscount(promoCount));  // ok

        originalPrice = basePrice;  // error: immutable
        promoCount = 0;             // error: parameters are always immutable

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

`def` infers type from the right-hand side by default. An explicit type annotation changes what the compiler expects the function to return.

```
service InferenceDemo {
    run() {
        // inferred as the default numeric type
        def x = 0;

        // explicit type annotation: compiler expects doSomething() to return i8
        def result: i8 = doSomething();

        // explicit type annotation: compiler expects doSomething() to return Stream<i8>
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

Marking a function `@Const` disallows any mutation across its entire execution path -- including mutations to instance fields and calls to non-`@Const` functions.

```
service Lexer {
    _position: u32;
    _text: String;

    position: u32 {
        get => _position;
    }

    // @Const: no mutation allowed anywhere in this call path
    @Const
    peek(): String {
        def current = _text;       // ok: reading is allowed
        _position += 1;            // error: mutating instance field
        advance();                 // error: advance() is not @Const
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

Declare and bind the full dependency graph. The compiler statically validates the entire graph -- circular dependencies, lifetime mismatches, missing bindings, and unused bindings are all compile errors.

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
