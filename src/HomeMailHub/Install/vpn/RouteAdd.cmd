@echo off
IF "%1"=="" GOTO :end
IF "%2"=="" GOTO :end
powershell -command "$wgi = Get-NetAdapter -Name %1; Set-NetIPInterface -InterfaceIndex $wgi.ifIndex -InterfaceMetric 30; route add 0.0.0.0 mask 0.0.0.0 0.0.0.0 IF $wgi.ifIndex metric 1; route add %2 0.0.0.0 IF $wgi.ifIndex;"
:end
