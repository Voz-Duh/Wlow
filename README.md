
# Wlow language systems introduction

### Termins
- `Value finallization` is a process of getting expression's value
- `Storing` is a process of saving value to container, list of container types: `mut *T`, `(T0, T1)`, `{field0 T0, field1 T1}`
___
### Strange variables
Variables is a jumpable label and mutable container in one time.

- Use like label: `0=i |> i`

Here variable will be used as label, because `... |> i` is must be readed like "from ... to i".
That constructions is can be used to create loops, etc.

- Use like container: `0=i |> (i)`

Here variable will be used as container, because `(i)` is an expression, you can't go into an expression, only return it.
___
### `never` type
`never` is the type of computations that never successfully produce a value, also, `never` is a constant-value with no produced value.

*`never` is unstorable and unfinallizable, but returnable.*
___
### `void` constant
`void` constant is a value that don't produce value, can be used in effect-only functions.

*`void` is unstorable and unfinallizable, but returnable.*
```
print :: 'v |> log' v |> void
```
___
### Smart functions
Functions in Wlow language uses a system which made coding a lot more comfortable - **lazy function definition**.

**Lazy function definition** is a system in where your function definition is a declaration with a body: `id :: 'a |> a`, that ident function can be used for any type in whole language, if that type is re turnable, code: `id' 1`, `id' true`, `id' 4.45`, `id' (true, 1)`, all will works because of that system.

For the first example we'll look at ident function example:
```
id :: 'a |> a
main
:: id' 1
|> id' true
|> id' 4.45
|> id' (true, 1)
```
That code will be compiled completely fine, because:
```
id :: 'a ? |> a -- argument "a" has placeholder instead of a real type
main
:: id' 1 -- here id is called as '(i32) i32 - we'll get a new definition for id
|> id' true -- here id is called as '(bool) bool - we'll get a new definition for id
|> id' 4.45 -- here id is called as '(f32) f32 - we'll get a new definition for id
|> id' (true, 1) -- here id is called as '((bool, i32)) (bool, i32) - we'll get a new definition for id
```
So, that system makes a declarartion with body instead of a real definition.

Notice, that function type is not storable while has placeholder in arguments, process of removing arguments placeholders is **function fixating**.

*Function is unstorable, finallizable, unreturnable.*
___
Function fixating is a process of removing placeholders in function arguments to get compilable function declaration which is will be defined at value finallization.
For example `('a |> a) |> a` is cannot be compiled, but after cast to specified function type `('a |> a) |> a ->  '(i32) i32` or specify arguments type in declaration `('a i32 |> a) |> a` - function will be successful compiled to fixated function.

Fixated functions is a functions that are compiled to a real function pointer, only fixated function can be stored.

*Fixated functions is storable, finallizable, returnable.*
