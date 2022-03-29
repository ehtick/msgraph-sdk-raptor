using FluentAssertions;

namespace UnitTests;

public class Tests
{
    private IdentifierReplacer _idReplacer;

    [SetUp]
    public void Setup()
    {
        // identifiers.json holds sample tree constructed from V1 urls
        var identifiersJson = System.IO.File.ReadAllText("identifiers.json");
        var tree = JsonSerializer.Deserialize<IDTree>(identifiersJson);

        _idReplacer = new IdentifierReplacer(tree,Languages.CSharp.ToString());
    }

    [TestCase("https://graph.microsoft.com/v1.0/applications/{application-id}/owners",
              "https://graph.microsoft.com/v1.0/applications/application/owners")]
    [TestCase("https://graph.microsoft.com/v1.0/teams/{team-id}/channels/{channel-id}/members/{conversationMember-id}",
              "https://graph.microsoft.com/v1.0/teams/team/channels/team_channel/members/team_channel_conversationMember")]
    [TestCase("https://graph.microsoft.com/v1.0/communications/callRecords/{callRecords.callRecord-id}?$expand=sessions($expand=segments)",
              "https://graph.microsoft.com/v1.0/communications/callRecords/callRecords.callRecord?$expand=sessions($expand=segments)")]
    [TestCase("https://graph.microsoft.com/v1.0/chats/{chat-id}/members/{conversationMember-id}",
              "https://graph.microsoft.com/v1.0/chats/chat/members/chat_conversationMember")]
    [TestCase("https://graph.microsoft.com/v1.0/teams/{team-id}/members/{conversationMember-id}",
              "https://graph.microsoft.com/v1.0/teams/team/members/team_conversationMember")]
    [TestCase("https://graph.microsoft.com/v1.0/education/schools/{educationSchool-id}/users",
              "https://graph.microsoft.com/v1.0/education/schools/educationSchool/users")]
#pragma warning disable CA1054 // URI-like parameters should not be strings
    public void TestIds(string snippetUrl, string expectedUrl)
#pragma warning restore CA1054 // URI-like parameters should not be strings
    {
        var newUrl = _idReplacer.ReplaceIds(snippetUrl,Languages.CSharp);
        Assert.AreEqual(expectedUrl, newUrl);
    }

    [TestCase]
    public void ShouldReplaceIdentifiersSpecifiedByEdgeCaseRegex()
    {
        const string snippet = @"
GraphServiceClient graphClient = new GraphServiceClient( authProvider );

var queryOptions = new List<QueryOption>()
{
	new QueryOption(""token"", ""2021-09-29T20:00:00Z"")
};
var delta = await graphClient.Me.Drive.Root
	.Delta()
	.Request( queryOptions )
	.GetAsync();
";
        var result = IdentifierReplacer.RegexReplaceEdgeCases(IdentifierReplacer.EdgeCaseRegexes, snippet);
        result.Should().NotContain("2021-09-29T20:00:00Z");
    }
}
