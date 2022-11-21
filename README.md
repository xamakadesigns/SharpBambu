# SharpBambu
## C# .Net Wrapper for Bambu Lab 3d Printers 

This project is in **no way related or supported by Bambu Lab**. It is being developed for .Net developers wishing to interface with Bambu 3d printers (X1, X1-Carbon or P1P).

- Project is in a very incomplete state.
- Only Windows OS is being targetted for support at this time.

## About bambu_networking.dll (Bambu Lab Network Plugin)
bambu_networking.dll is a closed source project maintained by Bambu Lab. It is a "plugin" for Bambu Studio (an open source project based on slic3r). Therefore most of the communication with the printer is relayed via the cloud using this plugin dll, and the implementation details are not disclosed. The expectation is that Bambu Slicer (or another application) will access the cloud services using the network plugin dll.

# Getting started

You will need:
- Bambu Studio - the network plugin is not included with Bambu Studio. It is downloaded during installation. After installing Bambu Studio, the plugin will be found at: 
%appdata%\BambuStudio\plugins\bambu_networking.dll
- Visual Studio Community 2022 with C++ and .Net components installed

The project:
- NetworkPluginWrapper - this is a C++ wrapper for bambu_networking.dll to handle implementation details / type conversions needed for interfacing with C#. The data types used by C++ are not directly compatible with .Net types. Type marshalling is necessary to "transform" the data types between the two languages and handle memory allocation/deallocation for non-managed code.
- SharpBambuPlugin - implementation details for simplifying communication with the printer.
- SharpBambuTestApp - console application for testing the network plugin implementation.

Compilation:
- Project and dll is 64 bit to match existing bambu_networking.dll
- The NetworkPluginWrapper.dll is compiled first and copied via post build event to the SharpBambuTestApp binaries folder.

Startup:
- SharpBambuTestApp binaries folder should be writable.
- bambu_networking.dll is copied from the Bambu Studio folder to the SharpBambuTestApp binaries folder.
- log folder is created by bambu_networking.dll in the SharpBambuTestApp binaries folder.
- Empty BambuNetworkEngine.conf is saved/created in the SharpBambuTestApp binaries folder. You may find it helpful to copy this file from Bambu Studio after logging into the application so you can test in relative isolation to Bambu Studio but the application presently interchanges between the two locations.

# What's working
- The bambu_networking.dll loads okay.
- The agent is created / destroyed.
- Possible to call get_version() (but it returns 00.00.00.00)

# What's not
- set_config_dir throws an exception
- get_user_id throws an exception
- Seeing some memory access / corruption issues indicating that things are either being called in the wrong order, marshalled incorrectly, or some other state related issue with the wrapper dll.
- Possibly a missed initialization call or registry implementation detail that has been ignored...

# Goals
- Able to log in
- Able to establish a connection with the printer
- Able to retrieve printer status such as progress or temperatures
- Able to issue movement commands
- Able to get the camera feed url
- Able to issue commands to the wrapper via PowerShell (such as start a print job or load/unload the AMS)

# Contact
- Want to help? I would appreciate a hand with sorting out the type marshalling and implementation details.
- xamaka@cnrdesigns.com 
