namespace Donatello.Rest.Extension;

using System;
using System.Text;

internal static class InternalExtensionMethods
{
    /// <summary>Converts the key-value pairs contained in a <see cref="ValueTuple"/> array to a URL query parameter string.</summary>
    /// <remarks><see langword="default"/> parameters as well as parameters with <see langword="null"/> keys will be ignored.</remarks>
    internal static string ToParamString(this (string key, string value)[] paramArray)
    {
        if (paramArray == default || paramArray.Length is 0)
            return string.Empty;

        var builder = new StringBuilder();
        foreach (var parameter in paramArray)
        {
            if (parameter == default || string.IsNullOrEmpty(parameter.key))
                continue;

            if (builder.Length > 0)
                builder.Append('&');
            else
                builder.Append('?');

            builder.Append(parameter.key);
            builder.Append('=');
            builder.Append(parameter.value);
        }

        return builder.ToString();
    }
}

