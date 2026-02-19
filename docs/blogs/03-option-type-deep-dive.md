# Mastering Option<T>: Say Goodbye to Null Reference Exceptions

**Published: February 19, 2026**  
**Author: AStar Development Team**  
**Target Audience: Entry-Level C# Developers**  
**Part 3 of the AStar.Dev.Functional.Extensions Series**

---

## Introduction: The Billion-Dollar Mistake

In 2009, Sir Tony Hoare, the inventor of null references, apologized for creating them. He called it his "billion-dollar mistake." Why? Because null reference exceptions have probably caused more crashes, bugs, and lost productivity than any other single programming construct.

Every C# developer has seen this:

```csharp
var user = GetUser(42);
Console.WriteLine($"Welcome, {user.Name}!"); // 💥 NullReferenceException
```

And every developer has written defensive code like this:

```csharp
var user = GetUser(42);
if (user != null)
{
    Console.WriteLine($"Welcome, {user.Name}!");
}
else
{
    Console.WriteLine("User not found");
}
```

But here's the problem: **it's too easy to forget that null check.** The method signature `User GetUser(int id)` doesn't tell you it might return null. You have to remember to check, or read the documentation, or discover it the hard way when your app crashes in production.

What if there was a way to make "might be absent" explicit? What if the compiler could help you remember to handle the empty case? What if you could chain operations on potentially-null values without a million nested if-statements?

**Welcome to `Option<T>`.**

## What Is Option<T>?

An `Option<T>` is a type that explicitly represents a value that might or might not be present. Instead of using `null` to represent absence, we use a type that can be either:

1. **Some(value)** - A value is present
2. **None** - No value is present

Think of it like a box that either contains something (`Some`) or is empty (`None`).

```csharp
// This Option either contains a User or is empty
Option<User> userOption = GetUser(42);
```

Just by looking at the return type, you **immediately know** that the user might not exist. The type system is telling you: "Hey, this might be empty, you need to handle that case!"

Compare this to:

```csharp
// Does this return null? Who knows! Better check the docs...
User user = GetUser(42);
```

The difference is night and day.

## Creating Options: Some and None

Creating an `Option<T>` is straightforward. You have several ways to do it:

### Method 1: Using Option.Some and Option.None

```csharp
using AStar.Dev.Functional.Extensions;

Option<string> nameOption = Option.Some("Alice");  // Contains "Alice"
Option<string> emptyOption = Option.None<string>(); // Empty
```

### Method 2: Implicit Conversion

Just like with `Result<T, TError>`, `Option<T>` supports implicit conversion:

```csharp
public Option<User> FindUser(int id)
{
    var user = _database.Users.FirstOrDefault(u => u.Id == id);
    
    if (user == null)
        return Option.None<User>(); // Explicit None
    
    return user; // Implicitly converts to Some(user)
}
```

### Method 3: Using ToOption Extension

The most convenient way is often the `ToOption()` extension method:

```csharp
public Option<User> FindUser(int id)
{
    var user = _database.Users.FirstOrDefault(u => u.Id == id);
    return user.ToOption(); // Automatically Some if not null, None if null
}
```

The `ToOption()` method checks if the value is null and automatically wraps it in `Some` or returns `None`. Simple and clean!

### Method 4: Conditional ToOption

You can also create an Option based on a condition:

```csharp
public Option<int> GetPositiveNumber(int number)
{
    return number.ToOption(n => n > 0); // Some if > 0, None otherwise
}

var option1 = GetPositiveNumber(5);  // Some(5)
var option2 = GetPositiveNumber(-3); // None
```

### Method 5: From Nullable Types

If you're working with nullable value types, you can convert them:

```csharp
int? nullableInt = GetNullableValue();
Option<int> optionInt = nullableInt.ToOption();
```

## Pattern Matching: Handling Both Cases

Once you have an `Option<T>`, you need to handle both the `Some` and `None` cases. The primary way to do this is with the `Match` method:

