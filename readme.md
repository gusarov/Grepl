# grepl

grepl is a command line tool for searching & replacing file content with RegEx expressions
This tool is inspired by JGS PowerGrep but due to cli nature it is also an attempt to respect unix grep


|Windows | Linux | macOS |
|--|--|--|
| [![Build status](https://dev.azure.com/xkit/Grepl/_apis/build/status/Grepl%20Windows?branchName=master)](https://dev.azure.com/xkit/Grepl/_build/latest?definitionId=29) |[![Build status](https://dev.azure.com/xkit/Grepl/_apis/build/status/Grepl%20Linux?branchName=master)](https://dev.azure.com/xkit/Grepl/_build/latest?definitionId=30) |[![Build status](https://dev.azure.com/xkit/Grepl/_apis/build/status/Grepl%20macOS?branchName=master)](https://dev.azure.com/xkit/Grepl/_build/latest?definitionId=31)|

## Downloading and Installing

_TBD_

```
choco install grepl
```

--OR--

Download a zip file from release page

## Examples

|Example|Description|
|--|--|
|```grepl Version=[\d\.]+ *.csproj -r```|Search for all csproj file references & their versions|

## Documentation

```
grepl [OPTION...] PATTERNS [FILE...]
```

_**NOTE**: a single letter options are not combinable as of today! So, instead of ```-io``` you should specify ```-i -o```_

## Built With

* [.NetCore](https://dotnet.microsoft.com/download)

## Authors

* **Dmitry Gusarov** - *Initial work* - [Grepl](https://github.com/gusarov/Grepl)

See also the list of [contributors](https://github.com/gusarov/SimpleGrep/contributors) who participated in this project.

## License

This project is licensed under the Apache 2.0 License - see the [LICENSE.txt](LICENSE.txt) file for details

## Acknowledgments

* Inspired by JGS PowerGrep & unix grep
