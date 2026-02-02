using NationalInstruments.Visa;
using Spectre.Console;
using System;

namespace HP7090ATest
{
    /// <summary>
    /// HP 7090A Performance Verification Program - Reimplementation of the HP-85 BASIC code from Table 4-3 (Paragraphs 4-12 and 4-13) in the HP 7090A Service Manual.
    /// This program tests the input/output (I/O) circuits of the HP 7090A, the majority of the logic circuits, and the paper and pen drive mechanisms.
    /// Tests include pen positioning, repeatability, coordinate system accuracy, and drawing capabilities.
    /// </summary>
    class Program
    {
        #region Constants
        
        /// <summary>
        /// GPIB timeout in milliseconds for plotting operations (Table 4-3 uses single timeout)
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
        
        // Circular fan pattern center coordinates and radii (from Table 4-3)
        /// <summary>
        /// X coordinate of circular fan pattern center (Table 4-3: 5080)
        /// </summary>
        private const int CircleCenterX = 5080;
        
        /// <summary>
        /// Y coordinate of circular fan pattern center (Table 4-3: 4064)
        /// </summary>
        private const int CircleCenterY = 4064;
        
        /// <summary>
        /// Inner circle radius for radial line pattern (Table 4-3: 400)
        /// </summary>
        private const int InnerCircleRadius = 400;
        
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
        
        // Pen wobble test pattern parameters (from Table 4-3)
        /// <summary>
        /// Horizontal offset in wobble test zigzag pattern (Table 4-3: A0 = 0)
        /// </summary>
        private const int WobbleTestHorizontalOffset = 0;
        
        /// <summary>
        /// Vertical amplitude of wobble test zigzag pattern (Table 4-3: A1 = 200)
        /// </summary>
        private const int WobbleTestZigzagAmplitude = 200;
        
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
        
        // GPIB Resource Name Format
        /// <summary>
        /// Format string for constructing GPIB resource name (e.g., "GPIB0::6::INSTR")
        /// </summary>
        private const string GpibResourceNameFormat = "GPIB0::{0}::INSTR";
        
        // Coordinate Response Validation
        /// <summary>
        /// Expected number of coordinate values in P1/P2 and OW responses
        /// </summary>
        private const int ExpectedCoordinateCount = 4;
        
        // Sleep Duration Constants
        /// <summary>
        /// Duration in milliseconds to display messages before continuing
        /// </summary>
        private const int MessageDisplayDurationMs = 1000;
        
        /// <summary>
        /// Duration in milliseconds to display error messages before continuing
        /// </summary>
        private const int ErrorMessageDisplayDurationMs = 2000;
        
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
                        System.Threading.Thread.Sleep(ErrorMessageDisplayDurationMs);
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
                System.Threading.Thread.Sleep(ErrorMessageDisplayDurationMs);
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
                    "[yellow]Performance Verification Program[/]\n\n" +
                    "A diagnostic and verification tool for the HP 7090A Graphics Plotter.\n\n" +
                    "[dim]This program implements the Performance Verification Program described in\n" +
                    "Table 4-3 (Paragraphs 4-12 and 4-13) of the HP 7090A Service Manual.[/]\n\n" +
                    "[bold red]Prerequisites:[/]\n" +
                    $" - HP 7090A plotter connected via GPIB (current address: [cyan]{currentGpibAddress}[/])\n" +
                    " - Paper loaded in the plotter\n" +
                    " - All 6 pens installed (required for HP 7090A)\n"
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
            try
            {
                // Initialize GPIB communication
                InitializeGpibConnection(gpibAddress);
                
                Console.WriteLine("Ensure paper is loaded");
                Console.WriteLine($"Connecting to HP 7090A at GPIB address {gpibAddress}...");

                // Execute the main plotting sequence (Table 4-3 flow: reads parameters inline, then draws)
                ExecutePlottingSequence();
                
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
                System.Threading.Thread.Sleep(MessageDisplayDurationMs); // Pause for a moment to let the user see the message

                return newAddress;
            }
            catch (OperationCanceledException)
            {
                AnsiConsole.MarkupLine("[yellow]GPIB address change cancelled. Keeping current address.[/]");
                System.Threading.Thread.Sleep(MessageDisplayDurationMs);
                return currentAddress;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Unexpected error setting GPIB address: {ex}[/]");
                System.Threading.Thread.Sleep(MessageDisplayDurationMs);
                throw;
            }
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Validates that the GPIB session is initialized and ready for communication.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when GPIB session is not initialized</exception>
        private static void ValidateGpibSession()
        {
            if (gpibSession == null)
            {
                throw new InvalidOperationException("GPIB session is not initialized. Call InitializeGpibConnection first.");
            }
        }

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
            // Validate GPIB address range
            if (gpibAddress < GpibAddressMin || gpibAddress > GpibAddressMax)
            {
                throw new ArgumentOutOfRangeException(nameof(gpibAddress), 
                    $"GPIB address must be between {GpibAddressMin} and {GpibAddressMax}");
            }

