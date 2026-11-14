using ErrorOr;
using Xunit;

namespace Domain.Tests;

internal static class ErrorAssert
{
    public static void Code<T>(ErrorOr<T> result, string code)
    {
        Assert.True(result.IsError);
        Assert.Equal(code, result.FirstError.Code);
    }
}
