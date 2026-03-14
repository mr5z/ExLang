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

ExLang has five fundamental keywords, each with a distinct and enforced purpose:

| Keyword | Purpose | Mutable | Dependencies | Identity |
|---|---|---|---|---|
| `dto` | Pure data shape, no behavior | ❌ | ❌ | By value |
| `object` | Self-contained behavioral type | ❌ | ❌ | By value |
| `contract` | Abstract dependency boundary | N/A | N/A | N/A |
| `service` | Stateful type with dependencies | ✅ | ✅ | By reference |
| `def` | Instantiation, brings anything into existence | contextual | N/A | N/A |

The key distinction:
- `dto`, `object`, `contract`, `service` are **declarations**: they describe shape and behavior
- `def` is **instantiation**: it brings something into existence

`def` appears everywhere: declaring a field, a function, a variable, or a dependency. It always means the same thing: *I am bringing something into existence here.*

---

# Declaration Types

## DTO

A `dto` is pure data. No behavior, no dependencies, no identity. Two DTOs with the same values are the same thing. DTOs are immutable by default and are stack-allocated. The compiler handles memory automatically with no developer involvement.

```
dto Point {
    def x: f32
    def y: f32
}

dto UserResponse {
    def id: u32
    def name: String
    def email: String
}
```

DTOs are the standard way to pass data across boundaries: between services, across network calls, in and out of functions. They are automatically serializable.

## Value Object

An `object` has behavior but no dependencies. It is self-contained, immutable, and defined by its values rather than its identity. Two `Money` objects with the same amount and currency are interchangeable.

```
object Money {
    def amount: f32
    def currency: String

    def add(other: Money): Money {
        // ...
    }

    def isZero(): Bool {
        // ...
    }
}
```

Objects are appropriate for domain concepts that have logic but don't need external services: money, coordinates, dates, ranges, identifiers.

## Contract

A `contract` defines an abstract dependency boundary. It describes *what* something can do without specifying *how*. Contracts are the backbone of ExLang's dependency injection system. Services depend on contracts, never on concrete implementations.

```
contract Logger {
    def log(message: String)
}

contract PaymentGateway {
    def charge(amount: Money): Result
}
```

Contracts can only be implemented by `service` types. A `dto` or `object` implementing a contract would imply external dependencies, which violates their guarantees.

## Service

A `service` has behavior, mutable state, and dependencies. It is the only declaration type that participates in dependency injection. Services are identified by reference, not by value — two instances of the same service are distinct objects.

ExLang bakes dependency injection in as a first class language feature. The rule is simple:

> **If a field's type is a `contract`, it is automatically a dependency. The compiler resolves and injects it.**

No annotations, no frameworks, no constructor boilerplate. The type itself is the signal.

```
service UserService(
    gateway: PaymentGateway,   // contract → injected automatically
    logger: Logger             // contract → injected automatically
) {
    def process(payment: Money): Result {
        // ...
    }
}
```

Constructors may **only** accept `contract` types. Primitives, DTOs, and value objects are not allowed in constructors. They are not dependencies, they are data. This is enforced by the compiler with no exceptions.

This constraint cleanly separates two concerns:
- **Constructors** are for wiring dependencies
- **Initializers / factory functions** are for providing data

### Module Bindings

Somewhere the compiler needs to know which concrete type fulfills each contract. This is declared in a `module`:

```
service ConsoleLogger: Logger { ... }
service StripeGateway: PaymentGateway { ... }
service PostgresSession: DatabaseSession { ... }

service AppModule {
    bind Logger -> ConsoleLogger @Singleton
    bind PaymentGateway -> StripeGateway @Scoped
    bind DatabaseSession -> PostgresSession @Scoped
}
```

The compiler statically analyzes the entire dependency graph from the module declaration. The following are all **compile errors**, not runtime crashes:

- Circular dependencies
- A `@Transient` service injected into a `@Singleton`
- A missing binding for a declared dependency
- Unused bindings

### Lifetime Scopes

Lifetime is declared at the binding site, not on the type itself:

- `@Singleton`: one instance for the lifetime of the application
- `@Scoped`: one instance per logical scope (e.g., a request, a session)
- `@Transient`: a fresh instance every time it is needed

### Testing

Test modules can shadow bindings from the application module:

```
service MockLogger: Logger { ... }
service StubGateway: PaymentGateway { ... }

service TestModule: AppModule {
    bind Logger -> MockLogger
    bind PaymentGateway -> StubGateway
}
```

---

# Variable Declaration

`def` is used for all instantiation. The compiler infers type from context.

```
// immutable variable, type inferred as a Numeric variant, initial value 0
def x = 0
```

---

# Function Aliases

```
contract Numeric {
    @Alias("+")
    def plus(other: Self): Self = self + other
}

object u8: Numeric {
    // ...
}

def n: u8 = 0
n = n.plus(1)
n = n + 1  // possible due to function alias
```

---

# Type Inference

```
// doSomething() returns either i8 or Stream<i8> based on inferred type
def result: i8 = doSomething()
def resultList: Stream<i8> = doSomething()
```

---

# Self and Access to Implementing Type

```
contract Role { self ->

    // Self: type of the implementing class
    // self: instance variable (like 'this'), renameable
    def assign(other: Self): Self {
        // ...
    }
}

object UserRole: Role { this ->

    // Self is now UserRole
    def assign(other: UserRole): UserRole {
        // ...
    }
}
```

---

# Mutability

Mutability is contextual:

1. Local variables are mutable by default
2. Parameters are immutable by default
3. Instance fields are mutable by default
4. `dto` and `object` types are always immutable

```
// #1 local variables
def doSomething() {
    def a: i32 = 0
    a = 1  // ok

    @Immutable
    def b: i32 = 0
    b = 1  // error
}

// #2 parameters
def doSomething(
    @Mutable
    a: i32,
    b: i32) {

    a = 0  // ok
    b = 1  // error
}

// #3 instance fields
def Rectangle: Shape {
    @Public
    def area: i32 -> width * length

    @Public
    @Immutable
    def name: String?
}

def rect = Rectangle()
rect.area = 0       // error
rect.name = "Box!"  // ok
```

---

# Const

Marking a function `@Const` disallows any mutation in its entire execution path.

```
@Private
def _position: u32

@Const
def doSomething() {
    self._position += 1  // error (mutating instance field)
    def i = 4            // local variable
    i = 2                // ok

    advance()            // error (advance is not @Const)
}

def advance() {
    if _position <= _text.length {
        _position++
    }
}
```

---

# Conditional Statement

`is` is equivalent to `if`, `no` is equivalent to `else`. There is no `else if`. Use `switch` for multi-branch logic.

```
is x == y {
    doThis()
}
no {
    doThat()
}

switch enumValue {
    case .North { turn(90) }
    case .South { turn(270) }
    case .East  { turn(0) }
    case .West  { turn(180) }
}
```

---

# Tagging

The standard library provides a way to tag functions based on compute bounds: CPU, IO, or custom tags. This gives developers a high-level overview of how functions are tied together. The linter warns about mixing tags that may cause performance issues.

```
@Tag(.IO)
def requestUserInfo(id: u32): User {
    // network request
}

@Tag(.CPU)
def crunchSomeNumber(data: Vec<f32>): f32 {
    // math-heavy computation
}

// linter warns about mixing bounds
def doWork() {
    def user = requestUserInfo(userId)
    def x = crunchSomeNumber(data)
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
