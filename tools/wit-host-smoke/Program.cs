// Smoke: the build itself is the test. If the source generator parses cuo-ecs.wit
// and emits compilable host bindings (resources, variants, borrows, lists),
// this compiles. Generated files land in obj/generated for inspection.
System.Console.WriteLine("wit-host-smoke: source-gen compiled cuo-ecs.wit OK");
