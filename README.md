# ExLang

ExLang is a programming language designed around a single core belief:

> **If a pattern is proven and universal, it shouldn't be a pattern. It should be the language.**

Design patterns exist largely because languages are missing features. ExLang is an attempt to be intentional and systematic about this from the start, baking industry-standard patterns in as first class language features, so developers spend cognitive energy on problems that matter, not on boilerplate that doesn't.

The guiding principle for every design decision is: **reduce cognitive load without sacrificing explicitness.**

---

# Philosophy

- **Proven patterns are language features.** Dependency injection, value semantics, data transfer: these are not conventions or frameworks in ExLang, they are built into the language itself.
- **Intent over mechanism.** Developers declare *what* they want. The compiler figures out *how*.
- **Smart defaults, explicit escape hatches.** The language should handle 99% of cases automatically. The 1% who need explicit control have the tools to do so.
- **Compiler as safety net.** Bugs that are caught at compile time cannot exist at runtime. ExLang moves as many error classes as possible into the compiler.

---

# Core Concepts

ExLang has six fundamental keywords, each with a distinct and enforced purpose:

| Keyword | Purpose | Mutable | Dependencies | Identity |
|---|---|---|---|---|
| `dto` | Pure data shape, no behavior | ❌ | ❌ | By value |
| `object` | Self-contained behavioral type | ❌ | ❌ | By value |
| `contract` | Abstract dependency boundary | N/A | N/A | N/A |
| `service` | Stateful type with dependencies | ✅ | ✅ | By reference |
| `module` | Declares and binds the dependency graph | N/A | N/A | N/A |
| `def` | Instantiation, brings anything into existence | contextual | N/A | N/A |

The key distinction:
- `dto`, `object`, `contract`, `service`, `module` are **declarations**: they describe shape, behavior, and wiring
- `def` is **instantiation**: it brings something into existence

`def` appears everywhere: declaring a field, a function, a variable, or a dependency. It always means the same thing: *I am bringing something into existence here.*

---

# Defaults

## `dto`
- Fields are public read-only properties by default
- Always immutable
- Always sealed: cannot be inherited under any circumstance
- Stack-allocated
- Automatically serializable

## `object`
- Fields are private by default, exposed only via explicit properties
- Always immutable
- Sealed by default. Use `@Extensible` to allow inheritance
- Can implement contracts via `@Implements`, but cannot be a module binding target

## `contract`
- Signatures only: no fields, no definitions, no default implementations
- Cannot be instantiated directly

## `service`
- Fields are private by default, exposed only via explicit properties
- Mutable
- Sealed by default. Use `@Extensible` to allow inheritance
- DI-managed: participates in dependency injection via module bindings
- Identified by reference, not by value

---

# Declaration Types

## DTO

A `dto` is pure data. No behavior, no dependencies, no identity. Two DTOs with the same values are the same thing. All fields are implicitly public read-only properties. DTOs are always sealed and cannot be inherited.

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

## Object

An `object` has behavior but no dependencies. It is self-contained, immutable, and defined by its values rather than its identity. Two `Money` objects with the same amount and currency are interchangeable.

Fields are private by default. Public state is exposed via explicit properties.

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

    add(other: Money): Money {
        // ...
    }

    isZero(): Bool {
        // ...
    }
}
```

Objects are sealed by default. Use `@Extensible` to allow inheritance, and `@Inherits` to inherit from another object. Only single inheritance is allowed.

```
@Extensible
object Money {
    _amount: f32;
    _currency: String;

    amount: f32 {
        get => _amount;
    }
}

@Inherits(Money)
object DiscountedMoney {
    _discountRate: f32;

    discountRate: f32 {
        get => _discountRate;
    }

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
    print() {
        // ...
    }
}
```

## Contract

A `contract` defines an abstract dependency boundary. It describes *what* something can do without specifying *how*. Contracts contain signatures only: no fields, no definitions, no default implementations.

```
contract Logger {
    log(message: String);
}

contract PaymentGateway {
    charge(amount: Money): Result;
}
```

Both `service` and `object` types can implement contracts using `@Implements`. Only `service` types can be bound in a module and participate in dependency injection.

## Service

A `service` has behavior, mutable state, and dependencies. It is the only declaration type that participates in dependency injection. Services are identified by reference, not by value. Two instances of the same service are distinct objects.

Fields are private by default. Public state is exposed via explicit properties.

```
service Counter {
    _count: i32;

    count: i32 {
        get => _count;
    }

    increment() {
        _count++;
    }
}
```

Mutable properties expose both a getter and a setter:

```
service Rectangle {
    _width: f32;

    width: f32 {
        get => _width;
        set => _width = value;
    }
}
```

ExLang bakes dependency injection in as a first class language feature. The rule is simple:

> **If a field's type is a `contract`, it is automatically a dependency. The compiler resolves and injects it.**

No annotations, no frameworks, no constructor boilerplate. The type itself is the signal.

A `service` declares which contracts it fulfills using `@Implements`. Multiple contracts are supported either by separating them with commas or by repeating the annotation.

```
@Implements(Logger)
service ConsoleLogger {
    log(message: String) {
        // ...
    }
}

