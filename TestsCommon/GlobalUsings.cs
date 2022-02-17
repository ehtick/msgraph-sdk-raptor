global using System;
global using System.Collections.Generic;
global using System.Diagnostics;
global using System.IO;
global using System.Globalization;
global using System.Linq;
global using System.Management.Automation;
global using System.Management.Automation.Runspaces;
global using System.Net.Http;
global using System.Text;
global using System.Text.Json;
global using System.Text.Json.Serialization;
global using System.Text.RegularExpressions;
global using System.Threading;
global using System.Threading.Tasks;

global using Azure.Identity;
global using NUnit.Framework;

global using Microsoft.CodeAnalysis;
global using Microsoft.CodeAnalysis.Text;
global using Microsoft.Graph;
global using Microsoft.Extensions.Configuration;

global using MsGraphSDKSnippetsCompiler;
global using MsGraphSDKSnippetsCompiler.Models;
global using static TestsCommon.KnownIssues;

// Microsoft.Graph has very generic names in the namespaces
// disambiguate collisions
global using Diagnostic = Microsoft.CodeAnalysis.Diagnostic;
global using Location = Microsoft.CodeAnalysis.Location;

global using Process = System.Diagnostics.Process;

global using Directory = System.IO.Directory;
global using File = System.IO.File;
