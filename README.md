# ExLang

> **If a pattern is proven and universal, it shouldn't be a pattern. It should be the language.**

Design patterns exist largely because languages are missing features. ExLang is an attempt to be intentional and systematic about this from the start, baking industry-standard patterns in as first-class language features, so developers spend cognitive energy on problems that matter, not on boilerplate that doesn't.

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
   - [conditions](#conditions)
4. [Cross-Cutting Rules](#cross-cutting-rules)
   - [Visibility](#visibility)
   - [Fields](#fields)
   - [Properties](#properties)
   - [Naming Conventions](#naming-conventions)
   - [Mutability](#mutability)
   - [Inheritance and Implementation](#inheritance-and-implementation)
5. [Instantiation](#instantiation)
   - [def](#def)
   - [Type Inference](#type-inference)
   - [Function Aliases](#function-aliases)
6. [Control Flow](#control-flow)
7. [Annotations Reference](#annotations-reference)
8. [Open Questions](#open-questions)

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
| `dto` | Pure data shape, no behavior | ❌ | ❌ | Value |
| `object` | Self-contained behavioral type | ⚠️ | ❌ | Value |
| `contract` | Abstract dependency boundary | ❌ | ❌ | N/A |
| `service` | Stateful type with dependencies | ✅ | ✅ | Reference |

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
| `def` | Instantiation. Brings anything into existence: fields, variables, functions, dependencies. |
| `conditions` | Classification. Promotes arbitrary runtime predicates into a named, exhaustive set of cases that can be switched over. |

---

## Declaration Reference

### `dto`

A `dto` is pure data with no behavior, no dependencies, and no identity. Two DTOs with the same property values are considered equal. DTOs are always sealed and cannot be inherited.

A `dto` has no fields. All members are read-only properties declared with bare `name: Type` syntax. The compiler manages storage implicitly. `@Mutable` and setter accessors are not valid on a `dto` and are compile errors.

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

---

### `object`

An `object` has behavior but no dependencies. It is defined by its values rather than its identity. Two `Money` objects with the same property values are considered equal. Assignment is copy-by-value. Mutating a copy never affects the original.

Properties are read-only by default. Use `@Mutable` to opt into mutability per property. Methods are private by default; use `@Exposed` to surface them as part of the public interface.

```
object Money {
    amount: f32;       // read-only
    currency: String;  // read-only

    @Exposed
    add(other: Money): Money {
        // ...
    }

    @Exposed
    isZero(): Bool {
        // ...
    }

    normalizeAmount(): f32 {
        // private helper, not exposed
    }
}
```

Properties can opt into mutability with `@Mutable`. Custom getter and setter logic is supported via an explicit accessor block. See [Properties](#properties) for the full property model.

```
object Temperature {
    @Mutable
    celsius: f32;                    // bare, mutable

    fahrenheit: f32 {                // read-only, custom getter
        get => field * 9 / 5 + 32;
    }

    @Mutable
    kelvin: f32 {                    // mutable, custom getter and setter
        get => field + 273.15;
        set => field = value - 273.15;
    }
}
```

Objects are copy-by-value. Bare assignment produces an independent copy:

```
def a = Temperature(celsius: 100.0);
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
object Money {
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
        set => field = value > 0.0 ? value : 0.0;  // clamp to positive
    }
}
```

#### Dependency Injection

Dependency injection is a first-class language feature in ExLang. Any field whose type is a `contract` is automatically treated as a dependency; the compiler resolves and injects the appropriate binding without any additional annotations or configuration.

Constructor dependencies are declared in the service signature. Only `contract` types are allowed as constructor parameters, and this is enforced by the compiler with no exceptions.

```
service UserService(
    gateway: PaymentGateway,   // contract -> injected automatically
    logger: Logger             // contract -> injected automatically
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

// Multiple contracts, single annotation
@Implements(Logger, Disposable)
service FileLogger {
    log(message: String) {
        // ...
    }

    dispose() {
        // ...
    }
}

// Multiple contracts, multiple annotations (equivalent to above)
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
@Implements(Logger)
service ConsoleLogger { ... }

@Implements(PaymentGateway)
service StripeGateway { ... }

@Implements(DatabaseSession)
service PostgresSession { ... }

module AppModule {
    @Singleton(Logger)
    ConsoleLogger;

    @Scoped(PaymentGateway)
    StripeGateway;

    @Scoped(DatabaseSession)
    PostgresSession;
}
```

The compiler statically analyzes the entire dependency graph from the module declaration. The following are all **compile errors**, not runtime crashes:

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
@Implements(Logger)
service MockLogger { ... }

@Implements(PaymentGateway)
service StubGateway { ... }

@Implements(DatabaseSession)
service InMemorySession { ... }

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

## Cross-Cutting Rules

### Visibility

Method visibility follows the nature of each type:

| Type | Fields | Methods | Override |
|---|---|---|---|
| `dto` | Always public read-only | N/A | Not allowed |
| `object` | Always private | Private by default | `@Exposed` to make public |
| `service` | Always private | Public by default | `@Hidden` to make private |

Fields on `object` and `service` are always private, exposed only via explicit properties. This is enforced by the type system, not by annotation.

Developers who prefer explicit annotations for consistency may annotate freely. Using `@Exposed` on an `object` method or `@Hidden` on a `service` method is never redundant, as it signals a deliberate choice.

```
object Money {
    _amount: f32;

    @Exposed
    add(other: Money): Money { ... }    // explicitly public

    normalize(): f32 { ... }           // private by default
}

service UserService {
    process(payment: Money): Result { ... }   // public by default

    @Hidden
    validate(payment: Money): Bool { ... }    // explicitly private
}
```

---

### Fields

A field is private storage owned by a type, used when a property requires custom accessor logic that references state not captured by the automatic `field` backing. Fields are always prefixed with `_` and are never accessible directly from outside the type.

Fields are valid on `object` and `service` only. `dto` has no fields.

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

When a property only needs simple storage, no explicit field is required. The compiler provides an automatic backing store accessible as `field` inside the accessor block. Explicit fields are only needed when state must be shared across multiple properties.

---

### Properties

A property is the public surface of a type's state. Properties are read-only by default across all types. Use `@Mutable` to opt into mutability.

There are two forms:

**Bare property.** No accessor block. The compiler manages storage implicitly.

```
dto Point {
    x: f32;        // read-only, always
    y: f32;
}

object Money {
    amount: f32;   // read-only by default

    @Mutable
    discount: f32; // opted into mutability
}

service Counter {
    @Mutable
    count: i32;    // opted into mutability
}
```

**Explicit property.** Has an accessor block with `get =>` and optionally `set;` or `set =>`. The compiler provides an automatic backing store accessible as `field` inside the block. `value` refers to the incoming value in a setter.

```
// read-only with custom getter
exchangeRate: f32 {
    get => field * adjustmentFactor;
}

// mutable, simple setter - stores value directly into field
@Mutable
discount: f32 {
    get => field * 100;
    set;
}

// mutable, custom setter logic
@Mutable
tax: f32 {
    get => field * taxRate;
    set => field = value / taxRate;
}
```

If a property needs to share state with another property, an explicit field can be declared at the type level and referenced inside the accessor block. See [Fields](#fields).

The following rules apply across all types:

| Rule | Detail |
|---|---|
| `dto` properties | Always read-only. `@Mutable` and `set` are compile errors. |
| `object` properties | Read-only by default. `@Mutable` to opt in. Copy-by-value on assignment. |
| `service` properties | Read-only by default. `@Mutable` to opt in. |
| `@Mutable` with `set` body | Compiler warning: redundant. |
| `field` | Automatic backing store inside accessor blocks. |
| `value` | The incoming value in a `set =>` body. |

---

### Naming Conventions

All fields must be prefixed with `_`. This is enforced by the compiler. The prefix makes the distinction between fields, properties, and local variables (including parameters) unambiguous at a glance. `dto` types have no fields and are therefore exempt.

```
object Money {
    _amount: f32;       // field: prefixed

    amount: f32 {       // property: no prefix
        get => _amount;
    }

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

---

### Mutability

Mutability is contextual:

| Context | Default | Override |
|---|---|---|
| Local variables | Immutable | `@Mutable` to make mutable |
| Parameters | Immutable | Not overridable |
| `object` properties | Read-only | `@Mutable` to opt in |
| `service` properties | Read-only | `@Mutable` to opt in |
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

def p = Point();
p.x = 1.0;  // error, dto properties are read-only
```

#### `@Const`

Marking a function `@Const` disallows any mutation in its entire execution path.

```
_position: u32;

@Const
doSomething() {
    self._position += 1;  // error (mutating instance field)
    def i = 4;            // immutable local variable
    i = 2;                // ok

    advance();            // error (advance is not @Const)
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
| `@Inherits` | May appear **at most once** on any type. Multiple inheritance is not allowed. |
| `@Inherits` requires `@Extensible` | Inheriting a sealed type is a compile error. |
| `@Implements` | May appear multiple times, or accept multiple contracts separated by commas. Both forms are equivalent. |
| `@Implements` on `dto` | Not valid. `dto` cannot implement contracts. |
| `object` as binding target | Not valid. Only `service` types can be bound in a module. |
| Inherited dependencies | A `service` that `@Inherits` another `service` automatically inherits its dependencies. |
| Contract enforcement | The compiler enforces that all contract method signatures are implemented. Missing implementations are compile errors. |

---

## Instantiation

### `def`

`def` is used for all instantiation. The compiler infers type from context.

```
// Local variable, type inferred as a Numeric variant, initial value 0, immutable by default
def x = 0;
```

---

### Type Inference

```
// doSomething() returns either i8 or Stream<i8> based on inferred type
def result: i8 = doSomething();
def resultList: Stream<i8> = doSomething();
```

---

### Function Aliases

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
n = n + 1;  // possible due to function alias
```

---

### Self and Access to Implementing Type

```
contract Role { self ->

    // Self: type of the implementing class
    // self: instance variable (like 'this'), renameable
    assign(other: Self): Self;
}

@Implements(Role)
object UserRole { this ->

    // Self is now UserRole
    assign(other: UserRole): UserRole {
        // ...
    }
}
```

---

## Control Flow

### `if` and `no`

`if` handles conditional branching. `no` is the fallback branch, equivalent to `else` in other languages. There is no `else if`; multi-branch logic belongs in `switch` or `conditions`, both of which are more explicit and compiler-enforced. This is a deliberate design decision: `else if` chains scale poorly, are easy to get wrong, and offer no exhaustiveness guarantees. `conditions` covers the same ground with more structure.

```
if x == y {
    doThis();
}
no {
    doThat();
}
```

### `switch`

`switch` is used for multi-branch logic over an enum or a `conditions` block. Cases are exhaustive by default; the compiler rejects a `switch` with missing cases.

`switch` can be used as a statement or as an expression. When used as an expression, every case must evaluate to a value of the same type.

```
// Statement form
switch direction {
    case .North => turn(90);
    case .South => turn(270);
    case .East  => turn(0);
    case .West  => turn(180);
}

// Expression form - switch evaluates to a value
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

### `conditions`

`conditions` promotes arbitrary runtime predicates into a named, exhaustive set of cases that can be switched over. Each case is a named predicate. Cases are evaluated top to bottom; the first matching case wins.

`conditions` blocks are always exhaustive. The developer is responsible for declaring cases that cover all possible states, and the compiler rejects any `switch` over a `conditions` block with missing cases, the same rule that applies to enum-based `switch`.

```
conditions WaterPhase {
    Ice:    temp < 0;
    Liquid: temp < 100;
    Steam:  temp >= 100;
}

switch WaterPhase {
    case .Ice    => freeze();
    case .Liquid => liquid();
    case .Steam  => boil();
}
```

`conditions` can also be used with the expression form of `switch`:

```
def label = switch WaterPhase {
    case .Ice    => "ice";
    case .Liquid => "liquid";
    case .Steam  => "steam";
}
```

The compiler warns if declared predicates overlap, as an unreachable case is almost certainly a bug:

```
conditions Access {
    Banned:      isBanned;
    BannedAdmin: isBanned && isAdmin;  // warning: unreachable, Banned covers this
    Allowed:     !isBanned;
}
```

The unique value of `conditions` over a plain `if`/`no` chain is that the predicate set is declared once and reused across multiple `switch` sites. If the classification logic changes, it changes in one place:

```
// Both switch sites stay consistent automatically
switch WaterPhase {
    case .Ice    => applyIceShader();
    case .Liquid => applyWaterShader();
    case .Steam  => applySteamShader();
}

switch WaterPhase {
    case .Ice    => playIceSound();
    case .Liquid => playWaterSound();
    case .Steam  => playSteamSound();
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

The following is a summary of all built-in annotations shipped with the standard library.

### Visibility and Exposure

| Annotation | Valid On | Effect |
|---|---|---|
| `@Exposed` | `object` methods | Makes a method public (overrides private-by-default) |
| `@Hidden` | `service` methods | Makes a method private (overrides public-by-default) |

### Mutability

| Annotation | Valid On | Effect |
|---|---|---|
| `@Mutable` | Local variables, `object` and `service` properties | Allows reassignment of a local variable, or opts a property into mutability. Compiler warning if used alongside a `set` body. |
| `@Const` | Functions | Disallows any mutation in the entire execution path of the function |

### Contracts and Implementation

| Annotation | Valid On | Effect |
|---|---|---|
| `@Implements(Contract, ...)` | `object`, `service` | Declares that this type fulfills one or more contracts |
| `@Alias("op")` | `contract` method signatures | Allows the method to be called using an operator or shorthand symbol |

### Inheritance

| Annotation | Valid On | Effect |
|---|---|---|
| `@Extensible` | `object`, `service` | Allows this type to be inherited. Types are sealed by default. |
| `@Inherits(Type)` | `object`, `service` | Inherits from the specified type. May appear at most once. Parent must be `@Extensible`. |

### Dependency Injection and Lifetime

| Annotation | Valid On | Effect |
|---|---|---|
| `@Singleton(Contract)` | Module binding | One instance for the lifetime of the application |
| `@Scoped(Contract)` | Module binding | One instance per logical scope (e.g., a request or session) |
| `@Transient(Contract)` | Module binding | A fresh instance every time it is needed |
| `@Mock(Module)` | `module` | Shadows bindings from the target module for testing purposes |

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
- How does error handling work? Exceptions, result types, or something new?
- Should generics support variance annotations?
- Should mutable local variables use `@Mutable` as an annotation or a dedicated keyword?
- Can a `conditions` block reference variables outside its declaration scope, or is it always bound to a single variable?
- For annotation-specific open questions, see [Annotations/README.md](Annotations/README.md).
