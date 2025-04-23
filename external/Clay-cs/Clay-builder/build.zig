const std = @import("std");
const builtin = @import("builtin");

// Although this function looks imperative, note that its job is to
// declaratively construct a build graph that will be executed by an external
// runner.
pub fn build(b: *std.Build) void {

    // Standard target options allows the person running `zig build` to choose
    // what target to build for. Here we do not override the defaults, which
    // means any target is allowed, and the default is native. Other options
    // for restricting supported target set are available.
    const target = b.standardTargetOptions(.{});

    // Standard optimization options allow the person running `zig build` to select
    // between Debug, ReleaseSafe, ReleaseFast, and ReleaseSmall. Here we do not
    // set a preferred release mode, allowing the user to decide how to optimize.
    const optimize = b.standardOptimizeOption(.{});

    const lib = b.addSharedLibrary(.{
        .name = "Clay",
        .target = target,
        .optimize = optimize,
    });

    const flags = [_][]const u8{
        "-std=c99",
    };

    lib.linkLibC();
    lib.addIncludePath(b.path("src/clay.h"));
    lib.addCSourceFile(.{ .file = b.path("src/clay.c"), .flags = &flags });

    // Add CLAY_DLL macro only on Windows
    if (target.result.os.tag == .windows) {
        lib.root_module.addCMacro("CLAY_DLL", "1");
    }

    b.installArtifact(lib);
}
