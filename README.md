
# THIS REPO IS ARCHIVED! THE ACTIVE REPO CAN BE FOUND [HERE](https://github.com/ao-data/albiondata-client)! (ao-data/albiondata-client)
<br/><br/><br/><br/><br/><br/><br/><br/>

# albiondata-sql-dotNet
.Net Core Cross-Platform MySQL data dump for albiondata

The [albiondata-client](https://github.com/ao-data/albiondata-client) pulls MarketOrders from the network traffic
and pushes them to NATS, albiondata-sql-dotNet dumps those from NATS to your MySQL Database.

# Usage
`albiondata-sql-dotNet.exe -s "server=localhost;port=3306;database=YOUR_DATABASE;user=YOUR_DB_USER;password=YOUR_DB_PASSWORD" -n "nats://public:thenewalbiondata@albion-online-data.com:4222"`

Database is usually `albion`