            // Setup the GPIB connection via the ResourceManager
            resManager = new NationalInstruments.Visa.ResourceManager();

            // Create a GPIB session for the specified address
            string gpibResourceName = string.Format(GpibResourceNameFormat, gpibAddress);
            gpibSession = (GpibSession)resManager.Open(gpibResourceName);
            
            // Set timeout for plotting operations (Table 4-3 uses single timeout)
            gpibSession.TimeoutMilliseconds = ExtendedTimeoutMs;
            gpibSession.TerminationCharacterEnabled = true;
            
            // Clear the session to ensure clean state
            gpibSession.Clear();
            
            Console.WriteLine($"Successfully connected to {gpibResourceName}");
        }

        /// <summary>
        /// Cleans up GPIB resources and closes the connection.
        /// Ensures proper disposal of resources even if exceptions occur.
        /// </summary>
        private static void CleanupGpibConnection()
        {
            if (gpibSession != null)
            {
                try
                {
                    gpibSession.Dispose();
                    Console.WriteLine("GPIB session closed.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error closing GPIB session: {ex.Message}");
                }
                finally
                {
                    gpibSession = null;
                }
            }
            
            if (resManager != null)
            {
                try
                {
                    resManager.Dispose();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Warning: Error disposing resource manager: {ex.Message}");
                }
                finally
                {
                    resManager = null;
                }
            }
        }

        #endregion

        #region Plotting Sequence

