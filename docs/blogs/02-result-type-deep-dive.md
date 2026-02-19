# Mastering Result<T, TError>: Error Handling Without the Pain

**Published: February 19, 2026**  
**Author: AStar Development Team**  
**Target Audience: Entry-Level C# Developers**  
**Part 2 of the AStar.Dev.Functional.Extensions Series**

---

## Introduction: The Exception Problem

Let's start with a scenario every developer has faced. You're building a user profile update feature, and you write something like this:

```csharp
public void UpdateUserProfile(int userId, string newEmail, string newName)
{
    if (userId <= 0)
        throw new ArgumentException("Invalid user ID");
    
    var user = _database.GetUser(userId);
    if (user == null)
        throw new UserNotFoundException($"User {userId} not found");
    
    if (!IsValidEmail(newEmail))
        throw new ArgumentException("Invalid email format");
    
    if (_database.EmailExists(newEmail) && user.Email != newEmail)
        throw new InvalidOperationException("Email already in use");
    
    user.Email = newEmail;
    user.Name = newName;
    
    try
    {
        _database.SaveChanges();
    }
    catch (DbException ex)
    {
        throw new DatabaseException("Failed to save changes", ex);
    }
}
```

Now, somewhere else in your code, you need to call this method. How do you handle all those possible exceptions?

```csharp
try
{
    UpdateUserProfile(userId, email, name);
    Console.WriteLine("Profile updated successfully!");
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Validation error: {ex.Message}");
}
catch (UserNotFoundException ex)
{
    Console.WriteLine($"User not found: {ex.Message}");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Operation failed: {ex.Message}");
}
catch (DatabaseException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
    // Should we retry? Log? Give up?
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
    // We missed something!
}
```

**This approach has serious problems:**

1. **The method signature lies** - `void UpdateUserProfile(...)` doesn't tell you it can throw five different exception types
2. **Easy to forget exception cases** - Did we catch everything? Who knows!
3. **Difficult to compose** - Try calling this from inside another try-catch block... yikes
4. **Exceptions are expensive** - Creating exceptions is slow and intended for exceptional cases
5. **Control flow is hidden** - The flow jumps unpredictably through catch blocks
6. **Testing is awkward** - You need `Assert.Throws<T>()` and careful setup

But what if there was a better way? What if errors were just... values? Values you could pass around, transform, and compose like any other data?

**Enter `Result<T, TError>`.**

## What Is Result<T, TError>?

A `Result<T, TError>` is a type that represents one of two possible outcomes:

1. **Success (Ok)** - The operation succeeded and produced a value of type `T`
2. **Failure (Error)** - The operation failed with an error of type `TError`

Think of it like a box that contains *either* a success value *or* an error value, but never both and never neither.

```csharp
// This Result contains EITHER a User OR a string error message
Result<User, string> result = GetUser(userId);
```

Just by looking at this type, you immediately know:
- ✅ Success case: You'll get a `User`
- ❌ Failure case: You'll get a `string` (error message)

No hidden exceptions. No surprises. Everything is explicit in the type signature.

## The Anatomy of Result

Let's peek inside the `Result<T, TError>` type. It's actually an abstract class with two concrete implementations:

```csharp
// Success case
public class Ok
{
    public T Value { get; }
}

// Failure case
public class Error
{
    public TError Reason { get; }
}
```

You can't directly access `Value` or `Reason` without first checking which case you have. This forces you to handle both possibilities—no forgotten error checking!

## Creating Results: Success and Failure

Creating a `Result` is straightforward. You have several options:

### Option 1: Using the Nested Classes

```csharp
// Create a success result
var successResult = new Result<User, string>.Ok(user);

// Create an error result
var errorResult = new Result<User, string>.Error("User not found");
```

### Option 2: Implicit Conversion (Even Cleaner!)

The `Result` type supports implicit conversion, which means you can just return the value directly:

```csharp
public Result<User, string> GetUser(int userId)
{
    var user = _database.FindUser(userId);
    
    if (user == null)
        return "User not found"; // Implicitly converts to Error!
    
    return user; // Implicitly converts to Ok!
}
```

C# automatically figures out whether you're returning success or failure based on the type. Neat!

