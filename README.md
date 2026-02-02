# HP 7090A Features Program

A C# reimplementation of the HP 7090A Performance Verification test program from Table 4-3 of the HP 7090A Service Manual.

## Overview

This program is a diagnostic and feature demonstration tool for the HP 7090A Measurement Plotting System. It tests various plotter capabilities including pen positioning accuracy, repeatability, and drawing operations through a comprehensive series of HPGL (Hewlett-Packard Graphics Language) commands sent over GPIB.

## About the HP 7090A

The HP 7090A is a professional 6-pen microprocessor-controlled Measurement Plotting System manufactured by Hewlett-Packard. It can function as a graphics plotter, data acquisition device, or conventional X-Y recorder. The HP 7090A uses HPGL (Hewlett-Packard Graphics Language) commands for controlling pen movements, drawing geometric shapes, and producing technical drawings. It recognizes HP-RL (Hewlett-Packard Recorder Language) for data acquisition and recorder modes. The plotter is controlled via HP-IB (GPIB/IEEE 488) interface.

## HP Documentation Reference

This program implements the Performance Verification test program from **Table 4-3** (referenced in Paragraph 4-12 and 4-13) of the **HP 7090A Service Manual**. The original HP test program was written in BASIC specifically for the HP-85 personal computer. It tests the input/output (I/O) circuits of the HP 7090A, the majority of the logic circuits, and the paper and pen drive mechanisms. The test verifies critical functions including:

- Hard clip limits (P1, P2) and output window boundaries
- Pen-to-pen repeatability across all 6 pens
- Coordinate system accuracy (both inches and centimeters)
- Fill patterns and shading capabilities
- Geometric shape rendering (circles, wedges, rectangles)
- Text labeling and character positioning
- Radial line drawing and circular fan patterns

## Features Tested

The program performs the following tests and demonstrations:

1. **Coordinate System Verification**
   - Reads and displays hard clip limits (P1, P2)
   - Reads and displays output window (OW) coordinates
   - Draws coordinate axes with tick marks
   - Labels axes in both inches and centimeters

2. **Pen Repeatability Tests**
   - Type 1: Star/cross pattern with 8 radial line segments (drawn with multiple pens to test repeatability)
   - Type 2: Simple cross pattern (vertical and horizontal)
   - Tests performed at multiple locations on the plot
   - Each test numbered to track pen-to-pen positioning consistency

3. **Geometric Patterns**
   - Filled rectangles with different fill patterns
   - Wedges (full and partial circles)
   - Concentric circles forming a circular fan
   - Radial lines emanating from center
   - Edge-drawn and filled polygons

4. **Text Rendering**
   - Absolute and relative character positioning
   - Vertical and horizontal text
   - Slanted text (7090A title)
   - Character size and direction control
   - ASCII character set display (characters 33-127: ! through ~)

5. **Deadband Tests**
   - Horizontal and vertical test scales
   - Tests pen start/stop precision
   - Verifies positioning accuracy without overshoot or undershoot
   - Multiple scales at different orientations

6. **Pen Wobble Test**
   - Zigzag pattern to test mechanical stability
   - Tests pen holder stability during rapid direction changes
   - Reveals mechanical resonance or loose bearings

7. **Frame and Window**
   - Draws nested frames around the output window
   - Tests input window (IW) clipping
   - Variable pen velocity demonstration

## Prerequisites

- Windows operating system
- Visual Studio 2022 or later
- [NI-VISA](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html) libraries installed
  - Provides GPIB communication interface
  - Required for communicating with the plotter
