# Howden.MAADevEng.Aspire.ResourceIsolation

Testable Aspire AppHost resource composition with runtime isolation modes:

- `Local`
- `Running`
- `Publishing`

You can selectively enable/disable resources and isolate names using prefix/suffix/overrides from config.

## Generated Host App model

The package now supports an AppModel style similar to the original `AppModels` pattern:

- Mark your host app with `[HostResources]`.
- Implement resources by inheriting `HostAppResource<THostApp, TResource>`.
- Register everything with `builder.AddHostAppResources()`.

The source generator will detect the host app and generate:

- `Initialize`, `Build`, and `Configure` orchestration on the host app
- automatic resource discovery and invocation
- IoC registration for host app + resources
- `AddHostAppResources(this IDistributedApplicationBuilder, Action<IServiceCollection>? configureServices = null)`

Use the optional `configureServices` callback to register custom dependencies needed by the host app/resources.
