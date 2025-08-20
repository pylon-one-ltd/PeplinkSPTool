using Serilog;
using System.Diagnostics;
using PeplinkSPTool.Classes;
using System.Runtime.InteropServices;

namespace PeplinkSPTool
{
    internal class Program
    {
        internal const string Bind = "http://127.0.0.1:5999";
        internal static string? PeplinkChallengeCode = null;
        
        private static async Task Main(string[] args)
        {
            var configuration = InitialisationHandler.LoadConfiguration(args);
            if( configuration == null )
            {
                Console.WriteLine("Error: Configuration file is missing. Please ensure the application has access to appsettings.json either in the binary directory or the direct parent directory.");
                Console.WriteLine("If the configuration file cannot be discovered automatically, you can specify it manually with the --config command line parameter");
                Environment.Exit(2);
            }
        
            try
            {
                Log.Logger = InitialisationHandler.InitLogger(configuration, "PeplinkSPTool");
                
                // Build auth path
                var APIEndpoint = configuration.GetSection("PeplinkSPTool").GetValue<string>("Endpoint");
                var ClientID = configuration.GetSection("PeplinkSPTool").GetValue<string>("ClientID");
                if( string.IsNullOrEmpty(ClientID) || string.IsNullOrEmpty(APIEndpoint) )
                {
                    Console.WriteLine("Please ensure ClientID and APIEndpoint are set properly.");
                    Environment.Exit(2);
                }
                
                // Start callback webserver
                var WebRunner = InitialisationHandler.CreateWebHostBuilder<Startup>($"{Bind}/", configuration).Build();
                _ = WebRunner.RunAsync();
                
                var AuthString = $"{APIEndpoint}/api/oauth2/auth?client_id={ClientID}&response_type=code&redirect_uri={Bind}/reply";
                Console.WriteLine("Please perform authentication with InControl using your browser. Your browser will now open.");
                Console.WriteLine("If it does not, the URL you need to visit is:");
                Console.WriteLine(AuthString);
                OpenUrl(AuthString);
                
                // Wait for callback
                while( string.IsNullOrEmpty(PeplinkChallengeCode) )
                {
                    Thread.Sleep(500);
                }
                
                // Shutdown webserver
                await WebRunner.StopAsync();
                var peplinkAPI = new PeplinkAPI(configuration);
                
                // Display root menu
                string? OrganisationName = null;
                string? OrganisationID = null;
                var IsRunning = true;
                while( IsRunning )
                {
                    if( string.IsNullOrEmpty(OrganisationID) )
                    {
                        Console.Clear();
                        Console.WriteLine("Please select an organisation:");
                        
                        var Organisations = await peplinkAPI.GetOrganisations();
                        for( var i=0; i < Organisations.Length; i++ )
                        {
                            Console.WriteLine($"    [{i+1:D2}] {Organisations[i].id} - {Organisations[i].name}");
                        }
                        
                        Console.WriteLine("");
                        Console.WriteLine("Please enter the number for the org you would like to use:");
                        if( int.TryParse(Console.ReadLine(), out var selectedID) )
                        {
                            if( selectedID < 0 || selectedID > Organisations.Length ) continue;
                            OrganisationName = Organisations[selectedID-1].name;
                            OrganisationID = Organisations[selectedID-1].id;
                        }
                    }
                    if( !string.IsNullOrEmpty(OrganisationID) )
                    {
                        Console.Clear();
                        Console.WriteLine($"Organisation: {OrganisationName} ({OrganisationID})");
                        Console.WriteLine("");
                        Console.WriteLine("Main Menu");
                        Console.WriteLine("    [00] Exit Application");
                        Console.WriteLine("    [01] Return to Organisation Selection");
                        Console.WriteLine("    [02] Configure Service Provider Default");
                        
                        Console.WriteLine("");
                        Console.WriteLine("Please enter the number for the org you would like to use:");
                        if( int.TryParse(Console.ReadLine(), out var selectedID) )
                        {
                            switch( selectedID )
                            {
                                case 0:
                                    IsRunning = false;
                                    break;
                                case 1:
                                    OrganisationName = null;
                                    OrganisationID = null;
                                    break;
                                case 2:
                                    await ServiceProviderDefault(peplinkAPI, OrganisationID);
                                    break;
                            }
                        }
                    }
                }
            }
            catch( Exception Ex )
            {
                Log.Fatal(Ex, "Fatal error occurred");
            }
            finally
            {
                await Log.CloseAndFlushAsync();
            }
        }
        
        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(url);
            }
            catch
            {
                // hack because of this: https://github.com/dotnet/corefx/issues/10361
                if( RuntimeInformation.IsOSPlatform(OSPlatform.Windows) )
                {
                    url = url.Replace("&", "^&");
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else if( RuntimeInformation.IsOSPlatform(OSPlatform.Linux) )
                {
                    Process.Start("xdg-open", url);
                }
                else if( RuntimeInformation.IsOSPlatform(OSPlatform.OSX) )
                {
                    Process.Start("open", url);
                }
                else
                {
                    throw;
                }
            }
        }
        