### Option 3: Using the Try Class

When you have code that might throw exceptions, use `Try.Run()`:

```csharp
public Result<string, Exception> ReadFile(string path)
{
    return Try.Run(() => File.ReadAllText(path));
}
```

If `ReadAllText` throws an exception, `Try.Run()` catches it and returns `Result<string, Exception>.Error(exception)`. If it succeeds, you get `Result<string, Exception>.Ok(content)`.

## Pattern Matching: Handling Both Cases

Once you have a `Result`, you need to handle both the success and failure cases. The primary way to do this is with the `Match` method:

```csharp
var result = GetUser(42);

string message = result.Match(
    onSuccess: user => $"Found user: {user.Name}",
    onFailure: error => $"Error: {error}"
);

Console.WriteLine(message);
```

**What's happening here?**

1. `Match` takes two functions: one for success, one for failure
2. Only ONE of these functions will be called (whichever matches the actual result)
3. Both functions must return the same type (in this case, `string`)
4. The result is the return value from whichever function was called

This is called **pattern matching**, and it ensures you handle both cases. If you forget one, your code won't compile!

### Match Variations

There are several variations of `Match` for different scenarios:

#### Void Match (Side Effects Only)

Sometimes you just want to do something without returning a value:

```csharp
result.Match(
    onSuccess: user => Console.WriteLine($"Hello, {user.Name}!"),
    onFailure: error => Console.WriteLine($"Error: {error}")
);
```

#### Async Match

When your handlers are async:

```csharp
var result = await GetUserAsync(42);

await result.MatchAsync(
    onSuccess: async user => await SendWelcomeEmail(user),
    onFailure: async error => await LogError(error)
);
```

#### Match with Different Result Types

You can even return a different `Result` from your match:

```csharp
Result<UserDto, string> dtoResult = userResult.Match(
    onSuccess: user => new Result<UserDto, string>.Ok(new UserDto(user)),
    onFailure: error => new Result<UserDto, string>.Error(error)
);
```

## Transforming Results: The Map Method

Often, you want to transform a successful result while letting errors pass through unchanged. That's where `Map` comes in:

```csharp
Result<int, string> result = GetUserAge(userId);

Result<string, string> message = result.Map(age => $"User is {age} years old");
```

**What happened?**

- If `result` was `Ok(25)`, then `message` becomes `Ok("User is 25 years old")`
- If `result` was `Error("User not found")`, then `message` becomes `Error("User not found")`

The error just passes through! Map only touches the success value.

### Real-World Map Example

Let's say you're building an e-commerce site and need to calculate the final price after discount and tax:

```csharp
public Result<decimal, string> CalculateFinalPrice(string productId)
{
    return GetProduct(productId)              // Result<Product, string>
        .Map(product => product.BasePrice)     // Result<decimal, string>
        .Map(price => price * 0.9m)            // Apply 10% discount
        .Map(price => price * 1.08m);          // Add 8% tax
}
```

If `GetProduct` fails, the entire chain short-circuits and returns the error. If it succeeds, each `Map` transforms the price. Beautiful!

### MapFailure: Transform the Error

What if you want to transform the *error* instead of the success value?

```csharp
Result<User, Exception> result = Try.Run(() => _database.GetUser(userId));

Result<User, string> friendlyResult = result.MapFailure(ex => 
    $"Database error: {ex.Message}");
```

This is useful for converting detailed exception information into user-friendly error messages.

## Chaining Operations: The Bind Method

Here's where things get really powerful. What if you have a sequence of operations, where each one returns a `Result`?

```csharp
Result<User, string> GetUser(int userId) { /*...*/ }
Result<Order, string> GetLatestOrder(User user) { /*...*/ }
Result<decimal, string> CalculateTotal(Order order) { /*...*/ }
```

You can't use `Map` here because each operation returns a `Result`, not a plain value. If you tried:

```csharp
var result = GetUser(userId)
    .Map(user => GetLatestOrder(user));  // Result<Result<Order, string>, string> ❌
```

You'd end up with a nested `Result<Result<...>>`. Yuck!

Instead, use `Bind`:

