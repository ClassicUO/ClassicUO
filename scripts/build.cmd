SET CURPATH=%~dp0
SET "root_path=%CURPATH%..\"
@SET CSCPATH=%windir%\Microsoft.NET\Framework64\v4.0.30319\

SET "cuo_proj=%root_path%src\"
SET "cuo_output=%root_path%bin\Release\ClassicUO.exe"

ECHO "CUO"
ECHO %cuo_output%

SET "REF_FNA=%cuo_proj%libs\FNA\FNA.dll"
SET "REF_CUOAPI=%cuo_proj%libs\api\cuoapi.dll"
SET "REF_NEWTONSOFTJSON=%root_path%tools\monokickstart\Newtonsoft.Json.dll"
SET "REF_SYSCOMPRESSION=%root_path%tools\monokickstart\System.IO.Compression.dll"
SET "REF_SYSCOMPRESSION_FS=%root_path%tools\monokickstart\System.IO.Compression.FileSystem.dll"

msbuild %cuo_proj%\ClassicUO.csproj /t:Clean;Build /p:Configuration=Release
pause
::%CSCPATH%csc /target:exe /out:%cuo_output%  /r:%REF_FNA%  /r:%REF_CUOAPI%  /r:%REF_NEWTONSOFTJSON%  /r:%REF_SYSCOMPRESSION%  /r:%REF_SYSCOMPRESSION_FS%  /recurse:"%cuo_proj%*.cs" /d:ClassicUO /d:DEV_BUILD /unsafe /optimize