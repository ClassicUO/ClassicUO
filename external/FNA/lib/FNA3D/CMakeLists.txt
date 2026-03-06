# CMake Project for FNA3D
# Written by @NeroBurner
cmake_minimum_required(VERSION 3.10)
project(FNA3D C)

# Options
option(BUILD_SHARED_LIBS "Build shared library" ON)
option(TRACING_SUPPORT "Build with tracing enabled" OFF)
option(BUILD_SDL3 "Build against SDL 3.0" ON)
option(MOJOSHADER_STATIC_SPIRVCROSS "Build against statically linked spirvcross" OFF)

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

# Platform Flags
if(APPLE)
	set(CMAKE_MACOSX_RPATH ON)
	set(CMAKE_OSX_DEPLOYMENT_TARGET 10.9)
	set(LOBJC "objc")
elseif(WIN32)
	# "FNA3D.dll", not "libFNA3D.dll"
	set(CMAKE_SHARED_LIBRARY_PREFIX "")
else()
	find_package(PkgConfig)
	if(PKG_CONFIG_FOUND)
		pkg_search_module(DXVK_NATIVE dxvk-dxgi)
		if(DXVK_NATIVE_FOUND)
			set(BUILD_DXVK_NATIVE ON)
		endif()
	endif()
endif()

# Defines
add_definitions(
	-DFNA3D_DRIVER_OPENGL
)
if(BUILD_SDL3)
	add_definitions(-DFNA3D_DRIVER_SDL)
endif()
add_definitions(
	-DMOJOSHADER_NO_VERSION_INCLUDE
	-DMOJOSHADER_USE_SDL_STDLIB
	-DMOJOSHADER_EFFECT_SUPPORT
	-DMOJOSHADER_DEPTH_CLIPPING
	-DMOJOSHADER_FLIP_RENDERTARGET
	-DMOJOSHADER_XNA4_VERTEX_TEXTURES
	-DSUPPORT_PROFILE_ARB1=0
	-DSUPPORT_PROFILE_ARB1_NV=0
	-DSUPPORT_PROFILE_BYTECODE=0
	-DSUPPORT_PROFILE_D3D=0
)
if(TRACING_SUPPORT)
	add_definitions(-DFNA3D_TRACING)
endif()
if(BUILD_SDL3)
	add_definitions(-DUSE_SDL3)
endif()
if (MOJOSHADER_STATIC_SPIRVCROSS)
	add_definitions(-DSDL_GPU_SHADERCROSS_STATIC)
endif()

if(WIN32 OR BUILD_DXVK_NATIVE)
	add_definitions(
		-DFNA3D_DRIVER_D3D11
	)
else()
	add_definitions(
		-DSUPPORT_PROFILE_HLSL=0
	)
endif()

if(EMSCRIPTEN)
	add_definitions(
		-DSUPPORT_PROFILE_GLSPIRV=0
		-DSUPPORT_PROFILE_SPIRV=0
	)
endif()

if(NOT APPLE)
	add_definitions(
		-DSUPPORT_PROFILE_METAL=0
	)
endif()

# Source lists
add_library(FNA3D
	# Public Headers
	include/FNA3D.h
	include/FNA3D_Image.h
	# Internal Headers
	src/FNA3D_Driver.h
	src/FNA3D_Driver_OpenGL.h
	src/FNA3D_Driver_OpenGL_glfuncs.h
	src/FNA3D_PipelineCache.h
	# Source Files
	src/FNA3D.c
	src/FNA3D_Driver_D3D11.c
	src/FNA3D_Driver_OpenGL.c
	src/FNA3D_Driver_SDL.c
	src/FNA3D_Image.c
	src/FNA3D_PipelineCache.c
	src/FNA3D_Tracing.c
)
add_library(mojoshader STATIC
	MojoShader/mojoshader.c
	MojoShader/mojoshader_effects.c
	MojoShader/mojoshader_common.c
	MojoShader/mojoshader_d3d11.c
	MojoShader/mojoshader_opengl.c
	MojoShader/mojoshader_sdlgpu.c
	MojoShader/profiles/mojoshader_profile_common.c
	MojoShader/profiles/mojoshader_profile_glsl.c
	MojoShader/profiles/mojoshader_profile_hlsl.c
	MojoShader/profiles/mojoshader_profile_spirv.c
	MojoShader/profiles/mojoshader_profile_metal.c
)
if(TRACING_SUPPORT)
	add_executable(fna3d_replay replay/replay.c)
	target_link_libraries(fna3d_replay FNA3D)
	target_include_directories(fna3d_replay PUBLIC
		$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/MojoShader>
	)
	if(BUILD_SDL3)
		add_executable(fna3d_dumpspirv dumpspirv/dumpspirv.c)
		target_link_libraries(fna3d_dumpspirv FNA3D)
		target_include_directories(fna3d_dumpspirv PUBLIC
			$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/MojoShader>
		)
	endif()
