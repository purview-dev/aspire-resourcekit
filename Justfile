set quiet

solution := "src/ResourceKit.slnx"
build_configuration := "Release"
#artifacts_folder := "./artifacts"
#default_test_filter := "/*/*/*/*/"
pipeline_project := "build/Pipeline/Pipeline.csproj"

current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Run the PR pipeline (restore, build, lint, unit tests)
pipeline-pr *args:
    echo "Running PR pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} {{ args }}

# Run the release pipeline (restore, build, lint, tests, pack, publish, GitHub release)
pipeline-release *args:
    echo "Running release pipeline..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Release__ShouldPublish=true {{ args }}

# Run the pipeline with integration tests enabled
pipeline-integration *args:
    echo "Running pipeline with integration tests..."
    dotnet run --project {{ pipeline_project }} --configuration {{ build_configuration }} -- --Build__RunIntegrationTests=true {{ args }}

# Fix code formatting issues using CSharpier
lint-fix *args:
    dotnet csharpier format . {{ args }}

# Displays the current version from package.json
current_version:
    echo "Current version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"

# Open the solution in Visual Studio/ Registered application
vs:
    open {{ solution }}

# Open the solution in Visual Studio/ Registered application
vs-pipeline:
    open {{ pipeline_project }}

# [legacy] Build and test with the specified configuration, defaulting to "Release"
# build *args:
#     echo "Building {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
#     dotnet build {{ solution }} -c {{ build_configuration }} {{ args }}

# [legacy] Build and test with the specified configuration, defaulting to "Release"
# clean *args:
#     echo "Cleaning {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
#     dotnet clean {{ solution }} -c {{ build_configuration }} {{ args }}

# [legacy] Run tests with the specified configuration, defaulting to "Release"
# test filter=default_test_filter *args:
#     echo "Running tests for {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
#     dotnet test {{ solution }} -c {{ build_configuration }} --ignore-exit-code 8 --treenode-filter "{{ filter }}" -- {{ args }}

# [legacy] Restore dependencies for the solution
# restore *args:
#     echo "Restoring dependencies for {{ BLUE }}{{ solution }}{{ NORMAL }}"
#     dotnet restore {{ solution }} {{ args }}

# [legacy] Create NuGet package for the project
# pack publish_folder=artifacts_folder *args:
#     echo "Packing {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
#     echo "  Current version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"
#     dotnet pack {{ solution }} -c {{ build_configuration }} -o {{ publish_folder }} {{ args }}

# [legacy] Check code formatting using CSharpier
# lint-check *args:
#     dotnet csharpier check . {{ args }}

# [legacy] Fix code formatting issues using CSharpier
# lint-fix *args:
#     dotnet csharpier format . {{ args }}