```csharp
var userOption = FindUser(42);

string message = userOption.Match(
    onSome: user => $"Found user: {user.Name}",
    onNone: () => "User not found"
);

Console.WriteLine(message);
```

**What's happening here?**

1. `Match` takes two functions: one for `Some`, one for `None`
2. Only ONE function will be called (whichever matches the actual state)
3. Both functions must return the same type (here, `string`)
4. You can't forget to handle a case—the compiler enforces it!

### Match Variations

#### Side Effects (Void Match)

Sometimes you just want to do something without returning a value:

```csharp
userOption.Match(
    onSome: user => Console.WriteLine($"Hello, {user.Name}!"),
    onNone: () => Console.WriteLine("No user found")
);
```

#### Match with Different Return Types

You can use `Match` to convert to different types:

```csharp
Option<User> userOption = FindUser(42);

// Convert Option<User> to bool
bool exists = userOption.Match(
    onSome: _ => true,
    onNone: () => false
);

// Convert to Result<User, string>
Result<User, string> result = userOption.Match(
    onSome: user => new Result<User, string>.Ok(user),
    onNone: () => new Result<User, string>.Error("User not found")
);
```

Wait, there's an easier way to convert to Result! We'll cover that soon.

## Checking Option State: IsSome and IsNone

Sometimes you just want to check if an Option contains a value:

```csharp
Option<User> userOption = FindUser(42);

if (userOption.IsSome())
{
    Console.WriteLine("User exists!");
}

if (userOption.IsNone())
{
    Console.WriteLine("User not found!");
}
```

However, be careful! **These methods don't give you the value.** They only tell you whether a value exists. To get the value, use `Match` or `TryGetValue`.

### TryGetValue: C#-Style Extraction

If you prefer the traditional C# pattern with `out` parameters:

```csharp
Option<User> userOption = FindUser(42);

if (userOption.TryGetValue(out var user))
{
    Console.WriteLine($"Found: {user.Name}");
}
else
{
    Console.WriteLine("Not found");
}
```

This works like `Dictionary.TryGetValue()` or `int.TryParse()`.

## Transforming Options: The Map Method

One of the most powerful features of `Option<T>` is the ability to transform the value inside without explicitly checking if it's `Some` or `None`.

```csharp
Option<User> userOption = FindUser(42);

Option<string> emailOption = userOption.Map(user => user.Email);
```

**What happened?**

- If `userOption` was `Some(user)`, then `emailOption` becomes `Some(user.Email)`
- If `userOption` was `None`, then `emailOption` becomes `None`

No null checks needed! The `Map` method handles it for you.

### Chaining Maps

You can chain multiple `Map` operations:

```csharp
Option<string> domainOption = FindUser(42)
    .Map(user => user.Email)              // Option<string>
    .Map(email => email.Split('@'))       // Option<string[]>
    .Map(parts => parts[1])                // Option<string>
    .Map(domain => domain.ToLower());      // Option<string>

string message = domainOption.Match(
    onSome: domain => $"Email domain: {domain}",
    onNone: () => "Could not determine email domain"
);
```

**Key insight:** If at ANY point in the chain we have `None`, all subsequent `Map` calls are skipped, and we end up with `None`. This is called **short-circuiting**, and it's incredibly powerful.

### Real-World Map Example

Let's say you're building an e-commerce site. You need to calculate shipping costs, but the user might not have entered an address yet:

```csharp
public Option<Address> GetUserAddress(int userId)
{
    return FindUser(userId)
        .Map(user => user.ShippingAddress)
        .Map(address => address); // Might be null
}

public Option<decimal> CalculateShipping(int userId)
{
    return GetUserAddress(userId)
        .Map(address => address.ZipCode)
        .Map(zipCode => _shippingService.GetRate(zipCode));
}

// Usage
var shippingOption = CalculateShipping(userId);

var message = shippingOption.Match(
    onSome: rate => $"Shipping: ${rate:F2}",
    onNone: () => "Please enter a shipping address"
);
```

