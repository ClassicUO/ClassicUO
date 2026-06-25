# ClassicUO task runner. Cross-platform (Linux / macOS / Windows git-bash·MSYS2).
# Requires: dotnet SDK; cargo + the wasm32-wasip2 target (Rust mods); node/npm (ui mod).
#
# Recipes are POSIX sh. Targets:
#   make build-mods    build the WASM mods into ecs-mods/<mod>/{mod.wasm,mod.json}
#   make test          build mods, then run the test suite
#   make run-debug     build mods, then run cuo-ecs (Debug)
#   make run-release   build mods, then run cuo-ecs (Release)
#   make publish       build mods, then AOT-publish (Bootstrap + Client lib + cuo-ecs)

UNAME := $(shell uname -s)
ifneq (,$(filter MINGW% MSYS% CYGWIN%,$(UNAME)))
  RID := win-x64
else ifeq ($(UNAME),Darwin)
  RID := osx-x64
else
  RID := linux-x64
endif

ECS       := src/ClassicUO.Ecs/ClassicUO.Ecs.csproj
TESTS     := tests/ClassicUO.Ecs.Tests/ClassicUO.Ecs.Tests.csproj
BOOTSTRAP := src/ClassicUO.Bootstrap/src/ClassicUO.Bootstrap.csproj
CLIENT    := src/ClassicUO.Client
RUST_MODS := ecs-netlog ecs-status ecs-topbar ecs-blocktest

.DEFAULT_GOAL := help
.PHONY: help build-mods test run-debug run-release publish

help:
	@echo "Targets: build-mods | test | run-debug | run-release | publish  (RID=$(RID))"

# Build each mod and assemble it into ecs-mods/<mod>/{mod.wasm,mod.json}. The mod
# folder name and manifest name drop the "ecs-" prefix (ecs-topbar -> topbar);
# cargo's cdylib output uses underscores (ecs_topbar.wasm).
build-mods:
	@for m in $(RUST_MODS); do \
	  short=$${m#ecs-}; lib=$$(echo $$m | tr '-' '_'); \
	  echo ">> mod $$short (rust)"; \
	  ( cd src/Mods/$$m && cargo build --release --target wasm32-wasip2 ); \
	  mkdir -p ecs-mods/$$short; \
	  cp src/Mods/$$m/target/wasm32-wasip2/release/$$lib.wasm ecs-mods/$$short/mod.wasm; \
	  printf '{\n  "name": "%s",\n  "version": "0.1.0",\n  "wasm": "mod.wasm",\n  "ruleset": {}\n}\n' "$$short" > ecs-mods/$$short/mod.json; \
	done
	@echo ">> mod ui (typescript / jco)"
	@cd src/Mods/ecs-ui && { [ -d node_modules ] || npm install; } && npm run build
	@mkdir -p ecs-mods/ui
	@cp src/Mods/ecs-ui/dist/ecs_ui.wasm ecs-mods/ui/mod.wasm
	@printf '{\n  "name": "ui",\n  "version": "0.1.0",\n  "wasm": "mod.wasm",\n  "ruleset": {}\n}\n' > ecs-mods/ui/mod.json
	@# C# mod (wit-bindgen-dotnet from NuGet + NativeAOT-LLVM). NativeAOT emits the .wasm
	@# only on `publish` (not `build`); ILCompiler.LLVM componentizes it. Needs the
	@# NativeAOT-LLVM toolchain (wasi-sdk) like the rust mods need cargo.
	@echo ">> mod csharp (c# / wit-bindgen-dotnet)"
	@dotnet publish src/Mods/ecs-csharp/ecs-csharp.csproj -c Release
	@mkdir -p ecs-mods/csharp
	@cp src/Mods/ecs-csharp/bin/Release/net10.0/wasi-wasm/native/ecs_csharp.wasm ecs-mods/csharp/mod.wasm
	@printf '{\n  "name": "csharp",\n  "version": "0.1.0",\n  "wasm": "mod.wasm",\n  "ruleset": {}\n}\n' > ecs-mods/csharp/mod.json

test: build-mods
	dotnet test $(TESTS)

run-debug: build-mods
	dotnet run --project $(ECS) -c Debug

run-release: build-mods
	dotnet run --project $(ECS) -c Release

# AOT publish: Bootstrap + Client (shared native lib) to bin/dist,
# the AOT cuo-ecs to bin/dist-ecs.
publish: build-mods
	dotnet publish $(BOOTSTRAP) -c Release -o bin/dist
	dotnet publish $(CLIENT) -c Release -p:NativeLib=Shared -p:OutputType=Library -r $(RID) -o bin/dist
	dotnet publish $(ECS) -c Release -r $(RID) -o bin/dist-ecs
