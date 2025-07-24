namespace Donjon;

public class TrackedRandom() : Random
{
    public override int Next()
    {
        return base.Next();
    }
    public override int Next(int maxValue)
    {
        return base.Next(maxValue);
    }
    public override int Next(int minValue, int maxValue)
    {
        return base.Next(minValue, maxValue);
    }
    public override void NextBytes(Span<byte> buffer)
    {
        base.NextBytes(buffer);
    }
    public override void NextBytes(byte[] buffer)
    {
        base.NextBytes(buffer);
    }
    public override double NextDouble()
    {
        return base.NextDouble();
    }
    public override long NextInt64()
    {
        return base.NextInt64();
    }
    public override long NextInt64(long maxValue)
    {
        return base.NextInt64(maxValue);
    }
    public override long NextInt64(long minValue, long maxValue)
    {
        return base.NextInt64(minValue, maxValue);
    }
    public override float NextSingle()
    {
        return base.NextSingle();
    }
    protected override double Sample()
    {
        return base.Sample();
    }
    new public void Shuffle<T>(T[] values) => base.Shuffle(values);
    new public void Shuffle<T>(Span<T> values) => base.Shuffle(values);


}