No nested null checks. No defensive programming. Just clean, linear code.

## Chaining Operations: The Bind Method

What if you have a sequence of operations where each one returns an `Option`?

```csharp
Option<User> FindUser(int id);
Option<Order> FindLatestOrder(User user);
Option<Product> FindProduct(Order order);
```

If you try to use `Map`:

```csharp
var result = FindUser(42)
    .Map(user => FindLatestOrder(user)); // Option<Option<Order>> ❌
```

You end up with a nested `Option<Option<Order>>`. That's not what you want!

Instead, use `Bind`:

```csharp
var productOption = FindUser(42)
    .Bind(user => FindLatestOrder(user))   // Option<Order>
    .Bind(order => FindProduct(order));    // Option<Product>

var message = productOption.Match(
    onSome: product => $"User's latest product: {product.Name}",
    onNone: () => "Could not determine latest product"
);
```

**What Bind does:**

1. If the current option is `None`, stop and return `None`
2. If the current option is `Some(value)`, call the function with that value
3. Return whatever Option the function produces (flattening the result)

This is exactly like `Bind` in `Result<T, TError>`—it chains operations that can "fail" (be empty) without nesting.

### Railway-Oriented Programming (Again!)

Just like with `Result`, you can think of `Option` operations as railway tracks:

```
FindUser ──Some──> FindOrder ──Some──> FindProduct ──Some──> ✓
   │                   │                    │
   └──None──> ✗        └──None──> ✗          └──None──> ✗
```

Once you hit the `None` track, you stay there. All subsequent operations are skipped.

## Converting Between Option and Result

Often you want to convert between `Option<T>` and `Result<T, TError>`. The package makes this easy:

### Option to Result

When you have an `Option` but need a `Result`:

```csharp
Option<User> userOption = FindUser(42);

Result<User, string> result = userOption.ToResult(() => "User not found");
```

The lambda function is only called if the Option is `None`. This creates an error message for that case.

### Result to Option (Ignoring Errors)

Sometimes you have a `Result` but only care about the success case:

```csharp
Result<User, string> result = GetUser(42);

Option<User> userOption = result.Match(
    onSuccess: user => Option.Some(user),
    onFailure: _ => Option.None<User>()
);
```

There isn't a built-in `ToOption()` on `Result`, but the pattern above is clean and explicit.

### Combined Example: Database Query

```csharp
public async Task<Result<User, string>> GetUserWithValidation(int userId)
{
    // Start with Option
    return await FindUser(userId)               // Option<User>
        .ToResult(() => "User not found")       // Result<User, string>
        .BindAsync(user => ValidateUser(user))  // Result<User, string>
        .BindAsync(user => EnrichUserData(user)); // Result<User, string>
}
```

This pattern is incredibly useful: use `Option` for queries that might return nothing, then convert to `Result` when you need error handling.

## Working with Collections

`Option<T>` works beautifully with collections. Let's explore common scenarios:

### Finding the First Match

Instead of returning null:

```csharp
// Traditional - returns null if not found
public User FindUserByEmail(string email)
{
    return _users.FirstOrDefault(u => u.Email == email);
}

// With Option - explicit about absence
public Option<User> FindUserByEmail(string email)
{
    return _users.FirstOrDefault(u => u.Email == email).ToOption();
}
```

Even better, use the `FirstOrNone` extension:

```csharp
public Option<User> FindUserByEmail(string email)
{
    return _users.FirstOrNone(u => u.Email == email);
}
```

This is like `FirstOrDefault`, but returns `Option<T>` instead of potentially null.

### Filtering Collections

```csharp
// Get all users who have a profile picture
var usersWithPictures = _users
    .Select(user => user.ProfilePicture.ToOption())
    .Where(option => option.IsSome())
    .Select(option => option.Match(pic => pic, () => null))
    .ToList();
```

However, this is verbose. A cleaner approach:

