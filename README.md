# 7D2D - Procedural Caves Generator

## Setup developpement environement

* Installing 7days to die
* setup the environemment variable `PATH_7D2D` (C:/.../steam/steamapps/common/7 days to die)
* Installing 7-zip [from here](https://7-zip.org/download.html)
* Adding the 7-zip executable (7z.exe) to your environement variable `path`

## Build the mod

* `helpers/compile.bat`: compiles the dll, then creates a ready to release zip archive of the mod

## Running local viewer

* Follow the above instructions to setup the developement environnement
* use the command line interface provided in tests: `tests/run.bat <sub-command>`

## Build documentation with shpinx

```
py -m venv env
env/scripts/activate
pip install -r docs/requirements.txt
sphinx-build -M html ./docs ./docs/_build/
```