endif()

# Build flags
if(NOT MSVC)
	set_property(TARGET FNA3D PROPERTY COMPILE_FLAGS "-std=gnu99 -Wall -Wno-strict-aliasing -pedantic")
endif()
if(BUILD_SHARED_LIBS)
	set_property(TARGET mojoshader PROPERTY POSITION_INDEPENDENT_CODE ON)
endif()

# FNA3D folders as includes, for other targets to consume
target_include_directories(FNA3D PUBLIC
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/src>
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/include>
)
if(BUILD_DXVK_NATIVE)
	add_definitions(-DFNA3D_DXVK_NATIVE)
	target_include_directories(FNA3D PUBLIC ${DXVK_NATIVE_INCLUDE_DIRS})
	target_include_directories(mojoshader PUBLIC ${DXVK_NATIVE_INCLUDE_DIRS})
endif()
target_include_directories(mojoshader PUBLIC
	$<BUILD_INTERFACE:${CMAKE_CURRENT_SOURCE_DIR}/MojoShader>
)

# MinGW builds should statically link libgcc
if(MINGW)
	target_link_libraries(FNA3D PRIVATE -static-libgcc)
endif()

# Soname
set_target_properties(FNA3D PROPERTIES OUTPUT_NAME "FNA3D"
	VERSION ${LIB_VERSION}
	SOVERSION ${LIB_MAJOR_VERSION}
)

# Internal Dependencies
target_link_libraries(FNA3D PRIVATE mojoshader ${LOBJC})

