# CMake Project for FAudio
# Written by @NeroBurner
cmake_minimum_required(VERSION 3.10)
project(FAudio C)

if(CMAKE_SOURCE_DIR STREQUAL PROJECT_SOURCE_DIR)
  set(FAUDIO_MAINPROJECT ON)
else()
  set(FAUDIO_MAINPROJECT OFF)
endif()

# Options
option(BUILD_UTILS "Build utils/ folder" OFF)
option(BUILD_TESTS "Build tests/ folder for unit tests to be executed on the host against FAudio" OFF)
option(BUILD_SDL3 "Build against SDL 3.0" ON)
if(WIN32)
option(PLATFORM_WIN32 "Enable native Win32 platform instead of SDL" OFF)
option(WMA_DECODING "Enable WMA decoding support" ${PLATFORM_WIN32})
endif()
option(XNASONG "Build with XNA_Song.c" ON)
option(LOG_ASSERTIONS "Bind FAudio_assert to log, instead of platform's assert" OFF)
option(FORCE_ENABLE_DEBUGCONFIGURATION "Enable DebugConfiguration in all build types" OFF)
option(DUMP_VOICES "Dump voices to RIFF WAVE files" OFF)
option(BUILD_SHARED_LIBS "Build shared library" ON)
if(WIN32)
option(INSTALL_MINGW_DEPENDENCIES "Add dependent libraries to MinGW install target" OFF)
endif()
option(FAUDIO_INSTALL "Enable installation of FAudio" ${FAUDIO_MAINPROJECT})

# C99 Requirement
if(${CMAKE_VERSION} VERSION_LESS "3.1.3")
	message(WARNING "Your CMake version is too old, set -std=c99 yourself!")
else()
	set(CMAKE_C_STANDARD 99)
	set(CMAKE_C_EXTENSIONS OFF)
endif()

# Version
SET(LIB_MAJOR_VERSION "0")
SET(LIB_MINOR_VERSION "26")
SET(LIB_REVISION "03")
SET(LIB_VERSION "${LIB_MAJOR_VERSION}.${LIB_MINOR_VERSION}.${LIB_REVISION}")

# Build Type
if(NOT CMAKE_BUILD_TYPE AND NOT CMAKE_CONFIGURATION_TYPES)
	# By default, we use Release
	message(STATUS "Setting build type to 'Release' as none was specified.")
	set(CMAKE_BUILD_TYPE "Release" CACHE
		STRING "Choose the type of build." FORCE
	)

	# Set the possible values of build type for cmake-gui
	set_property(CACHE CMAKE_BUILD_TYPE PROPERTY
		STRINGS "Debug" "Release" "RelWithDebInfo"
	)
endif()

# Add our repository's module path to CMake's module path list
set(CMAKE_MODULE_PATH "${CMAKE_CURRENT_SOURCE_DIR}/cmake/modules")

# Platform Flags
if(APPLE)
	set(CMAKE_MACOSX_RPATH ON)
	set(CMAKE_OSX_DEPLOYMENT_TARGET 10.9)
elseif(WIN32)
	# "FAudio.dll", not "libFAudio.dll"
	set(CMAKE_SHARED_LIBRARY_PREFIX "")
endif()

# Helper function for finding/installing MinGW libs
if(INSTALL_MINGW_DEPENDENCIES)
	include(cmake/install_shared_libs.cmake)
endif()

set(target FAudio-shared)

if(NOT BUILD_SHARED_LIBS)
	set(target FAudio-static)
endif()

# Source lists
add_library(${target}
	# Public Headers
	include/F3DAudio.h
	include/FACT3D.h
	include/FACT.h
	include/FAPOBase.h
	include/FAPOFX.h
	include/FAPO.h
	include/FAudioFX.h
	include/FAudio.h
	# Internal Headers
	src/FACT_internal.h
	src/FAudio_internal.h
	src/stb.h
	src/stb_vorbis.h
	src/qoa_decoder.h
	# Source Files
	src/F3DAudio.c
	src/FACT3D.c
	src/FACT.c
	src/FACT_internal.c
	src/FAPOBase.c
	src/FAPOFX.c
	src/FAPOFX_echo.c
	src/FAPOFX_eq.c
	src/FAPOFX_masteringlimiter.c
	src/FAPOFX_reverb.c
	src/FAudio.c
	src/FAudioFX_reverb.c
	src/FAudioFX_collector.c
	src/FAudioFX_volumemeter.c
	src/FAudio_internal.c
	src/FAudio_internal_simd.c
	src/FAudio_operationset.c
	src/FAudio_platform_sdl2.c
	src/FAudio_platform_sdl3.c
	src/FAudio_platform_win32.c
	src/FAudio_platform_win32_wmadec.c
	# Optional source files
	src/XNA_Song.c
)

