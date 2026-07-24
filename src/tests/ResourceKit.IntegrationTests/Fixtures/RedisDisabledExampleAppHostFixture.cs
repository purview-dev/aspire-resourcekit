using Projects;
using Purview.Aspire.ResourceKit.Example;
using TUnit.Aspire;

namespace Purview.Aspire.ResourceKit.Fixtures;

public sealed class RedisDisabledExampleAppHostFixture : AspireFixture<Example_AppHost>
{
  const string HostAppSectionName = "ExampleHostApp";
  const string DisabledResourcesSectionName = "DisabledResources";

  protected override string[] Args =>
  [
    .. base.Args,
    $"--{HostAppSectionName}:{DisabledResourcesSectionName}:0={Platform.ResourceKits.Redis}",
  ];
}