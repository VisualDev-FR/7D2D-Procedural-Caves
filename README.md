# 7D2D - Procedural Caves Generator

![Build Status](https://img.shields.io/badge/build-passing-brightgreen)
![Platform](https://img.shields.io/badge/platform-Windows-blue)
![Language](https://img.shields.io/badge/language-C%23-blue)

> **Advanced procedural generation system injecting complex, organic, and interconnected cave networks into the 7 Days to Die voxel engine.**

## Overview

This project is a C# modification for the Unity-based game *7 Days to Die*. It overrides the default world generator to implement a custom, algorithm-driven subterranean generation system.

Unlike standard runtime-noise-based generation, this module focuses on connectivity and scale:
* **Connectivity Guarantee:** Uses **[A* (A-Star)](https://en.wikipedia.org/wiki/A*_search_algorithm)** pathfinding through 3D noise fields to ensure fully navigable tunnels and organic flow.
* **Massive Scale:** Generates seamless cave networks spanning the entire map.
* **Pre-computation:** Fully computes the voxel map during the world generation phase to ensure zero performance impact during gameplay.

## Technical Architecture

This project implements advanced software engineering patterns to handle the high computational cost of voxel generation.

### Runtime Injection
* Uses the [Harmony](https://github.com/pardeike/Harmony) library for runtime **IL (Intermediate Language) patching**, allowing deep modification of the game's assembly without modifying source binaries.

### Algorithms
* **A\* Pathfinding over Noise:** Implements an approach where A* navigates through "virtual" noise density to carve natural-looking tunnels.
* **3D Cellular Automata:** Applied as a post-processing pass to create organic cavern shapes.

### Performance & Memory Optimization
* **Multithreading:** The generation pipeline is fully parallelized, leveraging modern CPU architectures to minimize world creation time.
* **Data Compression:** Implements **[Run-Length Encoding (RLE)](https://en.wikipedia.org/wiki/Run-length_encoding)** to significantly reduce the memory footprint of voxel data during both generation and runtime.
* **Low-level Optimization:** Extensive use of **Bitwise operations** and optimized data structures in performance-critical sections to reduce GC pressure and CPU cycles.
* **Optimized datas storage:** Implements a custom binary serialization format to minimize the disk footprint of generated cave networks.

### Tooling & Debugging

A standalone CLI runner is included to facilitate rapid iteration without the overhead of the game engine.

* **Visual Validation:** Tools to export/visualize noise maps, pathfinding results, or full cave systems.
* **Seed Testing:** Batch testing of generation seeds for stability and edge-case detection.

## Building the Project

1.  **Clone & Navigate:**
    ```bash
    git clone https://github.com/VisualDev-FR/7D2D-Procedural-Caves
    cd 7d2d-procedural-caves
    ```

2.  **Build:**
    ```bash
    dotnet build --configuration Release
    ```

## Testing & Debugging (Local Viewer)

To iterate on generation algorithms rapidly, a standalone viewer is included in the `tests/` directory. This allows for testing seeds and algorithms without the overhead of loading the full Unity engine.

1.  **Setup environment:**
    Ensure build dependencies are met as per the section above.

2.  **Run the CLI test suite:**
    ```bash
    # Syntax: tests/run.bat <command> <arguments>

    # Example: Exporting a 3D cave room to a .obj file
    tests/run.bat room
    ```

## Documentation (Work In Progress)

Comprehensive technical documentation is generated via Sphinx, detailing the API and algorithmic choices.

**Build requirements:**
* Python 3.x

**Build steps:**
```bash
# Create and activate virtual environment
py -m venv env
env/scripts/activate

# Install dependencies
pip install -r docs/requirements.txt

# Build HTML documentation with live reload
sphinx-autobuild docs docs/_build/html
```

The documentation will be served locally at: http://127.0.0.1:8000
