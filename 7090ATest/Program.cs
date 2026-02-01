using NationalInstruments.Visa;
using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Resources;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApp17090Test
{
    /// <summary>
    /// HP 7090A Features Program - Reimplementation of the HP 7090A Features program (Para 2-42 in the 7090A Service Manual).
    /// This program tests various plotter features including pen positioning, repeatability, and drawing capabilities.
    /// </summary>
    class Program
    {
        #region Constants
        
        /// <summary>
        /// Default GPIB timeout in milliseconds for initial operations
        /// </summary>
        private const int DefaultTimeoutMs = 2000;
        
        /// <summary>
        /// Extended GPIB timeout in milliseconds for longer operations
        /// </summary>
        private const int ExtendedTimeoutMs = 40000;
        
        /// <summary>
        /// Default GPIB address for the HP 7090A plotter
        /// </summary>
        private const int DefaultGpibAddress = 6;
        
        /// <summary>
        /// Minimum valid GPIB address
        /// </summary>
        private const int GpibAddressMin = 1;
        
        /// <summary>
        /// Maximum valid GPIB address
        /// </summary>
        private const int GpibAddressMax = 30;
        
        /// <summary>
        /// ASCII character code for escape (ESC)
        /// </summary>
        private const char EscapeChar = (char)27;
        
        /// <summary>
        /// ASCII character code for end of text (ETX)
        /// </summary>
        private const char EndOfTextChar = (char)3;
        
        /// <summary>
        /// ASCII character code for carriage return (CR)
        /// </summary>
        private const char CarriageReturnChar = (char)13;
        
        // Circular fan pattern center coordinates and radii
        /// <summary>
        /// X coordinate of circular fan pattern center
        /// </summary>
        private const int CircleCenterX = 5100;
        
        /// <summary>
        /// Y coordinate of circular fan pattern center
        /// </summary>
        private const int CircleCenterY = 4064;
        
        /// <summary>
        /// Inner circle radius for radial line pattern
        /// </summary>
        private const int InnerCircleRadius = 608;
        
        /// <summary>
        /// Outer circle radius for radial line pattern
        /// </summary>
        private const int OuterCircleRadius = 2200;
        
        // Character positioning offsets for repeatability test labels
        /// <summary>
        /// Character plot X offset for repeatability test label (Type 1)
        /// </summary>
        private const double LabelOffsetX1 = -1.2;
        
        /// <summary>
        /// Character plot Y offset for repeatability test label (Type 1)
        /// </summary>
        private const double LabelOffsetY1 = 0.4;
        
        /// <summary>
        /// Character plot X adjustment after label (Type 1)
        /// </summary>
        private const double LabelAdjustX1 = 0.2;
        
        /// <summary>
        /// Character plot Y adjustment after label (Type 1)
        /// </summary>
        private const double LabelAdjustY1 = -0.4;
        
        // Cross pattern dimensions for repeatability test
        /// <summary>
        /// Long line segment length in repeatability test cross pattern
        /// </summary>
        private const int CrossLongSegment = 247;
        
        /// <summary>
        /// Short line segment length in repeatability test cross pattern
        /// </summary>
        private const int CrossShortSegment = 18;
        
        // Progress bar increment values for plotting sequence
        /// <summary>
        /// Progress increment for drawing coordinate labels
        /// </summary>
        private const double ProgressCoordinateLabels = 5.0;
        
        /// <summary>
        /// Progress increment used for pen repeatability tests and related drawing checkpoints
        /// </summary>
        private const double ProgressPenRepeatability = 10.0;
        
        /// <summary>
        /// Progress increment for drawing axis grid
        /// </summary>
        private const double ProgressAxisGrid = 15.0;
        
        /// <summary>
        /// Progress increment for drawing axis labels
        /// </summary>
        private const double ProgressAxisLabels = 10.0;
        
        /// <summary>
        /// Progress increment for drawing circular fan pattern
        /// </summary>
        private const double ProgressCircularFan = 10.0;
        
        /// <summary>
        /// Progress increment for drawing radial lines
        /// </summary>
        private const double ProgressRadialLines = 10.0;
        
        /// <summary>
        /// Progress increment for drawing title labels
        /// </summary>
        private const double ProgressTitleLabels = 5.0;
        
        /// <summary>
        /// Progress increment for drawing frame window
        /// </summary>
        private const double ProgressFrameWindow = 5.0;
        
        // Note: Progress increments are used as follows in ExecutePlottingSequence:
        // ProgressCoordinateLabels (5%) + ProgressPenRepeatability x4 (40%) + ProgressAxisGrid (15%) +
        // ProgressAxisLabels (10%) + ProgressCircularFan (10%) + ProgressRadialLines (10%) +
        // ProgressTitleLabels (5%) + ProgressFrameWindow (5%) = 100%
        
        #endregion

        #region Fields
        
        /// <summary>
        /// GPIB session for communication with the HP 7090A plotter
        /// </summary>
        private static GpibSession gpibSession;
        
        /// <summary>
        /// VISA resource manager for managing GPIB resources
        /// </summary>
        private static NationalInstruments.Visa.ResourceManager resManager;
        
        #endregion

        /// <summary>
        /// Main entry point for the HP 7090A Features test program.
        /// Provides an interactive menu for running the plotter demo and configuring GPIB settings.
        /// </summary>
        /// <param name="args">Command line arguments (not used)</param>
        static void Main(string[] args)
        {
            int gpibAddress = DefaultGpibAddress; // This is the default address for the HP 7090A

            try
            {
                DisplayTitle(gpibAddress);

                // Ask for menu choice
                var menuChoice = ShowMenu();

                while (menuChoice != MenuChoiceExit)
                {
                    try
                    {
                        switch (menuChoice)
                        {
                            case MenuChoiceSetGpibAddress:
                                gpibAddress = SetGPIBAddress(gpibAddress);
                                break;
                            case MenuChoiceLaunchDemo:
                                RunPlotterDemo(gpibAddress);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        AnsiConsole.MarkupLine($"[red]Unexpected error in operation: {ex.Message}[/]");
                        System.Threading.Thread.Sleep(2000);
                    }

                    // Clear the screen & Display title
                    DisplayTitle(gpibAddress);

                    // Ask for menu choice
                    menuChoice = ShowMenu();
                }
                
                AnsiConsole.MarkupLine("[yellow]Goodbye![/]");
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Fatal error: {ex.Message}[/]");
                System.Threading.Thread.Sleep(2000);
                Environment.ExitCode = 1;
            }
        }

        #region Title Page

        /// <summary>
        /// Menu option text for setting the GPIB address
        /// </summary>
        private const string MenuChoiceSetGpibAddress = "Set GPIB Address";
        
        /// <summary>
        /// Menu option text for launching the demo
        /// </summary>
        private const string MenuChoiceLaunchDemo = "Launch Demo";
        
        /// <summary>
        /// Menu option text for exiting the application
        /// </summary>
        private const string MenuChoiceExit = "Exit";

        /// <summary>
        /// Displays the application title using Spectre.Console
        /// </summary>
        /// <param name="currentGpibAddress">The current GPIB address</param>
        private static void DisplayTitle(int currentGpibAddress)
        {
            AnsiConsole.Clear();
            
            // Create a fancy Figlet title
            var title = new FigletText("HP 7090A")
                .Centered()
                .Color(Color.Cyan1);
            
            AnsiConsole.Write(title);
            
            // Create an informational panel
            var panel = new Panel(
                new Markup(
                    "[yellow]Features Test Program[/]\n\n" +
                    "A diagnostic and demonstration tool for the HP 7090A Graphics Plotter.\n\n" +
                    "[dim]This program implements the features test described in Paragraph 2-42\n" +
                    "of the HP 7090A Service Manual.[/]\n\n" +
                    "[bold red]Prerequisites:[/]\n" +
                    $" - HP 7090A plotter connected via GPIB (current address: [cyan]{currentGpibAddress}[/])\n" +
                    " - Paper loaded in the plotter\n" +
                    " - All 8 pens installed (recommended)\n"
                ))
            {
                Header = new PanelHeader("[green bold]About This Program[/]"),
                Border = BoxBorder.Double,
                Padding = new Padding(2, 1)
            };
            
            AnsiConsole.Write(panel);
            AnsiConsole.WriteLine();
        }

        /// <summary>
        /// Runs the plotter demo sequence with the specified GPIB address.
        /// </summary>
        /// <param name="gpibAddress">The GPIB address to use for the plotter</param>
        private static void RunPlotterDemo(int gpibAddress)
        {
            int hardClipLowerLeftX = 0, hardClipLowerLeftY = 0, hardClipUpperRightX = 0, hardClipUpperRightY = 0;
            int outputWindowLowerLeftX = 0, outputWindowLowerLeftY = 0, outputWindowUpperRightX = 0, outputWindowUpperRightY = 0;

            try
            {
                // Initialize GPIB communication
                InitializeGpibConnection(gpibAddress);
                
                Console.WriteLine("Ensure paper is loaded");
                Console.WriteLine($"Connecting to HP 7090A at GPIB address {gpibAddress}...");

                // Read plotter coordinates and parameters
                ReadPlotterParameters(ref hardClipLowerLeftX, ref hardClipLowerLeftY, ref hardClipUpperRightX, ref hardClipUpperRightY, 
                                      ref outputWindowLowerLeftX, ref outputWindowLowerLeftY, ref outputWindowUpperRightX, ref outputWindowUpperRightY);

                // Execute the main plotting sequence
                ExecutePlottingSequence(hardClipLowerLeftX, hardClipLowerLeftY, hardClipUpperRightX, hardClipUpperRightY, 
                                        outputWindowLowerLeftX, outputWindowLowerLeftY, outputWindowUpperRightX, outputWindowUpperRightY);
                
                WaitForUserToReturnToMenu();
            }
            catch (Ivi.Visa.IOTimeoutException ex)
            {
                LogError("GPIB timeout occurred", ex);
                WaitForUserToReturnToMenu();
            }
            catch (Ivi.Visa.VisaException ex)
            {
                LogError("VISA communication error", ex);
                WaitForUserToReturnToMenu();
            }
            catch (Exception ex)
            {
                LogError("Unexpected error", ex);
                WaitForUserToReturnToMenu();
            }
            finally
            {
                // Clean up resources
                CleanupGpibConnection();
            }
        }

        #endregion

        #region GPIB Address Configuration

        /// <summary>
        /// Prompts the user to set a new GPIB address with validation.
        /// </summary>
        /// <param name="currentAddress">The current GPIB address</param>
        /// <returns>The new GPIB address (or the current address if an error occurs)</returns>
        private static int SetGPIBAddress(int currentAddress)
        {
            try
            {
                // Prompt for the GPIB Address with validation
                int newAddress = AnsiConsole.Prompt(
                    new TextPrompt<int>($"Enter HP 7090A GPIB address (press Enter for current: {currentAddress}, factory default: {DefaultGpibAddress}):")
                    .DefaultValue(currentAddress)
                    .Validate(n =>
                    {
                        bool isValidAddress = n >= GpibAddressMin && n <= GpibAddressMax;
                        return isValidAddress
                            ? ValidationResult.Success()
                            : ValidationResult.Error($"[red]Address must be between {GpibAddressMin} and {GpibAddressMax}[/]");
                    })
                );

                AnsiConsole.MarkupLine("[green]GPIB Address updated.[/]");
                System.Threading.Thread.Sleep(1000); // Pause for a moment to let the user see the message

                return newAddress;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]GPIB address change cancelled. Keeping current address.[/]");
                System.Threading.Thread.Sleep(1000);
                return currentAddress;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Unexpected error setting GPIB address: {ex}[/]");
                System.Threading.Thread.Sleep(1000);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Prompts the user to press any key to return to the menu.
        /// </summary>
        private static void WaitForUserToReturnToMenu()
        {
            Console.WriteLine("\nPress any key to return to menu...");
            Console.ReadKey();
        }

        /// <summary>
        /// Displays the menu and prompts the user to select an option.
        /// </summary>
        /// <returns>The selected menu choice</returns>
        private static string ShowMenu()
        {
            return AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                    .Title("[cyan]What would you like to do?[/]")
                    .PageSize(10)
                    .AddChoices(new[] { MenuChoiceSetGpibAddress, MenuChoiceLaunchDemo, MenuChoiceExit })
            );
        }

        #endregion

        #region Initialization and Cleanup

        /// <summary>
        /// Initializes the GPIB connection to the HP 7090A plotter.
        /// </summary>
        /// <param name="gpibAddress">The GPIB address to connect to</param>
        /// <exception cref="Ivi.Visa.VisaException">Thrown when GPIB connection cannot be established</exception>
        private static void InitializeGpibConnection(int gpibAddress)
        {
            // Setup the GPIB connection via the ResourceManager
            resManager = new NationalInstruments.Visa.ResourceManager();

            // Create a GPIB session for the specified address
            string gpibResourceName = string.Format("GPIB0::{0}::INSTR", gpibAddress);
            gpibSession = (GpibSession)resManager.Open(gpibResourceName);
            
            // Set the timeout to 2 seconds for initial operations
            gpibSession.TimeoutMilliseconds = DefaultTimeoutMs;
            gpibSession.TerminationCharacterEnabled = true;
            
            // Clear the session to ensure clean state
            gpibSession.Clear();
            
            Console.WriteLine($"Successfully connected to {gpibResourceName}");
        }

        /// <summary>
        /// Cleans up GPIB resources and closes the connection.
        /// </summary>
        private static void CleanupGpibConnection()
        {
            try
            {
                if (gpibSession != null)
                {
                    gpibSession.Dispose();
                    Console.WriteLine("GPIB session closed.");
                }
                
                if (resManager != null)
                {
                    resManager.Dispose();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error during cleanup: {ex.Message}");
            }
        }

        #endregion

        #region Plotter Parameter Reading

        /// <summary>
        /// Reads the plotter parameters including hard clip limits (P1, P2) and output window (OW).
        /// </summary>
        /// <param name="hardClipLowerLeftX">Hard clip limit P1 X coordinate (lower-left corner)</param>
        /// <param name="hardClipLowerLeftY">Hard clip limit P1 Y coordinate (lower-left corner)</param>
        /// <param name="hardClipUpperRightX">Hard clip limit P2 X coordinate (upper-right corner)</param>
        /// <param name="hardClipUpperRightY">Hard clip limit P2 Y coordinate (upper-right corner)</param>
        /// <param name="outputWindowLowerLeftX">Output window lower-left X coordinate</param>
        /// <param name="outputWindowLowerLeftY">Output window lower-left Y coordinate</param>
        /// <param name="outputWindowUpperRightX">Output window upper-right X coordinate</param>
        /// <param name="outputWindowUpperRightY">Output window upper-right Y coordinate</param>
        /// <exception cref="Ivi.Visa.IOTimeoutException">Thrown when GPIB communication times out</exception>
        private static void ReadPlotterParameters(ref int hardClipLowerLeftX, ref int hardClipLowerLeftY, ref int hardClipUpperRightX, ref int hardClipUpperRightY, 
                                                   ref int outputWindowLowerLeftX, ref int outputWindowLowerLeftY, ref int outputWindowUpperRightX, ref int outputWindowUpperRightY)
        {
            // Note: GPIB timeouts are handled by the Main method's exception handlers.
            // This method validates and parses the coordinate responses.
            try
            {
                // Setup IO buffer - ESC.T command sets the buffer size
                // T command parameters: threshold;size;flowXonXoff;flowEnqAck;filler
                // T1000;6000;0;0;5800: sets threshold=1000 bytes, size=6000 bytes, no XON/XOFF, no ENQ/ACK
                Console.WriteLine("Configuring IO buffer...");
                gpibSession.FormattedIO.WriteLine(EscapeChar + ".T1000;6000;0;0;5800:");
                
                // Confirm the IO Buffer size - ESC.L command queries buffer
                gpibSession.FormattedIO.WriteLine(EscapeChar + ".L");
                string bufferSize = gpibSession.FormattedIO.ReadString();
                Console.WriteLine($"IO Buffer is set to {bufferSize} bytes");

                // PG IN OP - Request P1 and P2 hard clip limits
                // PG: Page feed, IN: Initialize, OP: Output P1 and P2
                Console.WriteLine("Reading hard clip limits (P1, P2)...");
                gpibSession.FormattedIO.WriteLine("PG;IN;OP;");
                string hardClipResponse = gpibSession.FormattedIO.ReadString();

                if (string.IsNullOrWhiteSpace(hardClipResponse))
                {
                    throw new InvalidOperationException("Failed to read P1/P2 coordinates - empty response");
                }

                string[] values = hardClipResponse.Split(',');
                
                if (values.Length < 4)
                {
                    throw new InvalidOperationException($"Invalid P1/P2 response - expected 4 values, got {values.Length}");
                }

                hardClipLowerLeftX = int.Parse(values[0]);
                hardClipLowerLeftY = int.Parse(values[1]);
                hardClipUpperRightX = int.Parse(values[2]);
                hardClipUpperRightY = int.Parse(values[3]);

                Console.WriteLine($"P1 (hard clip lower-left): X={hardClipLowerLeftX}, Y={hardClipLowerLeftY}");
                Console.WriteLine($"P2 (hard clip upper-right): X={hardClipUpperRightX}, Y={hardClipUpperRightY}");

                // OW - Output Window command - requests current output window coordinates
                Console.WriteLine("Reading output window (OW)...");
                gpibSession.FormattedIO.WriteLine("OW;");
                string outputWindowResponse = gpibSession.FormattedIO.ReadString();

                if (string.IsNullOrWhiteSpace(outputWindowResponse))
                {
                    throw new InvalidOperationException("Failed to read OW coordinates - empty response");
                }

                values = outputWindowResponse.Split(',');
                
                if (values.Length < 4)
                {
                    throw new InvalidOperationException($"Invalid OW response - expected 4 values, got {values.Length}");
                }

                outputWindowLowerLeftX = int.Parse(values[0]);
                outputWindowLowerLeftY = int.Parse(values[1]);
                outputWindowUpperRightX = int.Parse(values[2]);
                outputWindowUpperRightY = int.Parse(values[3]);

                Console.WriteLine($"Output Window lower-left: X={outputWindowLowerLeftX}, Y={outputWindowLowerLeftY}");
                Console.WriteLine($"Output Window upper-right: X={outputWindowUpperRightX}, Y={outputWindowUpperRightY}");

                // Reset the timeout to 40 seconds for plotting operations
                gpibSession.TimeoutMilliseconds = ExtendedTimeoutMs;
                Console.WriteLine($"Timeout extended to {ExtendedTimeoutMs}ms for plotting operations");
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("Failed to parse plotter coordinates - invalid number format", ex);
            }
        }

        #endregion

        #region Plotting Sequence

        /// <summary>
        /// Executes the main plotting sequence with all test patterns and features.
        /// This includes drawing test patterns, pen repeatability tests, and various geometric shapes.
        /// </summary>
        /// <param name="hardClipLowerLeftX">Hard clip limit P1 X coordinate (lower-left corner)</param>
        /// <param name="hardClipLowerLeftY">Hard clip limit P1 Y coordinate (lower-left corner)</param>
        /// <param name="hardClipUpperRightX">Hard clip limit P2 X coordinate (upper-right corner)</param>
        /// <param name="hardClipUpperRightY">Hard clip limit P2 Y coordinate (upper-right corner)</param>
        /// <param name="outputWindowLowerLeftX">Output window lower-left X coordinate</param>
        /// <param name="outputWindowLowerLeftY">Output window lower-left Y coordinate</param>
        /// <param name="outputWindowUpperRightX">Output window upper-right X coordinate</param>
        /// <param name="outputWindowUpperRightY">Output window upper-right Y coordinate</param>
        private static void ExecutePlottingSequence(int hardClipLowerLeftX, int hardClipLowerLeftY, int hardClipUpperRightX, int hardClipUpperRightY, 
                                                     int outputWindowLowerLeftX, int outputWindowLowerLeftY, int outputWindowUpperRightX, int outputWindowUpperRightY)
        {
            AnsiConsole.Progress()
                .AutoClear(false)
                .Columns(new ProgressColumn[] 
                {
                    new TaskDescriptionColumn(),
                    new ProgressBarColumn(),
                    new PercentageColumn(),
                    new SpinnerColumn(),
                })
                .Start(ctx => 
                {
                    var task = ctx.AddTask("[cyan]Executing plotting sequence[/]", maxValue: 100);

                    // DRAW+ AT P1 & P2 & LABEL COORDINATES
                    task.Description = "[cyan]Drawing coordinate labels[/]";
                    // SP1: Select pen 1, PA: Plot Absolute, PD: Pen Down, SM+: Symbol mode plus, PU: Pen Up
                    gpibSession.FormattedIO.WriteLine("SP1;PA5100,4064;PD;SM+;PU" + hardClipLowerLeftX + "," + hardClipLowerLeftY);
                    // CP: Character Plot with offset, LB: Label with text
                    gpibSession.FormattedIO.WriteLine("CP2,-.3;LBP1=(" + hardClipLowerLeftX + "," + hardClipLowerLeftY + ")" + EndOfTextChar);
                    gpibSession.FormattedIO.WriteLine("PA" + hardClipUpperRightX + "," + hardClipUpperRightY + ";SM;");
                    gpibSession.FormattedIO.WriteLine("CP-16,-.3;LBP2=(" + hardClipUpperRightX + "," + hardClipUpperRightY + ")" + EndOfTextChar);
                    task.Increment(ProgressCoordinateLabels);

                    // Pen repeatability tests at various positions
                    task.Description = "[cyan]Drawing pen repeatability tests (1)[/]";
                    gpibSession.FormattedIO.WriteLine("PA2032,6236;");
                    PenRepeatabilityType1(1); // This routine is called multiple times and creates a cross that shows the pens hitting the same points
                    gpibSession.FormattedIO.WriteLine("PA8128,1892;");
                    PenRepeatabilityType2(1); // Same idea as the previous one but a slightly different process

                    // FT4: Fill type 4, RR: Rectangle Relative, SP2: Select pen 2, ER: Edge Rectangle Relative
                    gpibSession.FormattedIO.WriteLine("FT4,100,45;PA9372,6440;RR700,700;SP2;ER700,700");
                    task.Increment(ProgressPenRepeatability);

                    // DRAW & LABEL AXIS
                    task.Description = "[cyan]Drawing axis grid[/]";
                    gpibSession.FormattedIO.WriteLine("PA9124,1016;PD;");
                    // Draw X-axis tick marks
                    for (int i = 0; i < 8; i++)
                    {
                        gpibSession.FormattedIO.WriteLine("XT;PR-1016,0;"); // XT: X-axis tick, PR: Plot Relative
                    }

                    // Draw Y-axis tick marks
                    for (int i = 0; i < 15; i++)
                    {
                        gpibSession.FormattedIO.WriteLine("PR0,400;YT;"); // YT: Y-axis tick
                    }
                    task.Increment(ProgressAxisGrid);

                    // More pen repeatability tests
                    task.Description = "[cyan]Drawing pen repeatability tests (2)[/]";
                    gpibSession.FormattedIO.WriteLine("PU;PA2032,4788;");
                    PenRepeatabilityType1(2);
                    gpibSession.FormattedIO.WriteLine("PA8128,3340;");
                    PenRepeatabilityType2(2);

                    // WG: Wedge, EW: Edge Wedge
                    gpibSession.FormattedIO.WriteLine("FT4,50,90;PA9722,5600;WG350,0,360,40;SP3;EW350,0,360,40;");
                    task.Increment(ProgressPenRepeatability);
                    
                    // Draw "Centimetres" label vertically
                    // DI: Direction for text (0,1 = vertical)
                    task.Description = "[cyan]Drawing axis labels[/]";
                    gpibSession.FormattedIO.WriteLine("SP3;PA600,3500;DI0,1;LBCentimetres" + EndOfTextChar + ";");
                    gpibSession.FormattedIO.WriteLine("PA700,6966;DI;"); // Reset direction

                    // Draw Y-axis scale labels (15 down to 1)
                    for (int i = 15; i > 0; i--)
                    {
                        if (i < 10)
                        {
                            gpibSession.FormattedIO.WriteLine("CP1,0;"); // Adjust character position for single digit
                        }

                        gpibSession.FormattedIO.WriteLine("LB" + i + CarriageReturnChar + EndOfTextChar + ";PR0,-400;");
                    }
                    task.Increment(ProgressAxisLabels);

                    // Continue with more test patterns
                    task.Description = "[cyan]Drawing pen repeatability tests (3)[/]";
                    gpibSession.FormattedIO.WriteLine("PA2032,3340;");
                    PenRepeatabilityType1(3);
                    gpibSession.FormattedIO.WriteLine("PA8128,4788;");
                    PenRepeatabilityType2(3);

                    // UF: User-defined Fill, PT: Pen Thickness
                    gpibSession.FormattedIO.WriteLine("UF10,5,5;FT5;PA9722,4060;PT.5;WG700,60,60;");
                    
                    // Draw X-axis scale labels (0 through 7)
                    gpibSession.FormattedIO.WriteLine("PA948,756;SP4;");

                    for (int i = 0; i < 8; i++)
                    {
                        gpibSession.FormattedIO.WriteLine("LB" + i + CarriageReturnChar + EndOfTextChar + ";PR1016,0;");
                    }

                    gpibSession.FormattedIO.WriteLine("PA4810,516;LBInches" + EndOfTextChar);
                    task.Increment(ProgressPenRepeatability);
                    
                    // More repeatability tests
                    task.Description = "[cyan]Drawing pen repeatability tests (4)[/]";
                    gpibSession.FormattedIO.WriteLine("PA2032,1892;");
                    PenRepeatabilityType1(4);
                    gpibSession.FormattedIO.WriteLine("PA8128,6236;");
                    PenRepeatabilityType2(4);

                    // More geometric patterns
                    gpibSession.FormattedIO.WriteLine("UF12,8;FT5;PA9722,3570;PT.5;WG700,240,60;SP5;EW700,240,60;");
                    gpibSession.FormattedIO.WriteLine("PU8128,6236;");
                    PenRepeatabilityType1(5);
                    gpibSession.FormattedIO.WriteLine("PA2032,1892;");
                    PenRepeatabilityType2(5);
                    task.Increment(ProgressPenRepeatability);

                    // DRAW CIRCULAR FAN
                    task.Description = "[cyan]Drawing circular fan pattern[/]";
                    // PM0: Polygon mode 0 (start), CI: Circle
                    gpibSession.FormattedIO.WriteLine($"PA{CircleCenterX},{CircleCenterY};PM0;");

                    for (int i = 108; i <= InnerCircleRadius; i += 100)
                    {
                        gpibSession.FormattedIO.WriteLine("CI" + i + ";PM1;"); // PM1: Polygon mode 1 (continue)
                    }

                    // PM2: Polygon mode 2 (end), FP: Fill Polygon, EP: Edge Polygon
                    gpibSession.FormattedIO.WriteLine("PM2;UF;FT5;FP;SP6;EP;SP7;");
                    gpibSession.FormattedIO.WriteLine("PA8128,3340;");
                    PenRepeatabilityType1(7);
                    gpibSession.FormattedIO.WriteLine("PA2031,4788;");
                    PenRepeatabilityType2(7);

                    // IW: Input Window, ER: Edge Rectangle
                    gpibSession.FormattedIO.WriteLine("IW3600,2564,6600,5564;PA3600,2564;ER3000,3000;SP8;");
                    task.Increment(ProgressCircularFan);

                    // Draw radial lines from circle center
                    task.Description = "[cyan]Drawing radial lines[/]";
                    // Conversion factor from degrees to radians for trigonometric functions
                    double degreesToRadiansConversion = Math.PI / 180.0;

                    for (int i = 0; i < 360; i += 15)
                    {
                        double radians = i * degreesToRadiansConversion;

                        // Calculate the edge of the inner circle using InnerCircleRadius
                        int innerCircleLineX = (int)Math.Round(CircleCenterX + InnerCircleRadius * Math.Cos(radians));
                        int innerCircleLineY = (int)Math.Round(CircleCenterY + InnerCircleRadius * Math.Sin(radians));
                        
                        // Calculate the outer edge using OuterCircleRadius
                        int outerCircleLineX = (int)Math.Round(CircleCenterX + OuterCircleRadius * Math.Cos(radians));
                        int outerCircleLineY = (int)Math.Round(CircleCenterY + OuterCircleRadius * Math.Sin(radians));

                        gpibSession.FormattedIO.WriteLine("PU" + innerCircleLineX + "," + innerCircleLineY + ";PD" + outerCircleLineX + "," + outerCircleLineY + ";");
                    }

                    // IW: Reset Input Window
                    gpibSession.FormattedIO.WriteLine("IW;PU8128,1892;");
                    PenRepeatabilityType1(8);
                    gpibSession.FormattedIO.WriteLine("PA2032,6236;");
                    PenRepeatabilityType2(8);
                    task.Increment(ProgressRadialLines);

                    // DRAW LABELS
                    task.Description = "[cyan]Drawing title labels[/]";
                    // VS: Velocity Select, SI: Absolute Character Size, SL: Slant
                    gpibSession.FormattedIO.WriteLine("PA3610,6514;");
                    gpibSession.FormattedIO.WriteLine("VS;SI1,1;SL.45;LB7090A" + EndOfTextChar + ";");
                    gpibSession.FormattedIO.WriteLine("PA4645,1778;");
                    gpibSession.FormattedIO.WriteLine("SI;SL;LBFeatures" + EndOfTextChar + ";");
                    gpibSession.FormattedIO.WriteLine("CP-6,-1;LBPlot" + EndOfTextChar + ";");
                    
                    // Final repeatability tests
                    gpibSession.FormattedIO.WriteLine("PA8128,4788;");
                    PenRepeatabilityType1(6);
                    gpibSession.FormattedIO.WriteLine("PA2032,3340;");
                    PenRepeatabilityType2(6);
                    gpibSession.FormattedIO.WriteLine("FT4,100,45;PA9372,490;RR700,700;SP1;ER700,700");
                    task.Increment(ProgressTitleLabels);

                    // FRAME WINDOW
                    // EA: Edge Absolute - draw a rectangle from lower-left to upper-right
                    task.Description = "[cyan]Drawing frame window[/]";
                    gpibSession.FormattedIO.WriteLine("PU" + outputWindowLowerLeftX + "," + outputWindowLowerLeftY + ";EA" + outputWindowUpperRightX + "," + outputWindowUpperRightY + ";");
                    // CI25: Draw a circle with radius 25 at the center, SP0: Select pen 0 (pen up)
                    gpibSession.FormattedIO.WriteLine($"PU{CircleCenterX},{CircleCenterY};CI25;SP0;PA{outputWindowUpperRightX},{outputWindowUpperRightY};");
                    task.Increment(ProgressFrameWindow);
                    
                    task.Description = "[green]Plotting sequence completed[/]";
                });

            AnsiConsole.MarkupLine("[green]Plot completed successfully![/]");
        }

        #endregion

        #region Pen Repeatability Tests

        /// <summary>
        /// Pen to pen repeatability test - Type 1.
        /// Draws a cross pattern with multiple pen passes to test repeatability.
        /// </summary>
        /// <param name="pass">Pass number to label the test</param>
        private static void PenRepeatabilityType1(int pass)
        {
            // SI: Character size, CP: Character Plot position offset, LB: Label
            gpibSession.FormattedIO.WriteLine($"SI;CP{LabelOffsetX1},{LabelOffsetY1};LB{pass}{EndOfTextChar};CP{LabelAdjustX1},{LabelAdjustY1};");
            
            // PR: Plot Relative - draws a complex cross pattern with multiple line segments
            // The pattern draws a star/cross using segments of length 247 (long) and 18 (short)
            // to test pen positioning accuracy across multiple pen passes
            // Pattern consists of 8 lines radiating from center: right, up, left, down, etc.
            string crossPattern = $"PR9,-9;PD" +
                $"{CrossLongSegment},0," +           // Move right (long)
                $"0,{CrossShortSegment}," +          // Move up (short)
                $"-{CrossLongSegment},0," +          // Move left (long)
                $"0,{CrossLongSegment}," +           // Move up (long)
                $"-{CrossShortSegment},0," +         // Move left (short)
                $"0,-{CrossLongSegment}," +          // Move down (long)
                $"-{CrossLongSegment},0," +          // Move left (long)
                $"0,-{CrossShortSegment}," +         // Move down (short)
                $"{CrossLongSegment},0," +           // Move right (long)
                $"0,-{CrossLongSegment}," +          // Move down (long)
                $"{CrossShortSegment},0," +          // Move right (short)
                $"0,{CrossLongSegment};" +           // Move up (long)
                $"PU;";                              // Pen up
            gpibSession.FormattedIO.WriteLine(crossPattern);
        }

        /// <summary>
        /// Pen to pen repeatability test - Type 2.
        /// Draws a cross pattern using a different method to test repeatability.
        /// </summary>
        /// <param name="pass">Pass number to label the test</param>
        private static void PenRepeatabilityType2(int pass)
        {
            // CP: Set character position, LB: Label with pass number
            gpibSession.FormattedIO.WriteLine("CP.4,-.8;LB" + pass + EndOfTextChar + ";CP-1.4,.8;");
            // Draw a cross: vertical line down then up, horizontal line right
            gpibSession.FormattedIO.WriteLine("PR0,512;PD0,-1024;PU-512,512;PD1024,0;PU;");
        }

        #endregion

        #region Error Handling

        /// <summary>
        /// Logs an error message with exception details to the console.
        /// </summary>
        /// <param name="message">Error message description</param>
        /// <param name="ex">Exception that occurred</param>
        private static void LogError(string message, Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"\nERROR: {message}");
            Console.WriteLine($"Details: {ex.Message}");
            
            if (ex.InnerException != null)
            {
                Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
            }
            
            Console.ResetColor();
        }

        #endregion
    }
}
