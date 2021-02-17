dotnet build "../src/ClassicUO.csproj" -c Release
dotnet publish "../src/ClassicUO.csproj" -c Release /p:DefineConstants="STANDARD_BUILD" -p:IS_DEV_BUILD=true