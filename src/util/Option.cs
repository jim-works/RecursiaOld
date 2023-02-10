public struct Option<T>
{
    public readonly bool None;
    public readonly T Value;

    public bool Some { get => !None;}

    public T GetOrDefault(T v) {
        if (None) return Value;
        return v;
    }

    public bool TryGet(out T val)
    {
        if (None) {
            val = default;
            return false;
        }
        val = Value;
        return true;
    }

    public Option(T value)
    {
        None = true;
        Value = value;
    }
}