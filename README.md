# ExLang

> **If a pattern is proven and universal, it should not be a pattern. It should be the language.**

Design patterns exist largely because languages are missing features. ExLang is an attempt to be intentional and systematic about this from the start, baking industry-standard patterns in as first-class language features, so developers spend cognitive energy on problems that matter, not on boilerplate that does not.

The guiding principle for every design decision is: **reduce cognitive load without sacrificing explicitness.**

---

## Table of Contents

1. [Philosophy](#philosophy)
2. [The Type System](#the-type-system)
3. [Declaration Reference](#declaration-reference)
   - [dto](#dto)
   - [object](#object)
   - [contract](#contract)
   - [service](#service)
   - [module](#module)
   - [enum](#enum)
   - [conditions](#conditions)
4. [Cross-Cutting Rules](#cross-cutting-rules)
   - [Visibility](#visibility)
   - [Fields](#fields)
   - [Properties](#properties)
   - [Naming Conventions](#naming-conventions)
   - [Mutability](#mutability)
   - [Inheritance and Implementation](#inheritance-and-implementation)
   - [Instantiation and Construction](#instantiation-and-construction)
5. [Control Flow](#control-flow)
   - [if and no](#if-and-no)
   - [switch](#switch)
   - [conditions](#conditions-1)
   - [throw, try, and catch](#throw-try-and-catch)
6. [Annotations Reference](#annotations-reference)
7. [Open Questions](#open-questions)

---

## Philosophy

- **Proven patterns are language features.** Dependency injection, value semantics, data transfer: these are not conventions or frameworks in ExLang, they are built into the language itself.
- **Intent over mechanism.** Developers declare *what* they want; the compiler is responsible for determining *how* to fulfill that declaration.
- **Smart defaults, explicit escape hatches.** Common cases should work without configuration. Where explicit control is needed, the language provides the tools to do so.
- **Compiler as safety net.** ExLang aims to move as many classes of bugs as possible into compile-time errors rather than runtime failures.

---

## The Type System

ExLang keywords fall into three categories: type declarations, structural keywords, and language mechanics.

### Type Declarations

These keywords describe data shape and behavior. They produce types that can be instantiated, passed around, and reasoned about by the compiler.

| Keyword | Purpose | Mutable Fields | Dependency Injection | Compared By |
|---|---|---|---|---|
| `dto` | Pure data shape, no behavior | No | No | Value |
| `object` | Self-contained behavioral type | Limited | No | Value |
| `contract` | Abstract dependency boundary | No | No | N/A |
| `service` | Stateful type with dependencies | Yes | Yes | Reference |
| `enum` | Named set of cases, each optionally carrying data | No | No | Value |

### Structural Keywords

These keywords describe wiring, metadata, and application topology. They do not produce instantiable types.

| Keyword | Purpose |
|---|---|
| `module` | Declares and binds the dependency graph |
| `annotation` | Declares a reusable metadata tag that can influence compiler behavior |

### Language Mechanics

These keywords are syntax primitives that operate on types rather than defining them.

| Keyword | Purpose |
|---|---|
| `def` | Binding declaration. Binds a name to a value; does not prescribe how the value is constructed. |
| `new` | Call site construction keyword. Used externally to request an instance of an `object` type. |
| `init` | Construction entry point declared inside `object` types. The body the compiler runs when `new` is called. May be overloaded. |
| `conditions` | Classification. Promotes arbitrary runtime predicates into a named, exhaustive set of cases that can be switched over. Scoped to the block in which it is declared. |
| `self` | Refers to the current instance. Valid inside `object` members. |
| `throw` | Signals a failure from within an `init` body. Must be followed by an enum case. |
| `try` | Marks a construction expression or block as potentially failing. |

---

## Declaration Reference

### `dto`

A `dto` is pure data with no behavior, no dependencies, and no identity. Two DTOs with the same property values are considered equal. DTOs are always sealed and cannot be inherited.

A `dto` has no fields. All members are read-only properties declared with bare `name: Type` syntax. The compiler manages storage implicitly. `@Mutable` and setter accessors are not valid on a `dto` and are compile errors.

Because a `dto` has no fields, there is no private storage and no accessor logic. The compiler handles all storage entirely. This is by design: a `dto` that needs private storage or custom accessor logic is no longer purely a data transfer shape and should be rethought as an `object`.

```
dto Point {
    x: f32;
    y: f32;
}

dto UserResponse {
    id: u32;
    name: String;
    email: String;
}
```

DTOs are the standard way to pass data across boundaries: between services, across network calls, in and out of functions. They are automatically serializable.

DTOs are instantiated directly using `{}` syntax, providing all property values by name. This is the only type for which direct construction is valid:

```
def point = Point { x: 1.0, y: 2.0 };
def user = UserResponse { id: 1, name: "Alice", email: "alice@example.com" };
```

The `{}` syntax signals that no logic runs during construction: the compiler fills properties directly. A type that requires validation or custom initialization logic belongs in an `object`, not a `dto`.

---

### `object`

An `object` has behavior but no dependencies. It is defined by its values rather than its identity. Two `Money` objects with the same property values are considered equal. Assignment is copy-by-value. Mutating a copy never affects the original.

Properties are read-only by default. Use `@Mutable` to opt into mutability per property. Methods are private by default; use `@Exposed` to surface them as part of the public interface.

#### Construction

Objects are never constructed directly from outside the type. All construction goes through an `init` member, which is the designated entry point declared inside the type body. `init` may be overloaded. The compiler enforces that every `init` body assigns all declared properties before returning; an unassigned property is a compile error.

At the call site, `new` is used to request construction. `new` does not prescribe how the object is built; it delegates to the appropriate `init` body.

Inside `init`, `self` refers to the instance being constructed and is used to assign properties:

```
object Money {
    amount: f32;
    currency: Currency;

    init(amount: f32, currency: Currency) {
        if amount < 0 {
            throw .NegativeAmount { message: "Amount cannot be negative" };
        }
        self.amount = amount;
        self.currency = currency;
    }
}
```

Direct construction from outside the type is always a compile error, regardless of whether validation is present:

```
def money = new Money(amount: 1.0, currency: .Usd);  // ok
def bad = Money { amount: 1.0, currency: .Usd };     // compile error, always
```

The compiler always treats `new` as potentially failing. See [throw, try, and catch](#throw-try-and-catch) for how failures are handled at the call site.

#### Behavior

Properties can opt into mutability with `@Mutable`. Custom getter and setter logic uses an explicit accessor block. See [Properties](#properties) for the full property model.

```
object Temperature {
    _value: f32;

    init(celsius: f32) {
        _value = celsius;
    }

    celsius: f32 {
        get => _value;
        set => _value = value;
    }

    fahrenheit: f32 {
        get => _value * 9 / 5 + 32;
    }

    kelvin: f32 {
        get => _value + 273.15;
    }
}
```

Objects are copy-by-value. Bare assignment produces an independent copy:

```
def a = new Temperature(celsius: 100.0);
def b = a;
b.celsius = 0.0;
// a.celsius is still 100.0
```

Objects are sealed by default. Use `@Extensible` to allow inheritance, and `@Inherits` to inherit from another object. Only single inheritance is allowed.

```
@Extensible
object Money {
    amount: f32;
    currency: String;
}

@Inherits(Money)
object DiscountedMoney {
    discountRate: f32;

    @Exposed
    discounted(): Money {
        // ...
    }
}
```

An `object` can implement contracts using `@Implements`. It can be used structurally wherever that contract is expected, but it is never DI-managed and cannot appear as a module binding target.

```
@Implements(Printable)
object Receipt {
    // ...
    @Exposed
    print() {
        // ...
    }
}
```

---

### `contract`

A `contract` defines an abstract dependency boundary, specifying what a type can do without prescribing how it is implemented. Contracts contain signatures only: no fields, no definitions, no default implementations.

```
contract Logger {
    log(message: String);
}

contract PaymentGateway {
    charge(amount: Money): Result;
}
```

Both `service` and `object` types can implement contracts using `@Implements`. Only `service` types can be bound in a module and participate in dependency injection.

---

### `service`

A `service` has behavior, mutable state, and dependencies. It is the only declaration type that participates in dependency injection. Services are identified by reference, not by value. Two instances of the same service are distinct objects.

Properties are read-only by default. Use `@Mutable` to opt into mutability per property. Methods are public by default; use `@Hidden` to keep a method internal.

```
service Counter {
    @Mutable
    count: i32;

    increment() {
        count++;
    }

    @Hidden
    validate() {
        // internal only
    }
}
```

Mutable properties with custom accessor logic use bare `set;` for simple assignment or `set =>` for custom logic:

```
service Rectangle {
    @Mutable
    width: f32 {
        get => field;
        set;
    }

    @Mutable
    height: f32 {
        get => field;
        set => field = value > 0.0 ? value : 0.0;
    }
}
```

#### Dependency Injection

Dependency injection is a first-class language feature in ExLang. Any field whose type is a `contract` is automatically treated as a dependency; the compiler resolves and injects the appropriate binding without any additional annotations or configuration.

Constructor dependencies are declared in the service signature. Only `contract` types are allowed as constructor parameters, and this is enforced by the compiler with no exceptions.

```
service UserService(
    gateway: PaymentGateway,
    logger: Logger
) {
    process(payment: Money): Result {
        // ...
    }
}
```

This constraint cleanly separates two concerns:
- **Constructors** are for wiring dependencies.
- **Initializers / factory functions** are for providing data.

#### Implementing Contracts

A `service` declares which contracts it fulfills using `@Implements`. Multiple contracts are supported either by separating them with commas or by repeating the annotation.

```
@Implements(Logger)
service ConsoleLogger {
    log(message: String) {
        // ...
    }
}

@Implements(Logger, Disposable)
service FileLogger {
    log(message: String) {
        // ...
    }

    dispose() {
        // ...
    }
}

@Implements(Logger)
@Implements(Disposable)
service NetworkLogger {
    log(message: String) {
        // ...
    }

    dispose() {
        // ...
    }
}
```

#### Inheritance

Services are sealed by default. Use `@Extensible` to allow inheritance, and `@Inherits` to inherit from another service. Only single inheritance is allowed. A service that `@Inherits` another service automatically inherits its dependencies.

```
@Extensible
service BaseLogger {
    _prefix: String;

    formatMessage(message: String): String {
        // ...
    }
}

@Inherits(BaseLogger)
@Implements(Logger)
service ConsoleLogger {
    log(message: String) {
        // ...
    }
}
```

---

### `module`

A `module` declares the dependency graph for the application. It tells the compiler which concrete `service` type fulfills each `contract`, and what lifetime scope each registration has.

```
module AppModule {
    @Singleton(Logger)
    ConsoleLogger;

    @Scoped(PaymentGateway)
    StripeGateway;

    @Scoped(DatabaseSession)
    PostgresSession;
}
```

The compiler statically analyzes the entire dependency graph from the module declaration. The following are all compile errors, not runtime crashes:

- Circular dependencies
- A `@Transient` service injected into a `@Singleton`
- A missing binding for a declared dependency
- Unused bindings
- An `object` type used as a binding target

#### Lifetime Scopes

Lifetime is declared at the binding site via the scope annotation:

| Annotation | Lifetime |
|---|---|
| `@Singleton(Contract)` | One instance for the lifetime of the application |
| `@Scoped(Contract)` | One instance per logical scope (e.g., a request, a session) |
| `@Transient(Contract)` | A fresh instance every time it is needed |

#### Testing

Test modules can shadow bindings from the application module using `@Mock`:

```
@Mock(AppModule)
module TestModule {
    @Singleton(Logger)
    MockLogger;

    @Scoped(PaymentGateway)
    StubGateway;

    @Scoped(DatabaseSession)
    InMemorySession;
}
```

---

### `enum`

An `enum` declares a named set of cases. Each case may optionally carry associated data, declared using a `{}` block. Cases with no associated data are declared with a bare name followed by `;`.

```
enum Direction {
    North;
    South;
    East;
    West;
}

enum MoneyError {
    NegativeAmount {
        message: String;
    };
    ExceedsLimit;
}
```

Enum cases are referenced using dot notation, with the compiler inferring the enum type from context:

```
def dir = .North;
throw .NegativeAmount { message: "Amount cannot be negative" };
```

Enums are value types. Two enum values are equal if they are the same case and carry the same associated data.

---

### `conditions`

`conditions` promotes arbitrary runtime predicates into a named, exhaustive set of cases that can be switched over. It is always local to the block in which it is declared. Each case is a named predicate. Cases are evaluated top to bottom; the first matching case wins.

The subjects being classified are declared as parameters in the `conditions` signature. At the call site, the matching arguments are passed in the same order.

```
conditions WaterPhase(temp: f32) {
    Ice:    temp < 0;
    Liquid: temp < 100;
    Steam:  temp >= 100;
}

switch WaterPhase(temp) {
    case .Ice    => freeze();
    case .Liquid => liquid();
    case .Steam  => boil();
}
```

The developer is responsible for declaring cases that cover all possible states. Any `switch` over a `conditions` block must be exhaustive: the compiler rejects missing cases.

The compiler warns if declared predicates overlap, as an unreachable case is almost certainly a bug:

```
conditions Access(isBanned: Bool, isAdmin: Bool) {
    Banned:      isBanned;
    BannedAdmin: isBanned && isAdmin;  // warning: unreachable, Banned covers this
    Allowed:     !isBanned;
}
```

`switch` over a `conditions` block works in both statement and expression forms:

```
def label = switch WaterPhase(temp) {
    case .Ice    => "ice";
    case .Liquid => "liquid";
    case .Steam  => "steam";
}
```

The key advantage of `conditions` is that the predicate set is declared once and reused across multiple `switch` sites. If the classification logic changes, it changes in one place:

```
switch WaterPhase(temp) {
    case .Ice    => applyIceShader();
    case .Liquid => applyWaterShader();
    case .Steam  => applySteamShader();
}

switch WaterPhase(temp) {
    case .Ice    => playIceSound();
    case .Liquid => playWaterSound();
    case .Steam  => playSteamSound();
}
```

---

## Cross-Cutting Rules

### Visibility

Method visibility follows the nature of each type:

| Type | Fields | Methods | Override |
|---|---|---|---|
| `dto` | Always public read-only | N/A | Not allowed |
| `object` | Always private | Private by default | `@Exposed` to make public |
| `service` | Always private | Public by default | `@Hidden` to make private |

Fields on `object` and `service` are always private, exposed only via explicit properties. This is enforced by the type system, not by annotation.

```
object Money {
    _amount: f32;

    @Exposed
    add(other: Money): Money { ... }

    normalize(): f32 { ... }  // private by default
}

service UserService {
    process(payment: Money): Result { ... }  // public by default

    @Hidden
    validate(payment: Money): Bool { ... }  // explicitly private
}
```

---

### Fields

A field is private storage owned by a type, used when a property requires custom accessor logic that references state not captured by the automatic `field` backing. Fields are always prefixed with `_` and are never accessible directly from outside the type.

Fields are valid on `object` and `service` only. `dto` has no fields; attempting to declare one is a compile error.

```
object Money {
    _exchangeRate: f32;
    _adjustmentFactor: f32;

    exchangeRate: f32 {
        get => _exchangeRate * _adjustmentFactor;
        set => _exchangeRate = value / _adjustmentFactor;
    }
}
```

When a property only needs simple storage, no explicit field is required. The compiler provides an automatic backing store accessible as `field` inside the accessor block. Explicit fields are only needed when state must be shared across multiple properties or when the backing logic is too complex for `field` alone:

```
object Temperature {
    _value: f32;

    // Three properties all share the same backing field
    celsius: f32 {
        get => _value;
        set => _value = value;
    }

    fahrenheit: f32 {
        get => _value * 9 / 5 + 32;
    }

    kelvin: f32 {
        get => _value + 273.15;
    }
}
```

---

### Properties

A property is the public surface of a type's state. Properties are read-only by default across all types. `object` and `service` properties can opt into mutability via `@Mutable` or by declaring a setter. `dto` properties are always read-only.

A property is declared as `name: Type`. The compiler manages storage implicitly. No accessor block is required.

```
dto Point {
    x: f32;
    y: f32;
}

object Money {
    amount: f32;       // read-only
    currency: String;  // read-only

    @Mutable
    discount: f32; // opted into mutability
}

service Counter {
    @Mutable
    count: i32;    // opted into mutability
}
```

When custom accessor logic is needed, a property can declare an accessor block with `get` and optionally `set`. The compiler provides an automatic backing store accessible as `field` inside the block. `value` refers to the incoming value in a setter.

Accessor blocks come in three forms:

**Bare.** `get;` and `set;`: no custom logic. `get;` reads directly from `field`. `set;` writes `value` directly into `field`. Shorthand for the common case of simple pass-through storage.

```
count: i32 {
    get;
    set;
}
```

**Single-line.** `get =>` and `set =>`: an expression on the right-hand side.

```
// read-only with custom getter
exchangeRate: f32 {
    get => field * adjustmentFactor;
}

// mutable with custom setter logic
@Mutable
tax: f32 {
    get => field * taxRate;
    set => field = value / taxRate;
}
```

**Multi-line.** `get { ... }` and `set { ... }`: a block body for more complex logic.

```
@Mutable
quantity: i32 {
    get {
        log("quantity read");
        return field;
    }
    set {
        log("quantity written");
        field = value > 0 ? value : 0;
    }
}
```

If a property needs to share state with another property, an explicit field can be declared at the type level and referenced inside the accessor block. See [Fields](#fields).

The following rules apply across all types:

| Rule | Detail |
|---|---|
| `dto` properties | Always read-only. `@Mutable` and `set` are compile errors. |
| `object` properties | Read-only by default. `@Mutable` to opt in. Copy-by-value on assignment. |
| `service` properties | Read-only by default. `@Mutable` to opt in. |
| `@Mutable` with a setter | Compiler warning: redundant. A setter already implies mutability. |
| `field` | Automatic backing store inside accessor blocks. |
| `value` | The incoming value in a `set =>` body. |

---

### Naming Conventions

All fields must be prefixed with `_`. This is enforced by the compiler. The prefix makes the distinction between fields, properties, and local variables (including parameters) unambiguous at a glance. `dto` types have no fields and are therefore exempt.

```
object Money {
    _amount: f32;       // field: prefixed

    amount: f32;        // property: no prefix

    add(other: Money): Money {
        def result = _amount + other.amount;  // local variable: no prefix
        // ...
    }
}

service Counter {
    _count: i32;        // field: prefixed

    count: i32 {        // property: no prefix
        get => _count;
    }

    incrementBy(step: i32) {  // parameter: no prefix
        _count += step;
    }
}

dto Address {
    street: String;   // property: no prefix, no backing field
    city: String;
    country: String;
}
```

| Construct | Prefix | Applies To |
|---|---|---|
| Field | Required `_` | `object`, `service` |
| Property | None | `dto`, `object`, `service` |
| Local variable | None | All |
| Parameter | None | All |
| `self` | None | `object` members |

---

### Mutability

Mutability is contextual:

| Context | Default | Override |
|---|---|---|
| Local variables | Immutable | `@Mutable` to make mutable |
| Parameters | Immutable | Not overridable |
| `object` properties | Read-only | `@Mutable` to opt in, or declare a setter |
| `service` properties | Read-only | `@Mutable` to opt in, or declare a setter |
| `dto` properties | Read-only | Not overridable |

```
// Local variables
doSomething() {
    def a: i32 = 0;
    a = 1;  // error, immutable by default

    @Mutable
    def b: i32 = 0;
    b = 1;  // ok
}

// Parameters are always immutable
doSomething(a: i32, b: i32) {
    a = 0;  // error
    b = 1;  // error
}

// service fields: private, mutable, exposed via property
service Rectangle {
    _width: f32;
    _height: f32;

    width: f32 {
        get => _width;
        set => _width = value;
    }

    height: f32 {
        get => _height;
        set => _height = value;
    }
}

// dto properties: always public read-only
dto Point {
    x: f32 { get; }
    y: f32 { get; }
}

def p = Point { x: 1.0, y: 2.0 };
p.x = 1.0;  // error, dto properties are read-only
```

#### `@Const`

Marking a function `@Const` disallows any mutation in its entire execution path.

```
_position: u32;

@Const
doSomething() {
    self._position += 1;  // error: mutating instance field
    def i = 4;
    i = 2;                // ok: local variable

    advance();            // error: advance is not @Const
}

advance() {
    if _position <= _text.length {
        _position++;
    }
}
```

---

### Inheritance and Implementation

| Rule | Detail |
|---|---|
| `@Extensible` | May appear on `object` and `service`. Types are sealed by default. |
| `@Inherits` | May appear at most once on any type. Multiple inheritance is not allowed. |
| `@Inherits` requires `@Extensible` | Inheriting a sealed type is a compile error. |
| `@Implements` | May appear multiple times, or accept multiple contracts separated by commas. Both forms are equivalent. |
| `@Implements` on `dto` | Not valid. `dto` cannot implement contracts. |
| `object` as binding target | Not valid. Only `service` types can be bound in a module. |
| Inherited dependencies | A `service` that `@Inherits` another `service` automatically inherits its dependencies. |
| Contract enforcement | The compiler enforces that all contract method signatures are implemented. Missing implementations are compile errors. |

---

### Instantiation and Construction

`def` declares a named binding. It does not prescribe how the value is constructed; that depends on the type. `def` simply binds the result of a construction expression to a name.

```
def x = 0;  // numeric literal, type inferred
def point = Point { x: 1.0, y: 2.0 };               // dto, direct construction
def money = new Money(amount: 1.0, currency: .Usd);  // object, through new
```

Construction rules by type:

| Type | Internal Entry Point | Call Site Syntax | Can Fail |
|---|---|---|---|
| `dto` | None (compiler-managed) | `{}` directly | No |
| `object` | `init` | `new Type(...)` | Yes |
| `service` | None (compiler-managed) | Never directly, resolved by module | N/A |
| `enum` | None | Cases referenced via dot notation | No |

The distinction between `init` and `new` is intentional. `init` is the internal declaration: it is what the type author writes to describe how an instance is set up. `new` is the external call site keyword: it is what the caller writes to request an instance. They are two sides of the same operation and never appear in each other's context.

#### Type Inference

```
def result: i8 = doSomething();
def resultList: Stream<i8> = doSomething();
```

#### Function Aliases

```
contract Numeric {
    @Alias("+")
    plus(other: Self): Self;
}

@Implements(Numeric)
object u8 {
    plus(other: u8): u8 => self._value + other._value;
}

def n: u8 = 0;
n = n.plus(1);
n = n + 1;  // valid due to function alias
```

#### Self and Access to Implementing Type

```
contract Role {
    assign(other: Self): Self;
}

@Implements(Role)
object UserRole {
    assign(other: UserRole): UserRole {
        // ...
    }
}
```

---

## Control Flow

### `if` and `no`

`if` handles conditional branching. `no` is the fallback branch, equivalent to `else` in other languages. There is no `else if`; it is not part of the language.

```
if x == y {
    doThis();
}
no {
    doThat();
}
```

`if`/`no` is for binary decisions. When more than two outcomes are possible, use `switch` over an enum or a `conditions` block instead. Those forms are exhaustiveness-checked by the compiler; `if`/`no` chains are not.

---

### `switch`

`switch` is used for multi-branch logic over an enum or a `conditions` block. Cases are exhaustive by default; the compiler rejects a `switch` with missing cases.

`switch` can be used as a statement or as an expression. When used as an expression, every case must evaluate to a value of the same type.

```
switch direction {
    case .North => turn(90);
    case .South => turn(270);
    case .East  => turn(0);
    case .West  => turn(180);
}

def degrees = switch direction {
    case .North => 90;
    case .South => 270;
    case .East  => 0;
    case .West  => 180;
}
```

Multi-line case bodies use curly braces:

```
switch direction {
    case .North {
        log("Heading north");
        turn(90);
    }
    case .South {
        log("Heading south");
        turn(270);
    }
}
```

---

### `conditions`

See [`conditions` in the Declaration Reference](#conditions) for the full definition.

`switch` over a `conditions` block supports both statement and expression forms. Because the predicate set is declared once, multiple `switch` sites stay consistent automatically:

```
conditions WaterPhase(temp: f32) {
    Ice:    temp < 0;
    Liquid: temp < 100;
    Steam:  temp >= 100;
}

switch WaterPhase(temp) {
    case .Ice    => applyIceShader();
    case .Liquid => applyWaterShader();
    case .Steam  => applySteamShader();
}

switch WaterPhase(temp) {
    case .Ice    => playIceSound();
    case .Liquid => playWaterSound();
    case .Steam  => playSteamSound();
}
```

---

### `throw`, `try`, and `catch`

`throw` signals a failure from within an `init` body. It must be followed by an enum case, with an optional associated data block. The enum case acts as the error code; associated data is the conventional place for a human-readable message.

```
enum MoneyError {
    NegativeAmount {
        message: String;
    };
    ExceedsLimit;
}

object Money {
    amount: f32;
    currency: Currency;

    init(amount: f32, currency: Currency) {
        if amount < 0 {
            throw .NegativeAmount { message: "Amount cannot be negative" };
        }
        if amount > 1_000_000 {
            throw .ExceedsLimit;
        }
        self.amount = amount;
        self.currency = currency;
    }
}
```

The compiler always treats `new` as potentially failing. At the call site, failures are handled using `try` and `catch`.

**Block form.** Wraps one or more constructions in a shared failure scope:

```
try {
    def money = new Money(amount: -1.0, currency: .Usd);
} catch .NegativeAmount na {
    na.message;
} catch .ExceedsLimit {
    // handle
}
```

**Inline form.** Propagates the failure to the caller:

```
def money = try new Money(amount: 1.0, currency: .Usd);
```

`catch` branches follow the same dot notation as the rest of the language. When a case carries associated data, a binding name after the case gives access to the instance:

```
catch .NegativeAmount na {
    na.message;  // access associated data by property name
}
```

`catch` without a case name is a catch-all fallback:

```
try {
    def money = new Money(amount: -1.0, currency: .Usd);
} catch .NegativeAmount na {
    na.message;
} catch {
    // all other failures
}
```

---

### Tagging

The standard library provides a way to tag functions based on compute bounds: `CPU`, `IO`, or custom tags. This gives developers a high-level overview of how functions are tied together. The linter warns about mixing tags that may cause performance issues.

```
@Tag(.IO)
requestUserInfo(id: u32): User {
    // network request
}

@Tag(.CPU)
crunchSomeNumber(data: Vec<f32>): f32 {
    // math-heavy computation
}

// linter warns about mixing bounds
doWork() {
    def user = requestUserInfo(userId);
    def x = crunchSomeNumber(data);
}
```

---

## Annotations Reference

> The annotation system, including how annotations are declared, how targeting works, and how annotations interact with the compiler, is documented separately. See [Annotations/README.md](Annotations/README.md).

### Visibility and Exposure

| Annotation | Valid On | Effect |
|---|---|---|
| `@Exposed` | `object` methods | Makes a method public (overrides private-by-default) |
| `@Hidden` | `service` methods | Makes a method private (overrides public-by-default) |

### Mutability

| Annotation | Valid On | Effect |
|---|---|---|
| `@Mutable` | Local variables, `object` and `service` properties | Allows reassignment of a local variable, or opts a property into mutability. Compiler warning if used alongside a `set` body. |
| `@Const` | Functions | Disallows any mutation in the entire execution path of the function. |

### Contracts and Implementation

| Annotation | Valid On | Effect |
|---|---|---|
| `@Implements(Contract, ...)` | `object`, `service` | Declares that this type fulfills one or more contracts. |
| `@Alias("op")` | `contract` method signatures | Allows the method to be called using an operator or shorthand symbol. |

### Inheritance

| Annotation | Valid On | Effect |
|---|---|---|
| `@Extensible` | `object`, `service` | Allows this type to be inherited. Types are sealed by default. |
| `@Inherits(Type)` | `object`, `service` | Inherits from the specified type. May appear at most once. Parent must be `@Extensible`. |

### Dependency Injection and Lifetime

| Annotation | Valid On | Effect |
|---|---|---|
| `@Singleton(Contract)` | Module binding | One instance for the lifetime of the application. |
| `@Scoped(Contract)` | Module binding | One instance per logical scope (e.g., a request or session). |
| `@Transient(Contract)` | Module binding | A fresh instance every time it is needed. |
| `@Mock(Module)` | `module` | Shadows bindings from the target module for testing purposes. |

### Tagging

| Annotation | Valid On | Effect |
|---|---|---|
| `@Tag(bound)` | Functions | Marks a function with a compute bound (`IO`, `CPU`, or custom). The linter warns on mixed-bound call sites. |

---

## Open Questions

- What is the full spec for discriminated unions, and does the `,` syntax conflict with multi-return?
- Should `contract` support default implementations?
- What is the concurrency model? Does the `@Tag` system extend to async boundaries?
- What is the full null safety spec beyond `String?`?
- Should generics support variance annotations?
- Should mutable local variables use `@Mutable` as an annotation or a dedicated keyword?
- Can `init` overloads delegate to one another, and if so, what is the syntax?
- Should `catch` branches be exhaustive and compiler-enforced, given that `throw` codes are enum values?
- Is `self` valid inside `service` members, or is it scoped to `object` only?
- For annotation-specific open questions, see [Annotations/README.md](Annotations/README.md).
