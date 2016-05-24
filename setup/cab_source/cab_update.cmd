D:
cd D:\svn\git\RAC_switch\setup\cab_source
REM copy latest files
cp ..\..\RAC_switch\bin\Release\RAC_switch.exe .\[INSTALLDIR]

Cabwiz.exe "Honeywell rac_switch.inf"

cp "./Honeywell rac_switch.CAB" ../rac_switch.CAB

PAUSE 