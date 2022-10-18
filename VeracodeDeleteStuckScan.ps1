[Net.ServicePointManager]::SecurityProtocol = "Tls12"
Write-Output "Downloading the latest version of the Veracode Java API"
$versionstring = curl https://repo1.maven.org/maven2/com/veracode/vosp/api/wrappers/vosp-api-wrappers-java/maven-metadata.xml | findstr /r "latest";
$version = $versionstring.Trim().replace("<latest>", '');
$version = $version.replace("</latest>", "");
curl https://repo1.maven.org/maven2/com/veracode/vosp/api/wrappers/vosp-api-wrappers-java/$version/vosp-api-wrappers-java-$version-dist.zip -o dist.zip
Expand-Archive -Force .\dist.zip -DestinationPath .\veracode-java-api
$appname = "Retrace .Net Serilog Library"
Write-Output "Pulling the Application ID List"
java -jar .\veracode-java-api\VeracodeJavaAPI.jar -vid $env:vid -vkey $env:vkey -action getapplist | tee appidlist.txt
$appid = Get-Content .\appidlist.txt | Select-String -Pattern $appname | ForEach-Object{ $_.Line.Split('"')[1]; }
java -jar .\veracode-java-api\VeracodeJavaAPI.jar -vid $env:vid -vkey $env:vkey -action getbuildinfo -appid $appid | tee appstatus.txt
$appStatus = get-Content .\appstatus.txt | findstr /r "status=" | select -Last 1

If ($appStatus.Contains("Submitted to Engine"))
{
	Write-Output "[Info] A scan has been already been submitted to the engine, please wait for the previous scan to complete before submitting another job."
	exit
}
elseif ($appStatus.Contains("Pre-Scan Submitted"))
{
	Write-Output "[INFO] A pre-scan is still running from a previous job, please wait for the previous scan to complete."
	exit
}
elseif ($appStatus.Contains("Scan In Process"))
{
	Write-Output "[Info] A scan is in process, please wait for the previous scan to complete before submitting another job."
	exit
}
elseif ($appStatus.Contains("Pre-Scan Success"))
{
    java -jar VeracodeJavaAPI.jar -vid $env:vid -vkey $env:vkey -action getprescanresults -appid $appid > prescanerror.txt
	Write-Output ""
	Write-Output "[ERROR] Something went wrong during the prescan!"
	$precanError = Get-Content .\prescanerror.txt
	Write-Output $precanError
	Write-Output "Double-Check the errors printed above in the prescan file."
}
elseif ($appStatus.Contains("Incomplete"))
{
	Write-Output "[INFO] The results from the previous scan are incomplete."
	Write-Output "[INFO] The scan is missing required information which includes uploading files, selecting modules or information for vendor acceptance of 3rd party scan requests."
	Write-Output "[INFO] Check https://docs.veracode.com/r/Troubleshooting_Veracode_APIs_and_Wrappers for more information."
	Write-Output "[INFO] The previous policy scan attempt will now be deleted."
	java -jar .\veracode-java-api\VeracodeJavaAPI.jar -vid $env:vid -vkey $env:vkey -action deletebuild -appid $appid | tee incompletebuild.txt
	$deleteStatus = Get-Content .\incompletebuild.txt
	[regex]$resultReg = "(?<=result\s*>\s*)\w+"
	$result = $resultReg.Match($deleteStatus).Value
	Write-Output "[INFO] The attempt to delete the last build for this application: $result"
}
elseif ($appStatus.Contains("Results Ready"))
{
	Write-Output "[INFO] The results from the previous scan are ready."
	Write-Output "[INFO] The scan engine is ready for the next scan."
}
