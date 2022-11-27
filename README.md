# SharpBambu
### C# .Net Wrapper for Bambu Lab 3d Printers 

This project is in **no way related or supported by Bambu Lab**. It is being developed for .Net developers wishing to interface with Bambu 3d printers (X1, X1-Carbon or P1P).

- Project is subject to lots of changes while I finish the .Net wrapper functions
- Only Windows OS is being targetted for support at this time.

### About bambu_networking.dll (Bambu Lab Network Plugin)
bambu_networking.dll is a closed source project maintained by Bambu Lab. It is a "plugin" for [Bambu Studio](https://github.com/bambulab/BambuStudio) (an open source project based on slic3r). Therefore most of the communication with the printer is relayed via the cloud using this plugin dll, and the implementation details are not disclosed. The expectation is that Bambu Slicer (or another application) will access the cloud services using the network plugin dll.

# Getting started
- Install Visual Studio Community 2022 with C++ (MSVC 143) and .Net components
- Install Bambu Studio - the network plugin is not included with Bambu Studio. It is downloaded during installation.
  - After installing Bambu Studio, the plugin will be found at: 
%appdata%\BambuStudio\plugins\bambu_networking.dll
  - Log into your Bambu account
  - Bind the printer and confirm that the device status screen is connected and working
  - Exit Bambu Studio to save the networking config
- Build the project & restore nuget packages (follow steps in compilation, below)
  - Close and re-open the solution after the first successful build
  - This will "fix" the Visual Studio syntax checker and get rid of the "red squigglies"

# The project
- **NetworkPluginWrapper** - this is a C++ wrapper for bambu_networking.dll to handle implementation details / type conversions needed for interfacing with C#. The data types used by C++ are not directly compatible with .Net types. Type marshalling is necessary to "transform" the data types between the two languages and handle memory allocation/deallocation for non-managed code.
- **SharpBambuPlugin** - implementation details for simplifying communication with the printer.
- **SharpBambuTestApp** - console application for testing the network plugin implementation.

# The particulars (why the wrapper is needed)
- C# has no std::string equivalent - these must be converted to some other data type before they can be easily consumed by .Net with P/Invoke.
- C++ classes do not map directly to C# and must be reworked as COM components (or flattened into normal function calls, my approach).
- The typedef callback functions defined with "std::function<void(xxxxxxx)>" must be wrapped/rewritten as regular function pointers since P/Invoke has no equivalent.

# Compilation
- Set solution config to Debug/x64 (but leave NetworkPluginWrapper on Release/x64).
- Set Startup Project to SharpBambuTestApp.
- Project and dll is 64 bit to match existing bambu_networking.dll (x64 "fastcall" convention).
- The NetworkPluginWrapper.dll is compiled first and copied via post build event to the SharpBambuTestApp binaries folder.
- The NetworkPluginWrapper.dll must be compiled in release mode to match bambu_networking.dll (or bad things will happen).

# Startup
- SharpBambuTestApp binaries folder should be writable.
- bambu_networking.dll (built in x64 Release mode) is copied from the Bambu Studio folder to the SharpBambuTestApp binaries folder.
- log folder is created by bambu_networking.dll in the SharpBambuTestApp binaries folder.
- Empty BambuNetworkEngine.conf is saved/created in the SharpBambuTestApp binaries folder. You may find it helpful to copy this file from Bambu Studio after logging into the application so you can test in relative isolation to Bambu Studio but the application presently interchanges between the two locations.

# What's working
- ✅ The bambu_networking.dll loads okay.
- ✅ The agent is created / destroyed.
- ✅ Possible to call get_version() (and it returns the version as expected when build is set to Release mode)
- ✅ Able to set_config_dir and read existing config established by Bambu Studio when the user logs in

# Fixed
- set_config_dir throws an exception (fixed ✅)
- get_user_id throws an exception (fixed ✅)
- Seeing some memory access / corruption issues indicating that things are either being called in the wrong order, marshalled incorrectly, or some other state related issue with the wrapper dll. (fixed ✅ - use release mode build for NetworkPluginWrapper.dll)

# Goals
- ✅ Able to log in with existing settings saved / previously generated by Bambu Studio
- ✅ Able to establish a connection with the printer (via cloud / MQTT)
- ⬜ Able to connect locally with the printer
- ✅ Able to retrieve printer status such as progress or temperatures
- ✅ Able to issue gcode commands
- ✅ Able to view AMS humidity level
- ✅ Able to wipe nozzle while printing (just a macro copied from Bambu Studio start gcode - please make your own, better one)
- ⬜ Able to get the camera feed url and/or establish a tunnel
- ⬜ Able to upload gcode to the printer and begin a print
- ⬜ Able to issue commands to the wrapper via PowerShell (such as start a print job or load/unload the AMS)
- ⬜ Create a C# library (refactor) and separate it from the test console app
- ⬜ Creation of a small status app for the printer
- ⬜ Json/MQTT message inspector
- ⬜ Support for Repetier
- ⬜ Support for Octoprint
- ⬜ Support for Simplify3d
- ⬜ Support for Cura

# Contact
- Want to help? I would appreciate a hand with sorting out implementation details or adding a bridge for other slicers.
- xamaka@cnrdesigns.com 

# Demos
* Issuing a few nozzle wipes while the print is running 

Note - this is copied from Bambu Studio startup gcode. Do this at your own risk. Please make a proper gcode!


https://user-images.githubusercontent.com/12724275/203712806-e3dbeacb-d423-43e6-b30e-385d2020bbfe.mp4

* Printer commands/temperature updates being returned
![image](https://user-images.githubusercontent.com/12724275/204118305-f457f1f5-cb6e-487b-acd2-6458f30ceec9.png)
