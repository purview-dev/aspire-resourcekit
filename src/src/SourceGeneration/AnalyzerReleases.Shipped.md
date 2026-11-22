## Release 2.2.3

### New Rules

Rule ID | Category | Severity | Notes
-------- | ---------- | ---------- | ------
SG0044 | Purview.Aspire.ResourceKit.SourceGenerator | Info | BuildResource only adds the declared path
SG0045 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group must be declared at namespace scope
SG0046 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group accessibility differs from its host kit

## Release 2.1.0

### New Rules

Rule ID | Category | Severity | Notes
-------- | ---------- | ---------- | ------
SG0041 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | ProjectPath requires a DotnetProjectResource
SG0042 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | ProjectPath and ResourcePath conflict
SG0043 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | ResourcePath requires a path-based resource

## Release 2.0.0

### New Rules

Rule ID | Category | Severity | Notes
-------- | ---------- | ---------- | ------
SG0021 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Type is not a resource group
SG0022 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group parent cycle
SG0023 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Duplicate resource group name
SG0024 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | More than one primary group
SG0025 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group cannot have explicit base
SG0026 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Duplicate URL name
SG0027 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Endpoint URL on a resource without endpoints
SG0028 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | Relative URL on a resource without endpoints
SG0029 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group declares its parent more than once
SG0030 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group cannot be generic
SG0031 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource group cannot be a resource kit or a group member
SG0032 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource kit cannot be its own parent
SG0033 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Resource kit parent cycle
SG0034 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Parent type is not a resource kit
SG0035 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Parent resource kit is not part of this composition
SG0036 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Generated child property collides with a member on the parent
SG0037 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Duplicate child property name
SG0038 | Purview.Aspire.ResourceKit.SourceGenerator | Error | Generated Parent member collides with a user-authored member
SG0039 | Purview.Aspire.ResourceKit.SourceGenerator | Warning | Child resource kit uses a different primary group than its parent
SG0040 | Purview.Aspire.ResourceKit.SourceGenerator | Info | Resource kit is already a resource builder

## Release 1.0.0

### New Rules

 Rule ID | Category | Severity | Notes |
 --------|----------|----------|--------------------
 SG0001  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Class must be partial
 SG0002  | Purview.Aspire.ResourceKit.SourceGenerator | Info     | No host resources defined
 SG0003  | Purview.Aspire.ResourceKit.SourceGenerator | Warning  | No host app info defined
 SG0004  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Multiple host apps defined
 SG0005  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Duplicate resource property name
 SG0006  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | App resource must derive from generated base
 SG0007  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Resource name could not be determined
 SG0008  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Invalid property name
 SG0009  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | IServiceCollection type missing
 SG0010  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Configuration binder type missing
 SG0011  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Options configuration extension method missing
 SG0012  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Non-empty constructors are not supported
 SG0013  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Mixed ResourceDefinition attribute usage is not supported
 SG0014  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Non-generic ResourceDefinition requires explicit base
 SG0015  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | Generic ResourceDefinition cannot have explicit base
 SG0016  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | No Aspire resource found
 SG0017  | Purview.Aspire.ResourceKit.SourceGenerator | Warning  | Resource property is never set
 SG0018  | Purview.Aspire.ResourceKit.SourceGenerator | Warning  | Project resource definition mismatch
 SG0019  | Purview.Aspire.ResourceKit.SourceGenerator | Warning  | Project resource kit base must use ProjectResource
 SG0020  | Purview.Aspire.ResourceKit.SourceGenerator | Error    | OptionsHelper.Assign action must set exactly one property path
