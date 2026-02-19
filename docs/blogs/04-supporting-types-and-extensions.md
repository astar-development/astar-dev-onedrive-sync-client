# The Supporting Cast: Unit, Try, Pattern, and Extension Methods

**Published: February 19, 2026**  
**Author: AStar Development Team**  
**Target Audience: Entry-Level C# Developers**  
**Part 4 of the AStar.Dev.Functional.Extensions Series**

---

## Introduction: The Glue That Holds It All Together

In the previous three posts, we explored `Option<T>` and `Result<T, TError>`—the stars of functional programming in C#. But every great show needs a supporting cast, and the **AStar.Dev.Functional.Extensions** package includes several utility types and extension methods that make functional programming even more delightful.

Think of these as the tools in your utility belt. Individually, they might seem simple, but together they solve common problems elegantly and make your code more expressive.

In this final post, we'll explore:

1. **`Unit`** - Representing "no value" in a functional way
2. **`Try`** - Safe exception handling without try-catch blocks
3. **`Pattern`** - Utility methods for pattern checking
4. **`ErrorResponse`** - Standardized error representation
5. **Extension Methods** - Helpers that make everything work together smoothly
6. **ViewModel Helpers** - Special extensions for UI code
7. **Collection Helpers** - Working with Options and Results in collections

Let's dive in!

---

## Unit: The Functional Void

### The Problem with Void

In C#, when a method doesn't return anything meaningful, we use `void`:

```csharp
public void SaveSettings(Settings settings)
{
    _database.Save(settings);
}
```

This works fine until you want to use generic types. What if saving can fail and you want to return a `Result`?

```csharp
// ❌ This doesn't work - void can't be used in generic types
public Result<void, string> SaveSettings(Settings settings) 
{
    // ...
}
```

The compiler error: "Keyword 'void' cannot be used in this context."

### Enter Unit

`Unit` is a type that represents "no meaningful value." It's like `void`, but it's an actual type that can be used in generics:

```csharp
public Result<Unit, string> SaveSettings(Settings settings)
{
    return Try.Run(() => _database.Save(settings))
        .Map(_ => Unit.Value)
        .MapFailure(ex => $"Failed to save: {ex.Message}");
}
```

### What Is Unit?

`Unit` is a simple struct with only one possible value: `Unit.Value`. Think of it like a boolean that only has one value—it carries no information, it just exists.

```csharp
public readonly struct Unit
{
    public static Unit Value => default;
    
    public override string ToString() => "()";
}
```

The `ToString()` returns `"()"` because in functional programming, unit is often written as empty parentheses.

### When to Use Unit

Use `Unit` when you need to return a result that indicates success/failure, but there's no meaningful value to return on success:

#### Example 1: Validation

```csharp
public Result<Unit, string> ValidateEmail(string email)
{
    if (string.IsNullOrWhiteSpace(email))
        return "Email is required";
    
    if (!email.Contains("@"))
        return "Invalid email format";
    
    return Unit.Value; // Success, no value to return
}

// Usage
var result = ValidateEmail("test@example.com");
result.Match(
    onSuccess: _ => Console.WriteLine("Email is valid"),
    onFailure: error => Console.WriteLine($"Validation failed: {error}")
);
```

#### Example 2: Operations with Side Effects

```csharp
public Result<Unit, string> SendNotification(int userId, string message)
{
    return GetUser(userId)
        .ToResult(() => "User not found")
        .Bind(user => SendEmail(user.Email, message))
        .Map(_ => Unit.Value);
}

private Result<Unit, string> SendEmail(string email, string message)
{
    return Try.Run(() => _emailService.Send(email, message))
        .Map(_ => Unit.Value)
        .MapFailure(ex => $"Failed to send email: {ex.Message}");
}
```

#### Example 3: Chaining Operations

```csharp
public async Task<Result<Unit, string>> ProcessUserRegistration(User user)
{
    return await ValidateUser(user)
        .BindAsync(_ => SaveUser(user))
        .BindAsync(_ => SendWelcomeEmail(user))
        .BindAsync(_ => LogRegistration(user))
        .MapAsync(_ => Unit.Value);
}

// Each step returns Result<Unit, string>, allowing clean chaining
```

