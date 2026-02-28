# MojoShader

https://icculus.org/mojoshader/

MojoShader is a zlib-licensed library to work with Direct3D shaders on
alternate 3D APIs and non-Windows platforms. The primary motivation is
moving shaders to OpenGL languages on the fly. The developer deals with
"profiles" that represent various target languages, such as GLSL or
ARB_*_program.

This allows a developer to manage one set of shaders, presumably written
in Direct3D HLSL, and use them across multiple rendering backends. This
also means that the developer only has to worry about one (offline)
compiler to manage program complexity, while MojoShader itself deals with
the reduced complexity of the bytecode at runtime.

MojoShader provides both a simple API to convert bytecode to various
profiles, and (optionally) basic glue to rendering APIs to abstract the
management of the shaders at runtime.

The library is meant to be statically linked to an application (just add a
few .c files and headers to your build), allows the app to optionally
specify an allocator, and is thread safe (although OpenGL itself is not).
It is meant to be embedded in games with a minimum of fuss. 

To use this in your project:

- Add mojoshader*.c and mojoshader*.h to your project.
- Compile mojoshader*.c
- If you don't have a C99-compliant compiler, like Microsoft Visual Studio,
  you'll need to compile the .c files as C++ to get them to build.
- If you don't have cmake to generate mojoshader_version.h, you can either
  add a blank file with that name, or add MOJOSHADER_NO_VERSION_INCLUDE to
  your preprocessor definitions.
