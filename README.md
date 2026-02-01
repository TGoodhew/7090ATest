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
   - Type 1: Cross pattern with 8 radial segments
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

5. **Frame and Window**
   - Draws frame around the output window
   - Tests input window (IW) clipping

## Prerequisites

- Windows operating system
- Visual Studio 2022 or later
- [NI-VISA](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html) libraries installed
  - Provides GPIB communication interface
  - Required for communicating with the plotter
- HP 7090A Measurement Plotting System connected via HP-IB (GPIB) interface
  - Default GPIB address: 5 (configurable in code as per service manual Table 4-3)
- Paper loaded in the plotter (8.5 x 11 inch or A4 size recommended)

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
   - HP-IB address set to 5 (or match the configured address in code)
   - Has paper loaded (8.5 x 11 inch paper recommended as per service manual)
   - 6 narrow width pens installed (as specified in Table 4-3)

2. Run the compiled executable:
   ```
   7090ATest.exe
   ```

3. The program will:
   - Connect to GPIB address 5 (default, as per service manual Table 4-3)
   - Configure the IO buffer
   - Read plotter parameters
   - Execute the plotting sequence
   - Display progress in the console

4. Expected output:
   - Console shows connection status and progress
   - Plotter produces a comprehensive test pattern
   - Total plot time: approximately 2-5 minutes depending on plotter speed

## How the Program Works

### Communication Setup

The program uses the National Instruments VISA library to communicate with the plotter:

1. **Initialization**: Opens HP-IB (GPIB) session at address 5 (default per service manual Table 4-3)
2. **Buffer Configuration**: Sets IO buffer to 6000 bytes using ESC.T command
3. **Timeout Settings**: 
   - Initial: 2 seconds for quick operations
   - Extended: 40 seconds for plotting operations

### HPGL Command Sequence

The program sends a carefully orchestrated sequence of HPGL commands:

1. **Initialize**: `PG` (page feed), `IN` (initialize)
2. **Query Parameters**: `OP` (output P1/P2), `OW` (output window)
3. **Drawing Commands**: Various pen movements, shapes, and patterns
4. **Finalization**: Deselects the pen with `SP0` and moves to the upper-right corner of the output window

### Key HPGL Commands Used

| Command | Description | Example Usage |
|---------|-------------|---------------|
| `SP` | Select Pen | `SP1` - select pen 1 |
| `PA` | Plot Absolute | `PA5100,4064` - move to coordinates |
| `PR` | Plot Relative | `PR100,200` - move relative to current position |
| `PD` | Pen Down | `PD` - lower pen for drawing |
| `PU` | Pen Up | `PU` - raise pen for moving |
| `CI` | Circle | `CI608` - draw circle with radius 608 |
| `RR` | Rectangle Relative | `RR700,700` - draw rectangle |
| `WG` | Wedge | `WG350,0,360,40` - draw wedge/arc |
| `LB` | Label | `LBText\x03` - draw text (ends with ETX) |
| `FT` | Fill Type | `FT4,100,45` - set fill pattern |
| `IW` | Input Window | `IW3600,2564,6600,5564` - set clipping |
| `SI` | Character Size | `SI1,1` - set character size |
| `DI` | Direction | `DI0,1` - set text direction (vertical) |
| `CP` | Character Plot | `CP2,-.3` - set character offset |

## Code Structure

The program is organized into logical regions:

### Constants
- GPIB timeouts
- ASCII control characters (ESC, ETX, CR)
- Circular fan pattern parameters
- Label positioning offsets
- Cross pattern dimensions

### Main Workflow
1. `InitializeGpibConnection()` - Sets up GPIB communication
2. `ReadPlotterParameters()` - Queries plotter for P1, P2, and OW
3. `ExecutePlottingSequence()` - Sends all plotting commands
4. `CleanupGpibConnection()` - Closes GPIB resources

### Repeatability Test Functions
- `PenRepeatabilityType1()` - Draws 8-segment star/cross pattern
- `PenRepeatabilityType2()` - Draws simple cross pattern

### Error Handling
- Catches GPIB timeouts
- Validates coordinate responses
- Provides detailed error messages

## Modifying the GPIB Address

The default GPIB address is **5** (as specified in Table 4-3 of the service manual). There are two ways to change it:

1. **At runtime (recommended)**  
   When you run the program, use the menu option that allows you to set or change the GPIB address.  
   This invokes the `SetGPIBAddress` logic and updates the address without needing to recompile.

2. **By changing the default in code**  
   In `Program.cs`, the default address is defined by the `DefaultGpibAddress` constant.  
   You can change this value to match your plotter's configured address, then rebuild the program. For example:

   ```csharp
   private const int DefaultGpibAddress = 5;  // Default per service manual Table 4-3
   ```

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
- The HP 7090A supports multiple paper sizes via rear panel switch: A3 ISO (275 x 402 mm), ANSI B (10.2 x 16.3 in), A4 ISO (192 x 175 mm), and ANSI A (7.5 x 10.2 in)
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
