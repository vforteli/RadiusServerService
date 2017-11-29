$servicename = "radiusserverservice";
$installdir = ${env:ProgramFiles} + "\Flexinets\radiusserverservice";
$version = (Get-Item $PSScriptRoot\$servicename.exe).VersionInfo.FileVersion

echo "Installing $servicename version $version";
$service = Get-WmiObject -Class Win32_Service -Filter "Name='$servicename'";

if ($service) {
	echo "Stopping service $servicename";
	Stop-Service $servicename;
	$foo = Get-Service $servicename;	
	$foo.WaitForStatus("Stopped", '00:00:30')
	sleep 2;	# wait for gc?
}
else {
	echo "Service does not exist";
}

if(!(Test-Path -Path $installdir )){
	mkdir $installdir;
	echo "Creating install directory $installdir";
}
else {
	echo "Install directory exists, removing old version";
	Remove-Item $installdir\* -recurse
}
 
echo "Copying files to $installdir";
Copy-Item $PSScriptRoot\* $installdir -recurse;


if (!$service) {
	echo "Creating service $servicename";
    $arguments = "`"$installdir/radiusserverservice.exe`"";
    Start-Process –FilePath C:\Windows\Microsoft.NET\Framework\v4.0.30319\installutil.exe –ArgumentList $arguments –NoNewWindow -Wait
}


echo "Starting service $servicename";
Start-Service $servicename;
	