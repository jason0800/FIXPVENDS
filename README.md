# FIXPVENDS

A Civil 3D 2022 C# command to automate the placement of manual Chainage Band labels at the start and end of a Profile View.

## Problem
Civil 3D Data Bands often have "bugs" or display issues at the very start and end of an alignment/profile view (clipping, missing labels, etc.). 

## Solution
The `FIXPVENDS` command bypasses these issues by "literally inserting" native AutoCAD `MText` objects showing the station values directly into the band area.

## Features
- **Auto-Coordinate Calculation**: Automatically finds the correct (X, Y) position for start/end stations.
- **Persistent Settings**: Remembers your last used **Value Type**, **Vertical Offset**, and **Text Height** during the session.
- **Data Modes**: Choose between **Station** (Chainage) or **Elevation** (from the Ground Profile) to fill different band types like "Existing Levels".
- **Clean Output**: Inserts only the value (e.g., `0.000`) at a 90-degree rotation.

## Installation
1. Open the project in Visual Studio or use the command line.
2. Build the project using:
   ```powershell
   dotnet build
   ```
3. In Civil 3D 2022, type `NETLOAD` and select `FIXPVENDS.dll` (found in `bin\Debug\net48\`).

## Usage
1. Type `FIXPVENDS` in the Civil 3D command line.
2. Enter the **Vertical Offset** (how far to drop the text below the grid bottom, e.g., `7.5`).
3. Enter the **Text Height** (e.g., `2.5`).
4. Select the **Profile View**.
5. The labels will be inserted at the start and end stations inside the band area.