```csharp
var result = GetUser(userId)
    .Bind(user => GetLatestOrder(user))     // Result<Order, string>
    .Bind(order => CalculateTotal(order));  // Result<decimal, string>

var message = result.Match(
    onSuccess: total => $"Order total: ${total}",
    onFailure: error => $"Error: {error}"
);
```

**What Bind does:**

1. If the current result is an error, stop and return the error
2. If the current result is success, call the function with the value
3. Return whatever Result the function produces

It "flattens" the nested Results automatically. This is sometimes called **monadic binding** in functional programming, but you can just think of it as "chaining operations that can fail."

### Railway-Oriented Programming

There's a beautiful metaphor for this called **Railway-Oriented Programming** (coined by Scott Wlaschin):

Imagine a railway track that splits into two:
- **Success track** - Everything is going great
- **Failure track** - Something went wrong

```
GetUser ──Success──> GetOrder ──Success──> Calculate ──Success──> ✓
   │                    │                      │
   └──Failure──> ✗      └──Failure──> ✗        └──Failure──> ✗
```

Once you switch to the failure track (an error occurs), you stay on that track. All subsequent operations are skipped, and the error passes through to the end.

Here's a comprehensive example:

```csharp
public Result<OrderConfirmation, string> ProcessOrder(
    int userId, 
    int productId, 
    int quantity)
{
    return GetUser(userId)
        .Bind(user => ValidateUserAccount(user))
        .Bind(user => GetProduct(productId))
        .Bind(product => CheckInventory(product, quantity))
        .Bind(product => CalculatePrice(product, quantity))
        .Bind(price => ChargeCustomer(userId, price))
        .Bind(payment => CreateOrder(userId, productId, quantity, payment))
        .Bind(order => SendConfirmationEmail(order))
        .Map(order => new OrderConfirmation(order));
}
```

**What's beautiful about this code?**

1. **Linear flow** - Read top to bottom, no nesting
2. **Explicit error handling** - Each step can fail, and it's clear in the types
3. **Short-circuit on error** - First failure stops the whole chain
4. **Composable** - Each step is an independent, testable function
5. **Type-safe** - Compiler ensures you handle all cases

Compare this to the equivalent exception-based code... it would be a nightmare of nested try-catch blocks!

## Working with Multiple Results

Sometimes you need to combine multiple Results. Here are common patterns:

### Pattern 1: All Must Succeed

You have two (or more) independent operations, and you need all of them to succeed:

```csharp
public Result<UserProfile, string> GetUserProfile(int userId)
{
    var userResult = GetUser(userId);
    var settingsResult = GetUserSettings(userId);
    
    return userResult.Bind(user =>
        settingsResult.Map(settings =>
            new UserProfile(user, settings)));
}
```

If either operation fails, the whole thing fails with that error.

### Pattern 2: Validate Multiple Fields

When validating a form with multiple fields:

```csharp
public Result<RegistrationData, string> ValidateRegistration(
    string email, 
    string password, 
    string confirmPassword)
{
    return ValidateEmail(email)
        .Bind(_ => ValidatePassword(password))
        .Bind(_ => ValidatePasswordMatch(password, confirmPassword))
        .Map(_ => new RegistrationData(email, password));
}

private Result<Unit, string> ValidateEmail(string email)
{
    return email.Contains("@") 
        ? Unit.Value 
        : "Invalid email format";
}

private Result<Unit, string> ValidatePassword(string password)
{
    return password.Length >= 8 
        ? Unit.Value 
        : "Password must be at least 8 characters";
}

private Result<Unit, string> ValidatePasswordMatch(string password, string confirm)
{
    return password == confirm 
        ? Unit.Value 
        : "Passwords do not match";
}
```

Notice we're using `Unit` for validations that don't return a value but can fail. This is a common functional programming pattern.

### Pattern 3: Collecting Multiple Errors

Sometimes you want to collect ALL validation errors, not just the first one. For this, you'd typically create a custom error type:

