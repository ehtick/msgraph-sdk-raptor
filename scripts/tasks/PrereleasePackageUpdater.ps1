# updates prerelease dependencies for the package listed below as $package
# expected to be run where working directory is the root of the repo

$package = "Microsoft.Graph.Beta"
$projectFile = "TestsCommon/TestsCommon.csproj"

dotnet remove $projectFile package $package
dotnet add $projectFile package $package --prerelease
