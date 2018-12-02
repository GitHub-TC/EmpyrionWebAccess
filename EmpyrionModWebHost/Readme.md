# Empyrion Web Access

## Was ist das?
Empyrion Web Access ist eine MOD die den Zugriff auf das Spiel als Admin über einen Webbrowser ermöglicht.
Dadurch das die MOD ohne Oberfläche oder Remotedesktop auf dem Server auskommt ist sie auch für den Einsatz bei Gamehoster geeignet.
Sie startet und beendet sich automatisch mit dem Spiel und kann von beliebig vielen Admins gleichzeitig genutzt werden.

Empyrion Web Access ist frei zur nicht kommerziellen Benutzung.<br>
Über eine Aufmersamkeit würde ich mich aber freuen https://paypal.me/ASTICTC

Viel Spaß beim Spielen und dem Serverbetrieb wünscht<br>
ASTIC/TC

## Erstes Login
Damit der integrierte WebServer weis unter welcher URL er erreichbar sein soll muss eine Textdatei "appsettings.json" im
Savegameverzeichnis unter \[Savegame\]\\MODs\\EWA angelegt werden.
darin muss dan folgender Eintrag eingerichtet sein:

```
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
```
Statt "localhost" muss der Name oder die IP des Rechners angegeben werden. Der Port kann "nahezu" beliebig gewählt werden, muss jedoch von der Firewall freigegeben sein.
Der Standardport für HTTP ist 80 und der für HTTPS 443. 
Hinweis: Der Webserver läuft ausschließlich über HTTPS und nutzt das HTTP nur zur Weiterleitung auf HTTPS.

Wenn man nun den EGS-Server startet sollte Empyrion Web Access unter der Adresse
```
https://[Rechnername][:Port]
```
eine Anmeldemaske anzeigen.

Als erster Benutzer wird hier das Kürzel und Kennwort automatisch in der Benutzerdatenbank hinterlegt und akzeptiert. Alle Benutzer können nachher über die Oberfläche angelegt, geändert oder gelöscht werden.

## Das Hauptfenster
### System-/Spielinformation
Rechts oben werden Informationen zum Server (CPU, RAM, HDD), dem Spiel (Spieler online, Anzahl der Playfieldserver, der Reserveserver und deren Speicherverbrauch) und der Version angezeigt.
Auch befundet sich hier unter den drei sehrechten Punkten das Menü zu weiteren Fenstern un zum Logout.

### Chatbereich
Hier laufen alle Chatmeldungen des Spiels ein. Der Admin kann von hier ebenfalls Chatmeldungen in das Spiel absetzen in dem er den Text im "Message" Eingabefeld eingibt und mit Enter/Return bestätigt. Wenn der Haken "Chat as NNNN" gesetzt ist wird dabei automatisch für die Spieler im Spiel ein NNNN: vor die Chatmeldung gesetzt.

Um mit einem Spieler direkt zu Chatten kann dieser mit dem Chatsymbol ausgewählt werden. Sein name wird dann unter dem Eingabefeld angezeigt. Um wieder mit allen Spieler chatten zu können kann dann einfach der Haken bei "Chat to all" wieder gesetzt werden.

### Aktive Playfields und die Spieler welche sich darin aufhalten
Hier werden die aktiven Playfields mit ihrem Namen und der Anzahl Spieler aufgelistet.
Die Spieler werden mit Fraktion und Namen angezeigt.

Das Chatsymbol dient dazu mit dem Spieler direkten Kontakt aufzunehmen und das Fahnensymbol, dessen aktuelle Position zu speichern (s. Warp).

### Die Liste der bekannten Spieler
Hier werden alle Spieler angezeigt die seid in der Laufzeit von EWA mal online waren und deren PLY Datei sich noch im Savegame befindet.

Der Spieler wird hier mit seinem Onlinestatus, Namen, Fraktion, Herkunft ... angezeigt.
* Das Chatsymbol dient dazu mit dem Spieler direkten Kontakt aufzunehmen und das Fahnensymbol, dessen aktuelle Position zu speichern (s. Warp).
* Das Warpsymbol (Gamepadsymbol) dient dazu das Warpfenster für den Spieler aufzurufen mit dem die Position des Spielers im Spiel verändert werden kann.

