#!/bin/bash
# MonoKickstart Shell Script
# Written by Ethan "flibitijibibo" Lee

# Move to script's directory
cd "`dirname "$0"`"

# Get the system architecture
UNAME=`uname`
ARCH=`uname -m`

# MonoKickstart picks the right libfolder, so just execute the right binary.
if [ "$UNAME" == "Darwin" ]; then
	# ... Except on OSX.
	export DYLD_LIBRARY_PATH=$DYLD_LIBRARY_PATH:./libs/osx/

	# El Capitan is a total idiot and wipes this variable out, making the
	# Steam overlay disappear. This sidesteps "System Integrity Protection"
	# and resets the variable with Valve's own variable (they provided this
	# fix by the way, thanks Valve!). Note that you will need to update your
	# launch configuration to the script location, NOT just the app location
	# (i.e. Kick.app/Contents/MacOS/Kick, not just Kick.app).
	# -flibit
	if [ "$STEAM_DYLD_INSERT_LIBRARIES" != "" ] && [ "$DYLD_INSERT_LIBRARIES" == "" ]; then
		export DYLD_INSERT_LIBRARIES="$STEAM_DYLD_INSERT_LIBRARIES"
	fi

	mono ./ClassicUO.exe $@
# else
# 	# FIXME: Add support for Linux binaries
# 	if [ "$ARCH" == "x86_64" ]; then
# 		./ClassicUO.bin.x86_64 $@
# 	else
# 		./ClassicUO.bin.x86 $@
# 	fi
fi
