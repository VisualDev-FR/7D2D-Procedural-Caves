# 7D2D - Procedural Caves Generator

## Requirements

* Windows 10+
* 7 Days to die 1.3+
* .NET Framework 4.8+

## Build the mod

```
cd path/to/cloned/repo
dotnet build
```

## Running local viewer

* Follow the above instructions to setup the developement environnement
* use the command line interface provided in tests: `tests/run.bat <sub-command>`

## Build documentation with shpinx

```
py -m venv env
env/scripts/activate
pip install -r docs/requirements.txt
sphinx-autobuild docs docs/_build/html
```

then documentation is available at [./docs/_build/html/index.html](./docs/_build/html/index.html)