- [Spectre.Console](https://spectreconsole.net/) NuGet package (automatically restored during build)
  - Provides rich terminal UI with menus, progress bars, and formatted output
- HP 7090A Measurement Plotting System connected via HP-IB (GPIB) interface
  - GPIB address: configurable (program defaults to 6; service manual Table 4-3 recommends 5)
- Paper loaded in the plotter (8.5 x 11 inch or A4 size recommended)
- 6 pens recommended (program will work with fewer pens, but may have gaps in patterns)

## Building the Project

1. Open `7090ATest.sln` in Visual Studio 2022
2. Ensure NI-VISA libraries are properly referenced
3. Build the solution:
   - Debug: `Ctrl+Shift+B`
   - Release: Select Release configuration and build

The project targets .NET Framework 4.7.2.

## Running the Program

1. Ensure the HP 7090A Measurement Plotting System is:
   - Connected to your computer via HP-IB (GPIB)
   - Powered on
   - HP-IB address configured (program defaults to 6; see "Modifying the GPIB Address" section)
   - Has paper loaded (8.5 x 11 inch paper recommended as per service manual)
   - 6 pens installed (program will work with fewer pens but may have gaps in patterns).

2. Run the compiled executable:
   ```
   7090ATest.exe
   ```

3. The program will display an interactive menu with options:
   - **Set GPIB Address**: Change the GPIB address without recompiling
   - **Launch Demo**: Run the performance verification plotting sequence
   - **Exit**: Close the program

4. When you launch the demo, the program will:
   - Connect to the configured GPIB address
   - Read plotter parameters (P1, P2, OW)
   - Execute the plotting sequence with progress display
   - Draw all test patterns and return to menu

5. Expected output:
   - Console shows connection status and real-time progress with progress bar
   - Plotter produces a comprehensive test pattern
   - Total plot time: approximately 2-5 minutes depending on plotter speed

## How the Program Works

### Communication Setup

The program uses the National Instruments VISA library to communicate with the plotter:

1. **Interactive Menu**: Displays menu with options to set GPIB address, launch demo, or exit
2. **Initialization**: Opens HP-IB (GPIB) session at the configured address (defaults to 6)
3. **Timeout Settings**: 40 seconds to accommodate mechanical pen movements and plotting operations

### HPGL Command Sequence

The program sends a carefully orchestrated sequence of HPGL commands:

1. **Initialize**: `PG` (page feed), `IN` (initialize)
2. **Query Parameters**: `OP` (output P1/P2), `OW` (output window)
3. **Drawing Commands**: Various pen movements, shapes, and patterns
4. **Character Set Display**: Draws ASCII characters from 33 (!) to 127 (~)
5. **Finalization**: Deselects the pen with `SP0` and moves to the upper-right corner of the output window

### Key HPGL Commands Used

| Command | Description | Example Usage |
|---------|-------------|---------------|
| `PG` | Page Feed | `PG` - advance paper |
| `IN` | Initialize | `IN` - reset plotter to default state |
| `OP` | Output P1/P2 | `OP` - read hard clip limits |
| `OW` | Output Window | `OW` - read output window coordinates |
| `SP` | Select Pen | `SP1` - select pen 1 (SP0 deselects) |
| `PA` | Plot Absolute | `PA5100,4064` - move to coordinates |
| `PR` | Plot Relative | `PR100,200` - move relative to current position |
| `PD` | Pen Down | `PD` - lower pen for drawing |
| `PU` | Pen Up | `PU` - raise pen for moving |
| `CI` | Circle | `CI608` - draw circle with radius 608 |
| `RR` | Rectangle Relative | `RR700,700` - draw filled rectangle |
| `ER` | Edge Rectangle Relative | `ER700,700` - draw rectangle outline |
| `WG` | Wedge | `WG350,0,360,40` - draw filled wedge/arc |
| `EW` | Edge Wedge | `EW350,0,360,40` - draw wedge outline |
| `LB` | Label | `LBText\x03` - draw text (ends with ETX) |
| `FT` | Fill Type | `FT4,100,45` - set fill pattern |
| `UF` | User-defined Fill | `UF10,5,5` - define custom fill pattern |
| `IW` | Input Window | `IW3600,2564,6600,5564` - set clipping |
| `SI` | Character Size | `SI1,1` - set absolute character size |
| `SR` | Scale Relative | `SR0.7,1.5` - set relative scale |
| `CS` | Character Set | `CS1` - select character set |
| `DI` | Direction | `DI0,1` - set text direction (vertical) |
| `SL` | Slant | `SL0.27` - set text slant angle |
| `CP` | Character Plot | `CP2,-.3` - set character offset |
| `SM` | Symbol Mode | `SM+` - enable symbol mode |
| `VS` | Velocity Select | `VS` or `VS40` - set pen velocity |
| `XT` | X-axis Tick | `XT` - draw X-axis tick mark |
| `YT` | Y-axis Tick | `YT` - draw Y-axis tick mark |
| `PT` | Pen Thickness | `PT.5` - set pen thickness |

## Code Structure

The program is organized into logical regions:

### Constants
- GPIB timeouts and address ranges
- ASCII control characters (ESC, ETX, CR)
- Circular fan pattern parameters
- Label positioning offsets
- Cross pattern dimensions
- Progress bar increment values
- GPIB resource name format
- Message display durations

### Main Workflow
1. `Main()` - Entry point with interactive menu loop using Spectre.Console
2. `DisplayTitle()` - Shows application title and prerequisites
3. `ShowMenu()` - Displays menu options (Set GPIB Address, Launch Demo, Exit)
4. `SetGPIBAddress()` - Allows runtime GPIB address configuration with validation
5. `RunPlotterDemo()` - Orchestrates the complete plotting sequence
6. `InitializeGpibConnection()` - Sets up GPIB communication with validation
7. `ExecutePlottingSequence()` - Queries plotter parameters (P1, P2, OW) and sends all plotting commands
8. `CleanupGpibConnection()` - Properly disposes GPIB resources

### Repeatability Test Functions
- `PenRepeatabilityType1()` - Draws 8-segment star/cross pattern with validation
- `PenRepeatabilityType2()` - Draws simple cross pattern with validation

### Deadband and Wobble Test Functions
- `DrawDeadbandTests()` - Draws deadband test scales at various positions
- `DrawDeadbandScale()` - Draws individual deadband test scales
- `DrawPenWobbleTest()` - Tests pen stability during rapid direction changes

### Error Handling
- Validates GPIB address range before connection
- Validates GPIB session initialization before use
- Validates method parameters (e.g., pass numbers must be positive)
- Catches GPIB timeouts with detailed error messages
- Validates coordinate responses with proper error reporting
- Provides detailed error messages with inner exception details
- Ensures proper resource cleanup even when errors occur

## Modifying the GPIB Address

The program currently defaults to GPIB address **6**. The HP 7090A service manual (Table 4-3) recommends address **5** for the performance verification test. The address is configurable in two ways:

1. **At runtime (recommended)**  
   When you run the program, use the menu option that allows you to set or change the GPIB address.  
   This invokes the `SetGPIBAddress` logic and updates the address without needing to recompile.

2. **By changing the default in code**  
   In `Program.cs`, the default address is defined by the `DefaultGpibAddress` constant.  
   You can change this value to match your plotter's configured address, then rebuild the program. For example:

   ```csharp
   private const int DefaultGpibAddress = 6;  // Current program default
   ```
   Note: The service manual Table 4-3 uses address 5 for the performance verification test.

## Troubleshooting

**GPIB Timeout**
- Ensure plotter is powered on and responsive
- Check GPIB cable connections
- Verify GPIB address matches plotter configuration
- Increase timeout if plotter is slow

**Missing Pens**
- Program will work with fewer than 6 pens
- Missing pens will result in gaps in the pattern
- No errors will occur

**Paper Size**
- Program designed for 8.5 x 11 inch paper (as specified in Table 4-3)
- The HP 7090A supports multiple paper sizes via rear panel switch (per paragraph 4-9).
  Maximum writing areas for each paper size:
  - A3 ISO: 275 x 402 mm writing area
  - ANSI B: 10.2 x 16.3 in writing area
  - A4 ISO: 192 x 175 mm writing area
  - ANSI A: 7.5 x 10.2 in writing area
- Smaller paper may clip some patterns
- Hard clip limits (P1, P2) define drawable area

## License

This project is licensed under the MIT License - see the [LICENSE.txt](LICENSE.txt) file for details.

## References

- HP 7090A Service Manual (Part No. 07090-90000), Table 4-3 (Paragraphs 4-12 and 4-13)
- HP 7090A Interfacing and Programming Manual (Part No. 07090-90001)
- HP 7090A Operator's Manual (Part No. 07090-90002)
- HPGL (Hewlett-Packard Graphics Language) Reference Guide
- National Instruments VISA Documentation