### Unit vs Returning the Value

Sometimes you have a choice between returning `Unit` or the actual value:

```csharp
// Option 1: Return Unit
public Result<Unit, string> UpdateUser(User user)
{
    return Try.Run(() => _database.Update(user))
        .Map(_ => Unit.Value)
        .MapFailure(ex => "Update failed");
}

// Option 2: Return the updated user
public Result<User, string> UpdateUser(User user)
{
    return Try.Run(() => 
    {
        _database.Update(user);
        return user;
    })
    .MapFailure(ex => "Update failed");
}
```

**When to return Unit:**
- The caller doesn't need the value
- You want to emphasize the side effect (saving, sending, logging)
- You're chaining operations where the next step doesn't use the previous value

**When to return the value:**
- The caller might need the updated value
- You want to enable further transformations
- The operation enriches or modifies the value in a meaningful way

### Unit Equality

All `Unit` values are equal (there's only one value!):

```csharp
var unit1 = Unit.Value;
var unit2 = Unit.Value;

Console.WriteLine(unit1 == unit2); // True
Console.WriteLine(unit1.Equals(unit2)); // True
```

This is by design—`Unit` carries no information, so all instances are equivalent.

---

## Try: Exception Handling Made Functional

### The Problem with Try-Catch

Traditional exception handling requires verbose try-catch blocks:

```csharp
public User GetUser(int userId)
{
    try
    {
        return _database.Users.Find(userId);
    }
    catch (DatabaseException ex)
    {
        _logger.LogError(ex, "Database error");
        throw;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Unexpected error");
        throw;
    }
}
```

And when calling this method:

```csharp
try
{
    var user = GetUser(42);
    Console.WriteLine($"Found: {user.Name}");
}
catch (DatabaseException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}
```

Lots of boilerplate for a simple operation!

### Try.Run: Catch Exceptions as Values

The `Try` class provides methods to execute code and capture any exceptions as `Result<T, Exception>`:

```csharp
public Result<User, Exception> GetUser(int userId)
{
    return Try.Run(() => _database.Users.Find(userId));
}

// Usage
var result = GetUser(42);
result.Match(
    onSuccess: user => Console.WriteLine($"Found: {user.Name}"),
    onFailure: ex => Console.WriteLine($"Error: {ex.Message}")
);
```

**What happened?**
- `Try.Run()` executes the lambda
- If it succeeds, returns `Result<User, Exception>.Ok(user)`
- If it throws, returns `Result<User, Exception>.Error(exception)`
- No try-catch needed!

### Try Variations

#### Try.Run (Synchronous Function)

For functions that return a value:

```csharp
Result<int, Exception> result = Try.Run(() => int.Parse("42"));

result.Match(
    onSuccess: num => Console.WriteLine($"Parsed: {num}"),
    onFailure: ex => Console.WriteLine($"Parse failed: {ex.Message}")
);
```

#### Try.Run (Synchronous Action)

For actions that return nothing:

```csharp
Result<bool, Exception> result = Try.Run(() => 
{
    _database.Save(user);
    _logger.LogInfo("User saved");
});

result.Match(
    onSuccess: _ => Console.WriteLine("Save successful"),
    onFailure: ex => Console.WriteLine($"Save failed: {ex.Message}")
);
```

Returns `Result<bool, Exception>` where success is always `true`.

#### Try.RunAsync (Async Function)

For async operations that return a value:

```csharp
Result<User, Exception> result = await Try.RunAsync(async () => 
    await _database.Users.FindAsync(userId));
```

#### Try.RunAsync (Async Action)

For async operations that return nothing:

```csharp
Result<bool, Exception> result = await Try.RunAsync(async () =>
{
    await _database.SaveChangesAsync();
    await _cache.InvalidateAsync();
});
```

### Real-World Try Examples

#### Example 1: File Operations

```csharp
public Result<string, Exception> ReadConfigFile(string path)
{
    return Try.Run(() => File.ReadAllText(path));
}

public Result<Unit, Exception> WriteConfigFile(string path, string content)
{
    return Try.Run(() => File.WriteAllText(path, content))
        .Map(_ => Unit.Value);
}

// Usage
var readResult = ReadConfigFile("config.json");
readResult.Match(
    onSuccess: content => ProcessConfig(content),
    onFailure: ex => Console.WriteLine($"Failed to read config: {ex.Message}")
);
```

#### Example 2: JSON Serialization

```csharp
public Result<User, Exception> DeserializeUser(string json)
{
    return Try.Run(() => JsonSerializer.Deserialize<User>(json));
}

public Result<string, Exception> SerializeUser(User user)
{
    return Try.Run(() => JsonSerializer.Serialize(user));
}

// Usage
var result = ReadConfigFile("user.json")
    .Bind(json => DeserializeUser(json));

result.Match(
    onSuccess: user => Console.WriteLine($"Loaded user: {user.Name}"),
    onFailure: ex => Console.WriteLine($"Failed to load user: {ex.Message}")
);
```

#### Example 3: Database Operations

```csharp
public async Task<Result<User, Exception>> CreateUserAsync(string email, string name)
{
    return await Try.RunAsync(async () =>
    {
        var user = new User { Email = email, Name = name };
        await _database.Users.AddAsync(user);
        await _database.SaveChangesAsync();
        return user;
    });
}

// Usage
var result = await CreateUserAsync("test@example.com", "Test User");
result.Match(
    onSuccess: user => Console.WriteLine($"Created user with ID: {user.Id}"),
    onFailure: ex => Console.WriteLine($"Failed to create user: {ex.Message}")
);
```

### Converting Exception to User-Friendly Messages

The `ToErrorResponse` extension converts `Result<T, Exception>` to `Result<T, ErrorResponse>`:

```csharp
var result = Try.Run(() => int.Parse("invalid"))
    .ToErrorResponse();

// result is now Result<int, ErrorResponse>
result.Match(
    onSuccess: num => Console.WriteLine($"Number: {num}"),
    onFailure: error => Console.WriteLine($"Error: {error.Message}")
);
```

Or convert to simple string messages:

```csharp
var result = Try.Run(() => File.ReadAllText("missing.txt"))
    .MapFailure(ex => $"File error: {ex.Message}");

// result is now Result<string, string>
```

### Chaining Try Operations

`Try` works beautifully with `Bind` and `Map`:

```csharp
var result = Try.Run(() => File.ReadAllText("config.json"))
    .Bind(json => Try.Run(() => JsonSerializer.Deserialize<Config>(json)))
    .Bind(config => ValidateConfig(config))
    .Map(config => ApplyConfig(config));

result.Match(
    onSuccess: _ => Console.WriteLine("Configuration loaded successfully"),
    onFailure: ex => Console.WriteLine($"Configuration error: {ex.Message}")
);
```

If any step throws an exception, the chain short-circuits and returns the error.

---

## Pattern: Utility Methods for Type Checking

The `Pattern` class provides static utility methods for checking the state of `Option` and `Result` types. These are especially useful when you need to check types without extracting values.

### Pattern.IsSome and Pattern.IsNone

Check if an Option contains a value:

```csharp
var userOption = FindUser(42);

if (Pattern.IsSome(userOption))
{
    Console.WriteLine("User found!");
}

if (Pattern.IsNone(userOption))
{
    Console.WriteLine("User not found!");
}
```

This is equivalent to calling `.IsSome()` and `.IsNone()` as extension methods, but can be useful in certain scenarios like LINQ queries:

```csharp
var userOptions = ids.Select(id => FindUser(id));
var foundUsers = userOptions.Where(Pattern.IsSome).ToList();
```

### Pattern.IsOk and Pattern.IsError

Check if a Result represents success or failure:

```csharp
var result = GetUser(42);

if (Pattern.IsOk(result))
{
    Console.WriteLine("Operation succeeded!");
}

if (Pattern.IsError(result))
{
    Console.WriteLine("Operation failed!");
}
```

Again, useful in LINQ queries:

```csharp
var results = ids.Select(id => GetUser(id));
var successCount = results.Count(Pattern.IsOk);
var failureCount = results.Count(Pattern.IsError);
```

### Pattern.IsSuccess and Pattern.IsFailure

Specialized for `Result<T, Exception>` (from `Try` operations):

```csharp
var result = Try.Run(() => int.Parse("42"));

if (Pattern.IsSuccess(result))
{
    Console.WriteLine("Parse succeeded!");
}

if (Pattern.IsFailure(result))
{
    Console.WriteLine("Parse failed!");
}
```

### Why Use Pattern Methods?

**1. Method Group Conversion**

Pattern methods work well with method references:

```csharp
// ✅ Works - Pattern.IsSome is a method
var foundUsers = userOptions.Where(Pattern.IsSome);

// ❌ Doesn't work - IsSome() is an extension method
var foundUsers = userOptions.Where(opt => opt.IsSome());
```

**2. Null-Safe Checking**

Pattern methods handle nulls gracefully:

```csharp
Option<User> userOption = null;
Pattern.IsSome(userOption); // Returns false, doesn't throw
```

**3. Readability in Conditions**

Sometimes the static method reads better:

```csharp
// More explicit
if (Pattern.IsOk(result) && Pattern.IsSome(userOption))
{
    // ...
}

// Less explicit
if (result.IsOk() && userOption.IsSome())
{
    // ...
}
```

---

## ErrorResponse: Standardized Error Messages

`ErrorResponse` is a simple record that wraps an error message:

```csharp
public record ErrorResponse
{
    public ErrorResponse(string message) => Message = message;
    
    public string Message { get; }
}
```

### Why ErrorResponse?

When building APIs or UI applications, you often want to return user-friendly error messages without exposing exception details. `ErrorResponse` provides a standard way to do this.

### Using ErrorResponse

Convert exceptions to ErrorResponse:

```csharp
var result = Try.Run(() => _database.GetUser(userId))
    .ToErrorResponse(); // Result<User, ErrorResponse>

result.Match(
    onSuccess: user => DisplayUser(user),
    onFailure: error => ShowError(error.Message)
);
```

The `ToErrorResponse()` extension automatically extracts the base exception message.

### Custom Error Responses

You can create custom error responses with rich information:

```csharp
public record ValidationErrorResponse : ErrorResponse
{
    public ValidationErrorResponse(string message, Dictionary<string, string> fieldErrors) 
        : base(message)
    {
        FieldErrors = fieldErrors;
    }
    
    public Dictionary<string, string> FieldErrors { get; }
}

// Usage
public Result<User, ValidationErrorResponse> ValidateUser(UserInput input)
{
    var errors = new Dictionary<string, string>();
    
    if (string.IsNullOrEmpty(input.Email))
        errors["Email"] = "Email is required";
    
    if (string.IsNullOrEmpty(input.Name))
        errors["Name"] = "Name is required";
    
    if (errors.Any())
        return new ValidationErrorResponse("Validation failed", errors);
    
    return new User(input.Email, input.Name);
}
```

---

## Extension Methods: The Magic Glue

The package includes dozens of extension methods that make working with `Option` and `Result` seamless. Let's explore the most useful ones.

### Option Extensions

#### FirstOrNone

Like `FirstOrDefault`, but returns `Option<T>`:

```csharp
var users = new List<User> { /*...*/ };

Option<User> adminOption = users.FirstOrNone(u => u.IsAdmin);

adminOption.Match(
    onSome: admin => Console.WriteLine($"Admin: {admin.Name}"),
    onNone: () => Console.WriteLine("No admin found")
);
```

#### ToOption (Multiple Overloads)

Convert various types to Option:

```csharp
// From potentially null value
string name = GetName();
Option<string> nameOption = name.ToOption();

// From nullable value type
int? nullableId = GetId();
Option<int> idOption = nullableId.ToOption();

// With predicate
int age = GetAge();
Option<int> adultAgeOption = age.ToOption(a => a >= 18);
```

#### ToResult

Convert Option to Result:

```csharp
Option<User> userOption = FindUser(42);
Result<User, string> result = userOption.ToResult(() => "User not found");
```

#### Map and Bind (Async Versions)

Transform Options asynchronously:

```csharp
Option<User> userOption = FindUser(42);

Option<string> emailOption = await userOption
    .MapAsync(async user => await GetEmailAsync(user));

Option<Order> orderOption = await userOption
    .BindAsync(async user => await FindLatestOrderAsync(user));
```

#### ToNullable

Convert Option back to nullable:

```csharp
Option<int> ageOption = GetAge();
int? nullableAge = ageOption.ToNullable(); // int? or null
```

### Result Extensions

#### Map, Bind, and MapFailure (Async Versions)

All the Result operations you've learned have async equivalents:

```csharp
Result<User, string> result = GetUser(42);

Result<string, string> emailResult = await result
    .MapAsync(async user => await GetEmailAsync(user));

Result<Order, string> orderResult = await result
    .BindAsync(async user => await CreateOrderAsync(user));

Result<User, string> friendlyResult = await result
    .MapFailureAsync(async error => await TranslateErrorAsync(error));
```

#### MatchAsync

Match with async handlers:

```csharp
var result = GetUser(42);

await result.MatchAsync(
    onSuccess: async user => await SendWelcomeEmail(user),
    onFailure: async error => await LogError(error)
);
```

#### ToErrorResponse (Async Version)

```csharp
var result = await Try.RunAsync(async () => await _database.GetUserAsync(userId))
    .ToErrorResponseAsync();
```

---

## ViewModel Extensions: UI-Friendly Helpers

The package includes special extensions designed for ViewModel code, making it easy to work with Options and Results in UI scenarios.

### ApplyAsync (For Results)

Apply success/error handlers without explicit Match:

```csharp
public class UserViewModel : ReactiveObject
{
    private string _statusMessage;
    
    public async Task LoadUserAsync(int userId)
    {
        await _userService.GetUserAsync(userId)
            .ApplyAsync(
                onSuccess: user => 
                {
                    DisplayName = user.Name;
                    Email = user.Email;
                    _statusMessage = "User loaded successfully";
                },
                onError: ex => 
                {
                    _statusMessage = $"Failed to load user: {ex.Message}";
                }
            );
    }
}
```

### Apply (Synchronous Version)

```csharp
var result = GetUser(42);

result.Apply(
    onSuccess: user => DisplayUser(user),
    onError: ex => ShowError(ex.Message)
);
```

### ApplyAsync with Async Handlers

```csharp
await _userService.GetUserAsync(userId)
    .ApplyAsync(
        onSuccessAsync: async user => 
        {
            DisplayName = user.Name;
            await LoadUserPreferencesAsync(user.Id);
        },
        onErrorAsync: async ex => 
        {
            await LogErrorAsync(ex);
            ShowErrorMessage(ex.Message);
        }
    );
```

### Why These Extensions?

In ViewModels, you often want to perform side effects (updating UI properties, showing messages) rather than transforming values. The `Apply` methods make this cleaner than using `Match`:

```csharp
// ❌ Using Match (less clear intent)
var _ = result.Match(
    onSuccess: user => 
    {
        DisplayUser(user);
        return Unit.Value; // Awkward!
    },
    onFailure: error => 
    {
        ShowError(error);
        return Unit.Value; // Awkward!
    }
);

// ✅ Using Apply (clear intent)
result.Apply(
    onSuccess: user => DisplayUser(user),
    onError: error => ShowError(error)
);
```

---

## Collection Extensions: Working with Sequences

### ApplyToCollectionAsync

Update an ObservableCollection from a Result of enumerable:

```csharp
public class UsersViewModel : ReactiveObject
{
    public ObservableCollection<User> Users { get; } = new();
    
    public async Task LoadUsersAsync()
    {
        await _userService.GetAllUsersAsync()
            .ApplyToCollectionAsync(
                Users,
                onError: ex => ShowError($"Failed to load users: {ex.Message}")
            );
    }
}
```

**What it does:**
- If the Result is success, clears the collection and adds all items
- If the Result is error, calls the error handler
- Perfect for loading data into UI collections

### ToStatus

Convert a Result to a status string for display:

```csharp
Result<int, Exception> result = Try.Run(() => int.Parse(input));

string statusMessage = result.ToStatus(
    successFormatter: num => $"Successfully parsed: {num}",
    errorFormatter: ex => $"Parse failed: {ex.Message}"
);

StatusLabel.Text = statusMessage;
```

This is useful for showing status messages in UI without explicit Match calls.

---

## LINQ Extensions for Options

### Select (Same as Map)

Transform Option values in LINQ queries:

```csharp
var userOption = FindUser(42);
var emailOption = from user in userOption
                  select user.Email;
```

### SelectMany (Same as Bind)

Chain optional operations in LINQ syntax:

```csharp
var productOption = 
    from user in FindUser(userId)
    from order in FindLatestOrder(user)
    from product in FindProduct(order.ProductId)
    select product;
```

### Where

Filter Options based on a predicate:

```csharp
var adultUserOption = 
    from user in FindUser(userId)
    where user.Age >= 18
    select user;
```

If the condition is false, the Option becomes None.

### SelectAwait

For async transformations in LINQ:

```csharp
var emailOption = await 
    (from user in FindUserAsync(userId)
     select user)
    .SelectAwait(async user => await GetEmailAsync(user));
```

---

## Putting It All Together: Complete Example

Let's build a complete feature using all the tools we've learned:

```csharp
public class OrderProcessor
{
    private readonly IDatabase _database;
    private readonly IPaymentService _paymentService;
    private readonly IEmailService _emailService;
    private readonly ILogger<OrderProcessor> _logger;
    
    public async Task<Result<OrderConfirmation, ErrorResponse>> ProcessOrderAsync(
        int userId,
        List<OrderItem> items)
    {
        // Validate inputs
        var validationResult = ValidateOrderInputs(userId, items);
        if (Pattern.IsError(validationResult))
            return await Task.FromResult(validationResult);
        
        // Process order with Try for exception safety
        return await Try.RunAsync(async () =>
        {
            // Find user (Option -> Result)
            var user = await FindUserAsync(userId)
                .ToResult(() => new ErrorResponse("User not found"));
                
            if (Pattern.IsError(user))
                return user.Match(_ => null, error => new Result<OrderConfirmation, ErrorResponse>.Error(error));
            
            // Calculate total
            var totalResult = await CalculateTotalAsync(items);
            
            // Chain operations with Bind
            return await user
                .BindAsync(u => ValidateUserAccountAsync(u))
                .BindAsync(u => totalResult.MapAsync(total => (u, total)))
                .BindAsync(async data => 
                {
                    var (u, total) = data;
                    var paymentResult = await ProcessPaymentAsync(u, total);
                    return paymentResult.MapAsync(payment => (u, total, payment));
                })
                .BindAsync(async data =>
                {
                    var (u, total, payment) = data;
                    var order = await CreateOrderAsync(u, items, total, payment);
                    return order;
                })
                .BindAsync(async order =>
                {
                    // Send confirmation email (Unit for side effect)
                    var emailResult = await SendConfirmationEmailAsync(order);
                    return emailResult.MapAsync(_ => order);
                })
                .MapAsync(order => new OrderConfirmation
                {
                    OrderId = order.Id,
                    Total = order.Total,
                    ConfirmationNumber = order.ConfirmationNumber
                });
        })
        .ToErrorResponseAsync();
    }
    
    private Result<Unit, ErrorResponse> ValidateOrderInputs(int userId, List<OrderItem> items)
    {
        if (userId <= 0)
            return new ErrorResponse("Invalid user ID");
        
        if (items == null || items.Count == 0)
            return new ErrorResponse("Order must contain at least one item");
        
        if (items.Any(item => item.Quantity <= 0))
            return new ErrorResponse("All items must have positive quantity");
        
        return Unit.Value;
    }
    
    private async Task<Option<User>> FindUserAsync(int userId)
    {
        return (await Try.RunAsync(() => _database.Users.FindAsync(userId)))
            .Match(
                onSuccess: user => user.ToOption(),
                onFailure: ex =>
                {
                    _logger.LogError(ex, "Database error finding user");
                    return Option.None<User>();
                }
            );
    }
    
    private async Task<Result<User, ErrorResponse>> ValidateUserAccountAsync(User user)
    {
        if (!user.IsActive)
            return new ErrorResponse("User account is not active");
        
        if (user.EmailVerified == false)
            return new ErrorResponse("Email address must be verified");
        
        return user;
    }
    
    private async Task<Result<decimal, ErrorResponse>> CalculateTotalAsync(List<OrderItem> items)
    {
        return await Try.RunAsync(async () =>
        {
            decimal total = 0;
            foreach (var item in items)
            {
                var price = await _database.Products
                    .Where(p => p.Id == item.ProductId)
                    .Select(p => p.Price)
                    .FirstOrDefaultAsync();
                
                if (price <= 0)
                    throw new Exception($"Product {item.ProductId} not found or has invalid price");
                
                total += price * item.Quantity;
            }
            return total;
        })
        .ToErrorResponseAsync();
    }
    
    private async Task<Result<Payment, ErrorResponse>> ProcessPaymentAsync(User user, decimal amount)
    {
        return await Try.RunAsync(async () =>
        {
            var payment = await _paymentService.ChargeAsync(user.PaymentMethodId, amount);
            return payment;
        })
        .ToErrorResponseAsync();
    }
    
    private async Task<Result<Order, ErrorResponse>> CreateOrderAsync(
        User user,
        List<OrderItem> items,
        decimal total,
        Payment payment)
    {
        return await Try.RunAsync(async () =>
        {
            var order = new Order
            {
                UserId = user.Id,
                Items = items,
                Total = total,
                PaymentId = payment.Id,
                ConfirmationNumber = GenerateConfirmationNumber(),
                CreatedAt = DateTime.UtcNow
            };
            
            await _database.Orders.AddAsync(order);
            await _database.SaveChangesAsync();
            
            return order;
        })
        .ToErrorResponseAsync();
    }
    
    private async Task<Result<Unit, ErrorResponse>> SendConfirmationEmailAsync(Order order)
    {
        return await Try.RunAsync(async () =>
        {
            var user = await _database.Users.FindAsync(order.UserId);
            await _emailService.SendOrderConfirmationAsync(user.Email, order);
            return Unit.Value;
        })
        .MapFailureAsync(ex =>
        {
            // Log but don't fail the order
            _logger.LogWarning(ex, "Failed to send confirmation email for order {OrderId}", order.Id);
            return new ErrorResponse("Order created but confirmation email failed");
        });
    }
    
    private string GenerateConfirmationNumber()
    {
        return $"ORD-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
    }
}

// ViewModel usage
public class CheckoutViewModel : ReactiveObject
{
    private readonly OrderProcessor _orderProcessor;
    private string _statusMessage;
    
    public string StatusMessage
    {
        get => _statusMessage;
        set => this.RaiseAndSetIfChanged(ref _statusMessage, value);
    }
    
    public async Task CheckoutAsync()
    {
        StatusMessage = "Processing order...";
        
        await _orderProcessor.ProcessOrderAsync(CurrentUserId, CartItems)
            .ApplyAsync(
                onSuccess: confirmation =>
                {
                    StatusMessage = $"Order confirmed! Confirmation number: {confirmation.ConfirmationNumber}";
                    NavigateToConfirmationPage(confirmation);
                },
                onError: error =>
                {
                    StatusMessage = $"Order failed: {error.Message}";
                    ShowErrorDialog(error.Message);
                }
            );
    }
}
```

**What this example demonstrates:**

1. ✅ Input validation returns `Result<Unit, ErrorResponse>`
2. ✅ `Option<User>` from database query converted to `Result`
3. ✅ `Try.RunAsync` wraps operations that might throw
4. ✅ `Bind` chains operations that can fail
5. ✅ `Map` transforms successful values
6. ✅ `Unit` represents side effects (sending email)
7. ✅ `ToErrorResponse` converts exceptions to user-friendly messages
8. ✅ `Pattern.IsError` checks result state
9. ✅ `ApplyAsync` in ViewModel for clean UI updates
10. ✅ Graceful error handling (email failure logged but doesn't fail order)

---

## Best Practices Summary

### When to Use Unit

- ✅ Operations that perform side effects but return no value
- ✅ Validations that return success/failure without a value
- ✅ Chaining operations where the next step doesn't need the previous value
- ❌ Don't use Unit when you actually need to return a value

### When to Use Try

- ✅ Wrapping code that might throw exceptions
- ✅ File I/O operations
- ✅ Database operations
- ✅ JSON serialization/deserialization
- ✅ External API calls
- ❌ Don't use Try for expected business logic errors (use Result directly)

### When to Use Pattern

- ✅ LINQ queries with method group conversions
- ✅ Counting successes/failures in collections
- ✅ Early checks before extracting values
- ❌ Don't use Pattern when Match is more appropriate

### When to Use ErrorResponse

- ✅ API responses
- ✅ UI error messages
- ✅ Standardized error handling
- ✅ When you need to hide exception details
- ❌ Don't use ErrorResponse for internal domain errors (use domain-specific types)

### Extension Method Guidelines

- ✅ Use `ApplyAsync` for ViewModel side effects
- ✅ Use `ToOption()` at boundaries (database, external APIs)
- ✅ Use `ToResult()` when converting from Option to Result
- ✅ Use `ToErrorResponse()` for user-facing errors
- ✅ Use async variants (`MapAsync`, `BindAsync`) for async operations
- ❌ Don't mix sync and async unnecessarily

---

## Conclusion: The Complete Toolkit

We've explored the supporting cast of the AStar.Dev.Functional.Extensions package:

1. **`Unit`** - Represents "no value" in a type-safe way, enabling functional patterns with side effects
2. **`Try`** - Safely executes code that might throw, converting exceptions to Result values
3. **`Pattern`** - Utility methods for checking Option and Result states
4. **`ErrorResponse`** - Standardized error messages for APIs and UI
5. **Extension Methods** - Dozens of helpers that make functional programming seamless
6. **ViewModel Helpers** - Special extensions for UI code
7. **Collection Helpers** - Working with Options and Results in sequences

Combined with `Option<T>` and `Result<T, TError>` from the previous posts, you now have a complete toolkit for writing clean, safe, maintainable C# code using functional programming principles.

**Key Takeaways:**

- Use `Unit` when you need to return "success with no value"
- Use `Try` to safely execute exception-throwing code
- Use `Pattern` methods for state checks and LINQ queries
- Use `ErrorResponse` for user-friendly error messages
- Leverage extension methods to compose operations naturally
- Choose the right tool for each scenario

### The Journey Complete

Over this four-part series, we've covered:

1. **[Part 1](./01-functional-extensions-overview.md)** - Why functional programming makes code better
2. **[Part 2](./02-result-type-deep-dive.md)** - Mastering Result<T, TError> for elegant error handling
3. **[Part 3](./03-option-type-deep-dive.md)** - Mastering Option<T> to eliminate null reference exceptions
4. **[Part 4](./04-supporting-types-and-extensions.md)** (this post) - The supporting cast that makes it all work together

You now have everything you need to write functional C# code that is:

- ✅ **Explicit** - Types tell the whole story
- ✅ **Safe** - Compiler helps prevent errors
- ✅ **Composable** - Build complex logic from simple parts
- ✅ **Maintainable** - Easy to understand and modify
- ✅ **Testable** - Simple to verify correctness
- ✅ **Delightful** - Actually fun to write!

Start small, pick one area of your codebase, and introduce these patterns gradually. Before you know it, you'll be writing functional C# naturally, and you'll wonder how you ever lived without it.

Happy coding! 🚀

---

## Series Navigation

- [Part 1: Overview of AStar.Dev.Functional.Extensions](./01-functional-extensions-overview.md)
- [Part 2: Deep Dive into Result<T, TError>](./02-result-type-deep-dive.md)
- [Part 3: Mastering Option<T>](./03-option-type-deep-dive.md)
- [Part 4: The Supporting Cast](./04-supporting-types-and-extensions.md) (You are here)

---

## Additional Resources

- [AStar.Dev.Functional.Extensions GitHub Repository](https://github.com/astar-development/astar-dev-onedrive-sync-client)
- [Functional Programming in C# by Enrico Buonanno](https://www.manning.com/books/functional-programming-in-c-sharp)
- [Railway Oriented Programming by Scott Wlaschin](https://fsharpforfunandprofit.com/rop/)
- [Language Ext - Another Excellent FP Library for C#](https://github.com/louthy/language-ext)

---

*Thank you for joining us on this journey into functional programming with C#! Questions or feedback? We'd love to hear from you. Reach out to the AStar Development Team or leave a comment below.*