```csharp
public class ValidationErrors
{
    public List<string> Errors { get; } = new();
    
    public void Add(string error) => Errors.Add(error);
    public bool HasErrors => Errors.Count > 0;
}

public Result<User, ValidationErrors> ValidateUser(UserInput input)
{
    var errors = new ValidationErrors();
    
    if (string.IsNullOrEmpty(input.Email))
        errors.Add("Email is required");
    else if (!input.Email.Contains("@"))
        errors.Add("Email format is invalid");
    
    if (string.IsNullOrEmpty(input.Password))
        errors.Add("Password is required");
    else if (input.Password.Length < 8)
        errors.Add("Password must be at least 8 characters");
    
    if (string.IsNullOrEmpty(input.Name))
        errors.Add("Name is required");
    
    if (errors.HasErrors)
        return errors;
    
    return new User(input.Email, input.Password, input.Name);
}

// Usage
var result = ValidateUser(userInput);
result.Match(
    onSuccess: user => Console.WriteLine($"Valid user: {user.Name}"),
    onFailure: errors => 
    {
        Console.WriteLine("Validation failed:");
        foreach (var error in errors.Errors)
            Console.WriteLine($"  - {error}");
    }
);
```

## Async Results: A Perfect Match

Modern C# code is full of `async`/`await`. The good news? `Result` works beautifully with async code!

### Async Result-Returning Methods

```csharp
public async Task<Result<User, string>> GetUserAsync(int userId)
{
    return await Try.RunAsync(async () => 
        await _database.Users.FindAsync(userId))
        .MapFailureAsync(ex => $"Database error: {ex.Message}");
}
```

### Chaining Async Results

All the methods you've learned have async versions:

```csharp
var result = await GetUserAsync(userId)
    .BindAsync(user => GetOrdersAsync(user))
    .MapAsync(orders => orders.Sum(o => o.Total))
    .MapFailureAsync(error => $"Failed to calculate total: {error}");
```

The `Async` suffix methods handle the `Task<Result<T, TError>>` for you, so you can chain operations naturally.

### Mixing Sync and Async

You can mix synchronous and asynchronous operations:

```csharp
var result = await GetUserAsync(userId)
    .MapAsync(user => user.Email)              // Async map
    .Map(email => email.ToLower())             // Sync map
    .BindAsync(email => SendEmailAsync(email)) // Async bind
    .Map(confirmation => confirmation.Id);      // Sync map
```

The package handles the `Task` unwrapping for you automatically.

## Common Patterns and Idioms

Let's look at some common patterns you'll use frequently:

### Pattern 1: Early Return

When checking preconditions:

```csharp
public Result<Order, string> CreateOrder(int userId, List<OrderItem> items)
{
    if (items == null || items.Count == 0)
        return "Order must contain at least one item";
    
    return GetUser(userId)
        .Bind(user => ValidateUser(user))
        .Bind(user => CreateOrderForUser(user, items));
}
```

### Pattern 2: Try-Catch Replacement

Replace try-catch with `Try.Run`:

```csharp
// Before
public User DeserializeUser(string json)
{
    try
    {
        return JsonSerializer.Deserialize<User>(json);
    }
    catch (JsonException ex)
    {
        _logger.LogError(ex, "Failed to deserialize user");
        throw;
    }
}

// After
public Result<User, string> DeserializeUser(string json)
{
    return Try.Run(() => JsonSerializer.Deserialize<User>(json))
        .MapFailure(ex => 
        {
            _logger.LogError(ex, "Failed to deserialize user");
            return $"Invalid JSON: {ex.Message}";
        });
}
```

### Pattern 3: Fallback Values

Provide a default value if the operation fails:

```csharp
// Using Match for a fallback
var userName = GetUser(userId)
    .Map(user => user.Name)
    .Match(
        onSuccess: name => name,
        onFailure: _ => "Guest"
    );

// Or create a helper extension
public static T GetOrDefault<T, TError>(
    this Result<T, TError> result, 
    T defaultValue)
{
    return result.Match(
        onSuccess: value => value,
        onFailure: _ => defaultValue
    );
}

var userName = GetUser(userId)
    .Map(user => user.Name)
    .GetOrDefault("Guest");
```

### Pattern 4: Conditional Logic

Sometimes you need conditional logic based on success:

```csharp
public Result<decimal, string> ApplyDiscount(int userId, decimal price)
{
    return GetUser(userId)
        .Bind(user => user.IsPremium
            ? Result<decimal, string>.Ok(price * 0.8m) // 20% discount
            : Result<decimal, string>.Ok(price));       // No discount
}
```

