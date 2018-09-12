all:
	dotnet run --project ./albiondata-sql-dotNet

release:
	dotnet publish -c Release ./albiondata-sql-dotNet
