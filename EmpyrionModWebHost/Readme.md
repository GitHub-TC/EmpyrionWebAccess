First user login direct creates the first unser in the database.

============================================================================================
Create your own self signed certificate with windows admin powershell:

New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname EmpyrionWebAccess -NotAfter (Get-Date).AddYears(10)
--> CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4

$pwd = ConvertTo-SecureString -String "Pa$$w0rd" -Force -AsPlainText
Export-PfxCertificate -cert cert:\localMachine\my\CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4 -FilePath EmpyrionWebAccess.pfx -Password $pwd

now insert the EmpyrionWebAccess.pfx file in the EWA directory and insert your password in the appsettings.json

============================================================================================

netsh http add urlacl url=http://[computername]:80/ user=[domain/computer]\[user]
netsh http add urlacl url=http://[IP-Adress]:80/ user=[domain/computer]\[user]

netsh http add urlacl url=https://[computername]:443/ user=[domain/computer]\[user]
netsh http add urlacl url=https://[IP-Adress]:443/ user=[domain/computer]\[user]

============================================================================================
Configure your URL (and other custom settings) for the web server in the appsettings.json in the [Savegame]\MODs\EWA path

{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5010"
      },
      "HttpsDefaultCert": {
        "Url": "https://localhost:5011"
      }
    }
  }
}


============================================================================================
============================================================================================
============================================================================================

https://github.com/GitHub-TC/EmpyrionWebAccess

The internal plugins work with
Ist similiar to the original EmpyrionAPITools - only with async await and .NET 4.6
https://github.com/GitHub-TC/EmpyrionNetAPIAccess

(coming soon) mod managing via
https://github.com/GitHub-TC/EmpyrionModHost