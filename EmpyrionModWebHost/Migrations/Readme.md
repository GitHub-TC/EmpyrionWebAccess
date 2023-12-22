Add-Migration InitialCreate -Context PlayerContext 

Add-Migration InitialCreate -Context BackpackContext 

Add-Migration InitialCreate -Context ChatContext 

Add-Migration InitialCreate -Context FactionContext

Add-Migration InitialCreate -Context UserContext

Add-Migration InitialCreate -Context HistoryBookContext

Add-Migration InitialCreate -Context FactoryItemsContext

## Für DB Änderungen einfach in einer Powershell Console
dotnet-ef migrations add Beschreibung --context XYZContext