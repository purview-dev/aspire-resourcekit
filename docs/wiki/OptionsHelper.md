# OptionsHelper

`OptionsHelper` builds configuration arguments or environment variables for options objects by using
strongly typed assignment expressions. It is useful for integration-test fixtures, scenario toggles,
and CLI overrides.

## Assign

`Assign<TOptions>(...)` starts building entries from assignment expressions.

```csharp
var args = OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
    c => c.API.IsEnabled = false,
    c => c.API.Name = "api-test"
).Build();
```

Resulting args are in this form:

- `--ShopHostKit:API:IsEnabled=false`
- `--ShopHostKit:API:Name=api-test`

Each assignment action must set exactly one property path. To set multiple properties, pass one
assignment per property (as above). The compiler reports `SG0020` if an action assigns more than one
property path, and offers a code fix that splits it into separate assignments.

You can also pass an explicit root section name:

```csharp
OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>("MySection", c => c.API.IsEnabled = false);
```

## AsEnvironmentVariables

Call `AsEnvironmentVariables()` before `Build()` to produce environment variables instead:

```csharp
var envVars = OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
    c => c.API.IsEnabled = false,
    c => c.API.Name = "api-test"
).AsEnvironmentVariables().Build();
```

This returns a dictionary such as
`{"ShopHostKit__API__IsEnabled": "false", "ShopHostKit__API__Name": "api-test"}`.

## PathFor

If you need a property path as a plain string (for example, to build keys or log config), use
`PathFor` with a member selector:

```csharp
var path = OptionsHelper.PathFor<ShopHostKit.ShopHostKitOptions>(f => f.API.Name);
// "API.Name"
```

## SectionNameFor

Look up the resolved section name for any options type directly:

```csharp
var sectionName = OptionsHelper.SectionNameFor<ShopHostKit.ShopHostKitOptions>();
// "ShopHostKit"

var sectionNameFromType = OptionsHelper.SectionNameFor(typeof(ShopHostKit.ShopHostKitOptions));
// "ShopHostKit"
```

`SectionNameFor` accepts both a generic type argument and a `Type`, and applies the same resolution
rules as `Assign`:

1. `const string SectionName` on the options type
2. Type name trimmed by one suffix: `Options`, `Settings`, `Configuration`, `Config`
3. Original type name

Suffix trimming works for generic types by ignoring type arguments.

## TUnit.Aspire integration

`OptionsHelper` pairs well with [TUnit.Aspire](https://www.nuget.org/packages/TUnit.Aspire/) when
passing args to an AppHost under test:

```csharp
protected override string[] Args =>
[
    .. base.Args,
    .. OptionsHelper.Assign<ShopHostKit.ShopHostKitOptions>(
      c => c.API.IsEnabled = false,
      c => c.API.Name = "api-test"
    ).Build(),
];
```

See [Testing with ResourceKit](Testing-with-ResourceKit.md).

## The SG0020 rule

An `OptionsHelper.Assign<TOptions>(...)` action must set exactly one property path. A block-bodied
lambda such as the following is rejected at compile time:

```csharp
OptionsHelper.Assign<ShopHostKitOptions>(o =>
{
    o.API.IsEnabled = false;
    o.API.Name = "api-test";
});
```

Split each property into its own assignment argument instead:

```csharp
OptionsHelper.Assign<ShopHostKitOptions>(
    o => o.API.IsEnabled = false,
    o => o.API.Name = "api-test"
);
```

Visual Studio offers a **"Split into separate assignments"** code fix that performs this conversion for
you. See [Source Generator Behaviors](Source-Generator-Behaviors.md) for how the fix works.

## See also

- [Configuration and Options](Configuration-and-Options.md)
- [Testing with ResourceKit](Testing-with-ResourceKit.md)