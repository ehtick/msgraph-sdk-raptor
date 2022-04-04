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

        _idReplacer = new IdentifierReplacer(tree, Languages.PowerShell);
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

    private const string snippetWithMissingId = @"
        Import-Module Microsoft.Graph.Calendar
        # A UPN can also be used as -UserId.
        Get-MgUserEventAttachment -UserId $missingId";
    /// <summary>
    /// Snippet with multiple placeholders should pass
    /// </summary>
    /// <param name="testSnippet"></param>
    /// <param name="expectedTestSnippet"></param>
    [TestCase(snippetWithMultiplePlaceHolders, expectedSnippetWithMultiplePlaceHolders)]
    public void SnippetWithMultiplePlaceHoldersShouldPass(string testSnippet, string expectedTestSnippet)
    {
        var identifierReplacer = _idReplacer.ReplaceIds(testSnippet);
        Assert.AreEqual(identifierReplacer, expectedTestSnippet);
    }
    /// <summary>
    ///  Snippet with missing identifier should throw an InvalidDataException,
    /// </summary>
    /// <param name="testSnippet"></param>
    [TestCase(snippetWithMissingId)]
    public void SnippetWithMissingIdShouldThrowAnException(string testSnippet)
    {
        Assert.Throws<InvalidDataException>(() => _idReplacer.ReplaceIds(testSnippet));
    }

    /// <summary>
    ///  Snippet with a single placeholder should pass
    /// </summary>
    /// <param name="testSnippet"></param>
    /// <param name="expectedTestSnippet"></param>

    [TestCase(snippetWithSinglePlaceHolders, expectedSnippetWithSinglePlaceHolders)]
    public void SinglePlaceHolderSnippetShouldPass(string testSnippet, string expectedTestSnippet)
    {
        var identifierReplacer = _idReplacer.ReplaceIds(testSnippet);
        Assert.AreEqual(identifierReplacer, expectedTestSnippet);
    }
 

}
