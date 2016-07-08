Theory of operation

RAC with two profiles:
	WPA2
	OPEN
	
Only one profile is enabled at a time (exclusive)

app watches suspend/resume
app watches docked/undocked
app watches connect/disconnect state (valid IP)

on resume (optional on undock too) the tool will check if connected with primary profile
	if not connected with primary profile the tool will switch to secondary profile. No further change.
	if connected with primary profile the tool will not change to the secondary profile

Installation
  copy rac_switch.cab to device and install using file explorer
  then do a warmboot to have rac_switch started via Windows\StartUp

Configuration
  see app.config file in program dir
  example:
		<?xml version="1.0" encoding="utf-8" ?>
		<configuration>
		  <appSettings>
			<add key="profile1" value="Fuhrpark_PDA" />
			<add key="profile2" value="MI-RC.51" />
			<add key="checkOnUndock" value="false" />
			<add key="checkOnResume" value="true" />
			<add key="checkConnectIP" value="false=" />
			<add key="checkConnectAP" value="true=" />
			<add key="switchOnDisconnect" value="false" />
			<add key="switchTimeout" value="60"/>
			<add key="enableLogging" value="false" />
		  </appSettings>
		</configuration>
  
  meanings:
    profile1 is the name of the preferred RAC profile (not the SSID inside)
    profile1 is the name of the secondary RAC profile (not SSID)
    [use xml/html escape for extra characters]
    checkOnUndock true/false enables switching try to 1st profile when undock is recognized
    checkOnResume true/false enables switching try to 1st profile when Resume is encountered
    switchTimeout is the maximum time in seconds a swithing try to 1st profile is run
      if switching from one RAC profile to another it will take some time depending on the
      network etc. until the device is connected. The tool will switch to first profile
      and checks for a connection unless the timeout has been reached or a connection has
      been identified. If no connect was possible, the 2nd profile is activated.
    enableLogging true/false enables logging of events to a file for further analysis 
    
Program Start
  The tool is started automatically during Windows startup.
  There is normally no main window, as the tool minimizes itself on start
  To show the main window either use "switch to" in windows mobile taskmanager application
      
Log file
	optional
	name: \RAC_switch.log.txt
	bak (2MB): \RAC_switch.log.txt.bak

History
	v001
		initial version
	v002
		changed logger.cs to delete bak file first before rename...
		changed logger.cs to check for file exist before checking file info
		added version number and start line for log file
		corrected log file timestamp formatting
		corrected rac_profiles list to be sorted by profile1/2
		if more than one profile is enabled during start, doSwitch will not switch!
			corrected: on startup all profiles except the primary one are disabled
		reverted wifi.cs back to deprecated OpenNetCF functions, they do not throw
		irregular exceptions
	v003
		added setting switchOnDisconnect, was enabled by default
		added settings checkConnectIP
			if checkConnectIP is true, the app waits for a valid local IP
			if checkConnectIP is false, the app will only check if desired SSID is associated
		v003 was never published
	v004
		added code to wait for all APIs ready. This will prevent exceptions if rac_switch is
		started with a link from \Windows\Startup
	v005
		added try/catch around _checkConnectIP in MobileConfiguration.cs. This will avoid exception 
		but one does not know that the app.config is wrong!