# SDL Dependency
if (BUILD_SDL3)
	if (DEFINED SDL3_INCLUDE_DIRS AND DEFINED SDL3_LIBRARIES)
		message(STATUS "using pre-defined SDL3 variables SDL3_INCLUDE_DIRS and SDL3_LIBRARIES")
		target_include_directories(FNA3D PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
		target_include_directories(mojoshader PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
		target_link_libraries(FNA3D PUBLIC ${SDL3_LIBRARIES})
	else()
		# Only try to autodetect if both SDL3 variables aren't explicitly set
		find_package(SDL3 CONFIG)
		if (TARGET SDL3::SDL3)
			message(STATUS "using TARGET SDL3::SDL3")
			target_link_libraries(FNA3D PUBLIC SDL3::SDL3)
			target_link_libraries(mojoshader PUBLIC SDL3::SDL3)
		elseif (TARGET SDL3)
			message(STATUS "using TARGET SDL3")
			target_link_libraries(FNA3D PUBLIC SDL3)
			target_link_libraries(mojoshader PUBLIC SDL3)
		else()
			message(STATUS "no TARGET SDL3::SDL3, or SDL3, using variables")
			target_include_directories(FNA3D PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
			target_include_directories(mojoshader PUBLIC "$<BUILD_INTERFACE:${SDL3_INCLUDE_DIRS}>")
			target_link_libraries(FNA3D PUBLIC ${SDL3_LIBRARIES})
		endif()
	endif()
else()
	if (DEFINED SDL2_INCLUDE_DIRS AND DEFINED SDL2_LIBRARIES)
		message(STATUS "using pre-defined SDL2 variables SDL2_INCLUDE_DIRS and SDL2_LIBRARIES")
		target_include_directories(FNA3D PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
		target_include_directories(mojoshader PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
		target_link_libraries(FNA3D PUBLIC ${SDL2_LIBRARIES})
	else()
		# Only try to autodetect if both SDL2 variables aren't explicitly set
		find_package(SDL2 CONFIG)
		if (TARGET SDL2::SDL2)
			message(STATUS "using TARGET SDL2::SDL2")
			target_link_libraries(FNA3D PUBLIC SDL2::SDL2)
			target_link_libraries(mojoshader PUBLIC SDL2::SDL2)
		elseif (TARGET SDL2)
			message(STATUS "using TARGET SDL2")
			target_link_libraries(FNA3D PUBLIC SDL2)
			target_link_libraries(mojoshader PUBLIC SDL2)
		else()
			message(STATUS "no TARGET SDL2::SDL2, or SDL2, using variables")
			target_include_directories(FNA3D PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
			target_include_directories(mojoshader PUBLIC "$<BUILD_INTERFACE:${SDL2_INCLUDE_DIRS}>")
			target_link_libraries(FNA3D PUBLIC ${SDL2_LIBRARIES})
		endif()
	endif()
endif()

# define install directories
# on mingw-w64 cross compilation $CMAKE_INSTALL_LIBDIR is set to an absolute
# path. Work around that by hard coding the directories on windows
if(WIN32)
	set(FNA3D_INSTALL_INCLUDEDIR include)
	set(FNA3D_INSTALL_BINDIR bin)
	set(FNA3D_INSTALL_LIBDIR lib)
else()
	include(GNUInstallDirs)
	set(FNA3D_INSTALL_INCLUDEDIR ${CMAKE_INSTALL_INCLUDEDIR})
	set(FNA3D_INSTALL_BINDIR ${CMAKE_INSTALL_BINDIR})
	set(FNA3D_INSTALL_LIBDIR ${CMAKE_INSTALL_LIBDIR})
endif()

# Installation (Shared only!)

if(BUILD_SHARED_LIBS)
	# Public Headers...
	install(
		DIRECTORY include/
		DESTINATION ${FNA3D_INSTALL_INCLUDEDIR}
	)
	# Libraries...
	install(
		TARGETS ${PROJECT_NAME}
		EXPORT ${PROJECT_NAME}-targets
		INCLUDES DESTINATION ${FNA3D_INSTALL_INCLUDEDIR}
		RUNTIME DESTINATION ${FNA3D_INSTALL_BINDIR}
		LIBRARY DESTINATION ${FNA3D_INSTALL_LIBDIR}
		ARCHIVE DESTINATION ${FNA3D_INSTALL_LIBDIR}
	)

	# Generate a pkgconfig file
	include(cmake/JoinPaths.cmake)
	join_paths(FNA3D_PKGCONF_LIBDIR "\${prefix}" "${CMAKE_INSTALL_LIBDIR}")
	join_paths(FNA3D_PKGCONF_INCLUDEDIR "\${prefix}" "${CMAKE_INSTALL_INCLUDEDIR}")

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
		DESTINATION ${FNA3D_INSTALL_LIBDIR}/pkgconfig
	)

	# Generate cmake-config file, install CMake files
	include(CMakePackageConfigHelpers)
	configure_package_config_file(
		cmake/config.cmake.in
		${CMAKE_CURRENT_BINARY_DIR}/generated/${PROJECT_NAME}Config.cmake
		INSTALL_DESTINATION ${FNA3D_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)
	install(
		FILES ${CMAKE_CURRENT_BINARY_DIR}/generated/${PROJECT_NAME}Config.cmake
		DESTINATION ${FNA3D_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)

	install(
		EXPORT ${PROJECT_NAME}-targets
		NAMESPACE ${PROJECT_NAME}::
		DESTINATION ${FNA3D_INSTALL_LIBDIR}/cmake/${PROJECT_NAME}
	)
endif()