```csharp
var usersWithPictures = _users
    .Where(user => user.ProfilePicture != null)
    .Select(user => user.ProfilePicture)
    .ToList();

// Or keep it as Options
var pictureOptions = _users
    .Select(user => user.ProfilePicture.ToOption())
    .Where(opt => opt.IsSome())
    .ToList(); // List<Option<Picture>>
```

### Safe Indexing

Access collection elements safely:

```csharp
public Option<T> TryGetAt<T>(this IList<T> list, int index)
{
    return index >= 0 && index < list.Count
        ? Option.Some(list[index])
        : Option.None<T>();
}

// Usage
var items = new List<string> { "a", "b", "c" };

var firstOption = items.TryGetAt(0);  // Some("a")
var tenthOption = items.TryGetAt(10); // None

var message = tenthOption.Match(
    onSome: item => $"Found: {item}",
    onNone: () => "Index out of range"
);
```

No more `IndexOutOfRangeException`!

## LINQ-Style Queries with Option

The package includes LINQ-style query support for `Option<T>`:

### Select (Same as Map)

```csharp
Option<User> userOption = FindUser(42);

Option<string> nameOption = userOption.Select(user => user.Name);
// Equivalent to: userOption.Map(user => user.Name)
```

### SelectMany (Same as Bind)

```csharp
Option<Product> productOption =
    from user in FindUser(42)
    from order in FindLatestOrder(user)
    from product in FindProduct(order)
    select product;
```

This is equivalent to:

```csharp
Option<Product> productOption = FindUser(42)
    .Bind(user => FindLatestOrder(user))
    .Bind(order => FindProduct(order));
```

### Why Use LINQ Syntax?

For complex chains, LINQ syntax can be more readable:

```csharp
var discountOption =
    from user in FindUser(userId)
    from order in FindLatestOrder(user)
    from discount in GetDiscount(user, order)
    where discount.Amount > 0
    select discount;
```

Versus:

```csharp
var discountOption = FindUser(userId)
    .Bind(user => FindLatestOrder(user)
        .Bind(order => GetDiscount(user, order)))
    .Map(discount => discount)
    .Bind(discount => discount.Amount > 0 
        ? Option.Some(discount) 
        : Option.None<Discount>());
```

LINQ syntax keeps the parameters in scope and can be clearer for complex logic.

## Async Operations

Like `Result<T, TError>`, `Option<T>` works seamlessly with async code:

### Async Option-Returning Methods

```csharp
public async Task<Option<User>> FindUserAsync(int userId)
{
    var user = await _database.Users.FindAsync(userId);
    return user.ToOption();
}
```

### Chaining Async Operations

```csharp
var productOption = await FindUserAsync(userId)
    .BindAsync(user => FindLatestOrderAsync(user))
    .BindAsync(order => FindProductAsync(order));

await productOption.MatchAsync(
    onSome: async product => await DisplayProduct(product),
    onNone: async () => await ShowNoProductMessage()
);
```

### SelectAwait (Async LINQ)

For async transformations in LINQ queries:

```csharp
var emailOption = await FindUserAsync(userId)
    .SelectAwait(async user => await GetEmailAsync(user));
```

## Real-World Example: User Profile Page

Let's build a complete user profile page that safely handles missing data:

