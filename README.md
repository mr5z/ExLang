
# Questions
- What is the difference between an immutable and a constant object?
- Are primitive types object?
- Should we have distinction between primitive types vs object types?
- Should we make everything an object? (Java: will you ever learn?)

# Proposals
- Use keyword `def` for all declarations (class/interface/variable)
  - Simplifies syntax and learning curve
  - Compiler infers the declaration type from context

- No operator overloading, but allow function aliasing
  - Functions can have symbolic aliases (e.g., @Alias("+"))
  - When a function has an alias and is parameterless, the alias cannot appear on its right side
  - Maintains readability while providing flexibility

- No struct/class distinction
  - Behavior depends on implementation
  - Simplifies type system

- Parameters are immutable by default
  - Functions marked with @Const guarantee no mutations in their execution path
  - Provides clear guarantees about data modification

- Function overloading allows different return types
  - Return type is part of the function signature
  - Type inference determines correct version based on context

- Discriminated unions use comma syntax
  - `def x: TypeA, TypeB, TypeC`
  - Simpler than complex sum types

- Enum-style access uses dot notation
  - `.North, .South, .East, .West`
  - Consistent with other member access

- Attribute system provides compiler metadata access
  - Attributes can influence compilation
  - Provides extensibility without syntax complexity

- Module system
  - Explicit imports/exports
  - Clear dependency management

- Pattern matching for discriminated unions
  - Essential for working with union types
  - Ensures type-safe handling of variants


# Variable Declaration
```
// denotes x is an immutable variable with a type of a Numeric variant with an initial value of 0
def x = 0
```

# Function Aliases
```
contract Numeric {
    // ...
    @Alias("+")
    def plus(other: Self): Self = self + other
    // ...
}

def u8: Numeric {
    // ...
}

def n: u8 = 0
n = n.plus(1)
n = n + 1 // possible due to function alias
```

# Type Inference
```
// doSomething() is able to return both i8 and Stream<i8> based on the inferred type
def result: i8 = doSomething()
def resultList: Stream<i8> = doSomething()
```

# Self and access to implementing class type
```
contract Role { self ->

    // Self: type of implementing class
    // self: name of variable instance, aka 'this',
    //       and can be renamed to whatever you want as long as you define it above
    def assign(other: Self): Self {
        // ...
    }
}

def UserRole: Role { this -> // type of 'this' is Self but manually named to 'this'
    
    // Self is now UserRole
    def assign(other: UserRole): UserRole {
        // ...
    }
}
```

# Mutability
Mutability are contextual:

1. Local variables are mutable by default
2. Parameters are immutable by default
3. Instance fields are mutable by default

```

// #1
def doSomething() {

    def a: i32 = 0
    a = 1  // ok

    @Immutable
    def b: i32 = 0
    b = 1  // error
}

// #2
def doSomething(
    @Mutable
    a: i32,
    b: i32) {

    a = 0 // ok!
    b = 1 // error
}

// #3
def Rectangle: Shape {
    @Public
    def area: i32 -> width * length

    @Public
    @Immutable
    def name: String?
}

def rect = Rectangle()
rect.area = 0      // error
rect.name = "Box!" // ok
```

# Const
```
@Const
def doSomething() {
    self._position += 1 // error (mutating instance field)
    def i = 4           // (local variable)
    i = 2               // ok!
}
```
        
# Standard library code example

```
attribute Public { context -> // context: Context
    init() {
        // communicate with compiler
        def scope = context.getScope()
        scope.allow([.external, .internal])
        scope.assemblyAccess([.all])
    }
}
        
attribute Private {
    init() {
        // communicate with compiler
    }
}
        
attribute Alias {
    init(value: String) {
        // communicate with compiler
    }
}

@Public
def Stream<T>(array: [T]) {

    @Private
    def _array: [T] = array

    @Private
    def _currentIndex: u32

    @Public
    @Iterator
    def Iterator(): T {
        if (_currentIndex < _array.Length) {
            yield _array[_currentIndex]
            _currentIndex += 1
        }
    }
}
        
@Public
contract Numeric { self -> // self: Self
    @Public
    @Alias("+")
    def plus(other: Self): Self = self + other

    @Public
    @Alias("+=")
    def plusEquals(other: Self): Self = self += other
            
    @Public
    @Alias("-")
    def minus(other: Self): Self = self - other

    @Public
    @Alias("-=")
    def minusEquals(other: Self): Self = self -= other

    @Public
    @Alias("/")
    def divide(other: Self): Self = self / other
            
    @Public
    @Alias("*")
    def multiply(other: Self): Self = self * other

    @Public
    def typeSize: Self
}

@Public
def Bit { this ->
    @Public
    @Alias("<<")
    def leftShift(other: Bit, Stream<Bit>): Bit, Stream<Bit> {
        for bit in other {
            yield this << bit
        }
    }
        
    @Public
    @Alias(">>")
    def rightShift(other: Bit, Stream<Bit>): Bit, Stream<Bit> {
        for bit in other {
            yield this >> bit
        }
    }
        
    @Public
    @Alias("|")
    def or(other: Bit, Stream<Bit>): Bit, Stream<Bit> {
        for bit in other {
            yield this | bit
        }
    }
        
    @Public
    @Alias("&")
    def and(other: Bit, Stream<Bit>): Bit, Stream<Bit> {
        for bit in other {
            yield this & bit
        }
    }
        
    @Public
    @Alias("^")
    def xor(other: Bit, Stream<Bit>): Bit, Stream<Bit> {
        for bit in other {
            yield this ^ bit
        }
    }
        
    @Public
    @Alias("~")
    def not(): Bit, Stream<Bit> {
        for bit in this {
            yield ~bit
        }
    }
}


// signed
@Public
def i8: Numeric {
    def typeSize: i8 = 8
}

@Public
def i16: Numeric {
    def typeSize: i8 = 16
}

@Public
def i32: Numeric {
    def typeSize: i8 = 32
}
        
@Public
def i64: Numeric {
    def typeSize: i8 = 64
}

// unsigned
@Public
def u8: Numeric {
    def typeSize: u8 = 8
}
        
@Public
def u16: Numeric {
    def typeSize: u16 = 16
}
        
@Public
def u32: Numeric {
    def typeSize: u32 = 32
}
        
@Public
def u64: Numeric {
    def typeSize: u64 = 64
}
```
