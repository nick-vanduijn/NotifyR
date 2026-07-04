namespace NotifyR;

public readonly struct Unit : IEquatable<Unit>
{
    public static readonly Unit Value = new();

    public static readonly Task<Unit> Completed = Task.FromResult(Value);

    public bool Equals(Unit other) => true;

    public override bool Equals(object? obj) => obj is Unit;

    public override int GetHashCode() => 0;

    public override string ToString() => "()";

    public static bool operator ==(Unit left, Unit right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Unit left, Unit right)
    {
        return !(left == right);
    }
}
