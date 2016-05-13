Theory of operation

RAC with two profiles:
	WPA2
	OPEN
	
Only one profile is enabled at a time (exclusive)

app watches suspend/resume
app watches docked/undocked
app watches connect/disconnect state (valid IP)

on event checks 
	if connected
		if WPA2 active
			do nothing
		if OPEN profile 
			deactivate OPEN
			activate WPA2
			10 seconds wait for connect
			if not connected fire disconnect event
			
	if disconnected
		START:
			if WPA2 active
				activate OPEN
				10 seconds wait for connect
				if disconnected
					activate WPA2
					10 seconds wait for connect
					if disconnected goto START
			if OPEN active
				activate WPA2
				10 seconds wait for connect
				if disconnected
					goto START
				
or simpler

				