Theory of operation

Funk with two preferred profiles:
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
  copy funk_switch.cab to device and install using file explorer
  then do a warmboot to have funk_switch started via Windows\StartUp

Configuration
  see app.config file in program dir
  example:
		<?xml version="1.0" encoding="utf-8" ?>
		<configuration>
		  <appSettings>
			<add key="profile1" value="Profile_1" />
			<add key="profile2" value="Profile_2" />
			<add key="checkOnUndock" value="false" />
			<add key="checkOnResume" value="true" />
			<add key="checkConnectIP" value="false=" />
			<add key="checkConnectAP" value="true=" />
			<add key="switchOnDisconnect" value="false" />
			<add key="switchTimeout" value="20"/>
			<add key="enableLogging" value="false" />
		  </appSettings>
		</configuration>
  
  meanings:
    profile1 is the name of the preferred FUNK profile (not the SSID inside), for example Profile_1
    profile1 is the name of the secondary FUNK profile (not SSID), for example Profile_2
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
	bak (2MB): \FUNK_switch.log.txt.bak

History
	v001
		initial version, based on RAC_Switch
		Switching a FUNK profile is much faster (15 seconds for FUNK compared to about 50 seconds for a RAC profile)
		changed code to check association to Intermec WLAN API
		removed RAC related code
		removed OpenNetCF.Net related code and reference (causes Wifi Radio Power Off!)