        /// <summary>
        /// Executes the main plotting sequence with all test patterns and features.
        /// Follows the program flow from Table 4-3: initializes plotter, reads parameters inline, then draws.
        /// This includes drawing test patterns, pen repeatability tests, and various geometric shapes.
        /// </summary>
        private static void ExecutePlottingSequence()
        {
            // Validate GPIB session is initialized
            ValidateGpibSession();

            // Variables to hold plotter coordinates (Table 4-3 lines 1640-1680)
            int hardClipLowerLeftX = 0, hardClipLowerLeftY = 0, hardClipUpperRightX = 0, hardClipUpperRightY = 0;
            int outputWindowLowerLeftX = 0, outputWindowLowerLeftY = 0, outputWindowUpperRightX = 0, outputWindowUpperRightY = 0;

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

                    // INITIALIZE 7090A & OUTPUT P1, P2 & WINDOW COORDINATES (Table 4-3 lines 1640-1680)
                    task.Description = "[cyan]Initializing and reading parameters[/]";
                    
                    try
                    {
                        // PG IN OP - Page feed, Initialize, Output P1 and P2 (Table 4-3 line 1640)
                        gpibSession.FormattedIO.WriteLine("PG;IN;OP;");
                        string hardClipResponse = gpibSession.FormattedIO.ReadString();
                        
                        if (string.IsNullOrWhiteSpace(hardClipResponse))
                        {
                            throw new InvalidOperationException("Failed to read P1/P2 coordinates - empty response");
                        }
                        
                        // Parse P1, P2 coordinates (Table 4-3 line 1650: ENTER N; X1,Y1,X2,Y2)
                        string[] values = hardClipResponse.Split(',');
                        
                        if (values.Length < ExpectedCoordinateCount)
                        {
                            throw new InvalidOperationException($"Invalid P1/P2 response - expected {ExpectedCoordinateCount} values, got {values.Length}");
                        }
                        
                        hardClipLowerLeftX = int.Parse(values[0]);
                        hardClipLowerLeftY = int.Parse(values[1]);
                        hardClipUpperRightX = int.Parse(values[2]);
                        hardClipUpperRightY = int.Parse(values[3]);
                        
                        // OW - Output Window (Table 4-3 line 1660)
                        gpibSession.FormattedIO.WriteLine("OW;");
                        string outputWindowResponse = gpibSession.FormattedIO.ReadString();
                        
                        if (string.IsNullOrWhiteSpace(outputWindowResponse))
                        {
                            throw new InvalidOperationException("Failed to read OW coordinates - empty response");
                        }
                        
                        // Parse window coordinates (Table 4-3 line 1670: ENTER N; X3,Y3,X4,Y4)
                        values = outputWindowResponse.Split(',');
                        
                        if (values.Length < ExpectedCoordinateCount)
                        {
                            throw new InvalidOperationException($"Invalid OW response - expected {ExpectedCoordinateCount} values, got {values.Length}");
                        }
                        
                        outputWindowLowerLeftX = int.Parse(values[0]);
                        outputWindowLowerLeftY = int.Parse(values[1]);
                        outputWindowUpperRightX = int.Parse(values[2]);
                        outputWindowUpperRightY = int.Parse(values[3]);
                    }
                    catch (FormatException ex)
                    {
                        throw new InvalidOperationException("Failed to parse plotter coordinates - invalid number format", ex);
                    }

                    // DRAW+ AT P1 & P2 & LABEL COORDINATES (Table 4-3 lines 1690-1740)
                    task.Description = "[cyan]Drawing coordinate labels[/]";
                    // SP1: Select pen 1, PA: Plot Absolute, PD: Pen Down, SM+: Symbol mode plus, PU: Pen Up (Table 4-3)
                    gpibSession.FormattedIO.WriteLine($"SP1;PA5080,4064;PD;PU;SM+;PA{hardClipLowerLeftX},{hardClipLowerLeftY}");
                    // CP: Character Plot with offset, LB: Label with text (Table 4-3)
                    gpibSession.FormattedIO.WriteLine($"CP0.1,-1.3;LBP1=({hardClipLowerLeftX},{hardClipLowerLeftY}){EndOfTextChar}");
                    gpibSession.FormattedIO.WriteLine($"PA{hardClipUpperRightX},{hardClipUpperRightY};SM;");
                    gpibSession.FormattedIO.WriteLine($"CP-14,-1.5;LBP2=({hardClipUpperRightX},{hardClipUpperRightY}){EndOfTextChar}");
                    task.Increment(ProgressCoordinateLabels);

                    // Pen repeatability tests at various positions (Table 4-3 coordinates)
                    // Tests are labeled to show pairs at same location for repeatability verification
                    task.Description = "[cyan]Drawing pen repeatability tests (1)[/]";
                    gpibSession.FormattedIO.WriteLine("PA2022,2464;");
                    PenRepeatabilityType1(1); // Left-bottom, first test (pair 1/3)
                    gpibSession.FormattedIO.WriteLine("PA8088,4664;");
                    PenRepeatabilityType2(1); // Right-middle, pen 1 of pair 5/1

                    // FT4: Fill type 4, RR: Rectangle Relative, SP2: Select pen 2, ER: Edge Rectangle Relative
                    gpibSession.FormattedIO.WriteLine("FT4,100,45;PA9372,6440;RR700,700;SP2;ER700,700");
                    task.Increment(ProgressPenRepeatability);

                    // DRAW & LABEL AXIS (Table 4-3 coordinates)
                    task.Description = "[cyan]Drawing axis grid[/]";
                    gpibSession.FormattedIO.WriteLine("SP2;PA9184,1416;PD;");
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

                    // More pen repeatability tests (Table 4-3 coordinates)
                    task.Description = "[cyan]Drawing pen repeatability tests (2)[/]";
                    gpibSession.FormattedIO.WriteLine("PU;PA2022,4664;");
                    PenRepeatabilityType1(2); // Left-middle, first test (pair 2/4)
                    gpibSession.FormattedIO.WriteLine("PA8088,6864;");
                    PenRepeatabilityType2(2); // Right-top, pen 2 of pair 6/2

                    // WG: Wedge, EW: Edge Wedge
                    gpibSession.FormattedIO.WriteLine("FT4,50,90;PA9722,5600;WG350,0,360,40;SP3;EW350,0,360,40;");
                    task.Increment(ProgressPenRepeatability);
                    
                    // Draw "Y Axis" label vertically (Table 4-3)
                    // DI: Direction for text (0,1 = vertical)
                    task.Description = "[cyan]Drawing axis labels[/]";
                    gpibSession.FormattedIO.WriteLine($"SP3;PA600,4000;DI0,1;LBY Axis{EndOfTextChar};");
                    gpibSession.FormattedIO.WriteLine("PA700,7366;DI;"); // Reset direction

                    // Draw Y-axis scale labels (15 down to 1)
                    for (int i = 15; i > 0; i--)
                    {
                        if (i < 10)
                        {
                            gpibSession.FormattedIO.WriteLine("CP1,0;"); // Adjust character position for single digit
                        }

                        gpibSession.FormattedIO.WriteLine($"LB{i}{CarriageReturnChar}{EndOfTextChar};PR0,-400;");
                    }
                    task.Increment(ProgressAxisLabels);

                    // Continue with more test patterns (Table 4-3 coordinates)
                    task.Description = "[cyan]Drawing pen repeatability tests (3)[/]";
                    gpibSession.FormattedIO.WriteLine("PA2022,6864;");
                    PenRepeatabilityType1(3); // Left-top, first test (pair 3/5)
                    gpibSession.FormattedIO.WriteLine("PA2022,2464;");
                    PenRepeatabilityType2(3); // Left-bottom, second test (pair 1/3)

                    // UF: User-defined Fill, PT: Pen Thickness
                    gpibSession.FormattedIO.WriteLine("UF10,5,5;FT5;PA9722,4060;PT.5;WG700,60,60;");
                    
                    // Draw X-axis scale labels (0 through 8) - Table 4-3
                    gpibSession.FormattedIO.WriteLine("PA1032,1156;SP4;");

                    for (int i = 0; i < 9; i++)
                    {
                        gpibSession.FormattedIO.WriteLine($"LB{i}{CarriageReturnChar}{EndOfTextChar};PR1016,0;");
                    }

                    gpibSession.FormattedIO.WriteLine($"PA4830,1116;LBX Axis{EndOfTextChar}");
                    task.Increment(ProgressPenRepeatability);
                    
                    // More repeatability tests (Table 4-3 coordinates)
                    task.Description = "[cyan]Drawing pen repeatability tests (4)[/]";
                    gpibSession.FormattedIO.WriteLine("PA8088,2464;");
                    PenRepeatabilityType1(4); // Right-bottom, first test (pair 4/6)
                    gpibSession.FormattedIO.WriteLine("PA2022,4664;");
                    PenRepeatabilityType2(4); // Left-middle, second test (pair 2/4)

                    // DRAW CIRCULAR FAN (Table 4-3)
                    task.Description = "[cyan]Drawing circular fan pattern[/]";
                    // Position at center for circular fan
                    gpibSession.FormattedIO.WriteLine($"SP4;PA{CircleCenterX},{CircleCenterY};");
                    // Set input window and draw box (Table 4-3 line 2050-2060)
                    gpibSession.FormattedIO.WriteLine("SP1;IW3580,2564,6580,5564;PA3580,2564;");
                    gpibSession.FormattedIO.WriteLine("PD;PR0,3000,3000,0,0,-3000,-3000,0;PU;SP5;");
                    task.Increment(ProgressCircularFan);

                    // Draw radial lines from circle center (Table 4-3: loop 0 to 355 step 5, lines 2070-2090)
                    task.Description = "[cyan]Drawing radial lines[/]";
                    // Conversion factor from degrees to radians for trigonometric functions
                    double degreesToRadiansConversion = Math.PI / 180.0;

                    for (int i = 0; i <= 355; i += 5)
                    {
                        double radians = i * degreesToRadiansConversion;

                        // Calculate the edge of the inner circle using InnerCircleRadius (400)
                        int innerCircleLineX = (int)Math.Round(CircleCenterX + InnerCircleRadius * Math.Cos(radians));
                        int innerCircleLineY = (int)Math.Round(CircleCenterY + InnerCircleRadius * Math.Sin(radians));
                        
                        // Calculate the outer edge using OuterCircleRadius (2200)
                        int outerCircleLineX = (int)Math.Round(CircleCenterX + OuterCircleRadius * Math.Cos(radians));
                        int outerCircleLineY = (int)Math.Round(CircleCenterY + OuterCircleRadius * Math.Sin(radians));

                        // Table 4-3 lines 2080-2090: PA to inner with PD, then PA to outer with PU (draws line from inner to outer)
                        gpibSession.FormattedIO.WriteLine($"PA{innerCircleLineX},{innerCircleLineY};PD;PA{outerCircleLineX},{outerCircleLineY};PU;");
                    }

                    // IW: Reset Input Window (Table 4-3 coordinates)
                    gpibSession.FormattedIO.WriteLine("IW;PA8088,4664;");
                    PenRepeatabilityType1(5); // Right-middle, pen 5 of pair 5/1
                    gpibSession.FormattedIO.WriteLine("PA2022,6864;");
                    PenRepeatabilityType2(5); // Left-top, second test (pair 3/5)
                    task.Increment(ProgressRadialLines);

                    // DRAW LABELS (Table 4-3 coordinates)
                    task.Description = "[cyan]Drawing title labels[/]";
                    // VS: Velocity Select, SI: Absolute Character Size, SL: Slant
                    gpibSession.FormattedIO.WriteLine("SP6;PA3610,6800;");
                    gpibSession.FormattedIO.WriteLine($"VS;SI1,1.5;SL0.27;LB7090A{EndOfTextChar};");
                    gpibSession.FormattedIO.WriteLine("PA2900,6300;");
                    gpibSession.FormattedIO.WriteLine($"SI.23,.34;SL;LBPlotter Performance Verification{EndOfTextChar};");
                    
                    // Final repeatability tests (Table 4-3 coordinates)
                    gpibSession.FormattedIO.WriteLine("PA8088,6864;");
                    PenRepeatabilityType1(6); // Right-top, pen 6 of pair 6/2
                    gpibSession.FormattedIO.WriteLine("PA8088,2464;");
                    PenRepeatabilityType2(6); // Right-bottom, second test (pair 4/6)
                    task.Increment(ProgressTitleLabels);

                    // CHARACTER SET DISPLAY (Table 4-3 lines 2420-2460)
                    task.Description = "[cyan]Drawing character set[/]";
                    gpibSession.FormattedIO.Write("PA300,600;SR0.7,1.5;SL;LB");
                    // Display ASCII characters from 33 (!) to 127 (~)
                    for (int i = 33; i <= 127; i++)
                    {
                        gpibSession.FormattedIO.Write(((char)i).ToString());
                    }
                    gpibSession.FormattedIO.WriteLine($"{EndOfTextChar};SI;");

                    // FRAME WINDOW (Table 4-3 lines 2500-2590)
                    // Draw nested rectangles using PA/PD/VS commands (Table 4-3 loop 1 to 4)
                    task.Description = "[cyan]Drawing frame window[/]";
                    gpibSession.FormattedIO.WriteLine($"PA{CircleCenterX},{CircleCenterY};PD;PU;");
                    
                    // Draw nested rectangles with varying velocities (Table 4-3 loop 1 to 4)
                    int x3 = outputWindowLowerLeftX;
                    int y3 = outputWindowLowerLeftY;
                    int x4 = outputWindowUpperRightX;
                    int y4 = outputWindowUpperRightY;
                    
                    for (int i = 1; i <= 4; i++)
                    {
                        gpibSession.FormattedIO.WriteLine($"PA{x3},{y3};VS{i*10};PD;PA{x3},{y4},{x4},{y4},{x4},{y3},{x3},{y3};");
                        x3 += 25;
                        y3 += 25;
                        x4 -= 25;
                        y4 -= 25;
                    }
                    
                    // DEADBAND TESTS (Table 4-3 lines 2590, 2790-3090)
                    task.Description = "[cyan]Drawing deadband tests[/]";
                    gpibSession.FormattedIO.WriteLine("PU;");  // Pen up before starting deadband tests
                    DrawDeadbandTests();
                    
                    // PEN WOBBLE TEST (Table 4-3 lines 2600, 3280-3490)
                    task.Description = "[cyan]Drawing pen wobble test[/]";
                    DrawPenWobbleTest();
                    
                    // Final positioning: select pen 0 and move to upper right (Table 4-3 line 2610)
                    gpibSession.FormattedIO.WriteLine($"SP0;PA{outputWindowUpperRightX},{outputWindowUpperRightY};");
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
        /// <param name="pass">Pass number to label the test (must be positive)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pass is not positive</exception>
        private static void PenRepeatabilityType1(int pass)
        {
            ValidateGpibSession();
            
            if (pass <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pass), "Pass number must be positive");
            }

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
        /// <param name="pass">Pass number to label the test (must be positive)</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when pass is not positive</exception>
        private static void PenRepeatabilityType2(int pass)
        {
            ValidateGpibSession();
            
            if (pass <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(pass), "Pass number must be positive");
            }

