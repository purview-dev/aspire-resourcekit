set quiet

solution := "src/ResourceKit.slnx"
build_configuration := "Release"
artifacts_folder := "./artifacts"
default_test_filter := "/*/*/*/*/"

current_version := `node -p "require('./package.json').version"`

[private]
default:
    just --list

# Build and test with the specified configuration, defaulting to "Release"
build *args:
    echo "Building {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet build {{ solution }} -c {{ build_configuration }} {{ args }}

# Build and test with the specified configuration, defaulting to "Release"
clean *args:
    echo "Cleaning {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }}"
    dotnet clean {{ solution }} -c {{ build_configuration }} {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
test filter=default_test_filter *args:
    echo "Running tests for {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} and filter {{ GREEN }}{{ filter }}{{ NORMAL }}"
    dotnet test {{ solution }} -c {{ build_configuration }} --ignore-exit-code 8 --treenode-filter "{{ filter }}" -- {{ args }}

# Run tests with the specified configuration, defaulting to "Release"
restore *args:
    echo "Restoring dependencies for {{ BLUE }}{{ solution }}{{ NORMAL }}"
    dotnet restore {{ solution }} {{ args }}

# Create NuGet package for the project
pack publish_folder=artifacts_folder *args:
    echo "Packing {{ BLUE }}{{ solution }}{{ NORMAL }} with configuration {{ YELLOW }}{{ build_configuration }}{{ NORMAL }} to {{ GREEN }}{{ publish_folder }}{{ NORMAL }}"
    echo "\tCurrent version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"
    dotnet pack {{ solution }} -c {{ build_configuration }} -o {{ publish_folder }} {{ args }}

# Displays the current version from package.json
current_version:
    echo "Current version is {{ BLUE }}{{ current_version }}{{ NORMAL }}"

# Check code formatting using CSharpier
lint-check *args:
    dotnet csharpier check . {{ args }}

# Fix code formatting issues using CSharpier
lint-fix *args:
    dotnet csharpier format . {{ args }}

# Open the solution in Visual Studio/ Registered application
vs:
    open {{ solution }}
