@echo off

set version=%1%

if "%version%"=="" (
	echo Please specify a version!
	goto exit
)

msbuild Injectionist\Injectionist.csproj /p:Configuration=Release

if exist deploy (
  rd deploy /s/q
)

if not exist deploy (
  mkdir deploy
)

tools\nuget\nuget pack Injectionist\Injectionist.nuspec -OutputDirectory deploy -Version %version%
tools\nuget\nuget push deploy\*.nupkg

:exit