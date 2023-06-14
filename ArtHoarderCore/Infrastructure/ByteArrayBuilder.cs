namespace ArtHoarderCore.Infrastructure;

public class ByteArrayBuilder
{
    private static readonly byte[] Masks =
    {
        0b_1000_0000,
        0b_0100_0000,
        0b_0010_0000,
        0b_0001_0000,
        0b_0000_1000,
        0b_0000_0100,
        0b_0000_0010,
        0b_0000_0001
    };

    private byte[] _array;
    private byte _currentByteMask;
    private int _currentByte;

    public ByteArrayBuilder(int length = 8)
    {
        _array = new byte[length];
    }

    public void Append(bool bite)
    {
        if (_currentByte > _array.Length - 1)
        {
            var newBytes = new byte[_array.Length * 2];
            for (var i = 0; i < _array.Length; i++)
                newBytes[i] = _array[i];

            _array = newBytes;
        }

        if (bite)
            _array[_currentByte] |= Masks[_currentByteMask];

        _currentByteMask += 1;

        if (_currentByteMask > 7)
        {
            _currentByte += 1;
            _currentByteMask = 0;
        }
    }

    public byte[] ToArray()
    {
        if (_array.Length == _currentByte)
            return _array;

        var array = new byte[_currentByte];
        for (var i = 0; i < _currentByte; i++)
            array[i] = _array[i];
        return array;
    }

    // public override string ToString()
    // {
    //     return ToArray().Aggregate("", (current, b) => current + Dictionary[b]);
    // }
}