### Pattern 5: Logging Without Breaking the Chain

You can log at each step without breaking the chain:

```csharp
public Result<Order, string> ProcessOrder(int userId, int productId)
{
    return GetUser(userId)
        .Map(user => 
        {
            _logger.LogInformation("Processing order for user {UserId}", user.Id);
            return user;
        })
        .Bind(user => GetProduct(productId))
        .Map(product => 
        {
            _logger.LogInformation("Product {ProductId} found", product.Id);
            return product;
        })
        .Bind(product => CreateOrder(userId, product));
}
```

## Real-World Example: Complete CRUD Operations

Let's build a complete example with Create, Read, Update, and Delete operations using `Result`:

```csharp
public class UserService
{
    private readonly IDatabase _database;
    private readonly ILogger<UserService> _logger;
    
    // CREATE
    public async Task<Result<User, string>> CreateUserAsync(
        string email, 
        string password, 
        string name)
    {
        return await ValidateEmail(email)
            .ToResult(() => "Invalid email format")
            .BindAsync(_ => CheckEmailNotExists(email))
            .BindAsync(_ => ValidatePassword(password))
            .BindAsync(_ => HashPasswordAsync(password))
            .BindAsync(hashedPassword => SaveUserAsync(email, hashedPassword, name));
    }
    
    // READ
    public async Task<Result<User, string>> GetUserAsync(int userId)
    {
        if (userId <= 0)
            return "Invalid user ID";
        
        return await Try.RunAsync(async () => 
                await _database.Users.FindAsync(userId))
            .MapAsync(user => user ?? throw new Exception("User not found"))
            .MapFailureAsync(ex => $"Failed to get user: {ex.Message}");
    }
    
    // UPDATE
    public async Task<Result<User, string>> UpdateUserAsync(
        int userId, 
        string newEmail, 
        string newName)
    {
        return await GetUserAsync(userId)
            .BindAsync(user => ValidateEmail(newEmail)
                .ToResult(() => "Invalid email format")
                .MapAsync(_ => user))
            .BindAsync(user => CheckEmailAvailable(newEmail, user.Id)
                .MapAsync(_ => user))
            .BindAsync(user => UpdateUserFieldsAsync(user, newEmail, newName));
    }
    
    // DELETE
    public async Task<Result<Unit, string>> DeleteUserAsync(int userId)
    {
        return await GetUserAsync(userId)
            .BindAsync(user => ArchiveUserDataAsync(user))
            .BindAsync(user => RemoveUserAsync(user))
            .MapAsync(_ => Unit.Value);
    }
    
    // Helper methods
    private Option<string> ValidateEmail(string email)
    {
        return !string.IsNullOrWhiteSpace(email) && email.Contains("@")
            ? Option.Some(email)
            : Option.None<string>();
    }
    
    private async Task<Result<Unit, string>> CheckEmailNotExists(string email)
    {
        var exists = await _database.Users.AnyAsync(u => u.Email == email);
        return exists 
            ? "Email already in use" 
            : Unit.Value;
    }
    
    private async Task<Result<Unit, string>> CheckEmailAvailable(
        string email, 
        int currentUserId)
    {
        var existingUser = await _database.Users
            .FirstOrDefaultAsync(u => u.Email == email);
        
        if (existingUser == null || existingUser.Id == currentUserId)
            return Unit.Value;
        
        return "Email already in use by another user";
    }
    
    private async Task<Result<Unit, string>> ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            return "Password is required";
        
        if (password.Length < 8)
            return "Password must be at least 8 characters";
        
        if (!password.Any(char.IsDigit))
            return "Password must contain at least one digit";
        
        return Unit.Value;
    }
    
    private async Task<Result<string, string>> HashPasswordAsync(string password)
    {
        return await Try.RunAsync(async () => 
                await _passwordHasher.HashAsync(password))
            .MapFailureAsync(ex => $"Failed to hash password: {ex.Message}");
    }
    
    private async Task<Result<User, string>> SaveUserAsync(
        string email, 
        string hashedPassword, 
        string name)
    {
        var user = new User
        {
            Email = email,
            PasswordHash = hashedPassword,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        
        return await Try.RunAsync(async () =>
        {
            await _database.Users.AddAsync(user);
            await _database.SaveChangesAsync();
            return user;
        })
        .MapFailureAsync(ex => $"Database error: {ex.Message}");
    }
    
    private async Task<Result<User, string>> UpdateUserFieldsAsync(
        User user, 
        string newEmail, 
        string newName)
    {
        user.Email = newEmail;
        user.Name = newName;
        user.UpdatedAt = DateTime.UtcNow;
        
        return await Try.RunAsync(async () =>
        {
            _database.Users.Update(user);
            await _database.SaveChangesAsync();
            return user;
        })
        .MapFailureAsync(ex => $"Failed to update user: {ex.Message}");
    }
    
    private async Task<Result<Unit, string>> ArchiveUserDataAsync(User user)
    {
        return await Try.RunAsync(async () =>
        {
            await _archiveService.ArchiveUserAsync(user);
            return Unit.Value;
        })
        .MapFailureAsync(ex => $"Failed to archive user data: {ex.Message}");
    }
    
    private async Task<Result<Unit, string>> RemoveUserAsync(User user)
    {
        return await Try.RunAsync(async () =>
        {
            _database.Users.Remove(user);
            await _database.SaveChangesAsync();
            return Unit.Value;
        })
        .MapFailureAsync(ex => $"Failed to delete user: {ex.Message}");
    }
}
```

