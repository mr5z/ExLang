
# Questions
- what is the difference between an immutable and a constant object?
- are primitive types object?
- should we have distinction between primitive types vs object types?
- should we make everything an object? (Java: will you ever learn?)

# Proposals
- no operator overloading, devs are free to alias the function names with whatever symbol or combination of symbols
    - when a function has an alias and parameterless, the alias cannot appear on its right side
- no struct/class distinction, i.e., behavior depends on implementation
- by default, parameters are immutable
- function overloading allows having the same function name with different return type
- enumerating/looping an object is allowed
- when working with attributes, devs have access to code metadata and compiler contexts


```
def x = 0 // denotes x is a immutable variable with a type of a Number variant with a value of 0
```
        
# Syntax Examples

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