            // CP: Set character position, LB: Label with pass number
            gpibSession.FormattedIO.WriteLine($"CP.4,-.8;LB{pass}{EndOfTextChar};CP-1.4,.8;");
            // Draw a cross: vertical line down then up, horizontal line right
            gpibSession.FormattedIO.WriteLine("PR0,512;PD0,-1024;PU-512,512;PD1024,0;PU;");
        }

        #endregion

        #region Deadband and Pen Wobble Tests

        /// <summary>
        /// Draws deadband test scales at various positions (Table 4-3 lines 2790-3090).
        /// Tests the pen deadband (ability to start/stop precisely).
        /// </summary>
        private static void DrawDeadbandTests()
        {
            ValidateGpibSession();

            // First deadband test - horizontal scale at bottom (Table 4-3 lines 2820-2870)
            DrawDeadbandScale(4280, 1750, true, 0, 0);
            
            // Second deadband test - horizontal scale at bottom (Table 4-3 lines 2880-2920)
            DrawDeadbandScale(5880, 1952, true, 2, 0);
            
            // Draw horizontal line with arrow between scales (Table 4-3 lines 2930-2950)
            // Line from (4680,2200) to (5480,2200) with arrow at center pointing left
            gpibSession.FormattedIO.WriteLine("PA4680,2200;PD;PA5480,2200;PU;");
            gpibSession.FormattedIO.WriteLine("PA5080,2175;DI-1,0;CS1;CP-.33,-.75;");
            gpibSession.FormattedIO.WriteLine($"LB{(char)94}{EndOfTextChar}"); // CHR$(94) is ^ arrow character
            
            // Third deadband test - vertical scale on left (Table 4-3 lines 2960-3010)
            DrawDeadbandScale(3210, 3200, false, 0, 0);
            
            // Fourth deadband test - vertical scale on left (Table 4-3 lines 3020-3060)
            DrawDeadbandScale(2998, 4800, false, 0, 2);
            
            // Draw vertical line with arrow between scales (Table 4-3 lines 3070-3090)
            // Line from (2950,4400) to (2950,3600) with arrow at center pointing down
            gpibSession.FormattedIO.WriteLine("PA2950,4400;PD;PA2950,3600;PU;");
            gpibSession.FormattedIO.WriteLine($"PA2975,4000;DI0,-1;CP-.33,-.75;LB{(char)94}{EndOfTextChar}");
        }

        /// <summary>
        /// Draws a deadband test scale at specified position (Table 4-3 lines 3130-3270).
        /// Deadband testing verifies pen positioning accuracy during start/stop operations.
        /// </summary>
        /// <param name="x1">Starting X coordinate</param>
        /// <param name="y1">Starting Y coordinate</param>
        /// <param name="isHorizontal">True for horizontal scale, false for vertical</param>
        /// <param name="offsetM">Scale offset M parameter - when both M and L are 0, scale draws forward; otherwise backward</param>
        /// <param name="offsetL">Scale offset L parameter - when both M and L are 0, scale draws forward; otherwise backward</param>
        private static void DrawDeadbandScale(int x1, int y1, bool isHorizontal, int offsetM, int offsetL)
        {
            ValidateGpibSession();

            // Calculate scale direction multiplier based on offsets (Table 4-3 lines 3130-3140)
            int scaleDirection = (offsetM == 0 && offsetL == 0) ? 1 : -1;
            
            // Calculate increment values for horizontal or vertical orientation (Table 4-3 lines 3160-3200)
            int xIncrement = isHorizontal ? 200 : 0;
            int yIncrement = isHorizontal ? 0 : 200;
            
            // Draw 9 tick marks along the scale (Table 4-3 lines 3210-3260)
            for (int tickIndex = 1; tickIndex <= 9; tickIndex++)
            {
                int x2 = x1 + scaleDirection * ((tickIndex - 1) * xIncrement + offsetM * (5 - tickIndex));
                int y2 = y1 + scaleDirection * ((tickIndex - 1) * yIncrement + offsetL * (5 - tickIndex));
                
                // Move to tick position and draw perpendicular tick mark
                gpibSession.FormattedIO.WriteLine($"PA{x2},{y2};PD;PA{x2 + yIncrement},{y2 + xIncrement};PU;");
            }
        }

        /// <summary>
        /// Draws pen wobble test pattern (Table 4-3 lines 3280-3490).
        /// Tests pen stability during rapid direction changes by creating a zigzag pattern.
        /// </summary>
        private static void DrawPenWobbleTest()
        {
            ValidateGpibSession();

            // Start position at right side of plot (Table 4-3 line 3310)
            // Position (10200, 1450) with pen down, velocity select, then PR (plot relative) mode
            gpibSession.FormattedIO.Write("SP1;PA10200,1450;PD;VS;PR");
            
            // First loop: draw zigzag pattern moving up-left (Table 4-3 lines 3340-3360)
            for (int i = 1; i <= 10; i++)
            {
                gpibSession.FormattedIO.Write($"{WobbleTestHorizontalOffset},{WobbleTestZigzagAmplitude},-{WobbleTestZigzagAmplitude},{WobbleTestHorizontalOffset},");
            }
            
            // Second loop: continue zigzag moving up-right (Table 4-3 lines 3370-3390)
            for (int i = 1; i <= 10; i++)
            {
                gpibSession.FormattedIO.Write($"{WobbleTestHorizontalOffset},{WobbleTestZigzagAmplitude},{WobbleTestZigzagAmplitude},{WobbleTestHorizontalOffset},");
            }
            
            // Move and continue pattern (Table 4-3 line 3400)
            gpibSession.FormattedIO.Write("0,200;PU;PR15,-15;PD;PR");
            
            // Third loop: zigzag moving down-left (Table 4-3 lines 3410-3430)
            for (int i = 1; i <= 9; i++)
            {
                gpibSession.FormattedIO.Write($"{WobbleTestHorizontalOffset},-{WobbleTestZigzagAmplitude},-{WobbleTestZigzagAmplitude},{WobbleTestHorizontalOffset},");
            }
            
            // Final movement (Table 4-3 line 3440)
            gpibSession.FormattedIO.Write("0,-200,-200,0,0,-170,200,0,");
            
            // Fourth loop: zigzag moving down-right (Table 4-3 lines 3450-3470)
            for (int i = 1; i <= 9; i++)
            {
                gpibSession.FormattedIO.Write($"{WobbleTestHorizontalOffset},-{WobbleTestZigzagAmplitude},{WobbleTestZigzagAmplitude},{WobbleTestHorizontalOffset},");
            }
            
            // End position (Table 4-3 line 3480)
            gpibSession.FormattedIO.WriteLine("0,-200;PU;");
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