### Using the Service

```csharp
// Create
var createResult = await userService.CreateUserAsync(
    "john@example.com", 
    "SecureP@ss123", 
    "John Doe");

createResult.Match(
    onSuccess: user => Console.WriteLine($"Created user: {user.Name} (ID: {user.Id})"),
    onFailure: error => Console.WriteLine($"Failed to create user: {error}")
);

// Read
var getResult = await userService.GetUserAsync(userId);
getResult.Match(
    onSuccess: user => DisplayUserProfile(user),
    onFailure: error => ShowErrorMessage(error)
);

// Update
var updateResult = await userService.UpdateUserAsync(
    userId, 
    "newemail@example.com", 
    "John Smith");

updateResult.Match(
    onSuccess: user => Console.WriteLine($"Updated user: {user.Name}"),
    onFailure: error => Console.WriteLine($"Update failed: {error}")
);

// Delete
var deleteResult = await userService.DeleteUserAsync(userId);
deleteResult.Match(
    onSuccess: _ => Console.WriteLine("User deleted successfully"),
    onFailure: error => Console.WriteLine($"Delete failed: {error}")
);
```

## Comparing Approaches: Before and After

Let's see a side-by-side comparison of the same functionality:

### Exception-Based (Traditional)

```csharp
public class OrderService
{
    public decimal CalculateOrderTotal(int orderId)
    {
        Order order;
        try
        {
            order = _database.GetOrder(orderId);
        }
        catch (DatabaseException ex)
        {
            _logger.LogError(ex, "Database error");
            throw new ServiceException("Failed to get order", ex);
        }
        
        if (order == null)
            throw new OrderNotFoundException($"Order {orderId} not found");
        
        if (order.Status == OrderStatus.Cancelled)
            throw new InvalidOperationException("Cannot calculate total for cancelled order");
        
        decimal subtotal = 0;
        foreach (var item in order.Items)
        {
            try
            {
                var price = _pricingService.GetPrice(item.ProductId);
                subtotal += price * item.Quantity;
            }
            catch (ProductNotFoundException ex)
            {
                throw new ServiceException(
                    $"Product {item.ProductId} not found", ex);
            }
        }
        
        decimal tax;
        try
        {
            tax = _taxService.CalculateTax(subtotal, order.ShippingAddress);
        }
        catch (TaxServiceException ex)
        {
            _logger.LogError(ex, "Tax calculation failed");
            throw new ServiceException("Failed to calculate tax", ex);
        }
        
        return subtotal + tax;
    }
}

// Usage
try
{
    var total = orderService.CalculateOrderTotal(orderId);
    Console.WriteLine($"Order total: ${total:F2}");
}
catch (OrderNotFoundException ex)
{
    Console.WriteLine("Order not found");
}
catch (InvalidOperationException ex)
{
    Console.WriteLine($"Invalid operation: {ex.Message}");
}
catch (ServiceException ex)
{
    Console.WriteLine($"Service error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

### Result-Based (Functional)

```csharp
public class OrderService
{
    public async Task<Result<decimal, string>> CalculateOrderTotalAsync(int orderId)
    {
        return await GetOrderAsync(orderId)
            .BindAsync(order => ValidateOrderStatus(order))
            .BindAsync(order => CalculateSubtotalAsync(order))
            .BindAsync(result => CalculateTaxAsync(result.subtotal, result.order))
            .MapAsync(result => result.subtotal + result.tax);
    }
    
