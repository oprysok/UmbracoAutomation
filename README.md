# UmbracoAutomation
Set of tools for automating various actions in Umbraco CMS.

## Table of Contents
- [UmbracoAutomation.CLI](#UmbracoAutomation.CLI)
  - [Compatibility](#Compatibility)
  - [Installation](#Installation)
  - [Commands](#Commands)
    - [SetPropertyById](#SetPropertyById)
    - [SetPropertyByType](#SetPropertyByType)
    - [AddUser](#AddUser)
    - [SetHostnameByRule](#SetHostnameByRule)
    - [UpdateHostnames](#UpdateHostnames)
    - [RevertHostnames](#RevertHostnames)
    - [FetchMedia](#FetchMedia)

## UmbracoAutomation.CLI

### Compatibility
- Umbraco CMS 7.13.2 - 7.14.0
- Windows OS only
- .NET Framework 4.5.1

### Installation
The compiled solution can be packed into a NuGet package using Base.nuspec.

App.config must contain correct configuration including connection string and membership provider(s) settings.

### Commands
```
SetPropertyById         Set a value of doctype property by a node id.
SetPropertyByType       Set a value for a node(s) property by its doctype alias.
AddUser                 Adds a new Umbraco user. If target user already exist then password is updated.
SetHostnameByRule       Updates/sets hostname for a node by its name using special rules format.
UpdateHostnames         Update hostname for each SiteRoot node.
RevertHostnames         Reverts hostname updates made by UpdateHostnames command.
FetchMedia              Downloads missing media files from source URL.

help <name>             For help with one of the above commands
```
#### SetPropertyById

Set a value of doctype property by a node id. This can be used to update site settings by a node id.

```
Usage: 
UmbracoAutomation.CLI.exe SetPropertyById <options>

Options
  -i, --id=VALUE             The id of the node to be updated.
  -n, --name=VALUE           The name of the property.
  -v, --value=VALUE          The value of the property.

Example:
UmbracoAutomation.CLI.exe SetPropertyById -i=1234 -n=propertyAlias -v=newValue
```

Notes: tested only with Umbraco.Texbox properties.

#### SetPropertyByType

Set a value for a node(s) property by its doctype alias. This can be used to update site settings by a doctype alias.

```
Usage: 
UmbracoAutomation.CLI.exe SetPropertyByType <options>

Options
  -t, --type=VALUE           The type (doctype alias) of the node(s) to be updated.
  -n, --name=VALUE           The name of the property.
  -v, --value=VALUE          The value of the property.

Example:
UmbracoAutomation.CLI.exe SetPropertyByType -t=siteRoot -n=myProperty -v=newValue
Updates 'myProperty' for each published node of a 'siteRoot' doctype.
```


#### AddUser
Adds a new Umbraco backoffice user (not member). If target user already exists then the password is updated.

```
Usage: 
UmbracoAutomation.CLI.exe AddUser <options>

Options
  -n, --name=VALUE           User name.
  -e, --email=VALUE          Email.
  -p, --password=VALUE       Password.
  -g, --group=VALUE          Optional. Umbraco user group alias.

Examples:
UmbracoAutomation.CLI.exe AddUser -n=test -e=test@example.com -p=Password1
UmbracoAutomation.CLI.exe AddUser -n=test -e=test@example.com -p=Password1 -g=admin
```
Notes: 
- Email is not updated when target user already exists.

#### SetHostnameByRule
 Updates/sets hostname for a node by its name using special rules format.
 
 The general syntax of a single hostname rule is as follows: ``NodeName => Hostname[,Hostname]``

To enter multiple rules, each rule should be separated by a semicolon character (;):

``
NodeName => Hostname[,Hostname];
NodeName => Hostname[,Hostname];
``

```
Usage: 
UmbracoAutomation.CLI.exe SetHostnameByRule <options>

Options
  -r, --rule=VALUE           Hostname(s) to node assignement rule(s).

Example:
UmbracoAutomation.CLI.exe SetHostnameByRule -r="MyAwesomeSite => myawesomesite.local"
```

#### UpdateHostnames
Update hostname for each published site root node. Document type is taken from 'SiteRootDocType' setting (App.config).

```
Usage: 
UmbracoAutomation.CLI.exe UpdateHostnames <options>

Options
  -s, --suffix=VALUE         The expected suffix (ending) of the hostnames
                              after update. E.g.: app.dev.int
  -e, --exclude=VALUE        Regex pattern to skip updates for matched domains.
                              Default pattern '\.local$|\.int$|\.dmz$'.
Example:
UmbracoAutomation.CLI.exe UpdateHostnames -s=app.dev.int
With the given example above it transforms 'example.com' into 'example_com.app.dev.int'.
```

#### RevertHostnames
Reverts hostname updates made by UpdateHostnames command. Same domain suffix must be specified to sucessfully rollback previous updates.

```
Usage: 
UmbracoAutomation.CLI.exe RevertHostnames <options>

Options
  -s, --suffix=VALUE         The expected suffix (ending) of the hostnames
                               after update. E.g.: app.dev.int
Example:
UmbracoAutomation.CLI.exe RevertHostnames -s=app.dev.int
With the given example above it transforms 'example_com.app.dev.int' into 'example.com'.
```

#### FetchMedia

Downloads missing media files from source URL.

```
Usage: 
UmbracoAutomation.CLI.exe FetchMedia <options>

Options
  -o, --output=VALUE         Output directory. Umbraco app root directory.
  -s, --source=VALUE         Source Umbraco URL.

Example:
FetchMedia -s="http://example.com" -o="D:\UmbracoContent\Content"
```

Notes: tested only with Umbraco.Texbox properties.