### Inventaranzeige
Hier wird das Inventar des ausgewählten Spielers angezeigt. Von hier aus kann man
* Items hinzufügen
* Einen alten Zustand des Backpacks wiederherstellen

### Spielerdetails
Hier werden die Daten des ausgewählten Spielers angezeigt und können geändert werden.
Hinweis: Noch offen sind: ban/unban und wipe

## Strukturen
Hier werden alle Strukturen des Spiels aufgelistet:
* Sie können Teleportiert werden
* Die Position kann in den Speicher für ein Warp übertragen werden

## Start/Stop
Wenn in dem Verzeichnis \[Empyrion\]\\Content\\Mods\\EWALoader\\Client eine Datei "stop.txt" liegt wird der EWA automatisch gestoppt. 
Wird die Datei gelöscht oder umbenannt startet der EWA wieder. 

# Erweiterte Konfiguration
## Erstellen eines eigenen selbst signierten Zertifikates für die HTTPS Verbindung
Der EWA enthält bereits ein selbst signirtes Zertifikat. Sie können jedoch auch ein eigenes mit der PowerShell anfertigen:

1. New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname EmpyrionWebAccess -NotAfter (Get-Date).AddYears(10)
--> CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4
1. $pwd = ConvertTo-SecureString -String "Pa$$w0rd" -Force -AsPlainText
Export-PfxCertificate -cert cert:\localMachine\my\CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4 -FilePath EmpyrionWebAccess.pfx -Password $pwd
1. Nun muss die EmpyrionWebAccess.pfx Datei auf dem Server abgelegt werden und der Dateipfad und das Kennwort in der appsettings.json Datei im \[Savegame\]\\MODs\\EWA Verzeichnis eigetragen werden

## Freigabe von Ports
Ggf. müssen die Ports und Adressen noch für den Benutzer, unter dessen Account EGS läuft, freigegeben werden. Dazu muss man in einer Admin-PowerShel Console folgende Befehle absetzen.

1. Für HTTP
   * netsh http add urlacl url=http://[computername][:Port]/ user=[domain/computer]\[user]
   * netsh http add urlacl url=http://[IP-Adress][:Port]/ user=[domain/computer]\[user]
1. Für HTTPS
   * netsh http add urlacl url=https://[computername][:Port]/ user=[domain/computer]\[user]
   * netsh http add urlacl url=https://[IP-Adress][:Port]/ user=[domain/computer]\[user]


# Weitere Infos und den Quelltext gibt es hier
https://github.com/GitHub-TC/EmpyrionWebAccess

The internal plugins work with
Ist similiar to the original EmpyrionAPITools - only with async await and .NET 4.6<br>
https://github.com/GitHub-TC/EmpyrionNetAPIAccess

mod managing via<br>
https://github.com/GitHub-TC/EmpyrionModHost

# Was kommt noch?
* Backpack: Wiederherstellung und Manipulation
* Strukturen: Auflistung, warpen, löschen, ...
* MOD Manager: Einrichtung, Aktivierung/Deaktivierung, Update,... für weitere EGS Mods
* Server: Start, Stop
* Backup/Restore: von Strukturen und Spielern
* Scheduler: Für Zeitgesteuerte Aufgaben, Willkommensnachichten, Ankündigungen, ...
* ...
* was wir/ich sonst noch so brauchen :-)


=============================================================================
English Version
=============================================================================


# Empyrion Web Access

## What's this?
Empyrion Web Access is a MOD that allows access to the game as an admin via a web browser.
The fact that the MOD gets along without a surface or remote desktop on the server, it is also suitable for use with game host.
It starts and ends automatically with the game and can be used by any number of admins at the same time.

Empyrion Web Access is free for non-commercial use. <br>
About a Aufmersamkeit I would be happy but https://paypal.me/ASTICTC

Have fun playing and server operation wishes
ASTIC/TC

## First login
So that the integrated web server knows under which URL he should be attainable a text file "appsettings.json" in the
Savegame directory can be created under \[Savegame\]\\MODs\\EWA.
The following entry must be set up there:

`` `
{
  "Kestrel": {
    "Endpoints": {
      "Http": {
        "Url": "http://localhost:5000"
      },
      "HttpsDefaultCert": {
        "Url": "https://localhost:5001"
      }
    }
  }
}
`` `
Instead of "localhost" the name or the IP of the computer must be specified. The port can be chosen "almost" as desired, but must be enabled by the firewall.
The default port for HTTP is 80 and for HTTPS 443.
Note: The web server runs exclusively via HTTPS and uses the HTTP only for forwarding to HTTPS.

