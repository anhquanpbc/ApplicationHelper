# ClickOnceHelper

ClickOnceHelper is a C# console application that helps you install and uninstall ClickOnce applications.

## System Requirements

- .NET Framework 4.7.2 or later
- `System.Deployment` library

## Installation

### Adding System.Deployment Library

1. Open Visual Studio.
2. In Solution Explorer, right-click on your project.
3. Select "Add > Reference...".
4. Find and select `System.Deployment`.
5. Click "OK" to add the reference.

## Usage

The program can be used from the command line to install or uninstall ClickOnce applications.

### Installing an Application

```sh
ClickOnceHelper.exe install "<path_to_.application_file>"
```

Uninstalling an Application
```sh
ClickOnceHelper.exe uninstall "<application_name>"
```

Key Functions
InstallApplication
This method initiates the process of installing a ClickOnce application from a provided URL.

Uninstall
This method uninstalls an application based on the provided application name.

Logging
The methods log detailed information about the installation and uninstallation process, helping to track and diagnose issues.

Detailed Examples
Installation
Open Command Prompt as an administrator.

Navigate to the directory containing your executable (.exe):

```sh
cd path\to\your\project\bin\Debug\netcoreapp3.1
```
Run the install command:

```sh
ClickOnceHelper.exe install "<path_to_.application_file>"
```
Uninstallation
Open Command Prompt as an administrator.

Navigate to the directory containing your executable (.exe):

```sh
cd path\to\your\project\bin\Debug\
```
Run the uninstall command:

```sh
ClickOnceHelper.exe uninstall "<application_name>"
```

## Notes
Ensure the provided URL is valid and points to a ClickOnce manifest (.application) file. </br>
For uninstallation, the application name must match exactly as it is stored in the registry. </br>
The program may need to run with administrator privileges to access and modify the registry.

## Contribution
If you would like to contribute to this project, please create a pull request or open an issue on GitHub.

## License
This project is licensed under the MIT License.