```csharp
public class UserProfileViewModel
{
    public string DisplayName { get; set; }
    public string ProfilePictureUrl { get; set; }
    public string Bio { get; set; }
    public string Location { get; set; }
    public string WebsiteUrl { get; set; }
    public bool IsVerified { get; set; }
}

public class UserProfileService
{
    private readonly IDatabase _database;
    
    public async Task<Option<UserProfileViewModel>> GetUserProfileAsync(int userId)
    {
        return await FindUserAsync(userId)
            .MapAsync(user => new UserProfileViewModel
            {
                DisplayName = GetDisplayName(user),
                ProfilePictureUrl = GetProfilePicture(user),
                Bio = GetBio(user),
                Location = GetLocation(user),
                WebsiteUrl = GetWebsite(user),
                IsVerified = user.IsVerified
            });
    }
    
    private async Task<Option<User>> FindUserAsync(int userId)
    {
        var user = await _database.Users.FindAsync(userId);
        return user.ToOption();
    }
    
    private string GetDisplayName(User user)
    {
        // Prefer display name, fall back to username, fall back to email
        return user.DisplayName.ToOption()
            .Match(
                onSome: name => name,
                onNone: () => user.Username ?? user.Email.Split('@')[0]
            );
    }
    
    private string GetProfilePicture(User user)
    {
        return user.ProfilePictureUrl.ToOption()
            .Match(
                onSome: url => url,
                onNone: () => "/images/default-avatar.png"
            );
    }
    
    private string GetBio(User user)
    {
        return user.Bio.ToOption()
            .Map(bio => bio.Trim())
            .Match(
                onSome: bio => bio,
                onNone: () => "No bio available"
            );
    }
    
    private string GetLocation(User user)
    {
        return user.City.ToOption()
            .Bind(city => user.Country.ToOption()
                .Map(country => $"{city}, {country}"))
            .Match(
                onSome: location => location,
                onNone: () => user.Country ?? "Location not specified"
            );
    }
    
    private string GetWebsite(User user)
    {
        return user.Website.ToOption()
            .Map(url => url.StartsWith("http") ? url : $"https://{url}")
            .Match(
                onSome: url => url,
                onNone: () => string.Empty
            );
    }
}

// Controller usage
public async Task<IActionResult> Profile(int userId)
{
    var profileOption = await _profileService.GetUserProfileAsync(userId);
    
    return profileOption.Match(
        onSome: profile => View(profile),
        onNone: () => NotFound()
    );
}
```

**What's great about this code?**

1. ✅ Every optional field is handled explicitly
2. ✅ Fallback values are clear and intentional
3. ✅ No null checks scattered everywhere
4. ✅ Easy to test each method independently
5. ✅ Type-safe—compiler helps ensure we handle all cases

## Common Patterns and Idioms

### Pattern 1: Default Values with Match

Provide a default when the Option is None:

```csharp
var userName = FindUser(userId)
    .Map(user => user.Name)
    .Match(
        onSome: name => name,
        onNone: () => "Guest"
    );
```

Or create a helper extension:

```csharp
public static T GetOrDefault<T>(this Option<T> option, T defaultValue)
{
    return option.Match(
        onSome: value => value,
        onNone: () => defaultValue
    );
}

var userName = FindUser(userId)
    .Map(user => user.Name)
    .GetOrDefault("Guest");
```

### Pattern 2: Lazy Default Values

Sometimes computing the default is expensive:

```csharp
public static T GetOrElse<T>(this Option<T> option, Func<T> defaultFactory)
{
    return option.Match(
        onSome: value => value,
        onNone: defaultFactory
    );
}

var user = FindUser(userId)
    .GetOrElse(() => CreateGuestUser()); // Only called if None
```

### Pattern 3: Filtering Options

Keep an Option only if it satisfies a condition:

```csharp
public static Option<T> Where<T>(this Option<T> option, Func<T, bool> predicate)
{
    return option.Bind(value =>
        predicate(value) ? Option.Some(value) : Option.None<T>());
}

var adultUserOption = FindUser(userId)
    .Where(user => user.Age >= 18);
```

### Pattern 4: Combining Options

Combine two Options—both must be Some:

```csharp
public static Option<TResult> Combine<T1, T2, TResult>(
    Option<T1> option1,
    Option<T2> option2,
    Func<T1, T2, TResult> combiner)
{
    return option1.Bind(val1 =>
        option2.Map(val2 =>
            combiner(val1, val2)));
}

var fullNameOption = Combine(
    firstNameOption,
    lastNameOption,
    (first, last) => $"{first} {last}");
```

### Pattern 5: Execute Side Effects

Do something only if the Option is Some:

