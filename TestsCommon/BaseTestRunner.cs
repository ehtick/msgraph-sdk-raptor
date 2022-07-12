namespace TestsCommon;

public static class BaseTestRunner
{
    /// <summary>
    /// Embeds C# snippet from docs repo into a compilable template
    /// </summary>
    /// <param name="snippet">code snippet from docs repo</param>
    /// <param name="SDKShellTemplate">The template for the sdk code</param>
    /// <returns>
    /// code snippet embedded into compilable template
    /// </returns>
    internal static string ConcatBaseTemplateWithSnippet(string snippet, string SDKShellTemplate)
    {
        // there are mixture of line endings, namely \r\n and \n, normalize that into \r\n
        string codeToCompile = SDKShellTemplate
                   .Replace("//insert-code-here", snippet)
                   .Replace("\r\n", "\n").Replace("\n", "\r\n");

        return codeToCompile;
    }
}
