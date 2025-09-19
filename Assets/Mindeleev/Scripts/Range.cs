using System;

public readonly struct Range<T> where T : IComparable<T>
{
    public T Min { get; }
    public T Max { get; }

    public Range(T min, T max)
    {
        if (min.CompareTo(max) > 0)
            throw new ArgumentException("Min must be <= Max");

        Min = min;
        Max = max;
    }

    public bool Contains(T value) =>
        value.CompareTo(Min) >= 0 && value.CompareTo(Max) <= 0;

    public bool IsBelow(T value) => value.CompareTo(Min) < 0;

    public bool IsAbove(T value) => value.CompareTo(Max) > 0;

    public override string ToString() => $"[{Min}..{Max}]";
}