    private async Task<Result<Order, string>> GetOrderAsync(int orderId)
    {
        return await Try.RunAsync(() => _database.GetOrderAsync(orderId))
            .MapAsync(order => order ?? throw new Exception("Order not found"))
            .MapFailureAsync(ex =>
            {
                _logger.LogError(ex, "Failed to get order {OrderId}", orderId);
                return $"Failed to get order: {ex.Message}";
            });
    }
    
    private async Task<Result<Order, string>> ValidateOrderStatus(Order order)
    {
        return order.Status == OrderStatus.Cancelled
            ? "Cannot calculate total for cancelled order"
            : order;
    }
    
    private async Task<Result<(decimal subtotal, Order order), string>> 
        CalculateSubtotalAsync(Order order)
    {
        var results = new List<Result<decimal, string>>();
        
        foreach (var item in order.Items)
        {
            var itemResult = await Try.RunAsync(() => 
                    _pricingService.GetPriceAsync(item.ProductId))
                .MapAsync(price => price * item.Quantity)
                .MapFailureAsync(ex => $"Failed to get price for product {item.ProductId}");
            
            results.Add(itemResult);
        }
        
        // Check if any failed
        var firstError = results.FirstOrDefault(r => r is Result<decimal, string>.Error);
        if (firstError != null)
            return firstError.Match(_ => "", error => error);
        
        var subtotal = results.Sum(r => r.Match(val => val, _ => 0m));
        return (subtotal, order);
    }
    
    private async Task<Result<(decimal subtotal, decimal tax), string>> 
        CalculateTaxAsync(decimal subtotal, Order order)
    {
        return await Try.RunAsync(() => 
                _taxService.CalculateTaxAsync(subtotal, order.ShippingAddress))
            .MapAsync(tax => (subtotal, tax))
            .MapFailureAsync(ex =>
            {
                _logger.LogError(ex, "Tax calculation failed");
                return "Failed to calculate tax";
            });
    }
}

// Usage
var result = await orderService.CalculateOrderTotalAsync(orderId);

result.Match(
    onSuccess: total => Console.WriteLine($"Order total: ${total:F2}"),
    onFailure: error => Console.WriteLine($"Error: {error}")
);
```

**Benefits of the Result-based approach:**

1. ✅ No exception handling in the caller
2. ✅ Clear success/failure paths
3. ✅ Each step is isolated and testable
4. ✅ Errors are values, not control flow
5. ✅ Linear, readable code flow
6. ✅ Explicit in method signatures

## Testing with Results

Testing code that uses `Result` is much easier than testing exception-based code:

```csharp
public class UserServiceTests
{
    [Fact]
    public async Task CreateUser_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = await service.CreateUserAsync(
            "test@example.com", 
            "SecurePass123", 
            "Test User");
        
        // Assert
        Assert.True(result is Result<User, string>.Ok);
        var user = result.Match(user => user, _ => null);
        Assert.NotNull(user);
        Assert.Equal("test@example.com", user.Email);
    }
    
    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsError()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = await service.CreateUserAsync(
            "invalid-email", 
            "SecurePass123", 
            "Test User");
        
        // Assert
        Assert.True(result is Result<User, string>.Error);
        var error = result.Match(_ => "", error => error);
        Assert.Contains("Invalid email", error);
    }
    
    [Fact]
    public async Task CreateUser_WhenDatabaseFails_ReturnsError()
    {
        // Arrange
        var mockDb = new Mock<IDatabase>();
        mockDb.Setup(db => db.SaveChangesAsync())
            .ThrowsAsync(new DbException("Connection failed"));
        var service = CreateService(mockDb.Object);
        
        // Act
        var result = await service.CreateUserAsync(
            "test@example.com", 
            "SecurePass123", 
            "Test User");
        
        // Assert
        Assert.True(result is Result<User, string>.Error);
        var error = result.Match(_ => "", error => error);
        Assert.Contains("Database error", error);
    }
}
```

No `Assert.Throws`, no complex exception setup—just simple assertions on values!

## Best Practices

### 1. Choose Appropriate Error Types

For simple cases, `string` errors are fine:
```csharp
Result<User, string> GetUser(int id);
```

For complex cases, use custom error types:
```csharp
public class ValidationError
{
    public string Field { get; set; }
    public string Message { get; set; }
}

