﻿namespace ReportGenerator;

class ReportGenerator
{
    static void Main(string[] args)
    {
        KnownIssuesTextReport(Versions.V1, Languages.CSharp, IssueType.Execution);
        KnownIssuesVisualReport(Versions.V1, Languages.CSharp, IssueType.Execution);
    }

    // create text report for v1 execution known issues
    private static void KnownIssuesTextReport(Versions version, Languages language, IssueType issueType)
    {
        if (language != Languages.CSharp)
        {
            throw new NotImplementedException();
        }

        if (issueType != IssueType.Execution)
        {
            throw new NotImplementedException();
        }

        var issues = CSharpKnownIssues.GetCSharpExecutionKnownIssues(version);
        var documentationLinks = TestDataGenerator.GetDocumentationLinks(version, language);
        var testNameSuffix = $"{version}-{issueType.Suffix()}";

        var unreferencedIssues = new HashSet<string>();

        var reportEntries = new List<ReportEntry>();

        foreach (KeyValuePair<string, KnownIssue> kv in issues.Where(kv => kv.Key.EndsWith(testNameSuffix)))
        {
            var testName = kv.Key;
            var knownIssue = kv.Value;
            var documentationLinkLookupKey = testName.Replace(testNameSuffix, "snippets.md");
            if (!documentationLinks.ContainsKey(documentationLinkLookupKey))
            {
                unreferencedIssues.Add(testName);
            }
            else
            {
                var documentationLink = documentationLinks[documentationLinkLookupKey];
                reportEntries.Add(new ReportEntry
                (
                    testName,
                    documentationLink,
                    knownIssue
                ));
            }
        }

        if (unreferencedIssues.Count > 0)
        {
            var message = "There are known issue entries which are not referenced in the documentation," +
                " please make sure that you have the latest changes from docs repo" +
                " and remove the following from known issues list:" +
                $"{Environment.NewLine}{string.Join(Environment.NewLine, unreferencedIssues)}";
            throw new InvalidDataException(message);
        }

        var fileName = $"{version}-{language.AsString()}-{issueType.LowerName()}-known-issues.md";
        WriteReportEntriesToFile(fileName, reportEntries);
    }

    // output list of ReportEntry to a text file
    private static void WriteReportEntriesToFile(string fileName, IEnumerable<ReportEntry> reportEntries)
    {
        var filePath = Path.Combine(GetReportPath(), fileName);

        File.WriteAllLines(filePath, ReportEntry.GetMarkdownTable(reportEntries));

        Console.WriteLine($"Wrote markdown table report to {filePath}");
    }

    private static string GetReportPath()
    {
        return Path.Combine(Environment.GetEnvironmentVariable("BUILD_SOURCESDIRECTORY"), "msgraph-sdk-raptor", "report");
    }

    // create known issue visual report
    private static void KnownIssuesVisualReport(Versions version, Languages language, IssueType issueType)
    {
        if (language != Languages.CSharp)
        {
            throw new NotImplementedException();
        }

        if (issueType != IssueType.Execution)
        {
            throw new NotImplementedException();
        }

        var issues = CSharpKnownIssues.GetCSharpExecutionKnownIssues(version);
        var counter = new Dictionary<string, int>();
        foreach (KeyValuePair<string, KnownIssue> kv in issues)
        {
            if (!kv.Key.EndsWith($"{version}-{issueType.Suffix()}"))
            {
                continue;
            }

            if (counter.ContainsKey(kv.Value.Owner))
            {
                counter[kv.Value.Owner]++;
            }
            else
            {
                counter.Add(kv.Value.Owner, 1);
            }
        }

        var counterList = counter.ToList();
        var ordered = counterList.OrderByDescending(x => x.Value);

        Console.WriteLine($"{version} {issueType.LowerName()} known issues");
        foreach (KeyValuePair<string, int> kv in ordered)
        {
            Console.WriteLine($"{kv.Key}: {kv.Value}");
        }

        var fileName = Path.Combine(GetReportPath(), $"{version}-{language.AsString()}-{issueType.LowerName()}-known-issues-report.html");
        VisualizeData(ordered, fileName, version);
    }

    // visualize data using chart.js
    // https://www.chartjs.org/docs/latest/charts/bar.html
    public static void VisualizeData(IOrderedEnumerable<KeyValuePair<string, int>> data, string fileName, Versions version = Versions.V1)
    {
        var labels = data.Select(x => x.Key).ToList();
        var values = data.Select(x => x.Value).ToList();

        // create the HTML
        var html = @"<!DOCTYPE html>
<html>
<head>
    <title>" + $"{version} execution known issues" + @"</title>
    <script src=""https://cdn.jsdelivr.net/npm/chart.js@3.6.0/dist/chart.min.js""></script>
</head>
<body>

    <div style=""width: 800px; height: 600px;"">
        <canvas id=""myChart""></canvas>
    </div>

    <script>
        var ctx = document.getElementById('myChart').getContext('2d');
        var myChart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: [" + string.Join(",", labels.Select(x => $"'{x}'")) + @"],
                datasets: [{
                    label: '# of issues',
                    data: [" + string.Join(",", values) + @"],
                    backgroundColor: [
                        'rgba(255, 99, 132, 0.2)',
                        'rgba(54, 162, 235, 0.2)',
                        'rgba(255, 206, 86, 0.2)',
                        'rgba(75, 192, 192, 0.2)',
                        'rgba(153, 102, 255, 0.2)',
                        'rgba(255, 159, 64, 0.2)'
                    ],
                    borderColor: [
                        'rgba(255, 99, 132, 1)',
                        'rgba(54, 162, 235, 1)',
                        'rgba(255, 206, 86, 1)',
                        'rgba(75, 192, 192, 1)',
                        'rgba(153, 102, 255, 1)',
                        'rgba(255, 159, 64, 1)'
                    ],
                    borderWidth: 1
                }]
            },
            options: {
                scales: {
                    yAxes: [{
                        ticks: {
                            beginAtZero: true
                        }
                    }]
                }
            }
        });
    </script>
</body>
</html>";
        // write the HTML to a file
        File.WriteAllText(fileName, html);
        Console.WriteLine($"Wrote html bar chart report to {fileName}");
    }
}
