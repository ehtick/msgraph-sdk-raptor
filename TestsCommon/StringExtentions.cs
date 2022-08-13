// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.

namespace TestsCommon;

public static class StringExtentions
{
    public static string ReplaceOrRemove(this string stringval, bool condition, string text, string replacement)
    {
        ArgumentNullException.ThrowIfNull(stringval);
        ArgumentNullException.ThrowIfNull(condition);
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(replacement);

        if (condition)
            return stringval.Replace(text, replacement);

        return stringval.Replace(text, "");

    }

    public static string ToFirstCharacterUpperCase(this string stringValue)
    {
        if (string.IsNullOrEmpty(stringValue)) return stringValue;
        return char.ToUpper(stringValue[0], CultureInfo.InvariantCulture) + stringValue[1..];
    }

    public static bool ContainsAny(this string haystack, params string[] needles)
    {
        ArgumentNullException.ThrowIfNull(haystack);
        ArgumentNullException.ThrowIfNull(needles);
        return needles.Any(needle => haystack.Contains(needle));
    }
}