if(WMA_DECODING)
	target_compile_definitions(${target} PRIVATE HAVE_WMADEC=1)
	target_link_libraries(${target} PRIVATE ole32 mfplat mfuuid)
endif()

if(PLATFORM_WIN32)
	target_link_libraries(${target} PRIVATE dxguid uuid winmm ole32 advapi32 user32 mfplat mfreadwrite mfuuid propsys)
	target_compile_definitions(${target} PUBLIC FAUDIO_WIN32_PLATFORM)
	set(PLATFORM_CFLAGS "-DFAUDIO_WIN32_PLATFORM")
	set(XNASONG OFF)
else()
	if(BUILD_SDL3)
		target_compile_definitions(${target} PUBLIC FAUDIO_SDL3_PLATFORM)
		set(PLATFORM_CFLAGS "-DFAUDIO_SDL3_PLATFORM")
	else()
		set(PLATFORM_CFLAGS)
	endif()
endif()

# Only disable DebugConfiguration in release builds
if(NOT FORCE_ENABLE_DEBUGCONFIGURATION)
	target_compile_definitions(${target} PRIVATE $<$<CONFIG:Release>:FAUDIO_DISABLE_DEBUGCONFIGURATION>)
endif()

# FAudio_assert Customization
if(LOG_ASSERTIONS)
	target_compile_definitions(${target} PUBLIC FAUDIO_LOG_ASSERTIONS)
endif()

# FAudio folders as includes, for other targets to consume
target_include_directories(${target} PUBLIC
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
)

# MinGW builds should statically link libgcc
if(MINGW)
	target_link_libraries(${target} PRIVATE -static-libgcc)
endif()

# Soname
set_target_properties(${target} PROPERTIES OUTPUT_NAME "FAudio"
	VERSION ${LIB_VERSION}
	SOVERSION ${LIB_MAJOR_VERSION}
)

# XNA_Song Support
if(NOT XNASONG)
	target_compile_definitions(${target} PRIVATE DISABLE_XNASONG)
endif()

# Dump source voices to RIFF WAVE files
if(DUMP_VOICES)
	target_compile_definitions(${target} PRIVATE FAUDIO_DUMP_VOICES)
endif()

# SDL Dependency
set(FAUDIO_USING_SDL2_PACKAGE OFF)
set(FAUDIO_USING_SDL3_PACKAGE OFF)
if (PLATFORM_WIN32)
	message(STATUS "not using SDL")
