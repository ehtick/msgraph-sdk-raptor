// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the MIT License.  See License in the project root for license information.
namespace TestsCommon;

/// <summary>
/// Converts a runsettings file into an object after validating settings
/// </summary>
public record RunSettings
{
    public Versions Version
    {
        get; init;
    }
    public string DllPath
    {
        get; init;
    }
    public TestType TestType
    {
        get; init;
    }
    public Languages Language
    {
        get; init;
    }
    public string JavaPreviewLibPath
    {
        get; init;
    }
    private const string DashDash = "---";
    public RunSettings()
    {
    }

    public RunSettings(TestParameters parameters, RunSettings runsettingsOverride=null)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        var versionString = parameters.Get("Version");
        var dllPath = parameters.Get("DllPath");
        var testType = parameters.Get("TestType");
        var lng = parameters.Get("Language");

        /*
            Expected behaviour: runsettings will be provided via Test.runsettings file.
            If runsettings file contains placeholder defaults or no parameters, then override values must be provided.
            Prefer values on settings file, if set, over manual overrides
        */
        if (IsPlaceholderValueOrEmpty(lng)){
                Language = ReplaceUnprovidedSettingsValueWithOverride<Languages>(runsettingsOverride, nameof(Language));
        }
        else
        {
            Language = lng.ToUpperInvariant() switch
            {
                "CSHARP" => Languages.CSharp,
                "C#" => Languages.CSharp,
                "JAVA" => Languages.Java,
                "JAVASCRIPT" => Languages.JavaScript,
                "JS" => Languages.JavaScript,
                "OBJC" => Languages.ObjC,
                "OBJECTIVEC" => Languages.ObjC,
                "OBJECTIVE-C" => Languages.ObjC,
                "TYPESCRIPT" => Languages.TypeScript,
                "POWERSHELL" => Languages.PowerShell,
                _ => Languages.CSharp
            };
        }

        if (IsPlaceholderValueOrEmpty(dllPath)){
                DllPath = ReplaceUnprovidedSettingsValueWithOverride<string>(runsettingsOverride, nameof(DllPath));
        }
        else
        {
            DllPath = dllPath;
            if (Language == Languages.CSharp && !File.Exists(dllPath))
            {
                throw new ArgumentException("File specified with DllPath in Test.runsettings doesn't exist!");
            }
        }

        if (IsPlaceholderValueOrEmpty(versionString)){
                Version = ReplaceUnprovidedSettingsValueWithOverride<Versions>(runsettingsOverride, nameof(Version));
        }
        else
        {
            Version = VersionString.GetVersion(versionString);
        }

        if (IsPlaceholderValueOrEmpty(testType)){
            /*
                If TestType hasn't been supplied from Test.runsettings file it will contain ---(DashDash)/default placeholder values.
                If this is the case use the provided runsettingsOverride.TestType value
            */
            TestType = ReplaceUnprovidedSettingsValueWithOverride<TestType>(runsettingsOverride, nameof(TestType));
        }
        else{
            var testTypeExists = Enum.TryParse(testType, out TestType testTypeEnum);
            TestType = testTypeExists ? testTypeEnum : throw new ArgumentException($"Unexpected test type specified: {testType}");
        }

        JavaPreviewLibPath = InitializeParameter(parameters, nameof(JavaPreviewLibPath)) ?? JavaPreviewLibPath;
    }

    private static string InitializeParameter(TestParameters parameters, string parameterName)
    {
        var value = parameters.Get(parameterName);

        if (!string.IsNullOrEmpty(value) && !value.Contains(DashDash))
        {
            return value;
        }
        else
        {
            return null;
        }
    }
    private static bool IsPlaceholderValueOrEmpty(string propertyName){
        return string.IsNullOrEmpty(propertyName) || propertyName.Contains(DashDash);
    }

    private static T ReplaceUnprovidedSettingsValueWithOverride<T>(RunSettings overrideSettings, string propertyName){
        if (overrideSettings == null){
            throw new ArgumentException("override settings expected but not supplied");
        }
        bool propertyExists = overrideSettings.GetType().GetProperty(propertyName) != null;
        if (!propertyExists){
            throw new ArgumentException($"{propertyName} not specified in override settings");
        }
        TestContext.WriteLine($"{propertyName} is empty or contains placeholder value in file and will be replaced with value in override settings");
        return (T)overrideSettings.GetType().GetProperty(propertyName).GetValue(overrideSettings);
    }
}