```csharp
public static Option<T> Do<T>(this Option<T> option, Action<T> action)
{
    if (option is Option<T>.Some some)
    {
        action(some.Value);
    }
    return option;
}

FindUser(userId)
    .Do(user => _logger.LogInformation("Found user: {UserId}", user.Id))
    .Map(user => user.Name);
```

### Pattern 6: Option Chaining with Different Types

Chain operations that work with different types:

```csharp
public Option<decimal> GetUserAccountBalance(int userId)
{
    return FindUser(userId)
        .Bind(user => FindAccount(user.AccountId))
        .Map(account => account.Balance);
}
```

## Comparing Approaches: Before and After

Let's see a side-by-side comparison of null-based and Option-based code:

### Null-Based (Traditional)

```csharp
public class UserReportService
{
    public UserReport GenerateReport(int userId)
    {
        var user = _database.GetUser(userId);
        if (user == null)
            throw new UserNotFoundException($"User {userId} not found");
        
        var profile = _database.GetProfile(user.Id);
        string displayName;
        if (profile != null && profile.DisplayName != null)
        {
            displayName = profile.DisplayName;
        }
        else
        {
            displayName = user.Email != null 
                ? user.Email.Split('@')[0] 
                : "Unknown";
        }
        
        var orders = _database.GetOrders(user.Id);
        decimal totalSpent = 0;
        if (orders != null)
        {
            foreach (var order in orders)
            {
                if (order != null && order.Total.HasValue)
                {
                    totalSpent += order.Total.Value;
                }
            }
        }
        
        var lastOrder = orders?.LastOrDefault();
        string lastOrderDate = null;
        if (lastOrder != null && lastOrder.OrderDate.HasValue)
        {
            lastOrderDate = lastOrder.OrderDate.Value.ToString("yyyy-MM-dd");
        }
        
        var preferences = _database.GetPreferences(user.Id);
        bool emailNotifications = preferences?.EmailNotifications ?? true;
        
        return new UserReport
        {
            UserId = user.Id,
            DisplayName = displayName,
            TotalSpent = totalSpent,
            LastOrderDate = lastOrderDate,
            EmailNotifications = emailNotifications
        };
    }
}
```

**Problems:**
- Nested null checks everywhere
- Mix of different null-handling strategies (?, ??, throw)
- Easy to forget a null check
- Hard to read and maintain

### Option-Based (Functional)

```csharp
public class UserReportService
{
    public Option<UserReport> GenerateReport(int userId)
    {
        return FindUser(userId)
            .Map(user => new UserReport
            {
                UserId = user.Id,
                DisplayName = GetDisplayName(user),
                TotalSpent = GetTotalSpent(user),
                LastOrderDate = GetLastOrderDate(user),
                EmailNotifications = GetEmailNotifications(user)
            });
    }
    
    private Option<User> FindUser(int userId)
    {
        return _database.GetUser(userId).ToOption();
    }
    
    private string GetDisplayName(User user)
    {
        return FindProfile(user.Id)
            .Bind(profile => profile.DisplayName.ToOption())
            .Match(
                onSome: name => name,
                onNone: () => user.Email.ToOption()
                    .Map(email => email.Split('@')[0])
                    .GetOrDefault("Unknown")
            );
    }
    
    private decimal GetTotalSpent(User user)
    {
        return GetOrders(user.Id)
            .Map(orders => orders
                .Select(order => order.Total.ToOption())
                .Where(opt => opt.IsSome())
                .Sum(opt => opt.Match(total => total, () => 0m)))
            .GetOrDefault(0m);
    }
    
    private string GetLastOrderDate(User user)
    {
        return GetOrders(user.Id)
            .Bind(orders => orders.LastOrDefault().ToOption())
            .Bind(order => order.OrderDate.ToOption())
            .Map(date => date.ToString("yyyy-MM-dd"))
            .GetOrDefault(string.Empty);
    }
    
    private bool GetEmailNotifications(User user)
    {
        return GetPreferences(user.Id)
            .Map(prefs => prefs.EmailNotifications)
            .GetOrDefault(true);
    }
    
    private Option<Profile> FindProfile(int userId)
    {
        return _database.GetProfile(userId).ToOption();
    }
    
    private Option<List<Order>> GetOrders(int userId)
    {
        return _database.GetOrders(userId).ToOption();
    }
    
    private Option<Preferences> GetPreferences(int userId)
    {
        return _database.GetPreferences(userId).ToOption();
    }
}
```