elseif (BUILD_SDL3)
	if (DEFINED SDL3_INCLUDE_DIRS AND DEFINED SDL3_LIBRARIES)
		message(STATUS "using pre-defined SDL3 variables SDL3_INCLUDE_DIRS and SDL3_LIBRARIES")
		target_include_directories(${target} PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
		target_link_libraries(${target} PUBLIC ${SDL3_LIBRARIES})
		if(INSTALL_MINGW_DEPENDENCIES)
			install_shared_libs(${SDL3_LIBRARIES} DESTINATION bin NO_INSTALL_SYMLINKS)
		endif()
	else()
		# Only try to autodetect if both SDL3 variables aren't explicitly set
		find_package(SDL3 CONFIG)
		if (TARGET SDL3::SDL3)
			message(STATUS "using TARGET SDL3::SDL3")
			set(FAUDIO_USING_SDL3_PACKAGE ON)
			target_link_libraries(${target} PUBLIC SDL3::SDL3)
			if(INSTALL_MINGW_DEPENDENCIES)
				install_shared_libs(TARGETS SDL3::SDL3 DESTINATION bin NO_INSTALL_SYMLINKS REQUIRED)
			endif()
		elseif (TARGET SDL3)
			message(STATUS "using TARGET SDL3")
			set(FAUDIO_USING_SDL3_PACKAGE ON)
			target_link_libraries(${target} PUBLIC SDL3)
			if(INSTALL_MINGW_DEPENDENCIES)
				install_shared_libs(TARGETS SDL3 DESTINATION bin NO_INSTALL_SYMLINKS REQUIRED)
			endif()
		else()
			message(STATUS "no TARGET SDL3::SDL3, or SDL3, using variables")
			target_include_directories(${target} PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
			target_link_libraries(${target} PUBLIC ${SDL3_LIBRARIES})
			if(INSTALL_MINGW_DEPENDENCIES)
				install_shared_libs(${SDL3_LIBRARIES} DESTINATION bin NO_INSTALL_SYMLINKS)
			endif()
		endif()
	endif()
elseif (DEFINED SDL2_INCLUDE_DIRS AND DEFINED SDL2_LIBRARIES)
	message(STATUS "using pre-defined SDL2 variables SDL2_INCLUDE_DIRS and SDL2_LIBRARIES")
	target_include_directories(${target} PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
	target_link_libraries(${target} PUBLIC ${SDL2_LIBRARIES})
	if(INSTALL_MINGW_DEPENDENCIES)
		install_shared_libs(${SDL2_LIBRARIES} DESTINATION bin NO_INSTALL_SYMLINKS)
	endif()
else()
	# Only try to autodetect if both SDL2 variables aren't explicitly set
	find_package(SDL2 CONFIG)
	if (TARGET SDL2::SDL2)
		message(STATUS "using TARGET SDL2::SDL2")
		set(FAUDIO_USING_SDL2_PACKAGE ON)
		target_link_libraries(${target} PUBLIC SDL2::SDL2)
		if(INSTALL_MINGW_DEPENDENCIES)
			install_shared_libs(TARGETS SDL2::SDL2 DESTINATION bin NO_INSTALL_SYMLINKS REQUIRED)
		endif()
	elseif (TARGET SDL2)
		message(STATUS "using TARGET SDL2")
		set(FAUDIO_USING_SDL2_PACKAGE ON)
		target_link_libraries(${target} PUBLIC SDL2)
		if(INSTALL_MINGW_DEPENDENCIES)
			install_shared_libs(TARGETS SDL2 DESTINATION bin NO_INSTALL_SYMLINKS REQUIRED)
		endif()
	else()
		message(STATUS "no TARGET SDL2::SDL2, or SDL2, using variables")
		target_include_directories(${target} PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
		target_link_libraries(${target} PUBLIC ${SDL2_LIBRARIES})
		if(INSTALL_MINGW_DEPENDENCIES)
			install_shared_libs(${SDL2_LIBRARIES} DESTINATION bin NO_INSTALL_SYMLINKS)
		endif()
	endif()
endif()

# utils/ Folder
if(BUILD_UTILS)
	enable_language(CXX)

	# Shared ImGui Framework
	add_library(uicommon STATIC
		utils/uicommon/FAudioUI_main.cpp
		utils/uicommon/imconfig.h
		utils/uicommon/imgui.cpp
		utils/uicommon/imgui_demo.cpp
		utils/uicommon/imgui_draw.cpp
		utils/uicommon/imgui_widgets.cpp
		utils/uicommon/imgui.h
		utils/uicommon/imgui_internal.h
		utils/uicommon/imstb_rectpack.h
		utils/uicommon/imstb_textedit.h
		utils/uicommon/imstb_truetype.h
	)
	target_link_libraries(uicommon PUBLIC ${target})

	# Shared WAV Resources
	add_library(wavs STATIC utils/wavcommon/wavs.cpp)
	target_compile_definitions(wavs PUBLIC
		RESOURCE_PATH="${CMAKE_SOURCE_DIR}/utils/wavcommon/resources"
	)

	# These tools do NOT use uicommon
	add_executable(testparse utils/testparse/testparse.c)
	target_link_libraries(testparse PRIVATE ${target})
	add_executable(testxwma utils/testxwma/testxwma.cpp)
	target_link_libraries(testxwma PRIVATE ${target})
	add_executable(showriffheader utils/showriffheader/showriffheader.cpp)
	target_link_libraries(showriffheader PRIVATE ${target})

	# These tools use uicommon, but NOT wavs
	add_executable(facttool utils/facttool/facttool.cpp)
	target_link_libraries(facttool PRIVATE uicommon)
	add_executable(testfilter
		utils/testfilter/audio.cpp
		utils/testfilter/audio_faudio.cpp
		utils/testfilter/audio.h
		utils/testfilter/audio_player.h
		utils/testfilter/audio_xaudio.cpp
		utils/testfilter/oscillator.cpp
		utils/testfilter/oscillator.h
		utils/testfilter/testfilter.cpp
	)
	target_link_libraries(testfilter PRIVATE uicommon)

	# These tools use both uicommon and wavs
	add_executable(testreverb
		utils/testreverb/audio.cpp
		utils/testreverb/audio_faudio.cpp
		utils/testreverb/audio.h
		utils/testreverb/audio_xaudio.cpp
		utils/testreverb/testreverb.cpp
	)
	target_link_libraries(testreverb PRIVATE uicommon wavs)
	add_executable(testvolumemeter
		utils/testvolumemeter/audio.cpp
		utils/testvolumemeter/audio_faudio.cpp
		utils/testvolumemeter/audio.h
		utils/testvolumemeter/testvolumemeter.cpp
	)
	target_link_libraries(testvolumemeter PRIVATE uicommon wavs)
	add_executable(voicepool
		utils/voicepool/voicepool.cpp
	)
	target_link_libraries(voicepool PRIVATE uicommon wavs)
endif()

# define install directories
# on mingw-w64 cross compilation $CMAKE_INSTALL_LIBDIR is set to an absolute
# path. Work around that by hard coding the directories on windows
if(WIN32)
	set(FAudio_INSTALL_INCLUDEDIR include)
	set(FAudio_INSTALL_BINDIR bin)
	set(FAudio_INSTALL_LIBDIR lib)
else()
	include(GNUInstallDirs)
	set(FAudio_INSTALL_INCLUDEDIR ${CMAKE_INSTALL_INCLUDEDIR})
	set(FAudio_INSTALL_BINDIR ${CMAKE_INSTALL_BINDIR})
	set(FAudio_INSTALL_LIBDIR ${CMAKE_INSTALL_LIBDIR})
endif()

# tests/ Folder
if(BUILD_TESTS)
	add_executable(faudio_tests tests/xaudio2.c)
	target_compile_definitions(faudio_tests PRIVATE _DEFAULT_SOURCE _BSD_SOURCE)
	target_link_libraries(faudio_tests PRIVATE ${target})
endif()

# Installation

if(FAUDIO_INSTALL)

	# Public Headers...
	install(
		DIRECTORY include/
		DESTINATION ${FAudio_INSTALL_INCLUDEDIR}
	)
	# Libraries...
	set(export ${PROJECT_NAME}-targets-shared)

	if(NOT BUILD_SHARED_LIBS)
		set(export ${PROJECT_NAME}-targets-static)
	endif()

	install(
		TARGETS ${target}
		EXPORT ${export}
		INCLUDES DESTINATION ${FAudio_INSTALL_INCLUDEDIR}
		RUNTIME DESTINATION ${FAudio_INSTALL_BINDIR}
		LIBRARY DESTINATION ${FAudio_INSTALL_LIBDIR}
		ARCHIVE DESTINATION ${FAudio_INSTALL_LIBDIR}
	)

	# Generate a pkgconfig file
	include(cmake/JoinPaths.cmake)
	join_paths(FAUDIO_PKGCONF_LIBDIR "\${prefix}" "${FAudio_INSTALL_LIBDIR}")
	join_paths(FAUDIO_PKGCONF_INCLUDEDIR "\${prefix}" "${FAudio_INSTALL_INCLUDEDIR}")

	if(NOT PLATFORM_WIN32)
		if(BUILD_SDL3)
			set(PC_REQUIRES_PRIVATE "Requires.private: sdl3")
		else()
			set(PC_REQUIRES_PRIVATE "Requires.private: sdl2")
		endif()
	endif()

	configure_file(
		"${PROJECT_SOURCE_DIR}/cmake/${PROJECT_NAME}.pc.in"
		${PROJECT_BINARY_DIR}/generated/${PROJECT_NAME}.pc
		@ONLY
	)
	install(
		FILES ${CMAKE_CURRENT_BINARY_DIR}/generated/${PROJECT_NAME}.pc
		DESTINATION ${FAudio_INSTALL_LIBDIR}/pkgconfig
	)

	# Generate cmake-config file, install CMake files
	include(CMakePackageConfigHelpers)
	configure_package_config_file(
		cmake/config.cmake.in
		${CMAKE_CURRENT_BINARY_DIR}/generated/${PROJECT_NAME}Config.cmake
		INSTALL_DESTINATION ${FAudio_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)
	install(
		FILES ${CMAKE_CURRENT_BINARY_DIR}/generated/${PROJECT_NAME}Config.cmake
		DESTINATION ${FAudio_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)

	install(
		EXPORT ${export}
		NAMESPACE ${PROJECT_NAME}::
		DESTINATION ${FAudio_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)
endif()
