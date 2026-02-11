using System;

namespace AStar.Dev.Functional.Extensions;

/// <summary>
///     Represents a void/unit type for functional programming.
///     Used to represent successful operations that don't return a value.
/// </summary>
public readonly struct Unit : IEquatable<Unit>
{
    /// <summary>
    ///     Gets the singleton instance of Unit.
    /// </summary>
    public static Unit Value => default;

    /// <summary>
    ///     Determines whether the specified object is equal to the current Unit.
    /// </summary>
    public bool Equals(Unit other) => true;

    /// <summary>
    ///     Determines whether the specified object is equal to the current Unit.
    /// </summary>
    public override bool Equals(object? obj) => obj is Unit;

    /// <summary>
    ///     Returns the hash code for this instance.
    /// </summary>
    public override int GetHashCode() => 0;

    /// <summary>
    ///     Returns a string representation of Unit.
    /// </summary>
    public override string ToString() => "()";

    /// <summary>
    ///     Equality operator.
    /// </summary>
#pragma warning disable IDE0060 // Parameters required for operator overload
    public static bool operator ==(Unit left, Unit right) => true;

    /// <summary>
    ///     Inequality operator.
    /// </summary>
    public static bool operator !=(Unit left, Unit right) => false;
#pragma warning restore IDE0060
}
