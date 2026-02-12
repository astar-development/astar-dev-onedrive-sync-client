---
applyTo: '**/*.cs'
---

Coding standards and style guidelines / preferences for C# files in this repository that AI must follow.

## Namespaces

- Use file-scoped namespaces.
- Namespace names should follow the pattern: Company.Project.Module (e.g., Contoso.Sales.Reporting).
- Avoid unnecessary nested namespaces; keep the structure flat when possible.

## Classes and Methods
- Define one class per file, and name the file after the class.
- Follow SOLID principles for class design.
- Follow SOLID principles for method design.
- Ensure good cohesion within classes and methods.
- Ensure low coupling between classes and methods.
- Ensure methods do one thing and do it well.
- Keep methods short; ideally under 20 lines.
- Use meaningful names for classes and methods that clearly convey their purpose.
- Put all method / constructor overloads together in the same order as their parameters.
- Put all method / constructor parameters on one line when possible; otherwise, wrap parameters to multiple lines but avoid 1 parameter per line.
- Use expression-bodied members for simple methods and properties.
- Keep method and constructor parameters to a minimum; prefer using parameter objects when multiple parameters are needed.
- Avoid long parameter lists; consider using the Builder pattern for complex object construction.
- Use dependency injection for managing dependencies.
- Prefer composition over inheritance.

## Immutability
- Prefer immutable data structures and objects where possible.
- Prefer record over classes for immutable types.

## Record Design
- Define record properties on the same line with the record declaration when possible.
- Accompany each record `<name>` with a corresponding `<name>Factory` static factory class.
- Place the factory class in the same file as the record it creates.
- Expose static `Create` methods on the factory class for constructing instances of the record.
- Place argument validation logic within the factory methods.
- Never use the public constructor of a record directly; always use the factory methods.
- Use immutable collections (e.g., `IReadOnlyList<T>`, `IReadOnlyDictionary<TKey, TValue>`) for record properties that hold multiple values.
- Avoid methods on records; use extension methods instead for any behavior related to the record.

## Discriminated Unions
- Use records with inheritance to model discriminated unions.
- Define an abstract base record for the union type and derive specific case records from it.
- Place all case records in the same file as the base record.
- Define one static factory class per discriminated union type.
- Expose static `Create` methods on the factory class for constructing instances of each case record.

## Test Classes

- Name test classes with the suffix `Should` (e.g., `OrderServiceShould`).
- Organize test methods using the `<Action><Result>[<Exception>]` naming convention (e.g., `ReturnCorrectSumWhenMultipleItemsExist`). This pattern helps clarify the action being tested, the expected result, and any exceptions that may be thrown.
- Use the Arrange-Act-Assert (AAA) pattern within test methods to structure the code clearly. Divide the method into three distinct sections: setup (Arrange), execution (Act), and verification (Assert). Do not comment these sections; the structure should be clear from the code itself. Separate these sections with a single blank line for readability.
- Use test data builders to create complex test objects, enhancing readability and maintainability of tests.
- Avoid logic in test methods; keep tests simple and focused on behavior verification.
- Use Shouldly for more readable and expressive assertions in tests.
- Use NSubstitute for mocking dependencies in tests. Avoid mocking when possible; prefer using real instances or test doubles.