// multiple contracts, single annotation
@Implements(Logger, Disposable)
service FileLogger {
    log(message: String) {
        // ...
    }

    dispose() {
        // ...
    }
}

// multiple contracts, multiple annotations (equivalent to above)
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

Constructor dependencies are declared in the service signature. Only `contract` types are allowed as constructor parameters. This is enforced by the compiler with no exceptions.

```
service UserService(
    gateway: PaymentGateway,   // contract → injected automatically
    logger: Logger             // contract → injected automatically
) {
    process(payment: Money): Result {
        // ...
    }
}
```

This constraint cleanly separates two concerns:
- **Constructors** are for wiring dependencies
- **Initializers / factory functions** are for providing data

## Module

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

### Lifetime Scopes

Lifetime is declared at the binding site via the scope annotation:

- `@Singleton(Contract)`: one instance for the lifetime of the application
- `@Scoped(Contract)`: one instance per logical scope (e.g., a request, a session)
- `@Transient(Contract)`: a fresh instance every time it is needed

### Testing

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

# Inheritance and Implementation Rules

- `@Extensible` may appear on `object` and `service` types to allow inheritance. Types are sealed by default.
- `@Inherits` may appear **at most once** on any `service` or `object`. Multiple inheritance is not allowed.
- `@Inherits` requires the parent type to be marked `@Extensible`. Inheriting a sealed type is a compile error.
- `@Implements` may appear **multiple times**, or accept multiple contracts separated by commas. Both forms are equivalent.
- `@Implements` is valid on both `service` and `object` types. `dto` cannot implement contracts.
- Only `service` types can be bound in a module. An `object` implementing a contract cannot appear as a module binding target.
- A `service` that `@Inherits` another `service` automatically inherits its dependencies.
- The compiler enforces that all contract method signatures are implemented. Missing implementations are compile errors.

---

# Variable Declaration

`def` is used for all instantiation. The compiler infers type from context.

```
// immutable variable, type inferred as a Numeric variant, initial value 0
def x = 0;
```

---

# Function Aliases

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

# Type Inference

```
// doSomething() returns either i8 or Stream<i8> based on inferred type
def result: i8 = doSomething();
def resultList: Stream<i8> = doSomething();
```

---

# Self and Access to Implementing Type

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

# Mutability

Mutability is contextual:

1. Local variables are mutable by default
2. Parameters are immutable by default
3. Instance fields are private and mutable in `service`, private and immutable in `object`
4. `dto` fields are always public and read-only

```
// #1 local variables
doSomething() {
    def a: i32 = 0;
    a = 1;  // ok

    @Immutable
    def b: i32 = 0;
    b = 1;  // error
}

// #2 parameters
doSomething(
    @Mutable
    a: i32,
    b: i32) {

    a = 0;  // ok
    b = 1;  // error
}

// #3 service fields: private, mutable, exposed via property
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

// #4 dto fields: always public read-only
dto Point {
    x: f32;
    y: f32;
}

def p = Point();
p.x = 1.0;  // error, dto fields are read-only
```

---

# Const

Marking a function `@Const` disallows any mutation in its entire execution path.

```
_position: u32;

@Const
doSomething() {
    self._position += 1;  // error (mutating instance field)
    def i = 4;            // local variable
    i = 2;                // ok

    advance();            // error (advance is not @Const)
}

advance() {
    is _position <= _text.length {
        _position++;
    }
}
```

---

# Conditional Statement

`is` is equivalent to `if`, `no` is equivalent to `else`. There is no `else if`. Use `switch` for multi-branch logic.

```
is x == y {
    doThis();
}
no {
    doThat();
}

switch enumValue {
    case .North { turn(90); }
    case .South { turn(270); }
    case .East  { turn(0); }
    case .West  { turn(180); }
}
```

---

# Tagging

The standard library provides a way to tag functions based on compute bounds: CPU, IO, or custom tags. This gives developers a high-level overview of how functions are tied together. The linter warns about mixing tags that may cause performance issues.

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

# Open Questions

- What is the full spec for discriminated unions, and does the `,` syntax conflict with multi-return?
- How does `@Const` interact with injected dependencies?
- Should `contract` support default implementations?
- What is the concurrency model? Does the `@Tag` system extend to async boundaries?
- What is the full null safety spec beyond `String?`?
- How does error handling work? Exceptions, result types, or something new?
- Should generics support variance annotations?
