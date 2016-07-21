D:
copy "D:\svn\git\RAC_switch\Funk_switch\readme.txt" "D:\svn\git\RAC_switch\Funk_switch\setup\" 
 
cd D:\svn\git\RAC_switch\Funk_switch\setup\cab_source
REM copy latest files
copy D:\svn\git\RAC_switch\Funk_switch\bin\Release\FUNK_switch.exe "[INSTALLDIR]\FUNK_switch.exe"

Cabwiz.exe "Honeywell funk_switch.inf"

copy ".\Honeywell funk_switch.CAB" "..\funk_switch.CAB"

PAUSE 