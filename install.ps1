$servicename = "radiusserverservice";
$installdir = ${env:ProgramFiles} + "\Flexinets\radiusserverservice";


$service = Get-WmiObject -Class Win32_Service -Filter "Name='$servicename'";

if ($service) {
	echo "Stopping service $servicename";
	Stop-Service $servicename;
	$foo = Get-Service $servicename;	
	$foo.WaitForStatus("Stopped", '00:00:30')
	sleep 2;	# wait for gc?
}

if(!(Test-Path -Path $installdir )){
	mkdir $installdir;
	echo "Creating install directory $installdir";
}
else {
	echo "Install directory exists, removing old version";
	Remove-Item $installdir\*
}
 
echo "Copying files to $installdir";
Copy-Item * $installdir;

echo "Starting service $servicename";
Start-Service $servicename;