        private static async Task ServiceProviderDefault(PeplinkAPI peplinkAPI, string OrganisationID)
        {
            string? GroupName = null;
            long? GroupID = null;
            var IsInMenu = true;
            while( IsInMenu )
            {
                if( GroupID == null )
                {
                    Console.Clear();
                    Console.WriteLine($"Update Service Provider Default ({OrganisationID})");
                    Console.WriteLine("");
                    Console.WriteLine("Please select a group:");
                    Console.WriteLine("    [00] Return to Main Menu");
                    var Groups = await peplinkAPI.GetGroups(OrganisationID);
                    for( var i=0; i < Groups.Length; i++ )
                    {
                        Console.WriteLine($"    [{i+1:D2}] {Groups[i].name}");
                    }
                    
                    Console.WriteLine("");
                    Console.WriteLine("Please enter the number for the group you would like to use:");
                    if( int.TryParse(Console.ReadLine(), out var selectedID) )
                    {
                        if( selectedID < 0 || selectedID > Groups.Length ) continue;
                        if( selectedID == 0 )
                        {
                            IsInMenu = false;
                        }
                        else
                        {
                            GroupName = Groups[selectedID-1].name;
                            GroupID = Groups[selectedID-1].id;
                        }
                    }
                }
                if( GroupID != null )
                {
                    Console.Clear();
                    Console.WriteLine($"Update Service Provider Default ({OrganisationID}) - {GroupName}");
                    Console.WriteLine("");
                    Console.WriteLine("Please select devices:");
                    var Devices = await peplinkAPI.GetDevices(OrganisationID, (long)GroupID);
                    for( var i=0; i < Devices.Length; i++ )
                    {
                        Console.Write($"    [{i+1:D2}][");
                        if( Devices[i].sp_default )
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write("  LOCKED");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write("UNLOCKED");
                        }
                        
                        Console.ResetColor();
                        Console.WriteLine($"] {Devices[i].sn} - {Devices[i].name}");
                    }
                    
                    Console.WriteLine("");
                    Console.WriteLine("(To exit the menu, press enter with no input)");
                    Console.WriteLine("Please enter a list of devices you would like to update as a comma seperated list (EG, 1,4,6):");
                    var ReadInput = Console.ReadLine();
                    if( string.IsNullOrWhiteSpace(ReadInput) )
                    {
                        GroupName = null;
                        GroupID = null;
                    }
                    else
                    {
                        List<string> DeviceNames = [];
                        List<long> DeviceIDs = [];
                        var ArraySplit = ReadInput.Split(',');
                        foreach( var Item in ArraySplit )
                        {
                            if( int.TryParse(Item, out var selectedID) )
                            {
                                if( selectedID <= 0 || selectedID > Devices.Length ) continue;
                                if( Devices[selectedID-1].id == null || Devices[selectedID-1].name == null ) continue;
                                DeviceIDs.Add((long)Devices[selectedID-1].id!);
                                DeviceNames.Add(Devices[selectedID-1].name!);
                            }
                        }
                        
                        // Return to menu if no valid devices selected
                        if( DeviceIDs.Count == 0 ) continue;
                        
                        // Determine operation
                        var DoLockDevices = false;
                        
                        Console.Clear();
                        Console.WriteLine($"Update Service Provider Default ({OrganisationID}) - {GroupName}");
                        Console.WriteLine("");
                        Console.WriteLine("Please select the operation you would like to perform:");
                        Console.WriteLine("    [00] Return to Main Menu");
                        Console.WriteLine("    [01] Lock Devices");
                        Console.WriteLine("    [02] Unlock Devices");
                        if( int.TryParse(Console.ReadLine(), out var selectedID1) )
                        {
                            if( selectedID1 is <= 0 or > 2 ) continue;
                            if( selectedID1 == 1 )
                            {
                                DoLockDevices = true;
                            }
                        }
                        
                        // Perform Operation
                        Console.Clear();
                        Console.WriteLine($"Update Service Provider Default ({OrganisationID}) - {GroupName}");
                        Console.WriteLine("");
                        Console.Write("You will now ");
                        if( DoLockDevices )
                        {
                            Console.ForegroundColor = ConsoleColor.DarkRed;
                            Console.Write("LOCK");
                        }
                        else
                        {
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.Write("UNLOCK");
                        }
                        
                        Console.ResetColor();
                        Console.WriteLine(" the following devices:");
                        foreach( var Device in DeviceNames )
                        {
                            Console.WriteLine(Device);
                        }
                        
                        Console.WriteLine("");
                        Console.WriteLine("To confirm the operation, please type YES in capitals. Any other input will exit");
                        if( Console.ReadLine() == "YES" )
                        {
                            try
                            {
                                var Response = await peplinkAPI.SetDeviceSPStatus(OrganisationID, (long)GroupID, DeviceIDs, DoLockDevices);
                                if( string.IsNullOrEmpty(Response) )
                                {
                                   Console.ForegroundColor = ConsoleColor.DarkRed;
                                   Console.WriteLine("Failed to update SP Status. No Ticket ID returned. Press any key to continue.");
                                }
                                else
                                {
                                   Console.ForegroundColor = ConsoleColor.DarkGreen;
                                   Console.WriteLine("SP Status updated successfully. Press any key to continue.");
                                }
                            }
                            catch( Exception Ex )
                            {
                                Log.Error(Ex, "Error with SP update");
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine($"Failed to update SP Status. {Ex.Message}. Press any key to continue.");
                            }
                           
                            Console.ReadLine();
                            Console.ResetColor();
                        }
                    }
                }
            }
        }
    }
}