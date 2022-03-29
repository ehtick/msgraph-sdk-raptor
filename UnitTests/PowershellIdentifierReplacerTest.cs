namespace UnitTests;

public class PowershellIdentifierReplacerTest
{
    private IdentifierReplacer _idReplacer;
    [SetUp]
    public void Setup()
    {
        // identifiers.json holds sample tree constructed from V1 urls
        var identifiersJson = System.IO.File.ReadAllText("identifiers.json");
        var tree = JsonSerializer.Deserialize<IDTree>(identifiersJson);

        _idReplacer = new IdentifierReplacer(tree, Languages.PowerShell.ToString());
    }
    private const string snippetWithMultiplePlaceHolders = @"
        Import-Module Microsoft.Graph.Calendar
        # A UPN can also be used as -UserId.
        Get-MgUserEventAttachment -UserId $userId -EventId $eventId";

    private const string expectedSnippetWithMultiplePlaceHolders = @"
        Import-Module Microsoft.Graph.Calendar
        # A UPN can also be used as -UserId.
        Get-MgUserEventAttachment -UserId user -EventId event";

    private const string snippetWithSinglePlaceHolders = @"
        Import-Module Microsoft.Graph.Calendar
        # A UPN can also be used as -UserId.
        Get-MgUserEventAttachment -UserId $userId";

    private const string expectedSnippetWithSinglePlaceHolders = @"
        Import-Module Microsoft.Graph.Calendar
        # A UPN can also be used as -UserId.
        Get-MgUserEventAttachment -UserId user";
    /// <summary>
    /// Snippet with multiple placeholders should pass as long as the language is identified as powershell
    /// The replace id function was modified because,
    /// initially only one placeholder was replcaed with an Id
    /// </summary>
    /// <param name="testSnippet"></param>
    /// <param name="expectedTestSnippet"></param>
    [TestCase(snippetWithMultiplePlaceHolders, expectedSnippetWithMultiplePlaceHolders)]
    public void SnippetWithMultiplePlaceHoldersShouldPass(string testSnippet, string expectedTestSnippet)
    {
        var identifierReplacer = _idReplacer.ReplaceIds(testSnippet, Languages.PowerShell);
        Assert.AreEqual(identifierReplacer, expectedTestSnippet);
    }
    /// <summary>
    ///  Snippet with multiple placeholders should throw an InvalidDataException,
    ///  as long as the language is not identified as powershell
    ///  This test indicates what used to happen before the modification of the Id replacement function
    /// </summary>
    /// <param name="testSnippet"></param>
    [TestCase(snippetWithMultiplePlaceHolders)]
    public void SnippetWithMultiplePlaceHoldersShouldThrowAnException(string testSnippet)
    {
        Assert.Throws<InvalidDataException>(() => _idReplacer.ReplaceIds(testSnippet, Languages.CSharp));
    }

    /// <summary>
    ///  Snippet with a single placeholder should pass as long as the language is not identified as powershell
    /// </summary>
    /// <param name="testSnippet"></param>
    /// <param name="expectedTestSnippet"></param>

    [TestCase(snippetWithSinglePlaceHolders, expectedSnippetWithSinglePlaceHolders)]
    public void SinglePlaceHolderSnippetIdentifiedAsPowerShellShouldPass(string testSnippet, string expectedTestSnippet)
    {
        var identifierReplacer = _idReplacer.ReplaceIds(testSnippet, Languages.PowerShell);
        Assert.AreEqual(identifierReplacer, expectedTestSnippet);
    }

    /// <summary>
    ///  Snippet with a single placeholder should pass even if the language is not identified as powershell
    ///  This test indicates what used to happen before the modification.
    ///  There was no problem with snippetes that have a single placeholder
    /// </summary>
    /// <param name="testSnippet"></param>
    /// <param name="expectedTestSnippet"></param>

    [TestCase(snippetWithSinglePlaceHolders, expectedSnippetWithSinglePlaceHolders)]
    public void SinglePlaceHolderSnippetNotIdentifiedAsPowerShellShouldPass(string testSnippet, string expectedTestSnippet)
    {
        var identifierReplacer = _idReplacer.ReplaceIds(testSnippet, Languages.CSharp);
        Assert.AreEqual(identifierReplacer, expectedTestSnippet);
    }
 

}
