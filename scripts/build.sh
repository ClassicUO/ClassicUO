#!/bin/sh

output="../bin/"
config="$1"

if [ -z "$1" ] || [ "$1" = "release" ]; then
 	config="Release"
elif [ "$1" = "debug" ]; then
	config="Debug"
fi


if [ "${config}" = "Debug" ]; then
	echo "\n\n*** WARNING: USING DEBUG CONFIGURATION. IT WILL AFFECT PERFORMANCES OF THE GAME!! ***\n\n"
fi


echo "OUTPUT PATH: ${output}${config}"


msbuild "../src/ClassicUO.csproj" /p:Configuration=$config /p:OutputPath="${output}${config}"