If you now start the EGS server Empyrion Web Access should be at the address
`` `
https://[hostname][:port]
`` `
display a login mask.

As the first user, the abbreviation and password are automatically stored and accepted in the user database. All users can be subsequently created, changed or deleted via the interface.

## The main window
### System/Game Information
Top right information about the server (CPU, RAM, HDD), the game (online players, number of Playfield servers, the reserve server and their memory consumption) and the version is displayed.
Also under the three right-most points is the menu for further windows and the logout.

### chat area
Here are all the chat messages of the game. The admin can also place chat messages in the game from here by entering the text in the "Message" input field and confirming with Enter/Return. If the check mark "Chat as NNNN" is set, an NNNN: is automatically set for the players in the game before the chat message.

To chat directly with a player, you can select it with the chat icon. Its name is then displayed below the input field. To be able to chat again with all players then simply the hook at "Chat to all" be set again.

### Active playfields and the players who are in it
Here the active playfields are listed with their name and number of players.
Players are shown with faction and name.

The chat symbol is used to make direct contact with the player and to save the flag symbol, its current position (see Warp).

### The list of known players
Here are all players displayed that were in the runtime of EWA times online and their PLY file is still in the savegame.

The player is shown here with his online status, name, faction, origin ...
* The chat symbol is used to make direct contact with the player and to save the flag symbol, its current position (see Warp).
* The warp icon (gamepad icon) is used to bring up the Warp window for the player to change the position of the player in the game.

### Inventory display
Here the inventory of the selected player is displayed. From here you can
* Add items
* Restore an old condition of the backpack

### Player details
Here, the data of the selected player is displayed and can be changed.
Note: Still open: ban / unban and wipe

## Structures
Here are all the structures of the game listed:
* They can be teleported
* The position can be transferred to the memory for a warp

## Start/Stop
If there is a file "stop.txt" in the directory \[Empyrion\]\\Content\\Mods\\EWALoader\\Client the EWA will be stopped automatically.
If the file is deleted or renamed, the EWA restarts.

# Advanced configuration
## Create your own self-signed certificate for the HTTPS connection
The EWA already contains a self-signed certificate. But you can also make your own with PowerShell:

1. New-SelfSignedCertificate -certstorelocation cert: \localmachine \my -dnsname EmpyrionWebAccess -NotAfter (get-date) .AddYears (10)
-> CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4
1. $ pwd = ConvertTo-SecureString -String "Pa $$ w0rd" -Force -AsPlainText
Export-PfxCertificate -cert cert: \localMachine \my \CE0976529B02DE058C9CB2C0E64AD79DAFB18CF4 -FilePath EmpyrionWebAccess.pfx -Password $ pwd
1. Now the EmpyrionWebAccess.pfx file must be placed on the server and the file path and the password in the appsettings.json file in the \[Savegame\]\\MODs\\EWA directory must be entered

## Release of ports
Possibly. the ports and addresses must still be released for the user under whose EGS account is running. To do this, you need to issue the following commands in an Admin PowerShel Console.

1. For HTTP
   * netsh http add urlacl url = http://[computername] [: port]/user = [domain/computer]\[user]
   * netsh http add urlacl url = http://[ipaddress] [: port]/user = [domain/computer]\[user]
1. For HTTPS
   * netsh http add urlacl url = https://[computername] [: port]/user = [domain/computer]\[user]
   * netsh http add urlacl url = https://[ipaddress] [: port]/user = [domain/computer]\[user]


# Further information and the source code can be found here
https://github.com/GitHub-TC/EmpyrionWebAccess

The internal plugins work with
Is similiar to the original EmpyrionAPITools - only with async await and .NET 4.6 <br>
https://github.com/GitHub-TC/EmpyrionNetAPIAccess

mod managing via <br>
https://github.com/GitHub-TC/EmpyrionModHost

# What else is coming?
* Backpack: recovery and manipulation
* Structures: listing, warping, deleting, ...
* MOD Manager: Setup, Activation/Deactivation, Update, ... for more EGS mods
* Server: Start, Stop
* Backup/Restore: of structures and players
* Scheduler: For timed tasks, welcome stories, announcements, ...
* ...
* what else do we need :-)