**Benefits:**
- No nested null checks
- Each method has a single, clear responsibility
- Easy to test independently
- Consistent null-handling strategy
- Type-safe—compiler helps catch mistakes

## Testing with Options

Testing code that uses `Option<T>` is straightforward:

```csharp
public class UserServiceTests
{
    [Fact]
    public void FindUser_WithValidId_ReturnsSome()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = service.FindUser(42);
        
        // Assert
        Assert.True(result.IsSome());
        Assert.True(result.TryGetValue(out var user));
        Assert.Equal(42, user.Id);
    }
    
    [Fact]
    public void FindUser_WithInvalidId_ReturnsNone()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var result = service.FindUser(999);
        
        // Assert
        Assert.True(result.IsNone());
    }
    
    [Fact]
    public void GetUserEmail_WhenUserExists_ReturnsSomeEmail()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var emailOption = service.FindUser(42)
            .Map(user => user.Email);
        
        // Assert
        var email = emailOption.Match(
            onSome: e => e,
            onNone: () => null
        );
        Assert.NotNull(email);
        Assert.Contains("@", email);
    }
    
    [Fact]
    public void GetUserEmail_WhenUserDoesNotExist_ReturnsNone()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var emailOption = service.FindUser(999)
            .Map(user => user.Email);
        
        // Assert
        Assert.True(emailOption.IsNone());
    }
    
    [Fact]
    public void ChainedOperations_WhenAllSucceed_ReturnsFinalValue()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var domainOption = service.FindUser(42)
            .Map(user => user.Email)
            .Map(email => email.Split('@')[1]);
        
        // Assert
        Assert.True(domainOption.IsSome());
        var domain = domainOption.Match(d => d, () => null);
        Assert.Equal("example.com", domain);
    }
    
    [Fact]
    public void ChainedOperations_WhenFirstFails_ReturnsNone()
    {
        // Arrange
        var service = CreateService();
        
        // Act
        var domainOption = service.FindUser(999)
            .Map(user => user.Email)
            .Map(email => email.Split('@')[1]);
        
        // Assert
        Assert.True(domainOption.IsNone());
    }
}
```

Testing is simple—just check `IsSome()`, `IsNone()`, or use `Match` to extract values.

## Option vs Nullable Reference Types (C# 8+)

You might be thinking: "Doesn't C# 8 have nullable reference types? Why do I need Option?"

Good question! Nullable reference types (`string?`) are a compiler feature that helps catch some null reference issues, but they have limitations:

### Nullable Reference Types

```csharp
public string? FindUserName(int userId)
{
    var user = _database.GetUser(userId);
    return user?.Name;
}

// Usage
var name = FindUserName(42);
if (name != null)
{
    Console.WriteLine(name);
}
```

**Limitations:**
- ⚠️ Compiler warnings only (can be ignored or disabled)
- ⚠️ No functional operations (Map, Bind, etc.)
- ⚠️ Still using null—just with annotations
- ⚠️ Doesn't work well with generics
- ⚠️ Can't chain operations safely

### Option<T>

```csharp
public Option<string> FindUserName(int userId)
{
    return FindUser(userId).Map(user => user.Name);
}

// Usage
FindUserName(42).Match(
    onSome: name => Console.WriteLine(name),
    onNone: () => Console.WriteLine("Not found")
);
```

