namespace MsGraphSDKSnippetsCompiler
{
    public static class PermissionScopes
    {
        private static readonly Dictionary<string, string> EdgeCases = new()
        {
            {
                "valueAxis",
                "seriesAxis"
            }
        };

        /// <summary>
        /// Calls DevX Api to get required permissions
        /// </summary>
        /// <param name="testData"></param>
        /// <param name="path"></param>
        /// <param name="method"></param>
        /// <param name="edgeCases"></param>
        /// <returns>
        /// 1. delegated scopes if found
        /// 2. null only if application scopes are found (we don't care about the specific application permission as our app has all of them)
        /// </returns>
        /// <exception cref="AggregateException">
        /// If DevX API fails to return scopes for both application and delegation permissions,
        /// throws an AggregateException containing the last exception from the service
        /// </exception>
        public static async Task<Scope[]> GetScopes(LanguageTestData testData, string path, string method,
            Dictionary<string, string> edgeCases)
        {
            ArgumentNullException.ThrowIfNull(testData);
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(method);
            ArgumentNullException.ThrowIfNull(edgeCases);

            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("path shouldn't be white space!");
            }

            var versionSegmentLength = "/v1.0".Length;
            if (path.StartsWith("/v1.0", StringComparison.OrdinalIgnoreCase) || path.StartsWith("/beta", StringComparison.OrdinalIgnoreCase))
            {
                path = path[versionSegmentLength..];
            }
            // DevX API only knows about URLs from the documentation, so convert the URL back for DevX API call
            // if we had an edge case replacement
            foreach (var (key, value) in edgeCases)
            {
                path = path.Replace(key, value, StringComparison.OrdinalIgnoreCase);
            }

            using var httpClient = new HttpClient();

            async Task<Scope[]> GetScopesForScopeType(string scopeType)
            {
                using var scopesRequest = new HttpRequestMessage(HttpMethod.Get, $"https://graphexplorerapi.azurewebsites.net/permissions?requesturl={path}&method={method}&scopeType={scopeType}");
                scopesRequest.Headers.Add("Accept-Language", "en-US");

                using var response = await httpClient.SendAsync(scopesRequest).ConfigureAwait(false);
                var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                return JsonSerializer.Deserialize<Scope[]>(responseString);
            }

            try
            {
                return await GetScopesForScopeType("DelegatedWork").ConfigureAwait(false);
            }
            catch
            {
                await TestContext.Out.WriteLineAsync($"Can't get scopes for scopeType=DelegatedWork, url={path}").ConfigureAwait(false);
            }

            try
            {
                // we don't care about a specific Application permission, we only want to make sure that DevX API returns
                // either delegated or application permissions.
                _ = await GetScopesForScopeType("Application").ConfigureAwait(false);
                return null;
            }
            catch (Exception e)
            {
                await TestContext.Out.WriteLineAsync($"Can't get scopes for both delegated and application scopes").ConfigureAwait(false);
                await TestContext.Out.WriteLineAsync($"url={path}").ConfigureAwait(false);
                await TestContext.Out.WriteLineAsync($"docslink={testData.DocsLink}").ConfigureAwait(false);
                throw new AggregateException("Can't get scopes for both delegated and application scopes", e);
            }
        }

        public static async Task<Scope[]> GetScopes(HttpRequestMessage message, LanguageTestData testData)
        {
            ArgumentNullException.ThrowIfNull(message);
            ArgumentNullException.ThrowIfNull(testData);

            return await GetScopes(testData, message.RequestUri.LocalPath, message.Method.ToString(), EdgeCases)
                .ConfigureAwait(false);
        }

        public static async Task<Scope[]> GetScopes(LanguageTestData testData, string path, string method)
        {
            ArgumentNullException.ThrowIfNull(testData);
            ArgumentNullException.ThrowIfNull(path);
            ArgumentNullException.ThrowIfNull(method);

            return await GetScopes(testData, path, method, EdgeCases)
                .ConfigureAwait(false);
        }
    }
}
