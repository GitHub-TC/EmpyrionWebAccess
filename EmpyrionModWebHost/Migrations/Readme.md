Add-Migration InitialCreate -Context PlayerContext
Add-Migration InitialCreate -Context BackpackContext
Add-Migration InitialCreate -Context ChatContext
Add-Migration InitialCreate -Context FactionContext
Add-Migration InitialCreate -Context UserContext
Add-Migration InitialCreate -Context HistoryBookContext

Für DB Änderungen einfach
Add-Migration Beschreibung -Context XXXContext