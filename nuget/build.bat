mkdir bin
copy *.nuspec bin

mkdir bin\content
copy ..\src\TaskTimer.cs bin\content

pushd bin
nuget pack TaskTimer.nuspec
popd

