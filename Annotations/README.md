# Annotations

> This document is a work in progress. The designs described here are draft proposals and subject to change.

Annotations in ExLang are a meta-system that allows declarative, structured metadata to be attached to language constructs. They serve two purposes: communicating intent to other developers, and influencing compiler and analyzer behavior through a stable extension API.

All built-in annotations, including those that ship with the standard library, are implemented using the same annotation system available to any developer. The compiler and analyzer are not aware of any specific annotation by name; instead, they expose a set of hooks that annotations can declare access to.

---

## Table of Contents

1. [Declaring an Annotation](#declaring-an-annotation)
2. [Targeting](#targeting)
3. [Compiler Hooks](#compiler-hooks)
4. [Built-in Annotations](#built-in-annotations)
5. [Open Questions](#open-questions)

---

## Declaring an Annotation

Annotations are declared using the `annotation` keyword. An annotation may optionally define an `init` function that receives a `Context` parameter, through which it can interact with the compiler and analyzer.

```
annotation Deprecated {
    init(context: Context) {
        // ...
    }
}
```

Annotations with parameters declare them as additional `init` arguments, after `context`:

```
annotation Alias {
    init(context: Context, value: String) {
        // ...
    }
}
```

`init` is the standard construction entry point across all types in ExLang that declare one. For `annotation`, it is called by the compiler at the point the annotation is applied. It is not callable directly and cannot be overloaded, since an annotation is always applied in exactly one way.

---

## Targeting

The `@Target` annotation controls which language constructs an annotation is valid on. Applying an annotation to an invalid target is a compile error.

```
@Target(.Function)
annotation Const {
    init(context: Context) {
        // ...
    }
}

@Target(.LocalVariable)
annotation Mutable {
    init(context: Context) {
        // ...
    }
}

@Target(.Type)
annotation Implements {
    init(context: Context) {
        // ...
    }
}
```

Multiple targets are supported by passing additional values:

```
@Target(.Type, .Function)
annotation Deprecated {
    init(context: Context) {
        // ...
    }
}
```

The available target values are:

| Target | Applies To |
|---|---|
| `.Type` | `dto`, `object`, `service`, `contract`, `module` declarations |
| `.Function` | Any function or method |
| `.Property` | Explicit property declarations |
| `.Field` | Private fields |
| `.LocalVariable` | Variables declared inside a function body |
| `.Parameter` | Function parameters |

---

## Compiler Hooks

> This section is unresolved. The design of the `Context` API is an open question.

The `Context` parameter passed to `init` is the annotation's interface to the compiler and analyzer. The intent is that annotations declare constraints through a stable, namespaced API rather than accessing compiler internals directly. This keeps the compiler and analyzer decoupled from any specific annotation implementation.

A rough sketch of what this might look like:

```
@Target(.LocalVariable)
annotation Mutable {
    init(context: Context) {
        context.binding.allowReassignment()
    }
}

@Target(.Function)
annotation Const {
    init(context: Context) {
        context.executionPath.disallowMutation()
    }
}
```

The compiler exposes stable namespaced surfaces such as `context.binding`, `context.executionPath`, and `context.visibility`. The exact shape of this API, how expressive it needs to be, and whether third-party annotations should have the same level of access as stdlib annotations are all unresolved.

---

## Built-in Annotations

The following annotations ship with the standard library. They are implemented using the same annotation system described in this document and have no privileged compiler status.

### Visibility and Exposure

| Annotation | Target | Effect |
|---|---|---|
| `@Exposed` | `.Function` on `object` | Makes a method public (overrides private-by-default) |
| `@Hidden` | `.Function` on `service` | Makes a method private (overrides public-by-default) |

### Mutability

| Annotation | Target | Effect |
|---|---|---|
| `@Mutable` | `.LocalVariable` | Allows reassignment of a local variable (immutable by default) |
| `@Const` | `.Function` | Disallows any mutation in the entire execution path of the function |

### Contracts and Implementation

| Annotation | Target | Effect |
|---|---|---|
| `@Implements(Contract, ...)` | `.Type` | Declares that this type fulfills one or more contracts. |
| `@Alias("op")` | `.Function` on `contract` | Allows the method to be called using an operator or shorthand symbol |

### Inheritance

| Annotation | Target | Effect |
|---|---|---|
| `@Extensible` | `.Type` | Allows this type to be inherited. Types are sealed by default. |
| `@Inherits(Type)` | `.Type` | Inherits from the specified type. May appear at most once. Parent must be `@Extensible`. |

### Dependency Injection and Lifetime

| Annotation | Target | Effect |
|---|---|---|
| `@Singleton(Contract)` | Module binding | One instance for the lifetime of the application |
| `@Scoped(Contract)` | Module binding | One instance per logical scope (e.g., a request or session) |
| `@Transient(Contract)` | Module binding | A fresh instance every time it is needed |
| `@Mock(Module)` | `.Type` on `module` | Shadows bindings from the target module for testing purposes |

### Tagging

| Annotation | Target | Effect |
|---|---|---|
| `@Tag(bound)` | `.Function` | Marks a function with a compute bound (`IO`, `CPU`, or custom). The linter warns on mixed-bound call sites. |

### Meta

| Annotation | Target | Effect |
|---|---|---|
| `@Target(...)` | `.Type` on `annotation` | Declares which constructs an annotation is valid on. Applying an annotation outside its declared targets is a compile error. |

---

## Open Questions

- What is the full shape of the `Context` API? Which namespaces and methods does it expose?
- Should third-party annotations have the same level of `Context` access as stdlib annotations, or should access be tiered?
- How does `@Target` handle its own self-referential case? What is `@Target`'s own target?
- Can annotations be applied to other annotations beyond `@Target`? For example, could `@Deprecated` be applied to a stdlib annotation?
- What is the execution model for `init`? Is it run at compile time, and if so, what guarantees exist about ordering when multiple annotations are applied to the same construct?
- Should annotations support inheritance or composition?
