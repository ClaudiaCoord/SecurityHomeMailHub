@echo off
IF "%1"=="" GOTO :end
IF "%2"=="" GOTO :end
powershell -command "$wgi = Get-NetAdapter -Name %1; route delete 0.0.0.0 mask 0.0.0.0 0.0.0.0 if $wgi.ifIndex; route delete %2 0.0.0.0 if $wgi.ifIndex;"
:end