**Benefits:**
- ✅ Compile-time enforcement (can't ignore)
- ✅ Rich functional operations (Map, Bind, Match, etc.)
- ✅ No null—explicit Some/None states
- ✅ Works beautifully with generics
- ✅ Chainable operations with short-circuiting

**Can you use both?** Absolutely! Nullable reference types help catch nulls at the boundaries, while `Option<T>` provides rich functional operations inside your domain logic.

## Best Practices

### 1. Use Option for Query Results

When querying data that might not exist:

```csharp
// ✅ Good - explicit about absence
Option<User> FindUser(int id);

// ❌ Avoid - null is implicit
User FindUser(int id);
```

### 2. Don't Wrap Non-Nullable Values

If a value is always present, don't use Option:

```csharp
// ❌ Bad - user ID always exists
Option<int> GetUserId(User user);

// ✅ Good - no need for Option
int GetUserId(User user);
```

### 3. Convert at Boundaries

Convert to/from Option at application boundaries:

```csharp
// API layer - might receive null
public Option<User> GetUser(int? userId)
{
    return userId.ToOption()
        .Bind(id => FindUser(id));
}

// Database layer - might return null
private Option<User> FindUser(int userId)
{
    return _database.Users.Find(userId).ToOption();
}
```

### 4. Use Match, Not IsSome + Extraction

```csharp
// ❌ Avoid - verbose
if (userOption.IsSome() && userOption.TryGetValue(out var user))
{
    Console.WriteLine(user.Name);
}

// ✅ Better - concise
userOption.Match(
    onSome: user => Console.WriteLine(user.Name),
    onNone: () => { }
);
```

### 5. Chain Operations Instead of Nesting

```csharp
// ❌ Avoid - nested
var result = FindUser(userId);
if (result.IsSome() && result.TryGetValue(out var user))
{
    var orderResult = FindOrder(user);
    if (orderResult.IsSome() && orderResult.TryGetValue(out var order))
    {
        return order.Total;
    }
}
return 0;

// ✅ Better - chained
return FindUser(userId)
    .Bind(user => FindOrder(user))
    .Map(order => order.Total)
    .GetOrDefault(0);
```

### 6. Be Consistent

Pick a pattern and stick with it across your codebase:
- Use `Option<T>` consistently for optional values
- Use `.Match()` as the primary way to extract values
- Use `.Map()` and `.Bind()` for transformations

## Conclusion: Null-Safe Code Made Simple

The `Option<T>` type transforms how you handle optional values in C#. Instead of relying on null and defensive null checks everywhere, you make absence explicit in your type signatures and use powerful functional operations to work with optional values safely.

**Key takeaways:**

1. **`Option<T>` makes absence explicit** - Your method signatures tell the full story
2. **Pattern matching ensures you handle all cases** - No forgotten null checks
3. **Map transforms values safely** - Short-circuits on None automatically
4. **Bind chains optional operations** - Build complex flows from simple parts
5. **Convert to Result when needed** - Bridge between Option and error handling
6. **Works with collections and LINQ** - Integrate seamlessly with C# features
7. **Async support** - All operations have async equivalents

By using `Option<T>`, you write code that is:
- ✅ More explicit and honest
- ✅ Safer (no null reference exceptions
)
- ✅ More composable
- ✅ Easier to test
- ✅ Easier to maintain

In the next and final post, we'll explore the remaining classes in the package: `Unit`, `Try`, `Pattern`, and all the helper extensions that make functional programming in C# a joy!

---

## Additional Resources

- [Part 1: Overview of AStar.Dev.Functional.Extensions](./01-functional-extensions-overview.md)
- [Part 2: Deep Dive into Result<T, TError>](./02-result-type-deep-dive.md)
- [Understanding Null Object Pattern](https://refactoring.guru/design-patterns/null-object)
- [AStar.Dev.Functional.Extensions GitHub Repository](https://github.com/astar-development/astar-dev-onedrive-sync-client)

**Coming Next:** Post 4 - The Supporting Cast: Unit, Try, Pattern, and Extension Methods

---

*Questions or feedback? We'd love to hear from you! Reach out to the AStar Development Team or leave a comment below.*
