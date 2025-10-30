
rename language

`;`/`;;` delimiter {cm:2025-10-30}
`if cond0 = val0; elif cond1 = val1; else = val2` {cm:2025-10-30}
`mut name = value` {cm:2025-10-30}
`let name = value` {cm:2025-10-30}
`fn` function {cm:2025-10-30}
- arguments {cm:2025-10-30}
- - mutability {cm:2025-10-30}
- - name {cm:2025-10-30}
- - type {cm:2025-10-30}
- `->` with return type {cm:2025-10-30}
-  body {cm:2025-10-30}
compound assignment operators {cm:2025-10-30}
- `=` {cm:2025-10-30}
- `+=` {cm:2025-10-30}
- `-=` {cm:2025-10-30}
- `*=` {cm:2025-10-30}
- `/=` {cm:2025-10-30}
- `%=` {cm:2025-10-30}
- `~=` {cm:2025-10-30}
- `<<<=` {cm:2025-10-30}
- `>>>=` {cm:2025-10-30}
- `<<=` {cm:2025-10-30}
- `>>=` {cm:2025-10-30}
- `|=` {cm:2025-10-30}
- `&=` {cm:2025-10-30}
`'` calling {cm:2025-10-30}
- `,` arguments delimiter {cm:2025-10-30}
binary
- `&&` {cm:2025-10-30}
- `||` {cm:2025-10-30}
- `==` `!=` `<` `<=` `>` `>=` {cm:2025-10-30}
- `+` `-` `|` {cm:2025-10-30}
- `*` `/` `&` `%` `>>` `>>>` `<<` `<<<` {cm:2025-10-30}
- `->` cast
- `->>` bitcast
- `??` left - errored expression, right - default lue on error
unary {cm:2025-10-30}
- prefixes {cm:2025-10-30}
- - `-` minus {cm:2025-10-30}
- - `+` plus {cm:2025-10-30}
- - `^` deref {cm:2025-10-30}
- - `&` ref {cm:2025-10-30}
- - `!` logical not {cm:2025-10-30}
- - `~` xor {cm:2025-10-30}
- suffixes {cm:2025-10-30}
- - `^` another deref {cm:2025-10-30}
- - `?` error propogation {cm:2025-10-30}
- - `!` panic on error {cm:2025-10-30}
- - `.0` index field access {cm:2025-10-30}
- - `.field` name field access {cm:2025-10-30}
`type` to define types
`let type` to define immutable types
types
- `i8` {cm:2025-10-30}
- `i16` {cm:2025-10-30}
- `i32` {cm:2025-10-30}
- `i64` {cm:2025-10-30}
- `i128`
- `iptr`
- `u8` {cm:2025-10-30}
- `u16` {cm:2025-10-30}
- `u32` {cm:2025-10-30}
- `u64` {cm:2025-10-30}
- `u128`
- `uptr`
- `f16`
- `f32`
- `f64`
- `f128`
- `bool`
- `&(expr)` expression dependent type
- - in `let`/`mut` {cm:2025-10-30}
- - in (`let`) `type`
- - in function arguments {cm:2025-10-30}
- `^T` immutable pointer {cm:2025-10-30}
- `^mut T` mutable pointer {cm:2025-10-30}
- `[]T` immutable pointer with length (aka array)
- `[]mut T` mutable pointer with length (aka array)
- `(T1, T2, ..., Tn)` tuple {cm:2025-10-30}
- - callable tuple {cm:2025-10-30}
- - homogeneous {cm:2025-10-30}
- - heterogeneous {cm:2025-10-30}
- `?` placeholder {cm:2025-10-30}
- `int` signed integer placeholder
- `uint` unsigned integer placeholder
- `float` floating dot placeholder
- `num` number placeholder
values
- numerics
- - integer {cm:2025-10-30}
- - floating
- `in` as "goto variable" {cm:2025-10-30}
- - with compound assignment operators support {cm:2025-10-30}


LLVM backend compilation
fix function's return type with `&(expr)` {cm:2025-10-30}