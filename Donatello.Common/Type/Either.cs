namespace Donatello.Type;

using System;
using System.Threading.Tasks;

public readonly struct Either<TLeft, TRight>
    where TLeft : class
    where TRight : class
{
    private readonly TLeft _leftValue;
    private readonly TRight _rightValue;

    public Either(TLeft left)
    {
        _leftValue = left;
    }

    public Either(TRight right)
    {
        _rightValue = right;
    }

    public bool IsLeft(out TLeft value)
    {
        value = _leftValue;
        return _rightValue == null;
    }

    public bool IsRight(out TRight value)
    {
        value = _rightValue;
        return _leftValue == null;
    }

    public void Consume(Action<TLeft> leftDelegate, Action<TRight> rightDelegate)
    {
        if (_leftValue is not null)
            leftDelegate(_leftValue);
        else if (_rightValue is not null)
            rightDelegate(_rightValue);
        else
            throw new InvalidOperationException("Invalid state: both values are null.");
    }

    public Task ConsumeAsync(Func<TLeft, Task> leftDelegate, Func<TRight, Task> rightDelegate)
    {
        if (_leftValue is not null)
            return leftDelegate(_leftValue);
        else if (_rightValue is not null)
            return rightDelegate(_rightValue);
        else
            throw new InvalidOperationException("Invalid state: both values are null.");
    }

    public static implicit operator Either<TLeft, TRight>(TLeft value)
        => new(value);

    public static implicit operator Either<TLeft, TRight>(TRight value)
        => new(value);
}
