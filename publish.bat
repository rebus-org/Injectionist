msbuild Injectionist\Injectionist.csproj /p:Configuration=Release

if exist deploy (
  rd deploy /s/q
)

if not exist deploy (
  mkdir deploy
)

tools\nuget\nuget pack Injectionist\Injectionist.nuspec -OutputDirectory deploy
