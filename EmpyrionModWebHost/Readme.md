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
Hier wird das Inventar des ausgewählten Spielers angezeigt.


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
