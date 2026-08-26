using System.IO;

namespace Reactor.Utilities.Extensions;

/// <summary>
/// Provides extension methods for <see cref="Stream"/>.
/// </summary>
public static class StreamExtensions
{
    /// <summary>
    /// Fully reads the <paramref name="input"/> stream.
    /// </summary>
    /// <param name="input">The stream to read.</param>
    /// <returns>A byte array read from the <see cref="Stream"/>.</returns>
    public static byte[] ReadFully(this Stream input)
    {
        using var ms = new MemoryStream();
        input.CopyTo(ms);
        return ms.ToArray();
    }
}