Result<User, List<ValidationError>> ValidateUser(UserInput input);
```

For exception wrapping, use `Exception`:
```csharp
Result<User, Exception> GetUserFromDatabase(int id);
```

### 2. Be Consistent

Pick a pattern and stick with it across your codebase:
- Use `Result<T, string>` for simple error messages
- Use `Result<T, Exception>` when wrapping legacy code
- Use custom error types for complex scenarios

### 3. Don't Mix Approaches

Avoid mixing exceptions and `Result` in the same method:

```csharp
// ❌ Bad - mixes exceptions and Result
public Result<User, string> GetUser(int userId)
{
    if (userId <= 0)
        throw new ArgumentException("Invalid ID"); // Don't throw!
    
    return _database.FindUser(userId).ToOption()
        .ToResult(() => "User not found");
}

// ✅ Good - pure Result
public Result<User, string> GetUser(int userId)
{
    if (userId <= 0)
        return "Invalid user ID";
    
    return _database.FindUser(userId).ToOption()
        .ToResult(() => "User not found");
}
```

### 4. Use Async Consistently

When working with async code, use the async extension methods:

```csharp
// ✅ Good
var result = await GetUserAsync(userId)
    .BindAsync(user => GetOrdersAsync(user))
    .MapAsync(orders => orders.Count);

// ❌ Bad - unnecessary awaits
var userResult = await GetUserAsync(userId);
var ordersResult = await userResult.BindAsync(user => GetOrdersAsync(user));
var countResult = ordersResult.MapAsync(orders => orders.Count);
var count = await countResult;
```

### 5. Keep Error Messages User-Friendly

```csharp
// ❌ Bad - technical error
return $"SqlException: Cannot open database 'Users' login failed for user 'sa'";

// ✅ Good - user-friendly error
return "Unable to access user database. Please try again later.";
```

## Conclusion: Error Handling Made Beautiful

The `Result<T, TError>` type transforms error handling from a chore into an elegant, composable pattern. Instead of scattering try-catch blocks throughout your code and hoping you've caught everything, you make errors explicit, first-class values that flow through your application naturally.

**Key takeaways:**

1. **`Result<T, TError>` makes errors explicit** - Your method signatures tell the whole story
2. **Pattern matching ensures you handle all cases** - No forgotten error checks
3. **Map transforms success values** - While letting errors pass through
4. **Bind chains operations that can fail** - Creating beautiful pipelines
5. **Railway-oriented programming** - Visualize your code as success/failure tracks
6. **Async works seamlessly** - All operations have async equivalents
7. **Testing becomes simpler** - No exception testing ceremony

By using `Result`, you write code that is:
- ✅ More explicit and honest
- ✅ Easier to read and maintain
- ✅ Safer and less error-prone
- ✅ More composable and reusable
- ✅ Simpler to test

In the next post, we'll dive deep into `Option<T>` and learn how to eliminate null reference exceptions from your code forever!

---

## Additional Resources

- [Part 1: Overview of AStar.Dev.Functional.Extensions](./01-functional-extensions-overview.md)
- [Railway Oriented Programming by Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [AStar.Dev.Functional.Extensions GitHub Repository](https://github.com/astar-development/astar-dev-onedrive-sync-client)

**Coming Next:** Post 3 - Mastering Option<T>: Null-Safe Code Made Simple

---

*Questions or feedback? We'd love to hear from you! Reach out to the AStar Development Team or leave a comment below.*
